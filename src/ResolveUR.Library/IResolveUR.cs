

namespace ResolveUR
{
    public delegate void HasBuildErrorsEventHandler(string projectName);

    interface IResolveUR
    {
        event HasBuildErrorsEventHandler HasBuildErrorsEvent;

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
    }
}
