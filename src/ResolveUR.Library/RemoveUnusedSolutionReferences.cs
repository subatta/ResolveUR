namespace ResolveUR.Library
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class RemoveUnusedSolutionReferences : IResolveUR
    {
        bool _isCancel;
        IResolveUR _resolveur;
        public event HasBuildErrorsEventHandler HasBuildErrorsEvent;
        public event ProgressMessageEventHandler ProgressMessageEvent;
        public event ReferenceCountEventHandler ReferenceCountEvent;
        public event EventHandler ItemGroupResolvedEvent;
        public event PackageResolveProgressEventHandler PackageResolveProgressEvent;

        public bool IsResolvePackage { get; set; }

        public string BuilderPath { get; set; }

        public string FilePath { get; set; }

        public void Resolve()
        {
            // get all project file full paths from solution file
            var projectFiles = LoadProjects(FilePath);

            _resolveur = new RemoveUnusedProjectReferences
            {
                BuilderPath = BuilderPath
            };
            _resolveur.HasBuildErrorsEvent += resolver_HasBuildErrorsEvent;
            _resolveur.ProgressMessageEvent += resolver_ProgressMessageEvent;
            _resolveur.ReferenceCountEvent += _resolveur_ReferenceCountEvent;
            _resolveur.ItemGroupResolvedEvent += _resolveur_ItemGroupResolvedEvent;
            _resolveur.PackageResolveProgressEvent += _resolveur_PackageResolveProgressEvent;
            _resolveur.IsResolvePackage = IsResolvePackage;
            foreach (var projectFile in projectFiles)
            {
                if (_isCancel)
                    break;

                if (!File.Exists(projectFile))
                    continue;

                _resolveur.FilePath = projectFile;
                _resolveur.Resolve();
            }
        }

        public void Cancel()
        {
            _isCancel = true;
        }

        IEnumerable<string> LoadProjects(string solutionPath)
        {
            const string projectRegEx = "Project\\(\"\\{[\\w-]*\\}\"\\) = \"([\\w _]*.*)\", \"(.*\\.(cs|vcx|vb)proj)\"";
            var content = File.ReadAllText(solutionPath);
            var projReg = new Regex(projectRegEx, RegexOptions.Compiled);
            var matches = projReg.Matches(content).Cast<Match>();
            var projects = matches.Select(x => x.Groups[2].Value).ToList();
            for (var i = 0; i < projects.Count; ++i)
            {
                if (!Path.IsPathRooted(projects[i]))
                {
                    var folderName = Path.GetDirectoryName(solutionPath);
                    if (folderName != null)
                        projects[i] = Path.Combine(folderName, projects[i]);
                }
                try
                {
                    projects[i] = Path.GetFullPath(projects[i]);
                }
                catch (NotSupportedException ex)
                {
                    resolver_ProgressMessageEvent($"Path: {projects[i]}, Error: {ex.Message}");
                }
            }
            return projects;
        }

        void _resolveur_PackageResolveProgressEvent(string message)
        {
            PackageResolveProgressEvent?.Invoke(message);
        }

        void _resolveur_ItemGroupResolvedEvent(object sender, EventArgs e)
        {
            ItemGroupResolvedEvent?.Invoke(sender, e);
        }

        void _resolveur_ReferenceCountEvent(int count)
        {
            ReferenceCountEvent?.Invoke(count);
        }

        void resolver_ProgressMessageEvent(string message)
        {
            ProgressMessageEvent?.Invoke(message);
        }

        // rethrow event
        void resolver_HasBuildErrorsEvent(string projectName)
        {
            if (HasBuildErrorsEvent != null)
            {
                HasBuildErrorsEvent(projectName);
                _isCancel = true;
            }
        }
    }
}