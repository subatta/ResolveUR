namespace ResolveURVisualStudioPackage
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using EnvDTE;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Thread = System.Threading.Thread;

    internal class Helper : Package
    {
        private bool _dialogCanceled;

        public Helper()
        {
            ProgressDialog = null;
        }

        public IVsThreadedWaitDialog2 ProgressDialog { get; set; }

        public OutputWindow OutputWindow { get; set; }

        public string CurrentProject { get; set; }
        public int ItemGroupCount { get; set; }

        public int CurrentReferenceCountInItemGroup { get; set; }

        public int TotalReferenceCount { get; set; }

        public IVsUIShell UiShell { get; set; }
        public event EventHandler ResolveurCanceled;

        public void ShowMessageBox(
            string title,
            string message)
        {
            Thread.Sleep(1000);
            var clsid = Guid.Empty;
            int result;
            UiShell.ShowMessageBox(
                0,
                ref clsid,
                title,
                string.Format(CultureInfo.CurrentCulture, message, ToString()),
                string.Empty,
                0,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                OLEMSGICON.OLEMSGICON_INFO,
                0,
                out result);
        }

        public void SetMessage(
            string message)
        {
            if (OutputWindow != null)
            {
                OutputWindow.ActivePane.OutputString(message);
                OutputWindow.ActivePane.OutputString(Environment.NewLine);
            }

            Debug.WriteLine(message);

            ProgressDialog.UpdateProgress(
                string.Empty,
                CurrentProject + Environment.NewLine + "Resolving Reference Group: " + ItemGroupCount +
                Environment.NewLine + message,
                message,
                ++CurrentReferenceCountInItemGroup,
                TotalReferenceCount,
                false,
                out _dialogCanceled);
            if (_dialogCanceled)
                HandleResolveurCancelation(_dialogCanceled);
        }

        public void EndWaitDialog()
        {
            int userCanceled;
            ProgressDialog.EndWaitDialog(out userCanceled);
            if (userCanceled != 0)
                HandleResolveurCancelation(userCanceled != 0);
        }

        private void HandleResolveurCancelation(
            bool userCanceled)
        {
            ResolveurCanceled?.Invoke(null, null);
            if (userCanceled)
                ShowMessageBox(Constants.AppName + " Status", "Canceled");
        }
    }
}