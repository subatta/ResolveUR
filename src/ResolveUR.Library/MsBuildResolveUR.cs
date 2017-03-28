namespace ResolveUR.Library
{
    using System.IO;

    public class MsBuildResolveUR
    {
        const string Msbuild = "MSBuild.exe";

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

            return path;
        }

        static string GetX86Path()
        {
            if (File.Exists($"{Constants.Msbuildx8620}/{Msbuild}"))
                return $"{Constants.Msbuildx8620}/{Msbuild}";

            if (File.Exists($"{Constants.Msbuildx8635}/{Msbuild}"))
                return $"{Constants.Msbuildx8635}/{Msbuild}";

            if (File.Exists($"{Constants.Msbuildx8640}/{Msbuild}"))
                return $"{Constants.Msbuildx8640}/{Msbuild}";

            if (File.Exists($"{Constants.Msbuildx86V12}/{Msbuild}"))
                return $"{Constants.Msbuildx86V12}/{Msbuild}";

            if (File.Exists($"{Constants.Msbuildx86V14}/{Msbuild}"))
                return $"{Constants.Msbuildx86V14}/{Msbuild}";

            return null;
        }

        static string GetX64Path()
        {
            if (File.Exists($"{Constants.Msbuildx6420}/{Msbuild}"))
                return $"{Constants.Msbuildx6420}/{Msbuild}";

            if (File.Exists($"{Constants.Msbuildx6435}/{Msbuild}"))
                return $"{Constants.Msbuildx6435}/{Msbuild}";

            if (File.Exists($"{Constants.Msbuildx6440}/{Msbuild}"))
                return $"{Constants.Msbuildx6440}/{Msbuild}";

            if (File.Exists($"{Constants.Msbuildx64V12}/{Msbuild}"))
                return $"{Constants.Msbuildx64V12}/{Msbuild}";

            if (File.Exists($"{Constants.Msbuildx64V14}/{Msbuild}"))
                return $"{Constants.Msbuildx64V14}/{Msbuild}";

            return null;
        }
    }
}