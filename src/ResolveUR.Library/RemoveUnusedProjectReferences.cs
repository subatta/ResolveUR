using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace ResolveUR.Library
{

    public class RemoveUnusedProjectReferences: IResolveUR
    {

        private const string Include = "Include";
        private const string Reference = "Reference";
        private const string Project = "Project";

        public event HasBuildErrorsEventHandler HasBuildErrorsEvent;
        public event ProgressMessageEventHandler ProgressMessageEvent;
        public event ReferenceCountEventHandler ReferenceCountEvent;
        public event EventHandler ItemGroupResolved;

        private PackageConfig _packageConfig = new PackageConfig();

        private string _filePath;
        public string FilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                _filePath = value;
                var dirPath = Path.GetDirectoryName(_filePath);
                _packageConfig.FilePath = value;
                _packageConfig.PackageConfigPath = dirPath + "\\packages.config";
            }
        }
        public string BuilderPath
        {
            get;
            set;
        }

        // returns false if there were build errors
        public void Resolve()
        {
            // do nothing if project already has build errors
            if (projectHasBuildErrors()) raiseBuildErrorsEvent();

            _packageConfig.LoadPackagesIfAny();

            resolve(Reference);

            if (_isCancel) return;

            resolve(Project + Reference);
        }

        private void resolve(string referenceType)
        {
            raiseProgressMessageEvent(string.Format("Resolving {0}s in {1}", referenceType, Path.GetFileName(FilePath)));

            var projectXmlDocument = getXmlDocument();
            var projectXmlDocumentToRestore = getXmlDocument();

            var itemIndex = 0;
            var item = getReferenceGroupItemIn(projectXmlDocument, referenceType, itemIndex);
            while (item != null)
            {

                if (_isCancel) break;

                // use string names to match up references, using nodes themselves will mess references
                if (item.ChildNodes.Count == 0) continue;

                var referenceNodeNames = getReferenceNodeNamesIn(item);

                if (ReferenceCountEvent != null) ReferenceCountEvent(referenceNodeNames.Count());

                foreach (var referenceNodeName in referenceNodeNames)
                {
                    if (_isCancel) break;

                    var nodeToRemove = findNodeByAttributeName(item, referenceNodeName);
                    if (nodeToRemove == null) continue;

                    nodeToRemove.ParentNode.RemoveChild(nodeToRemove);
                    projectXmlDocument.Save(FilePath);

                    if (projectHasBuildErrors())
                    {
                        raiseProgressMessageEvent(string.Format("\tKept: {0}", referenceNodeName));
                        _packageConfig.CopyPackageToKeep(nodeToRemove);

                        // restore original
                        projectXmlDocumentToRestore.Save(FilePath);
                        // reload item group from doc
                        projectXmlDocument = getXmlDocument();
                        item = getReferenceGroupItemIn(projectXmlDocument, referenceType, itemIndex); // prevents from using yield return?
                        if (item == null) break;
                    }
                    else
                    {
                        raiseProgressMessageEvent(string.Format("\tRemoved: {0}", referenceNodeName));
                        projectXmlDocumentToRestore = getXmlDocument();
                        _packageConfig.RemoveUnusedPackage(nodeToRemove);
                    }
                }

                raiseItemGroupResolvedEvent();

                item = getReferenceGroupItemIn(projectXmlDocument, referenceType, ++itemIndex);
            }

            projectXmlDocument = null;
            projectXmlDocumentToRestore = null;

            if (_isCancel) return;

            _packageConfig.UpdatePackageConfig();

            raiseProgressMessageEvent("Done with: " + Path.GetFileName(FilePath));

        }

        private XmlDocument getXmlDocument()
        {
            var doc = new XmlDocument();
            doc.Load(FilePath);
            return doc;
        }

        private XmlNodeList getItemGroupNodesIn(XmlDocument document)
        {
            const string ItemGroupXPath = @"//*[local-name()='ItemGroup']";
            return document.SelectNodes(ItemGroupXPath);
        }

        private XmlNode getReferenceGroupItemIn(XmlDocument document, string referenceNodeName, int startIndex)
        {
            var itemGroups = getItemGroupNodesIn(document);

            if (itemGroups == null || itemGroups.Count == 0) return null;

            for (int i = startIndex; i < itemGroups.Count; i++)
            {
                if (itemGroups[i].ChildNodes == null || itemGroups[i].ChildNodes.Count == 0) return null;

                if (itemGroups[i].ChildNodes[0].Name == referenceNodeName) return itemGroups[i];
            }

            return null;
        }

        private IEnumerable<string> getReferenceNodeNamesIn(XmlNode itemGroup)
        {
            return itemGroup
                        .ChildNodes
                        .OfType<XmlNode>()
                        .Select(x => x.Attributes[Include].Value)
                        .ToList();
        }

        private XmlNode findNodeByAttributeName(XmlNode itemGroup, string attributeName)
        {
            foreach (XmlNode item in itemGroup.ChildNodes)
                if (item.Attributes[Include].Value == attributeName) return item;

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
            if (File.Exists(logFile)) File.Delete(logFile);

            using (var exeProcess = Process.Start(startInfo))
                exeProcess.WaitForExit();

            // open build log to check for errors
            if (File.Exists(logFile))
            {
                var s = File.ReadAllText(logFile);
                File.Delete(logFile);

                // if build error, the reference cannot be removed
                return s.Contains(Error);
            }
            return true;
        }

        private void raiseProgressMessageEvent(string message)
        {
            if (ProgressMessageEvent != null) ProgressMessageEvent(message);
        }

        private void raiseBuildErrorsEvent()
        {
            if (HasBuildErrorsEvent != null) HasBuildErrorsEvent(Path.GetFileNameWithoutExtension(FilePath));
        }

        private void raiseItemGroupResolvedEvent()
        {
            if (ItemGroupResolved != null) ItemGroupResolved(null, null);
        }

        private bool _isCancel;
        public void Cancel()
        {
            _isCancel = true;
        }
    }

}
