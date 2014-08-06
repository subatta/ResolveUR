using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace ResolveUR
{

    public class RemoveUnusedProjectReferences: IResolveUR
    {

        private const string Include = "Include";

        public event HasBuildErrorsEventHandler HasBuildErrorsEvent;
        public event ProgressMessageEventHandler ProgressMessageEvent;

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

            raiseProgressMessageEvent(string.Format("Resolving {0}s in {1}", referenceType, FilePath));

            var projectXmlDocument = getXmlDocument();
            var projectXmlDocumentToRestore = getXmlDocument();

            var itemIndex = 0;
            var item = getReferenceGroupItem(projectXmlDocument, referenceType, itemIndex);
            while (item != null)
            {
                var referenceNodeNames = getReferenceNodeNames(item);
                if (referenceNodeNames == null || referenceNodeNames.Count() == 0)
                    continue;

                foreach (var reference in referenceNodeNames)
                {
                    var nodeToRemove = findNodeByAttributeName(item, reference);
                    if (nodeToRemove == null)
                        continue;

                    nodeToRemove.ParentNode.RemoveChild(nodeToRemove);
                    projectXmlDocument.Save(FilePath);

                    if (projectHasBuildErrors())
                    {
                        raiseProgressMessageEvent(string.Format("Keep: {0}", reference));
                        // restore original
                        projectXmlDocumentToRestore.Save(FilePath);
                        // reload item group from its doc
                        projectXmlDocument = getXmlDocument();
                        item = getReferenceGroupItem(projectXmlDocument, referenceType, itemIndex); // prevents from using yield return?
                        if (item == null)
                            break;
                    }
                    else
                    {
                        raiseProgressMessageEvent(string.Format("Removed: {0}", reference));
                        projectXmlDocumentToRestore = getXmlDocument();
                    }
                }

                item = getReferenceGroupItem(projectXmlDocument, referenceType, ++itemIndex);
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

        private XmlNode getReferenceGroupItem(XmlDocument document, string referenceNodeName, int startIndex)
        {
            var itemGroups = getItemGroupNodes(document);

            if (itemGroups == null || itemGroups.Count == 0)
                return null;

            for (int i = startIndex; i < itemGroups.Count; i++)
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

        private IEnumerable<string> getReferenceNodeNames(XmlNode itemGroup)
        {
            if (itemGroup.ChildNodes.Count == 0)
                return null;

            return itemGroup
                        .ChildNodes
                        .OfType<XmlNode>()
                        .Select(x => x.Attributes[Include].Value)
                        .ToList();
        }

        private XmlNode findNodeByAttributeName(XmlNode itemGroup, string attributeName)
        {
            foreach (XmlNode item in itemGroup.ChildNodes)
            {
                if (item.Attributes[Include].Value == attributeName)
                    return item;
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

        private void raiseProgressMessageEvent(string message)
        {
            if (ProgressMessageEvent != null)
                ProgressMessageEvent(message);
        }
    }

}
