using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using ResolveUR.Library;

namespace ResolveUR
{
    internal class ProjectReferences
    {
        const string X86 = "x86";
        const string X64 = "x64";

        public static void Main(string[] args)
        {
            // at least solution path is required
            if (args == null || args.Length == 0)
                return;

            // 1st arg must be valid solution path 
            if (!File.Exists(args[0]))
                return;

            // a name, readable than args[0] ;-)
            string filePath = args[0];

            // 2nd argument can be choice to resolve nuget packages or not.
            bool isResolvePackage = args.Length >= 2 && (args[1] == "true");

            // 3rd arg can be platform - x86 or x64
            string platform = string.Empty;
            if (args.Length >= 3 && (args[2] == X86 || args[2] == X64))
                platform = args[2];

            // preset msbuild path checking if it were present
            string msbuildPath = findMsBuildPath(platform);
            if (string.IsNullOrWhiteSpace(msbuildPath))
            {
                Console.WriteLine("MsBuild Not found on system. Aborting...");
                return;
            }

            // resolve for project or soluion
            IResolveUR resolveur = null;
            if (filePath.EndsWith("proj"))
                resolveur = new RemoveUnusedProjectReferences();
            else if (filePath.EndsWith(".sln"))
                resolveur = new RemoveUnusedSolutionReferences();

            if (resolveur != null)
            {
                resolveur.IsResolvePackage = isResolvePackage;
                resolveur.BuilderPath = msbuildPath;
                resolveur.FilePath = filePath;
                resolveur.HasBuildErrorsEvent += resolveur_HasBuildErrorsEvent;
                resolveur.ProgressMessageEvent += resolveur_ProgressMessageEvent;
                resolveur.PackageResolveProgressEvent += resolveur_PackageResolveProgressEvent;
                resolveur.Resolve();
            }
            else
            {
                Console.WriteLine("Unrecognized project or solution type");
            }
        }

        static void resolveur_PackageResolveProgressEvent(string message)
        {
            Console.WriteLine(message);
        }

        static void resolveur_ProgressMessageEvent(string message)
        {
            Console.WriteLine(message);
        }

        static void resolveur_HasBuildErrorsEvent(string projectName)
        {
            Console.WriteLine("{0} has build errors.", projectName);
        }


        static string findMsBuildPath(string platform)
        {
            var x86Keys = new[] {"msbuildx8640", "msbuildx8635", "msbuildx8620"};
            var x64Keys = new[] {"msbuildx6440", "msbuildx6435", "msbuildx6420"};

            // if user specified platform look by it
            string path = string.Empty;
            if (string.IsNullOrWhiteSpace(platform))
            {
                path = getValidPath(x64Keys);
                if (string.IsNullOrEmpty(path))
                    path = getValidPath(x86Keys);
            }
            else
            {
                if (platform == X86)
                    path = getValidPath(x86Keys);
                else if (platform == X64)
                    path = getValidPath(x64Keys);
            }
            return path;
        }

        /// <summary>
        ///     Reads msbuild platform-framework key values which are paths
        ///     and returns first valid one
        /// </summary>
        /// <param name="keys">x86 or x64 keys in config</param>
        /// <returns>a valid msbuild path, can be empty</returns>
        static string getValidPath(IEnumerable<string> keys)
        {
            foreach (string key in keys)
            {
                string path = readSetting(key);
                if (File.Exists(path))
                    return path;
            }
            return string.Empty;
        }

        static string readSetting(string key)
        {
            try
            {
                NameValueCollection appSettings = ConfigurationManager.AppSettings;
                return appSettings[key] ?? "Not Found";
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error reading app settings");
                return string.Empty;
            }
        }
    }
} // namespace