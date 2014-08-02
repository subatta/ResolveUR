using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ResolveUR;
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
            var builderPath = findMsBuildPath();
            if (string.IsNullOrWhiteSpace(builderPath))
            {
                showNoMsBuildFoundMessage();
                return;
            }

            var activeProjects = (Array) (this.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE)) as EnvDTE80.DTE2).ActiveSolutionProjects;
            if (activeProjects.Length <= 0)
                return;

            var project = (EnvDTE.Project)(activeProjects.GetValue(0));
            var resolveur = new RemoveUnusedProjectReferences
            {
                BuilderPath = builderPath,
                FilePath = project.FullName
            };
            resolveur.HasBuildErrorsEvent += resolveur_HasBuildErrorsEvent;
            resolveur.Resolve();
        }
        private void SolutionMenuItemCallback(object sender, EventArgs e)
        {
            var builderPath = findMsBuildPath();
            if (string.IsNullOrWhiteSpace(builderPath))
            {
                showNoMsBuildFoundMessage();
                return;
            }

            var solutionObject = (this.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE)) as EnvDTE80.DTE2).Solution;
            if (solutionObject == null)
                return;

            var solution = solutionObject.Properties.Item(5).Value.ToString();
            if (!File.Exists(solution))
                return;
            var resolveur = new RemoveUnusedSolutionReferences
            {
                BuilderPath = builderPath,
                FilePath = solution
            };
            resolveur.HasBuildErrorsEvent += resolveur_HasBuildErrorsEvent;
            resolveur.Resolve();
        }

        private void resolveur_HasBuildErrorsEvent(string projectName)
        {
            showMessageBox
            (
                "Remove Unused References",
                "Project " + projectName + " already has compile errors. Please ensure it has no build errors and retry removing references."
            );
        }

        private void showNoMsBuildFoundMessage()
        {
            showMessageBox("MsBuild Exe not found", "MsBuild Executable required to compile project was not found on this machine. Aborting...");        
        }

        private static string findMsBuildPath()
        {
            if (File.Exists(Settings.Default.msbuildx6440))
                return Settings.Default.msbuildx6440;
            if (File.Exists(Settings.Default.msbuildx8640))
                return Settings.Default.msbuildx8640;
            if (File.Exists(Settings.Default.msbuildx6435))
                return Settings.Default.msbuildx6435;
            if (File.Exists(Settings.Default.msbuildx8635))
                return Settings.Default.msbuildx8635;
            if (File.Exists(Settings.Default.msbuildx6420))
                return Settings.Default.msbuildx6420;
            if (File.Exists(Settings.Default.msbuildx8620))
                return Settings.Default.msbuildx8620;

            return string.Empty;
        }

        private void showMessageBox(string title, string message)
        {
            var uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            var clsid = Guid.Empty;
            int result;
            uiShell.ShowMessageBox(
                    0,
                    ref clsid,
                    title,
                    string.Format(CultureInfo.CurrentCulture,
                    message,
                    this.ToString()),
                    string.Empty,
                    0,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                    OLEMSGICON.OLEMSGICON_INFO,
                    0,
                    out result);
        }
    }
}
