namespace ResolveUR.Library
{
    using System;

    public delegate void HasBuildErrorsEventHandler(string projectName);

    public delegate void ProgressMessageEventHandler(string message);

    public delegate void ReferenceCountEventHandler(int count);

    public delegate void PackageResolveProgressEventHandler(string message);

    public interface IResolve
    {
        void Resolve();
        void Cancel();
    }

    public interface IResolveUR : IResolve
    {
        string BuilderPath { get; set; }

        string FilePath { get; set; }

        bool ShouldResolvePackage { get; set; }

        bool ShouldPreview { get; set; }

        event HasBuildErrorsEventHandler HasBuildErrorsEvent;
        event ProgressMessageEventHandler ProgressMessageEvent;
        event ReferenceCountEventHandler ReferenceCountEvent;
        event EventHandler ItemGroupResolvedEvent;
        event PackageResolveProgressEventHandler PackageResolveProgressEvent;
    }
}