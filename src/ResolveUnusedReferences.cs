using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace ResolveUR
{
    static class ResolveUnusedReferences
    {
        private const string Include = "Include";

        public static void Resolve(string solutionPath, string msbuildPath)
        {
            // get all project file full paths from solution file
            var projectFiles = loadProjects(solutionPath);

            foreach (var projectFile in projectFiles)
            {
                if (!File.Exists(projectFile))
                    continue;
                resolveReferences(projectFile, msbuildPath, "Reference");
                resolveReferences(projectFile, msbuildPath, "ProjectReference");
            }
        }

        private static void resolveReferences(string projectFile, string msbuildPath, string referenceType)
        {
            var projectXmlDocument = getXmlDocument(projectFile);
            var projectXmlDocumentToRestore = getXmlDocument(projectFile);

            var item = getReferenceGroupItem(projectXmlDocument, referenceType);
            if (item == null)
                return;

            var referenceNodeNames = getReferenceNodeNames(item);
            if (referenceNodeNames == null || referenceNodeNames.Count == 0)
                return;

            foreach (var reference in referenceNodeNames)
            {
                var nodeToRemove = findNodeByAttributeName(item, reference);
                if (nodeToRemove == null)
                    continue;

                nodeToRemove.ParentNode.RemoveChild(nodeToRemove);
                projectXmlDocument.Save(projectFile);

                if (projectNeedsReference(projectFile, msbuildPath))
                {
                    // restore original
                    projectXmlDocumentToRestore.Save(projectFile);
                    // reload item group from its doc
                    projectXmlDocument = getXmlDocument(projectFile);
                    item = getReferenceGroupItem(projectXmlDocument, referenceType);
                    if (item == null)
                        break;
                }
                else
                {
                    Console.WriteLine("Project: {0} - Removed reference: {1}", projectFile, reference);
                    projectXmlDocumentToRestore = getXmlDocument(projectFile);
                }
            }

        }
        private static IEnumerable<string> loadProjects(string solutionPath)
        {
            const string ProjectRegEx = "Project\\(\"\\{[\\w-]*\\}\"\\) = \"([\\w _]*.*)\", \"(.*\\.(cs|vcx|vb)proj)\"";
            var content = File.ReadAllText(solutionPath);
            var projReg = new Regex
            (
                ProjectRegEx,
                RegexOptions.Compiled
            );
            var matches = projReg.Matches(content).Cast<Match>();
            var projects = matches.Select(x => x.Groups[2].Value).ToList();
            for (int i = 0; i < projects.Count; ++i)
            {
                if (!Path.IsPathRooted(projects[i]))
                    projects[i] = Path.Combine(Path.GetDirectoryName(solutionPath), projects[i]);
                try
                {
                    projects[i] = Path.GetFullPath(projects[i]);
                }
                catch (NotSupportedException ex)
                {
                    Console.WriteLine("Path: {0}, Error: {1}", projects[i], ex.Message);
                }
            }
            return projects;
        }

        private static XmlDocument getXmlDocument(string projectFile)
        {
            var doc = new XmlDocument();
            doc.Load(projectFile);
            return doc;
        }

        private static XmlNodeList getItemGroupNodes(XmlDocument document)
        {
            const string ItemGroupXPath = @"//*[local-name()='ItemGroup']";
            return document.SelectNodes(ItemGroupXPath);
        }

        private static XmlNode getReferenceGroupItem(XmlDocument doc, string referenceNodeName)
        {
            var itemGroups = getItemGroupNodes(doc);

            if (itemGroups == null || itemGroups.Count == 0)
                return null;

            for (int i = 0; i < itemGroups.Count; i++)
            {
                if (itemGroups[i].ChildNodes == null || itemGroups[i].ChildNodes.Count == 0)
                    return null;

                if (itemGroups[i].ChildNodes[0].Name == referenceNodeName)
                {
                    return itemGroups[i];
                }
            }
            
            return null;
        }

        private static List<string> getReferenceNodeNames(XmlNode itemGroupNode)
        {
            if (itemGroupNode.ChildNodes.Count == 0)
                return null;

            return itemGroupNode
                        .ChildNodes
                        .OfType<XmlNode>()
                        .Select(x => x.Attributes[Include].Value)
                        .ToList();
        }

        private static XmlNode findNodeByAttributeName(XmlNode itemGroup, string attributeName)
        {
            for (var i = 0; i < itemGroup.ChildNodes.Count; i++)
            {
                if (itemGroup.ChildNodes[i].Attributes[Include].Value == attributeName)
                {
                    return itemGroup.ChildNodes[i];

                }
            }
            return null;
        }

        private static bool projectNeedsReference(string projectFile, string msbuildPath)
        {
            const string LogFile = "buildlog.txt";
            const string ArgumentsFormat = "{0} /clp:ErrorsOnly /nologo /m /flp:logfile={1};Verbosity=Quiet";
            const string Error = "error";

            var startInfo = new ProcessStartInfo
            {
                FileName = msbuildPath,
                Arguments = string.Format(CultureInfo.CurrentCulture, ArgumentsFormat, projectFile, LogFile),
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using (var exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }

            // open build log to check for errors
            if (File.Exists(LogFile))
            {
                var s = File.ReadAllText(LogFile);
                File.Delete(LogFile);

                // if we have a build error, the reference cannot be removed
                return s.Contains(Error);
            }
            return true;
        }

    }
}
