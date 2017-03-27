namespace ResolveUR
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;

    class MsBuildResolveUR
    {
        public static string FindMsBuildPath(string platform)
        {
            var x86Keys = new[]
            {
                "msbuildx86v14",
                "msbuildx86v12",
                "msbuildx8640",
                "msbuildx8635",
                "msbuildx8620"
            };
            var x64Keys = new[]
            {
                "msbuildx64v14",
                "msbuildx64v12",
                "msbuildx6440",
                "msbuildx6435",
                "msbuildx6420"
            };

            // if user specified platform look by it
            var path = string.Empty;
            if (string.IsNullOrWhiteSpace(platform))
            {
                path = GetValidPath(x64Keys);
                if (string.IsNullOrEmpty(path))
                    path = GetValidPath(x86Keys);
            }
            else
            {
                if (platform == Constants.X86)
                    path = GetValidPath(x86Keys);
                else if (platform == Constants.X64)
                    path = GetValidPath(x64Keys);
            }

            return path;
        }

        /// <summary>
        ///     Reads msbuild platform-framework key values which are paths
        ///     and returns first valid one
        /// </summary>
        /// <param name="keys">x86 or x64 keys in config</param>
        /// <returns>a valid msbuild path, can be empty</returns>
        static string GetValidPath(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                var path = ReadSetting(key);
                path += "MSBuild.exe";

                if (File.Exists(path))
                    return path;
            }

            return string.Empty;
        }

        static string ReadSetting(string key)
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
}