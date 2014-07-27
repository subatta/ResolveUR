using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace ReferenceResolution
{
    static class ResolveReferences
    {
        private const string Include = "Include";

        public static void Resolve(string solutionPath)
        {
            // get all project file full paths from solution file
            var projectFiles = loadProjects(solutionPath);

            foreach (var projectFile in projectFiles)
            {
                // list all references in project
                XmlDocument projectXmlDocument;
                var referenceNodesInProject = getReferenceNodes(projectFile, out projectXmlDocument);
                var referenceNames = referenceNodesInProject
                                        .OfType<XmlNode>()
                                        .Select(x => x.Attributes[Include].Value);
                foreach (var reference in referenceNames)
                {
                    var indexOfNodeToRemove = findNodeIndexByAttributeName(referenceNodesInProject, reference);
                    if (indexOfNodeToRemove == -1)
                        continue;

                    var nodeToRemove = referenceNodesInProject[indexOfNodeToRemove];
                    referenceNodesInProject[0].ParentNode.RemoveChild(nodeToRemove);
                    projectXmlDocument.Save(projectFile);

                    referenceNodesInProject = getReferenceNodes(projectFile, out projectXmlDocument);

                    if (projectNeedsReference(projectFile))
                    {
                        var importedNode = referenceNodesInProject[0].OwnerDocument.ImportNode(nodeToRemove, true);
                        referenceNodesInProject[0].ParentNode.AppendChild(importedNode);
                        projectXmlDocument.Save(projectFile);
                    }
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
                projects[i] = Path.GetFullPath(projects[i]);
            }
            return projects;
        }

        private static XmlNodeList getReferenceNodes(string projectFile, out XmlDocument projectXmlDocument)
        {
            var doc = new XmlDocument();
            doc.Load(projectFile);
            projectXmlDocument = doc;
            return doc.SelectNodes(@"//*[local-name()='Reference']");
        }

        private static int findNodeIndexByAttributeName(XmlNodeList nodeList, string attributeName)
        {
            for (var i = 0; i < nodeList.Count; i++ )
            {
                if (nodeList[i].Attributes[Include].Value == attributeName)
                    return i;
            }
            return -1;
        }

        private static bool projectNeedsReference(string projectFile)
        {
            const string MsbuildExe = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe";
            const string LogFile = "buildlog.txt";
            const string ArgumentsFormat = "{0} /clp:ErrorsOnly /m /flp:logfile={1};Verbosity=Quiet";
            const string Error = "error";

            var startInfo = new ProcessStartInfo
            {
                FileName = MsbuildExe,
                Arguments = string.Format(CultureInfo.CurrentCulture, ArgumentsFormat, projectFile, LogFile),
                CreateNoWindow = true
            };

            using (var exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }

            // open build log to check for errors
            var s = File.ReadAllText(LogFile);
            File.Delete(LogFile);

            // if we have a build error, the reference cannot be removed
            return s.Contains(Error);
        }
    }
}
