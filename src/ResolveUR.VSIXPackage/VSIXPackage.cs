using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ResolveUR.Library;
using Constants = EnvDTE.Constants;
using Thread = System.Threading.Thread;

namespace ResolveUR.VSIXPackage
{
    /// <summary>
    ///     is the class that implements the package exposed by assembly.
    ///     The minimum requirement for a class to be considered a valid package for Visual Studio
    ///     is to implement the IVsPackage interface and register itself with the shell.
    ///     package uses the helper classes defined inside the Managed Package Framework (MPF)
    ///     to do it: it derives from the Package class that provides the implementation of the
    ///     IVsPackage interface and uses the registration attributes defined in the framework to
    ///     register itself and its components with the shell.
    /// </summary>
    // attribute tells the PkgDef creation utility (CreatePkgDef.exe) that class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // attribute is used to register the information needed to show package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // attribute is needed to let the shell know that package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.GuidResolveUrVisualStudioPackagePkgString)]
    // ReSharper disable once InconsistentNaming - Product Name
    public sealed class ResolveURVSIXPackage : Package
    {
        /// <summary>
        ///     Default constructor of the package.
        ///     Inside method you can place any initialization code that does not require
        ///     any Visual Studio service because at point the package object is created but
        ///     not sited yet inside Visual Studio environment. The place to do all the other
        ///     initialization is the Initialize method.
        /// </summary>
        public ResolveURVSIXPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", ToString()));
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation

        /// <summary>
        ///     function is the callback used to execute a command when the a menu item is clicked.
        ///     See the Initialize method to see how the menu item is associated to function using
        ///     the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        void ProjectMenuItemCallback(object sender, EventArgs e)
        {
            HandleCallBack(GetProjectName);
        }

        void SolutionMenuItemCallback(object sender, EventArgs e)
        {
            HandleCallBack(GetSolutionName);
        }

        void HandleCallBack(Func<string> activeFileNameGetter)
        {
            CreateOutputWindow();
            CreateProgressDialog();
            CreateUiShell();

            try
            {
                var options = new ResolveUROptions
                {
                    MsBuilderPath = MsBuildResolveUR.FindMsBuildPath(),
                    FilePath = activeFileNameGetter(),
                    ShouldResolvePackages = PackageOption()
                };

                if (string.IsNullOrEmpty(options.FilePath))
                    return;

                _resolveur = ResolveURFactory.GetResolver(
                    options,
                    Resolveur_HasBuildErrorsEvent,
                    Resolveur_ProjectResolveCompleteEvent);

                _helper.ResolveurCanceled += Helper_ResolveurCanceled;

                _resolveur.Resolve();
            }
            catch (FileNotFoundException fnfe)
            {
                _helper.ShowMessageBox("File Not Found", fnfe.Message);
            }
            catch (InvalidDataException ide)
            {
                _helper.ShowMessageBox("Invalid Data", ide.Message);
            }
            catch (NotSupportedException nse)
            {
                _helper.ShowMessageBox("Selected file type invalid for resolution", nse.Message);
            }
            finally
            {
                _helper.EndWaitDialog();
            }
        }

        static bool PackageOption()
        {
            var packageResolveOptionDialog = new PackageDialog();
            packageResolveOptionDialog.ShowModal();
            return packageResolveOptionDialog.IsResolvePackage;
        }

        static bool RemoveConfirmed()
        {
            var removeConfirmDialog = new RemoveConfirmDialog();
            removeConfirmDialog.ShowModal();
            return removeConfirmDialog.IsRemoveConfirm;
        }

        public string GetSolutionName()
        {
            var dte2 = GetService(typeof(SDTE)) as DTE;

            var solutionObject = dte2?.Solution;
            if (solutionObject == null)
                return string.Empty;

            var solution = solutionObject.Properties.Item(5).Value.ToString();
            if (!File.Exists(solution))
                return string.Empty;

            return solution;
        }

