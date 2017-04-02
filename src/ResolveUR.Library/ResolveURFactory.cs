namespace ResolveUR.Library
{
    using System.IO;

    public class ResolveURFactory
    {
        const string Proj = "proj";
        const string Sln = "sln";

        public static IResolve GetResolver(
            ResolveUROptions options,
            HasBuildErrorsEventHandler hasBuildErrorsEvent,
            ProjectResolveCompleteEventHandler projectResolveCompleteEvent)
        {
            IResolveUR resolveUr = new ProjectReferencesResolveUR
            {
                ShouldResolvePackage = options.ShouldResolvePackages,
                BuilderPath = options.MsBuilderPath,
                FilePath = options.FilePath
            };
            resolveUr.HasBuildErrorsEvent += hasBuildErrorsEvent;
            resolveUr.ProjectResolveCompleteEvent += projectResolveCompleteEvent;

            if (options.FilePath.EndsWith(Proj))
                return resolveUr;

            if (options.FilePath.EndsWith(Sln))
                return new SolutionReferencesResolveUR(resolveUr);

            if (resolveUr == null)
            {
                throw new InvalidDataException(
                    "The file path supplied(arg 0) must either be a solution or project file");
            }

            return resolveUr;
        }
    }
}