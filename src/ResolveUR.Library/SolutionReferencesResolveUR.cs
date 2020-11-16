namespace ResolveUR.Library
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class SolutionReferencesResolveUR : IResolve
    {
        readonly IResolveUR _resolveur;
        bool _isCancel;

        public SolutionReferencesResolveUR(IResolveUR resolveUr)
        {
            _resolveur = resolveUr;
        }

        public void Resolve()
        {
            // get all project file full paths from solution file
            var projectFiles = LoadProjects(_resolveur.FilePath);

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

        public void Clean()
        {
            _resolveur.Clean();
        }

        public void Cancel()
        {
            _isCancel = true;
        }

        static IEnumerable<string> LoadProjects(string solutionPath)
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
                    throw new NotSupportedException($"Path: {projects[i]}, Error: {ex.Message}");
                }
            }

            return projects;
        }
    }
}