using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ResolveUR.Library
{
    public class RemoveUnusedSolutionReferences: IResolveUR
    {

        public event HasBuildErrorsEventHandler HasBuildErrorsEvent;
        public event ProgressMessageEventHandler ProgressMessageEvent;
        public event ReferenceCountEventHandler ReferenceCountEvent;
        public event EventHandler ItemGroupResolvedEvent;

        private IEnumerable<string> loadProjects(string solutionPath)
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
                if (!Path.IsPathRooted(projects[i])) projects[i] = Path.Combine(Path.GetDirectoryName(solutionPath), projects[i]);
                try
                {
                    projects[i] = Path.GetFullPath(projects[i]);
                }
                catch (NotSupportedException ex)
                {
                    resolver_ProgressMessageEvent(string.Format("Path: {0}, Error: {1}", projects[i], ex.Message));
                }
            }
            return projects;
        }

        public bool IsResolvePackage
        {
            get;
            set;
        }

        public string BuilderPath
        {
            get;
            set;
        }

        public string FilePath
        {
            get;
            set;
        }

        private IResolveUR _resolveur;
        public void Resolve()
        {
            // get all project file full paths from solution file
            var projectFiles = loadProjects(FilePath);

            _resolveur = new RemoveUnusedProjectReferences
            {
                BuilderPath = BuilderPath
            };
            _resolveur.HasBuildErrorsEvent += resolver_HasBuildErrorsEvent;
            _resolveur.ProgressMessageEvent += resolver_ProgressMessageEvent;
            _resolveur.ReferenceCountEvent += _resolveur_ReferenceCountEvent;
            _resolveur.ItemGroupResolvedEvent += _resolveur_ItemGroupResolved;
            _resolveur.IsResolvePackage = IsResolvePackage;
            foreach (var projectFile in projectFiles)
            {
                if (_isCancel) break;

                if (!File.Exists(projectFile)) continue;

                _resolveur.FilePath = projectFile;
                _resolveur.Resolve();
            }
        }

        void _resolveur_ItemGroupResolved(object sender, EventArgs e)
        {
            if (ItemGroupResolvedEvent != null) ItemGroupResolvedEvent(sender, e);
        }

        void _resolveur_ReferenceCountEvent(int count)
        {
            if (ReferenceCountEvent != null) ReferenceCountEvent(count);
        }

        void resolver_ProgressMessageEvent(string message)
        {
            if (ProgressMessageEvent != null) ProgressMessageEvent(message);
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

        private bool _isCancel;
        public void Cancel()
        {
            _isCancel = true;
        }

    }

}
