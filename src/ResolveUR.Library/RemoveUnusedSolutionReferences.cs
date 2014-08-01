using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ResolveUR
{
    public class RemoveUnusedSolutionReferences: IResolveUR
    {

        public event HasBuildErrorsEventHandler HasBuildErrorsEvent;

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

        public void Resolve()
        {
            // get all project file full paths from solution file
            var projectFiles = loadProjects(FilePath);

            var resolver = new RemoveUnusedProjectReferences
            {
                BuilderPath = BuilderPath
            };
            resolver.HasBuildErrorsEvent += resolver_HasBuildErrorsEvent;
            foreach (var projectFile in projectFiles)
            {
                if (!File.Exists(projectFile))
                    continue;

                resolver.FilePath = projectFile;
                resolver.Resolve();
            }            
        }

        // rethrow event
        void resolver_HasBuildErrorsEvent(string projectName)
        {
            if (HasBuildErrorsEvent != null)
                HasBuildErrorsEvent(projectName);
        }
    }

}
