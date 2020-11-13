namespace ResolveUR
{
    using System;
    using System.IO;
    using Library;

    public static class Program
    {
        static IResolve _resolveur;

        public static void Main(string[] args)
        {
            try
            {
                var resolveUrOptions = ConsoleArgsResolveUR.Resolve(args);

                resolveUrOptions.MsBuilderPath = MsBuildResolveUR.FindMsBuildPath(resolveUrOptions.Platform);

                _resolveur = ResolveURFactory.GetResolver(
                    resolveUrOptions,
                    Resolveur_HasBuildErrorsEvent,
                    Resolveur_ProjectResolveCompleteEvent);

                _resolveur.Resolve();
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

        static void Resolveur_ProjectResolveCompleteEvent()
        {
            Console.WriteLine("Continue with removal of references (y/n)? Default is y: ");
            var response = Console.ReadLine();

            const string yes = "y";
            response = string.IsNullOrWhiteSpace(response) ? yes : response;
            if (string.Equals(response, yes, StringComparison.CurrentCultureIgnoreCase))
                _resolveur.Clean();
        }

        static void Resolveur_HasBuildErrorsEvent(string projectName)
        {
            Console.WriteLine("{0} has build errors.", projectName);
        }
    }
}