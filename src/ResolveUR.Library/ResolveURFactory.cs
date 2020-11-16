namespace ResolveUR.Library
{
    using System.IO;

    public static class ResolveURFactory
    {
        const string Proj = "proj";
        const string Sln = "sln";

        public static IResolve GetResolver(
            ResolveUROptions options,
            HasBuildErrorsEventHandler hasBuildErrorsEvent,
            ProjectResolveCompleteEventHandler projectResolveCompleteEvent)
        {
            _ = options ?? throw new System.ArgumentNullException(nameof(options));

            IResolveUR resolveUr = new ProjectReferencesResolveUR
            {
                ShouldResolvePackage = options.ShouldResolvePackages,
                BuilderPath = options.MsBuilderPath,
                FilePath = options.FilePath
            };
            resolveUr.HasBuildErrorsEvent += hasBuildErrorsEvent;
            resolveUr.ProjectResolveCompleteEvent += projectResolveCompleteEvent;

            if (options.FilePath.EndsWith(Proj, System.StringComparison.CurrentCultureIgnoreCase))
                return resolveUr;

            if (options.FilePath.EndsWith(Sln, System.StringComparison.CurrentCultureIgnoreCase))
                return new SolutionReferencesResolveUR(resolveUr);

            if (resolveUr == null)
                throw new InvalidDataException(
                    "The file path supplied(arg 0) must either be a solution or project file");

            return resolveUr;
        }
    }
}