namespace ResolveUR.Library
{
    using System;
    using System.IO;

    public class ResolveURFactory
    {
        public static IResolve GetResolver(
            ResolveUROptions options,
            HasBuildErrorsEventHandler hasBuildErrorsEventHandler,
            ProgressMessageEventHandler progressMessageEventHandler,
            PackageResolveProgressEventHandler packageResolveProgressEventHandler,
            ReferenceCountEventHandler referenceCountEventHandler = null,
            EventHandler itemGroupResolverEventHandler = null)
        {
            IResolveUR resolveUr = new ProjectReferencesResolveUR
            {
                ShouldResolvePackage = options.ShouldResolvePackages,
                BuilderPath = options.MsBuilderPath,
                FilePath = options.FilePath
            };

            resolveUr.HasBuildErrorsEvent += hasBuildErrorsEventHandler;
            resolveUr.ProgressMessageEvent += progressMessageEventHandler;
            resolveUr.PackageResolveProgressEvent += packageResolveProgressEventHandler;

            if (referenceCountEventHandler != null)
                resolveUr.ReferenceCountEvent += referenceCountEventHandler;
            if (itemGroupResolverEventHandler != null)
                resolveUr.ItemGroupResolvedEvent += itemGroupResolverEventHandler;

            if (options.FilePath.EndsWith("proj"))
                return resolveUr;
            if (options.FilePath.EndsWith(".sln"))
                return new SolutionReferencesResolveUR(resolveUr);

            if (resolveUr == null)
                throw new InvalidDataException(
                    "The file path supplied(arg 0) must either be a solution or project file");

            return resolveUr;
        }
    }
}