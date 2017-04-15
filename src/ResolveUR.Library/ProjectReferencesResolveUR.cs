namespace ResolveUR.Library
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using static System.String;

    public class ProjectReferencesResolveUR : IResolveUR
    {
        const string Include = "Include";
        const string Reference = "Reference";
        const string Project = "Project";
        const char IgnoreChar = '#';

        static readonly string TempPath = Path.GetTempPath();
        static readonly string RefsReviewFilePath = $@"{TempPath}\\refs.txt";

        string _filePath;
        bool _isCancel;
        List<XmlNode> _nodesToRemove;
        PackageConfig _packageConfig;

        public string RefsIgnorePath => $"{Path.GetDirectoryName(FilePath)}\\.refsignored";

        public event HasBuildErrorsEventHandler HasBuildErrorsEvent;
        public event ProjectResolveCompleteEventHandler ProjectResolveCompleteEvent;

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
                if (!ShouldResolvePackage)
                    return;

                _packageConfig = new PackageConfig
                {
                    FilePath = value,
                    PackageConfigPath = dirPath + "\\packages.config"
                };
            }
        }

        public string BuilderPath { get; set; }

        public bool ShouldResolvePackage { get; set; }

        // returns false if there were build errors
        public void Resolve()
        {
            _isCancel = false;

            // do nothing if project already has build errors
            if (ProjectHasBuildErrors())
            {
                RaiseBuildErrorsEvent();
                return;
            }

            _nodesToRemove = Resolve(Reference);
            _nodesToRemove.AddRange(Resolve(Project + Reference));

            if (!_nodesToRemove.Any())
                return;

            if (_isCancel)
                return;

            WriteRefsToFile();

            if (_isCancel)
                return;

            LaunchRefsFile();

            ProjectResolveCompleteEvent?.Invoke();
        }

        public void Cancel()
        {
            _isCancel = true;
        }

        public void Clean()
        {
            ProcessRefsFromFile();

            RemoveNodesInProject(Reference);
            RemoveNodesInProject(Project + Reference);

            if (ShouldResolvePackage)
                ResolvePackages(_nodesToRemove);
        }

        /// <summary>
        ///     Writes references to be removed to a temp file inluding marking already ignored refs
        /// </summary>
        void WriteRefsToFile()
        {
            var ignored = LoadIgnoredRefs();
            using (var sw = new StreamWriter(RefsReviewFilePath))
            {
                sw.WriteLine($"### The project \"{Path.GetFileNameWithoutExtension(FilePath)}\" is being resolved...");
                sw.WriteLine(
                    $"### Please Prefix Reference Line to exclude with ONE pound sign! If you've previously ignored any and those are found, they are pre-marked.");
                foreach (var xmlNode in _nodesToRemove)
                {
                    var refIgnored = $"{IgnoreChar}{xmlNode.Attributes[0].Value}";
                    sw.WriteLine(ignored.Contains(refIgnored) ? refIgnored : xmlNode.Attributes[0].Value);
                }
            }
        }

        void LaunchRefsFile()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = RefsReviewFilePath
                }
            };

            process.Start();
            process.WaitForExit();
        }

        void ProcessRefsFromFile()
        {
            var refsSelectedToRemove = new List<string>();
            var refsIgnored = new List<string>();
            using (var sr = new StreamReader(RefsReviewFilePath))
            {
                string line;
                while (!IsNullOrWhiteSpace(line = sr.ReadLine()))
                {
                    if (line.StartsWith(new string(IgnoreChar, 3)))
                        continue;

                    if (line.StartsWith(IgnoreChar.ToString()))
                        refsIgnored.Add(line);
                    else
                        refsSelectedToRemove.Add(line);
                }
            }

            // append distinct ignored refs to .refsignored in project folder.
            var existingRefsIgnored = LoadIgnoredRefs();
            foreach (var existing in existingRefsIgnored)
            {
                if (!refsIgnored.Contains(existing))
                    refsIgnored.Add(existing);
            }
            WriteRefsIgnored(refsIgnored);

            var finalRefsToRemove = refsSelectedToRemove.Except(existingRefsIgnored).ToList();

            // trim final node list
            for (var i = 0; i < _nodesToRemove.Count; i++)
            {
                if (!finalRefsToRemove.Contains(_nodesToRemove[i].Attributes[0].Value))
                    _nodesToRemove[i] = null;
            }

            _nodesToRemove.RemoveAll(x => x == null);
        }

        List<string> LoadIgnoredRefs()
        {
            var refsIgnored = new List<string>();

            if (!File.Exists(RefsIgnorePath))
                return refsIgnored;

            using (var sr = new StreamReader(RefsIgnorePath))
            {
                string line = null;
                while (!IsNullOrWhiteSpace(line = sr.ReadLine()))
                    refsIgnored.Add(line);
            }

            return refsIgnored;
        }

        void WriteRefsIgnored(IEnumerable<string> list)
        {
            using (var sw = new StreamWriter(RefsIgnorePath))
            {
                foreach (var i in list)
                    sw.WriteLine(i);
            }
        }

        List<XmlNode> Resolve(string referenceType)
        {
            var originalProjectXmlDocument = GetXmlDocument();
            var projectXmlDocument = GetXmlDocument();
            var nodesToRemove = new List<XmlNode>();

            var itemIndex = 0;
            XmlNode item;
            while (((item = GetReferenceGroupItemIn(projectXmlDocument, referenceType, itemIndex)) != null) &&
                   item.ChildNodes.Count > 0)
            {
                if (_isCancel)
                    break;

                // use string names to match up references, using nodes themselves will mess up references
                var referenceNodeNames = item.ChildNodes.OfType<XmlNode>().Select(x => x.Attributes?[Include].Value);
                var nodeNames = referenceNodeNames as IList<string> ?? referenceNodeNames.ToList();

                nodesToRemove.AddRange(FindNodesToRemoveForItemGroup(projectXmlDocument, nodeNames, item, referenceType, itemIndex++));
            }

            // restore original project
            if (itemIndex > 0)
                originalProjectXmlDocument.Save(FilePath);

            return nodesToRemove;
        }

        void ResolvePackages(IEnumerable<XmlNode> nodesToRemove)
        {
            if (!_packageConfig.Load())
                return;

            foreach (var xmlNode in nodesToRemove)
            {
                if (_isCancel)
                    break;

                _packageConfig.Remove(xmlNode);
            }

            _packageConfig.Save();
        }

        IEnumerable<XmlNode> FindNodesToRemoveForItemGroup(
            XmlDocument projectXmlDocument,
            IList<string> nodeNames,
            XmlNode item,
            string referenceType,
            int itemIndex)
        {
            var nodesToRemove = new List<XmlNode>();
            var projectXmlDocumentToRestore = GetXmlDocument();

            foreach (var referenceNodeName in nodeNames)
            {
                if (_isCancel)
                    break;

                var nodeToRemove =
                    item.ChildNodes.Cast<XmlNode>().FirstOrDefault(
                        i => i.Attributes != null && i.Attributes[Include].Value == referenceNodeName);

                if (nodeToRemove?.ParentNode == null)
                    continue;

                nodeToRemove.ParentNode.RemoveChild(nodeToRemove);

                projectXmlDocument.Save(FilePath);

                if (ProjectHasBuildErrors())
                {
                    // restore original
                    projectXmlDocumentToRestore.Save(FilePath);
                    // reload item group from doc
                    projectXmlDocument = GetXmlDocument();
                    item = GetReferenceGroupItemIn(projectXmlDocument, referenceType, itemIndex);

                    if (item == null)
                        break;
                }
                else
                {
                    nodesToRemove.Add(nodeToRemove);
                    projectXmlDocumentToRestore = GetXmlDocument();
                }
            }

            return nodesToRemove;
        }

        void RemoveNodesInProject(string referenceType)
        {
            var projectXmlDocument = GetXmlDocument();

            var itemIndex = 0;
            XmlNode item;
            while (((item = GetReferenceGroupItemIn(projectXmlDocument, referenceType, itemIndex++)) != null) &&
                   item.ChildNodes.Count > 0)
            {
                if (_isCancel)
                    break;

                for (var i = 0; i < item.ChildNodes.Count; i++)
                {
                    if (_isCancel)
                        break;

                    if (_nodesToRemove.Any(x => x.Attributes[0].Value == item.ChildNodes[i].Attributes[0].Value))
                        item.RemoveChild(item.ChildNodes[i]);
                }
            }

            projectXmlDocument.Save(FilePath);
        }

        XmlDocument GetXmlDocument()
        {
            var doc = new XmlDocument();
            doc.Load(FilePath);
            return doc;
        }

        XmlNode GetReferenceGroupItemIn(XmlDocument document, string referenceNodeName, int startIndex)
        {
            var itemGroups = document.SelectNodes(@"//*[local-name()='ItemGroup']");

            if (itemGroups == null || itemGroups.Count == 0)
                return null;

            for (var i = startIndex; i < itemGroups.Count; i++)
            {
                if (!itemGroups[i].HasChildNodes)
                    return null;

                if (itemGroups[i].ChildNodes[0].Name == referenceNodeName)
                    return itemGroups[i];
            }

            return null;
        }

        bool ProjectHasBuildErrors()
        {
            const string logFileName = "buildlog.txt";
            const string argumentsFormat = "\"{0}\" /clp:ErrorsOnly /nologo /m /flp:logfile={1};Verbosity=Quiet";
            const string error = "error";

            var logFile = $"{TempPath}\\{logFileName}";

            var startInfo = new ProcessStartInfo
            {
                FileName = BuilderPath,
                Arguments = Format(CultureInfo.CurrentCulture, argumentsFormat, FilePath, logFile),
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = TempPath
            };

            // clear build log file if it was left out for some reason
            if (File.Exists(logFile))
                File.Delete(logFile);

            var status = 0;
            using (var exeProcess = Process.Start(startInfo))
            {
                exeProcess?.WaitForExit();
                if (exeProcess != null)
                    status = exeProcess.ExitCode;
            }

            // open build log to check for errors
            if (!File.Exists(logFile))
                return true;

            var s = File.ReadAllText(logFile);
            File.Delete(logFile);

            // if build error, the reference cannot be removed
            return status != 0 && (s.Contains(error) || IsNullOrWhiteSpace(s));
        }

        void RaiseBuildErrorsEvent()
        {
            HasBuildErrorsEvent?.Invoke(Path.GetFileNameWithoutExtension(FilePath));
        }
    }
}