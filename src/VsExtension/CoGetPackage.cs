using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using CoGet.Options;
using CoGet.VisualStudio;
using CoGet.VisualStudio.Resources;

using ManagePackageDialog = CoGet.Dialog.PackageManagerWindow;

namespace CoGet.Tools
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", CoGetPackage.ProductVersion, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    
    [ProvideOptionPage(typeof(PackageSourceOptionsPage), "CoApp Package Manager", "Package Feeds", 113, 114, true)]
    [ProvideOptionPage(typeof(GeneralOptionPage), "CoApp Package Manager", "General", 113, 115, true)]
    [ProvideBindingPath] // Definition dll needs to be on VS binding path
    [ProvideAutoLoad(GuidList.guidAutoLoadCoGetString)]
    
    [Guid(GuidList.guidCoGetPkgString)]
    public sealed class CoGetPackage : Package
    {
        public const string ProductVersion = "1.0.0.0";

        private uint _debuggingContextCookie, _solutionBuildingContextCookie;
        private DTE _dte;
        private IVsMonitorSelection _vsMonitorSelection;
        private ISolutionManager _solutionManager;

        public CoGetPackage()
        {
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // get the UI context cookie for the debugging mode
            _vsMonitorSelection = (IVsMonitorSelection)GetService(typeof(IVsMonitorSelection));
            // get debugging context cookie
            Guid debuggingContextGuid = VSConstants.UICONTEXT_Debugging;
            _vsMonitorSelection.GetCmdUIContextCookie(ref debuggingContextGuid, out _debuggingContextCookie);

            // get the solution building cookie
            Guid solutionBuildingContextGuid = VSConstants.UICONTEXT_SolutionBuilding;
            _vsMonitorSelection.GetCmdUIContextCookie(ref solutionBuildingContextGuid, out _solutionBuildingContextCookie);

            _dte = ServiceLocator.GetInstance<DTE>();
            Debug.Assert(_dte != null);
            //_packageRestoreManager = ServiceLocator.GetInstance<IPackageRestoreManager>();
            //Debug.Assert(_packageRestoreManager != null);
            _solutionManager = ServiceLocator.GetInstance<ISolutionManager>();
            Debug.Assert(_solutionManager != null);

            // Add our command handlers for menu (commands must exist in the .vsct file)
            AddMenuCommandHandlers();

            // when CoGet loads, if the current solution has package 
            // restore mode enabled, we make sure every thing is set up correctly.
            // For example, projects which were added outside of VS need to have
            // the <Import> element added.
            /*if (_packageRestoreManager.IsCurrentSolutionEnabledForRestore)
            {
                _packageRestoreManager.EnableCurrentSolutionForRestore(fromActivation: false);
            }*/
        }

        private void AddMenuCommandHandlers()
        {
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // menu command for opening Manage CoGet packages dialog
                CommandID managePackageDialogCommandID = new CommandID(GuidList.guidCoGetDialogCmdSet, PkgCmdIDList.cmdidAddPackageDialog);
                OleMenuCommand managePackageDialogCommand = new OleMenuCommand(ShowManageLibraryPackageDialog, null, BeforeQueryStatusForAddPackageDialog, managePackageDialogCommandID);
                mcs.AddCommand(managePackageDialogCommand);

                // menu command for opening "Manage CoGet packages for solution" dialog
                CommandID managePackageForSolutionDialogCommandID = new CommandID(GuidList.guidCoGetDialogCmdSet, PkgCmdIDList.cmdidAddPackageDialogForSolution);
                OleMenuCommand managePackageForSolutionDialogCommand = new OleMenuCommand(ShowManageLibraryPackageForSolutionDialog, null, BeforeQueryStatusForAddPackageForSolutionDialog, managePackageForSolutionDialogCommandID);
                mcs.AddCommand(managePackageForSolutionDialogCommand);

                // menu command for opening Package Source settings options page
                CommandID settingsCommandID = new CommandID(GuidList.guidCoGetConsoleCmdSet, PkgCmdIDList.cmdidSourceSettings);
                OleMenuCommand settingsMenuCommand = new OleMenuCommand(ShowPackageSourcesOptionPage, settingsCommandID);
                mcs.AddCommand(settingsMenuCommand);

                // menu command for opening General options page
                CommandID generalSettingsCommandID = new CommandID(GuidList.guidCoGetToolsGroupCmdSet, PkgCmdIDList.cmdIdGeneralSettings);
                OleMenuCommand generalSettingsCommand = new OleMenuCommand(ShowGeneralSettingsOptionPage, generalSettingsCommandID);
                mcs.AddCommand(generalSettingsCommand);
            }
        }

        private void ShowManageLibraryPackageDialog(object sender, EventArgs e)
        {
            if (_vsMonitorSelection.GetIsSolutionNodeSelected())
            {
                ShowManageLibraryPackageDialog(null);
            }
            else
            {
                Project project = _vsMonitorSelection.GetActiveProject();
                if (project != null && !project.IsUnloaded() && project.IsSupported())
                {
                    ShowManageLibraryPackageDialog(project);
                }
                else
                {
                    // show error message when no supported project is selected.
                    string projectName = project != null ? project.Name : String.Empty;

                    string errorMessage;
                    if (String.IsNullOrEmpty(projectName))
                    {
                        errorMessage = Resources.NoProjectSelected;
                    }
                    else
                    {
                        errorMessage = String.Format(CultureInfo.CurrentCulture, VsResources.DTE_ProjectUnsupported, projectName);
                    }

                    MessageHelper.ShowWarningMessage(errorMessage, Resources.ErrorDialogBoxTitle);
                }
            }
        }

        private void ShowManageLibraryPackageForSolutionDialog(object sender, EventArgs e)
        {
            ShowManageLibraryPackageDialog(null);
        }

        private static void ShowManageLibraryPackageDialog(Project project)
        {
            DialogWindow window = GetPackageManagerWindow(project);
            try
            {
                window.ShowModal();
            }
            catch (TargetInvocationException exception)
            {
                MessageHelper.ShowErrorMessage(exception, Resources.ErrorDialogBoxTitle);
                ExceptionHelper.WriteToActivityLog(exception);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static DialogWindow GetPackageManagerWindow(Project project)
        {
            return new ManagePackageDialog(project);
        }

        private void BeforeQueryStatusForAddPackageDialog(object sender, EventArgs args)
        {
            bool isSolutionSelected = _vsMonitorSelection.GetIsSolutionNodeSelected();

            OleMenuCommand command = (OleMenuCommand)sender;
            command.Visible = _solutionManager.IsSolutionOpen && !IsIDEInDebuggingOrBuildingContext() && (isSolutionSelected || HasActiveLoadedSupportedProject);
            // disable the dialog menu if the console is busy executing a command;
            command.Enabled = true;
            if (command.Visible)
            {
                command.Text = isSolutionSelected ? Resources.ManagePackageForSolutionLabel : Resources.ManagePackageLabel;
            }
        }

        private void BeforeQueryStatusForAddPackageForSolutionDialog(object sender, EventArgs args)
        {
            OleMenuCommand command = (OleMenuCommand)sender;
            command.Visible = _solutionManager.IsSolutionOpen && !IsIDEInDebuggingOrBuildingContext();
            // disable the dialog menu if the console is busy executing a command;
            command.Enabled = true;
        }

        private bool IsIDEInDebuggingOrBuildingContext()
        {
            int pfActive;
            int result = _vsMonitorSelection.IsCmdUIContextActive(_debuggingContextCookie, out pfActive);
            if (result == VSConstants.S_OK && pfActive > 0)
            {
                return true;
            }

            result = _vsMonitorSelection.IsCmdUIContextActive(_solutionBuildingContextCookie, out pfActive);
            if (result == VSConstants.S_OK && pfActive > 0)
            {
                return true;
            }

            return false;
        }

        private void ShowPackageSourcesOptionPage(object sender, EventArgs args)
        {
            ShowOptionPageSafe(typeof(PackageSourceOptionsPage));
        }

        private void ShowGeneralSettingsOptionPage(object sender, EventArgs args)
        {
            ShowOptionPageSafe(typeof(GeneralOptionPage));
        }

        private void ShowOptionPageSafe(Type optionPageType)
        {
            try
            {
                ShowOptionPage(optionPageType);
            }
            catch (Exception exception)
            {
                MessageHelper.ShowErrorMessage(exception, Resources.ErrorDialogBoxTitle);
                ExceptionHelper.WriteToActivityLog(exception);
            }
        }

        /// <summary>
        /// Gets whether the current IDE has an active, supported and non-unloaded project, which is a precondition for
        /// showing the Add Library Package Reference dialog
        /// </summary>
        private bool HasActiveLoadedSupportedProject
        {
            get
            {
                Project project = _vsMonitorSelection.GetActiveProject();
                return project != null && !project.IsUnloaded() && project.IsSupported();
            }
        }
    }
}
