namespace ResolveUR.Library
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class MsBuildResolveUR
    {
        public static string FindMsBuildPath(string platform = "")
        {
            var path = string.Empty;
            if (string.IsNullOrWhiteSpace(platform))
            {
                path = GetX64Path();
                if (string.IsNullOrWhiteSpace(path))
                    path = GetX86Path();
            }
            else
            {
                // if user specified platform look by it
                switch (platform)
                {
                    case Constants.X64:
                        path = GetX64Path();
                        break;

                    case Constants.X86:
                        path = GetX86Path();
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(path))
                throw new FileNotFoundException("MsBuild not found on system!");

            return path;
        }

        private static string GetX64Path()
        {
            return GetPath(new List<string>
            {
                Constants.Msbuildx64VSCmd,
                Constants.Msbuildx64VS,
                Constants.Msbuildx64V14,
                Constants.Msbuildx64V12,
                Constants.Msbuildx6440,
                Constants.Msbuildx6435,
                Constants.Msbuildx6420
            });
        }

        private static string GetX86Path()
        {
            return GetPath(new List<string>
            {
                Constants.Msbuildx86VSCmd,
                Constants.Msbuildx86VS,
                Constants.Msbuildx86V14,
                Constants.Msbuildx86V12,
                Constants.Msbuildx8640,
                Constants.Msbuildx8635,
                Constants.Msbuildx8620
            });
        }

        private static string GetPath(List<string> searchPaths)
        {
            foreach (var path in searchPaths)
            {
                var msbuildPath = Environment.ExpandEnvironmentVariables(path);
                if (File.Exists(msbuildPath))
                {
                    return msbuildPath;
                }
            }

            return null;
        }
    }
}