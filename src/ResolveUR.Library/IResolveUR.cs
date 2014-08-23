

using System;
namespace ResolveUR.Library
{
    public delegate void HasBuildErrorsEventHandler(string projectName);
    public delegate void ProgressMessageEventHandler(string message);
    public delegate void ReferenceCountEventHandler(int count);
    public delegate void PackageResolveProgressEventHandler(string message);

    public interface IResolveUR
    {
        event HasBuildErrorsEventHandler HasBuildErrorsEvent;
        event ProgressMessageEventHandler ProgressMessageEvent;
        event ReferenceCountEventHandler ReferenceCountEvent;
        event EventHandler ItemGroupResolvedEvent;
        event PackageResolveProgressEventHandler PackageResolveProgressEvent;

        string BuilderPath
        {
            get;
            set;
        }
        string FilePath
        {
            get;
            set;
        }
        void Resolve();

        void Cancel();

        bool IsResolvePackage
        {
            get;
            set;
        }
    }
}