        public string GetProjectName()
        {
            var dte2 = GetService(typeof(SDTE)) as DTE;

            var activeProjects = (Array)dte2?.ActiveSolutionProjects;
            if (activeProjects == null || activeProjects.Length == 0)
                return string.Empty;

            var project = (Project)activeProjects.GetValue(0);

            return project.FileName;
        }

        #region Package Members

        Helper _helper;
        IResolve _resolveur;

        /// <summary>
        ///     Initialization of the package; method is called right after the package is sited, so is the place
        ///     where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            if (!(GetService(typeof(IMenuCommandService)) is OleMenuCommandService mcs))
                return;

            // Create the command for the menu item.
            var menuCommandId = new CommandID(
                GuidList.GuidResolveUrVisualStudioPackageCmdSet,
                (int)PkgCmdIdList.CmdRemoveUnusedProjectReferences);
            var menuItem = new MenuCommand(ProjectMenuItemCallback, menuCommandId);
            mcs.AddCommand(menuItem);
            menuCommandId = new CommandID(
                GuidList.GuidResolveUrVisualStudioPackageCmdSet,
                (int)PkgCmdIdList.CmdRemoveUnusedSolutionReferences);
            menuItem = new MenuCommand(SolutionMenuItemCallback, menuCommandId);
            mcs.AddCommand(menuItem);
            _helper = new Helper();
        }

        #endregion

        #region Create Members

        void CreateOutputWindow()
        {
            if (!(GetService(typeof(SDTE)) is DTE dte2))
                return;

            var window = dte2.Windows.Item(Constants.vsWindowKindOutput);
            var outputWindow = (OutputWindow)window.Object;
            OutputWindowPane outputWindowPane = null;

            const string outputWindowName = "Output";
            for (uint i = 1; i <= outputWindow.OutputWindowPanes.Count; i++)
            {
                if (!outputWindow.OutputWindowPanes.Item(i).Name.Equals(
                    outputWindowName,
                    StringComparison.CurrentCultureIgnoreCase))
                    continue;

                outputWindowPane = outputWindow.OutputWindowPanes.Item(i);
                break;
            }

            if (outputWindowPane != null)
                return;

            outputWindowPane = outputWindow.OutputWindowPanes.Add(outputWindowName);
            if (outputWindowPane != null)
                _helper.OutputWindow = outputWindow;
        }

        void CreateProgressDialog()
        {
            IVsThreadedWaitDialog2 progressDialog = null;
            if (GetService(typeof(SVsThreadedWaitDialogFactory)) is IVsThreadedWaitDialogFactory dialogFactory)
                dialogFactory.CreateInstance(out progressDialog);

            if (progressDialog != null && progressDialog.StartWaitDialog(
                    Library.Constants.AppName + " Working...",
                    "Visual Studio is busy. Cancel ResolveUR by clicking Cancel button",
                    string.Empty,
                    null,
                    string.Empty,
                    0,
                    true,
                    true) == VSConstants.S_OK)
                Thread.Sleep(1000);

            _helper.ProgressDialog = progressDialog;

            var dialogCanceled = false;
            if (progressDialog != null)
                progressDialog.HasCanceled(out dialogCanceled);

            if (!dialogCanceled)
                return;

            _resolveur.Cancel();
            _helper.ShowMessageBox(Library.Constants.AppName + " Status", "Canceled");
        }

        void CreateUiShell()
        {
            _helper.UiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
        }

        #endregion

        #region Resolveur Events

        void Resolveur_HasBuildErrorsEvent(string projectName)
        {
            _helper.ShowMessageBox(
                "Resolve Unused References",
                "Project " + projectName +
                " already has compile errors. Please ensure it has no build errors and retry removing references.");
            _helper.EndWaitDialog();
        }

        void Resolveur_ProjectResolveCompleteEvent()
        {
            if (RemoveConfirmed())
                _resolveur.Clean();
        }

        void Helper_ResolveurCanceled(object sender, EventArgs e)
        {
            _resolveur.Cancel();
        }

        #endregion
    }
}