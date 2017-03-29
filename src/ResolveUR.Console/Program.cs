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
                var resolveUrOptions = ConsoleArgsResolveUR.Resolve(args);

                resolveUrOptions.MsBuilderPath = MsBuildResolveUR.FindMsBuildPath(resolveUrOptions.Platform);

                var resolveur = ResolveURFactory.GetResolver(
                    resolveUrOptions,
                    resolveur_HasBuildErrorsEvent,
                    resolveur_PackageResolveProgressEvent,
                    resolveur_ProgressMessageEvent);

                resolveur.Resolve();
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
            catch (NotSupportedException nse)
            {
                Console.WriteLine(nse.Message);
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
    }
}