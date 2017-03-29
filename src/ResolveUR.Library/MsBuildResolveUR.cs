namespace ResolveUR.Library
{
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

        static string GetX86Path()
        {
            if (File.Exists(Constants.Msbuildx8620))
                return Constants.Msbuildx8620;

            if (File.Exists(Constants.Msbuildx8635))
                return Constants.Msbuildx8635;

            if (File.Exists(Constants.Msbuildx8640))
                return Constants.Msbuildx8640;

            if (File.Exists(Constants.Msbuildx86V12))
                return Constants.Msbuildx86V12;

            if (File.Exists(Constants.Msbuildx86V14))
                return Constants.Msbuildx86V14;

            return null;
        }

        static string GetX64Path()
        {
            if (File.Exists(Constants.Msbuildx6420))
                return Constants.Msbuildx6420;

            if (File.Exists(Constants.Msbuildx6435))
                return Constants.Msbuildx6435;

            if (File.Exists(Constants.Msbuildx6440))
                return Constants.Msbuildx6440;

            if (File.Exists(Constants.Msbuildx64V12))
                return Constants.Msbuildx64V12;

            if (File.Exists(Constants.Msbuildx64V14))
                return Constants.Msbuildx64V14;

            return null;
        }
    }
}