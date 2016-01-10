namespace ResolveUR.Library
{
    using System;

    public delegate void HasBuildErrorsEventHandler(string projectName);

    public delegate void ProgressMessageEventHandler(string message);

    public delegate void ReferenceCountEventHandler(int count);

    public delegate void PackageResolveProgressEventHandler(string message);

// ReSharper disable once InconsistentNaming -- Product Name
    public interface IResolveUR
    {
        string BuilderPath { get; set; }
        string FilePath { get; set; }
        bool IsResolvePackage { get; set; }
        event HasBuildErrorsEventHandler HasBuildErrorsEvent;
        event ProgressMessageEventHandler ProgressMessageEvent;
        event ReferenceCountEventHandler ReferenceCountEvent;
        event EventHandler ItemGroupResolvedEvent;
        event PackageResolveProgressEventHandler PackageResolveProgressEvent;

        void Resolve();

        void Cancel();
    }
}