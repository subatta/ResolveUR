using Microsoft.VisualStudio.Setup.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ResolveUR.Library
{
    public static class MsBuildResolveUR
    {
        public static string FindMsBuildPath(string platform = "")
        {
            var path = FindInstallationMSBuildPath();

            // If the new method fails, use the old methods.
            if (string.IsNullOrWhiteSpace(path))
            {
                if (string.IsNullOrWhiteSpace(platform))
                {
                    path = GetX64Path();
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        path = GetX86Path();
                    }
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
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new FileNotFoundException("MsBuild.exe not found on system!");
            }

            return path;
        }

        static string GetX86Path()
        {
            return GetPath(
                new List<string>
                {
                    Constants.Msbuildx86VSCurrent,
                    Constants.Msbuildx86VSCmd,
                    Constants.Msbuildx86VS,
                    Constants.Msbuildx86V14,
                    Constants.Msbuildx86V12,
                    Constants.Msbuildx8640,
                    Constants.Msbuildx8635,
                    Constants.Msbuildx8620
                });
        }

        static string GetX64Path()
        {
            return GetPath(
                new List<string>
                {
                    Constants.Msbuildx64VSCurrent,
                    Constants.Msbuildx64VSCmd,
                    Constants.Msbuildx64VS,
                    Constants.Msbuildx64V14,
                    Constants.Msbuildx64V12,
                    Constants.Msbuildx6440,
                    Constants.Msbuildx6435,
                    Constants.Msbuildx6420
                });
        }

        static string GetPath(List<string> searchPaths)
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



        private const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);

        /// <summary>
        ///     Find MSBuild using Microsoft.VisualStudio.Setup.Configuration
        /// </summary>
        /// <returns>The path to MS-Build found.</returns>
        internal static string FindInstallationMSBuildPath()
        {
            try
            {
                var query = new SetupConfiguration();
                var allInstances = query.EnumAllInstances();

                string foundPath = "";
                string selectedFile = "";
                var creationDate = DateTime.MinValue;
                var instances = new ISetupInstance[1];
                allInstances.Next(1, instances, out int fetched);
                while (fetched > 0)
                {
                    foundPath = instances[0].GetInstallationPath();
                    allInstances.Next(1, instances, out fetched);
                    var msBuildPath = Path.Combine(foundPath, "MSBuild");
                    var file = Directory.EnumerateFiles(msBuildPath, "MSBuild.exe", SearchOption.AllDirectories).First();
                    File.GetCreationTime(file);
                    if (File.GetCreationTime(file) > creationDate)
                    {
                        selectedFile = file;
                        creationDate = File.GetCreationTime(file);
                    }
                };

                return selectedFile;
            }
            catch (COMException ex) when (ex.HResult == REGDB_E_CLASSNOTREG)
            {
                Debug.WriteLine("The query API is not registered. Assuming no instances are installed.");
                return null;
            }
        }
    }
}
