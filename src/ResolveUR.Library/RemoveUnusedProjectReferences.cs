using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace ResolveUR.Library
{
    public class RemoveUnusedProjectReferences : IResolveUR
    {
        const string Include = "Include";
        const string Reference = "Reference";
        const string Project = "Project";
        string _filePath;
        bool _isCancel;
        PackageConfig _packageConfig;

        public event HasBuildErrorsEventHandler HasBuildErrorsEvent;
        public event ProgressMessageEventHandler ProgressMessageEvent;
        public event ReferenceCountEventHandler ReferenceCountEvent;
        public event EventHandler ItemGroupResolvedEvent;
        public event PackageResolveProgressEventHandler PackageResolveProgressEvent;

        public string FilePath
        {
            get { return _filePath; }
            set
            {
                _filePath = value;
                string dirPath = Path.GetDirectoryName(_filePath);
                if (!IsResolvePackage) return;
                _packageConfig = new PackageConfig {FilePath = value, PackageConfigPath = dirPath + "\\packages.config"};
            }
        }

        public string BuilderPath { get; set; }

        public bool IsResolvePackage { get; set; }

        // returns false if there were build errors
        public void Resolve()
        {
            // do nothing if project already has build errors
            if (projectHasBuildErrors())
            {
                raiseBuildErrorsEvent();
                return;
            }
            if (IsResolvePackage) _packageConfig.LoadPackagesIfAny();

            resolve(Reference);

            if (_isCancel) return;

            resolve(Project + Reference);
        }

        public void Cancel()
        {
            _isCancel = true;
        }

        void resolve(string referenceType)
        {
            raiseProgressMessageEvent(string.Format("Resolving {0}s in {1}", referenceType, Path.GetFileName(FilePath)));

            XmlDocument projectXmlDocument = getXmlDocument();
            XmlDocument projectXmlDocumentToRestore = getXmlDocument();

            int itemIndex = 0;
            XmlNode item = getReferenceGroupItemIn(projectXmlDocument, referenceType, itemIndex);
            while (item != null)
            {
                if (_isCancel) break;

                // use string names to match up references, using nodes themselves will mess references
                if (item.ChildNodes.Count == 0) continue;

                IEnumerable<string> referenceNodeNames = getReferenceNodeNamesIn(item);

                IList<string> nodeNames = referenceNodeNames as IList<string> ?? referenceNodeNames.ToList();
                if (ReferenceCountEvent != null) ReferenceCountEvent(nodeNames.Count());

                foreach (string referenceNodeName in nodeNames)
                {
                    if (_isCancel) break;

                    XmlNode nodeToRemove = findNodeByAttributeName(item, referenceNodeName);
                    if (nodeToRemove == null) continue;

                    if (nodeToRemove.ParentNode == null) continue;

                    nodeToRemove.ParentNode.RemoveChild(nodeToRemove);

                    projectXmlDocument.Save(FilePath);

                    if (projectHasBuildErrors())
                    {
                        raiseProgressMessageEvent(string.Format("\tKept: {0}", referenceNodeName));
                        if (IsResolvePackage) _packageConfig.CopyPackageToKeep(nodeToRemove);

                        // restore original
                        projectXmlDocumentToRestore.Save(FilePath);
                        // reload item group from doc
                        projectXmlDocument = getXmlDocument();
                        item = getReferenceGroupItemIn(projectXmlDocument, referenceType, itemIndex);
                        // prevents from using yield return?
                        if (item == null) break;
                    }
                    else
                    {
                        raiseProgressMessageEvent(string.Format("\tRemoved: {0}", referenceNodeName));
                        projectXmlDocumentToRestore = getXmlDocument();
                        if (IsResolvePackage) _packageConfig.RemoveUnusedPackage(nodeToRemove);
                    }
                }

                raiseItemGroupResolvedEvent();

                item = getReferenceGroupItemIn(projectXmlDocument, referenceType, ++itemIndex);
            }

            if (_isCancel) return;

            if (IsResolvePackage) _packageConfig.UpdatePackageConfig();
            raisePackageResolveProgressEvent("Packages resolved!");

            raiseProgressMessageEvent("Done with: " + Path.GetFileName(FilePath));
        }

        XmlDocument getXmlDocument()
        {
            var doc = new XmlDocument();
            doc.Load(FilePath);
            return doc;
        }

        XmlNodeList getItemGroupNodesIn(XmlDocument document)
        {
            const string ItemGroupXPath = @"//*[local-name()='ItemGroup']";
            return document.SelectNodes(ItemGroupXPath);
        }

        XmlNode getReferenceGroupItemIn(XmlDocument document, string referenceNodeName, int startIndex)
        {
            XmlNodeList itemGroups = getItemGroupNodesIn(document);

            if (itemGroups == null || itemGroups.Count == 0) return null;

            for (int i = startIndex; i < itemGroups.Count; i++)
            {
                if (itemGroups[i].ChildNodes.Count == 0) return null;

                if (itemGroups[i].ChildNodes[0].Name == referenceNodeName) return itemGroups[i];
            }

            return null;
        }

        IEnumerable<string> getReferenceNodeNamesIn(XmlNode itemGroup)
        {
            return itemGroup
                .ChildNodes
                .OfType<XmlNode>()
                .Select(x => x.Attributes != null ? x.Attributes[Include].Value : null)
                .ToList();
        }

        XmlNode findNodeByAttributeName(XmlNode itemGroup, string attributeName)
        {
            return
                itemGroup
                    .ChildNodes
                    .Cast<XmlNode>()
                    .FirstOrDefault
                    (
                        item => item.Attributes != null
                                && item.Attributes[Include].Value == attributeName
                    );
        }

        bool projectHasBuildErrors()
        {
            const string LogFileName = "buildlog.txt";
            const string ArgumentsFormat = "\"{0}\" /clp:ErrorsOnly /nologo /m /flp:logfile={1};Verbosity=Quiet";
            const string Error = "error";

            string tempPath = Path.GetTempPath();
            string logFile = tempPath + @"\" + LogFileName;

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

            using (Process exeProcess = Process.Start(startInfo))
                if (exeProcess != null) exeProcess.WaitForExit();

            // open build log to check for errors
            if (File.Exists(logFile))
            {
                string s = File.ReadAllText(logFile);
                File.Delete(logFile);

                // if build error, the reference cannot be removed
                return s.Contains(Error);
            }
            return true;
        }

        void raiseProgressMessageEvent(string message)
        {
            if (ProgressMessageEvent != null) ProgressMessageEvent(message);
        }

        void raiseBuildErrorsEvent()
        {
            if (HasBuildErrorsEvent != null) HasBuildErrorsEvent(Path.GetFileNameWithoutExtension(FilePath));
        }

        void raiseItemGroupResolvedEvent()
        {
            if (ItemGroupResolvedEvent != null) ItemGroupResolvedEvent(null, null);
        }

        void raisePackageResolveProgressEvent(string message)
        {
            if (PackageResolveProgressEvent != null) PackageResolveProgressEvent(message);
        }
    }
}