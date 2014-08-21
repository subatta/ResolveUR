using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ResolveUR.Library;
using ResolveURVisualStudioPackage.Properties;

namespace ResolveURVisualStudioPackage
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidResolveURVisualStudioPackagePkgString)]
    public sealed class ResolveURVisualStudioPackagePackage: Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public ResolveURVisualStudioPackagePackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }


        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        private Helper _helper;
        private IResolveUR _resolveur = null;

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                var menuCommandID = new CommandID(GuidList.guidResolveURVisualStudioPackageCmdSet, (int)PkgCmdIDList.cmdRemoveUnusedProjectReferences);
                var menuItem = new MenuCommand(ProjectMenuItemCallback, menuCommandID);
                mcs.AddCommand(menuItem);
                menuCommandID = new CommandID(GuidList.guidResolveURVisualStudioPackageCmdSet, (int)PkgCmdIDList.cmdRemoveUnusedSolutionReferences);
                menuItem = new MenuCommand(SolutionMenuItemCallback, menuCommandID);
                mcs.AddCommand(menuItem);
                _helper = new Helper();
            }
        }
        #endregion

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ProjectMenuItemCallback(object sender, EventArgs e)
        {
            handleCallBack(getProjectName);
        }
        private void SolutionMenuItemCallback(object sender, EventArgs e)
        {
            handleCallBack(getSolutionName);
        }

        private void handleCallBack(Func<string> activeFileNameGetter)
        {
            createOutputWindow();
            createProgressDialog();
            createUiShell();

            var builderPath = findMsBuildPath();
            if (string.IsNullOrWhiteSpace(builderPath))
            {
                _helper.ShowMessageBox("MsBuild Exe not found", "MsBuild Executable required to compile project was not found on this machine. Aborting...");
                return;
            }

            var filePath = activeFileNameGetter();
            if (string.IsNullOrEmpty(filePath))
            {
                resolveur_ProgressMessageEvent("Invalid file");
                return;
            }

            _resolveur = createResolver(filePath);
            if (_resolveur == null)
                resolveur_ProgressMessageEvent("Unrecognized project or solution type");
            else
            {
                _helper.ResolveurCancelled += helper_ResolveurCancelled;
                _resolveur.BuilderPath = builderPath;
                _resolveur.FilePath = filePath;
                _resolveur.HasBuildErrorsEvent += resolveur_HasBuildErrorsEvent;
                _resolveur.ProgressMessageEvent += resolveur_ProgressMessageEvent;
                _resolveur.ReferenceCountEvent += resolveur_ReferenceCountEvent;
                _resolveur.ItemGroupResolved += resolveur_ItemGroupResolved;
                _resolveur.Resolve();
            }

            _helper.EndWaitDialog();
        }

        private void helper_ResolveurCancelled(object sender, EventArgs e)
        {
            _resolveur.Cancel();
        }

        private string getSolutionName()
        {
            var solutionObject = (this.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE)) as EnvDTE80.DTE2).Solution;
            if (solutionObject == null) return string.Empty;

            var solution = solutionObject.Properties.Item(5).Value.ToString();
            if (!File.Exists(solution)) return string.Empty;
        
            return solution;
        }
        private string getProjectName()
        {
            var activeProjects = (Array)(this.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE)) as EnvDTE80.DTE2).ActiveSolutionProjects;
            if (activeProjects == null || activeProjects.Length == 0) return string.Empty;

            var project = (EnvDTE.Project)(activeProjects.GetValue(0));

            return project.FileName;
        }

        #region Resolveur Events

        private void resolveur_HasBuildErrorsEvent(string projectName)
        {
            _helper.ShowMessageBox
            (
                "Remove Unused References",
                "Project " + projectName + " already has compile errors. Please ensure it has no build errors and retry removing references."
            );
            _helper.EndWaitDialog();
        }

        private void resolveur_ProgressMessageEvent(string message)
        {
            if (message.Contains("Resolving"))
            {
                _helper.ItemGroupCount = 1;
                _helper.CurrentProject = message;
            }
            _helper.SetMessage(message);
        }

        private void resolveur_ItemGroupResolved(object sender, EventArgs e)
        {
            _helper.CurrentReferenceCountInItemGroup = 0;
            _helper.ItemGroupCount++;
        }

        private void resolveur_ReferenceCountEvent(int count)
        {
            _helper.TotalReferenceCount = count;
        }

        #endregion

        private static string findMsBuildPath()
        {
            if (File.Exists(Settings.Default.msbuildx6440)) return Settings.Default.msbuildx6440;
            if (File.Exists(Settings.Default.msbuildx8640)) return Settings.Default.msbuildx8640;
            if (File.Exists(Settings.Default.msbuildx6435)) return Settings.Default.msbuildx6435;
            if (File.Exists(Settings.Default.msbuildx8635)) return Settings.Default.msbuildx8635;
            if (File.Exists(Settings.Default.msbuildx6420)) return Settings.Default.msbuildx6420;
            if (File.Exists(Settings.Default.msbuildx8620)) return Settings.Default.msbuildx8620;

            return string.Empty;
        }

        #region Create memebers

        private void createOutputWindow()
        {
            var window = (this.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE)) as EnvDTE80.DTE2).Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
            var outputWindow = (OutputWindow)window.Object;
            OutputWindowPane outputWindowPane = null;

            const string OutputWindowName = "Output";
            for (uint i = 1; i <= outputWindow.OutputWindowPanes.Count; i++)
            {
                if (outputWindow.OutputWindowPanes.Item(i).Name.Equals(OutputWindowName, StringComparison.CurrentCultureIgnoreCase))
                {
                    outputWindowPane = outputWindow.OutputWindowPanes.Item(i);
                    break;
                }
            }

            if (outputWindowPane == null)
            {
                outputWindowPane = outputWindow.OutputWindowPanes.Add(OutputWindowName);
                _helper.OutputWindow = outputWindow;
            }
        }
        private void createProgressDialog()
        {
            var dialogFactory = GetService(typeof(SVsThreadedWaitDialogFactory)) as IVsThreadedWaitDialogFactory;
            IVsThreadedWaitDialog2 progressDialog = null;
            if (dialogFactory != null) dialogFactory.CreateInstance(out progressDialog);

            if (progressDialog != null && progressDialog.StartWaitDialog(
                    Constants.AppName + " Working...", "Visual Studio is busy. Cancel ResolveUR by clicking Cancel button",
                    string.Empty, null,
                    string.Empty,
                    0, true,
                    true) == Microsoft.VisualStudio.VSConstants.S_OK)
                System.Threading.Thread.Sleep(1000);

            _helper.ProgressDialog = progressDialog;

            bool dialogCancelled;
            progressDialog.HasCanceled(out dialogCancelled);
            if (dialogCancelled)
            {
                _resolveur.Cancel();
                _helper.ShowMessageBox(Constants.AppName + " Status", "Cancelled");
            }
        }
        private IResolveUR createResolver(string filePath)
        {
            if (filePath.EndsWith("proj"))
                return new RemoveUnusedProjectReferences();
            else if (filePath.EndsWith(".sln"))
                return new RemoveUnusedSolutionReferences();

            return null;
        }

        private void createUiShell()
        {
            _helper.UiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
        }

        #endregion
    }
}
