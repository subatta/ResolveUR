using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace ResolveUR
{
    public class RemoveUnusedProjectReferences : IResolveUR
    {

        private const string Include = "Include";

        public event HasBuildErrorsEventHandler HasBuildErrorsEvent;

        public string FilePath
        {
            get;
            set;
        }
        public string BuilderPath
        {
            get;
            set;
        }

        // returns false if there were build errors
        public void Resolve()
        {
            if (resolve("Reference"))
                resolve("ProjectReference");
        }

        private bool resolve(string referenceType)
        {
            // do nothing if project already has build errors
            if (projectHasBuildErrors())
            {
                if (HasBuildErrorsEvent != null)
                {
                    HasBuildErrorsEvent(Path.GetFileNameWithoutExtension(FilePath));
                }
                return false;
            }

            var projectXmlDocument = getXmlDocument();
            var projectXmlDocumentToRestore = getXmlDocument();

            var item = getReferenceGroupItem(projectXmlDocument, referenceType);
            if (item == null)
                return true;

            var referenceNodeNames = getReferenceNodeNames(item);
            if (referenceNodeNames == null || referenceNodeNames.Count == 0)
                return true;

            foreach (var reference in referenceNodeNames)
            {
                var nodeToRemove = findNodeByAttributeName(item, reference);
                if (nodeToRemove == null)
                    continue;

                Console.WriteLine("Project: {0} - Attempting to remove reference: {1}", FilePath, reference);

                nodeToRemove.ParentNode.RemoveChild(nodeToRemove);
                projectXmlDocument.Save(FilePath);

                if (projectHasBuildErrors())
                {
                    Console.WriteLine("Project: {0} - Restored reference: {1}", FilePath, reference);
                    // restore original
                    projectXmlDocumentToRestore.Save(FilePath);
                    // reload item group from its doc
                    projectXmlDocument = getXmlDocument();
                    item = getReferenceGroupItem(projectXmlDocument, referenceType);
                    if (item == null)
                        break;
                }
                else
                {
                    Console.WriteLine("Project: {0} - Removed reference: {1}", FilePath, reference);
                    projectXmlDocumentToRestore = getXmlDocument();
                }
            }
            return true;
        }

        private XmlDocument getXmlDocument()
        {
            var doc = new XmlDocument();
            doc.Load(FilePath);
            return doc;
        }

        private XmlNodeList getItemGroupNodes(XmlDocument document)
        {
            const string ItemGroupXPath = @"//*[local-name()='ItemGroup']";
            return document.SelectNodes(ItemGroupXPath);
        }

        private XmlNode getReferenceGroupItem(XmlDocument doc, string referenceNodeName)
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

        private List<string> getReferenceNodeNames(XmlNode itemGroupNode)
        {
            if (itemGroupNode.ChildNodes.Count == 0)
                return null;

            return itemGroupNode
                        .ChildNodes
                        .OfType<XmlNode>()
                        .Select(x => x.Attributes[Include].Value)
                        .ToList();
        }

        private XmlNode findNodeByAttributeName(XmlNode itemGroup, string attributeName)
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

        private bool projectHasBuildErrors()
        {
            const string LogFileName = "buildlog.txt";
            const string ArgumentsFormat = "\"{0}\" /clp:ErrorsOnly /nologo /m /flp:logfile={1};Verbosity=Quiet";
            const string Error = "error";

            var tempPath = Path.GetTempPath();
            var logFile = tempPath + @"\" + LogFileName;

            var startInfo = new ProcessStartInfo
            {
                FileName = BuilderPath,
                Arguments = string.Format(CultureInfo.CurrentCulture, ArgumentsFormat, FilePath, logFile),
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = tempPath
            };

            // clear build log file if it was left out for some reason
            if (File.Exists(logFile))
                File.Delete(logFile);

            using (var exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }

            // open build log to check for errors
            if (File.Exists(logFile))
            {
                var s = File.ReadAllText(logFile);
                File.Delete(logFile);

                // if we have a build error, the reference cannot be removed
                return s.Contains(Error);
            }
            return true;
        }

    }

}
