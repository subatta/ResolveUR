
using System;
using System.Configuration;
using System.IO;

namespace ResolveUR
{
    class ProjectReferences
    {
        public static void Main(string[] args)
        {
            // at least solution path is required
            if (args.Length <= 0)
                return;

            // 1st arg must be valid solution path 
            if (!File.Exists(args[0]))
                return;
            
            // 2nd arg can be platform - x86 or x64
            var platform = string.Empty;
            if (args.Length >= 2 && (args[1] == "x86" || args[1] == "x64"))
                platform = args[1];

            // preset msbuild path checking if it were present
            var msbuildPath = FindMsBuildPath(platform);
            if (string.IsNullOrWhiteSpace(msbuildPath))
            {
                Console.WriteLine("MsBuild Not found on system. Aborting...");
                return;
            }

            // resolve
            var remover = new RemoveUnusedSolutionReferences
            {
                BuilderPath = msbuildPath,
                FilePath = args[0]
            };
            remover.Resolve();
        }


        public static string FindMsBuildPath(string platform = "")
        {
            var x86Keys = new string[] { "msbuildx8640", "msbuildx8635", "msbuildx8620" };
            var x64Keys = new string[] { "msbuildx6440", "msbuildx6435", "msbuildx6420" };

            // if user specified platform look by it
            var path = string.Empty;
            if (string.IsNullOrWhiteSpace(platform))
            {
                path = getValidPath(x64Keys);
                if (string.IsNullOrEmpty(path))
                    path = getValidPath(x86Keys);
            }
            else
            {
                if (platform == "x86")
                    path = getValidPath(x86Keys);
                else if (platform == "x64")
                    path = getValidPath(x64Keys);
            }
            return path;
        }

        /// <summary>
        /// Reads msbuild platform-framework key values which are paths
        /// and returns first valid one
        /// </summary>
        /// <param name="keys">x86 or x64 keys in config</param>
        /// <returns>a valid msbuild path, can be empty</returns>
        private static string getValidPath(string[] keys)
        {
            foreach (var key in keys)
            {
                var path = readSetting(key);
                if (File.Exists(path))
                    return path;
            }
            return string.Empty;
        }

        private static string readSetting(string key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
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