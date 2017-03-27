namespace ResolveUR
{
    using System;
    using System.IO;
    using Library;

    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var consoleArgs = ConsoleArgsResolveUR.Resolve(args);

                var msbuild = CheckMsBuildExists(consoleArgs.Platform);

                var resolveur = GetSolutionOrProjectResolver(consoleArgs.FilePath);

                if (resolveur != null)
                {
                    resolveur.IsResolvePackage = consoleArgs.ShouldResolveNugets;
                    resolveur.BuilderPath = msbuild;
                    resolveur.FilePath = consoleArgs.FilePath;
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
            catch (ArgumentException ae)
            {
                Console.WriteLine(ae.Message);
            }
            catch (FileNotFoundException fnfe)
            {
                Console.WriteLine(fnfe.Message);
            }
            catch (InvalidDataException ide)
            {
                Console.WriteLine(ide.Message);
            }
        }

        static string CheckMsBuildExists(string platform)
        {
            var msbuildPath = MsBuildResolveUR.FindMsBuildPath(platform);
            if (string.IsNullOrWhiteSpace(msbuildPath))
                throw new FileNotFoundException("MsBuild not found on system!");

            return msbuildPath;
        }

        static IResolveUR GetSolutionOrProjectResolver(string filePath)
        {
            if (filePath.EndsWith("proj"))
                return new RemoveUnusedProjectReferences();

            if (filePath.EndsWith(".sln"))
                return new RemoveUnusedSolutionReferences();

            throw new InvalidDataException("The file path supplied(arg 0) must either be a solution or project file");
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
    }
}