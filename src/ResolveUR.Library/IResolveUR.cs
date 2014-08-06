

namespace ResolveUR
{
    public delegate void HasBuildErrorsEventHandler(string projectName);
    public delegate void ProgressMessageEventHandler(string message);

    public interface IResolveUR
    {
        event HasBuildErrorsEventHandler HasBuildErrorsEvent;
        event ProgressMessageEventHandler ProgressMessageEvent;

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
