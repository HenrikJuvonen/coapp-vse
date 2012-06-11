using System;
using System.ComponentModel.Design;
using System.Reflection;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using CoApp.VisualStudio.Dialog;
using CoApp.VisualStudio.Options;
using CoApp.VisualStudio.VsCore;

namespace CoApp.VisualStudio.Tools
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", VsPackage.ProductVersion, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]

    [ProvideOptionPage(typeof(FeedOptionsPage), "CoApp Package Manager", "Package Feeds", 113, 114, true)]
    [ProvideOptionPage(typeof(GeneralOptionPage), "CoApp Package Manager", "General", 113, 115, true)]
    [ProvideBindingPath] // Definition dll needs to be on VS binding path
    [ProvideAutoLoad(UIContextGuids80.NoSolution)]
    
    [Guid(GuidList.guidVSPkgString)]
    public sealed class VsPackage : Package
    {
        public const string ProductVersion = "0.2.0.0";

        private uint _debuggingContextCookie, _solutionBuildingContextCookie;
        private DTE _dte;
        private IVsMonitorSelection _vsMonitorSelection;
        private ISolutionManager _solutionManager;
        private IPackageRestoreManager _packageRestoreManager;

        public VsPackage()
        {
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            CoAppWrapper.Initialize();

            // get the UI context cookie for the debugging mode
            _vsMonitorSelection = (IVsMonitorSelection)GetService(typeof(IVsMonitorSelection));
            // get debugging context cookie
            Guid debuggingContextGuid = VSConstants.UICONTEXT_Debugging;
            _vsMonitorSelection.GetCmdUIContextCookie(ref debuggingContextGuid, out _debuggingContextCookie);

            // get the solution building cookie
            Guid solutionBuildingContextGuid = VSConstants.UICONTEXT_SolutionBuilding;
            _vsMonitorSelection.GetCmdUIContextCookie(ref solutionBuildingContextGuid, out _solutionBuildingContextCookie);

            _dte = ServiceLocator.GetInstance<DTE>();
            _packageRestoreManager = ServiceLocator.GetInstance<IPackageRestoreManager>();
            _solutionManager = ServiceLocator.GetInstance<ISolutionManager>();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            AddMenuCommandHandlers();
        }

        private void AddMenuCommandHandlers()
        {
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // menu command for opening Manage CoApp packages dialog
                CommandID managePackageDialogCommandID = new CommandID(GuidList.guidVSDialogCmdSet, PkgCmdIDList.cmdidAddPackageDialog);
                OleMenuCommand managePackageDialogCommand = new OleMenuCommand(ShowManageLibraryPackageDialog, null, BeforeQueryStatusForAddPackageDialog, managePackageDialogCommandID);
                mcs.AddCommand(managePackageDialogCommand);

                // menu command for opening Package feed settings options page
                CommandID settingsCommandID = new CommandID(GuidList.guidVSConsoleCmdSet, PkgCmdIDList.cmdidSourceSettings);
                OleMenuCommand settingsMenuCommand = new OleMenuCommand(ShowPackageSourcesOptionPage, settingsCommandID);
                mcs.AddCommand(settingsMenuCommand);

                // menu command for opening General options page
                CommandID generalSettingsCommandID = new CommandID(GuidList.guidVSToolsGroupCmdSet, PkgCmdIDList.cmdIdGeneralSettings);
                OleMenuCommand generalSettingsCommand = new OleMenuCommand(ShowGeneralSettingsOptionPage, generalSettingsCommandID);
                mcs.AddCommand(generalSettingsCommand);

                // menu command for Package Restore command
                CommandID restorePackagesCommandID = new CommandID(GuidList.guidVSPackagesRestoreCmdSet, PkgCmdIDList.cmdidRestorePackages);
                var restorePackagesCommand = new OleMenuCommand(EnablePackagesRestore, null, QueryStatusEnablePackagesRestore, restorePackagesCommandID);
                mcs.AddCommand(restorePackagesCommand);
            }
        }

        private void EnablePackagesRestore(object sender, EventArgs args)
        {
            _packageRestoreManager.BeginRestore(fromActivation: true);
        }

        private void QueryStatusEnablePackagesRestore(object sender, EventArgs args)
        {
            OleMenuCommand command = (OleMenuCommand)sender;
            command.Visible = _solutionManager.IsSolutionOpen && _packageRestoreManager.CheckForMissingPackages();
            command.Enabled = true;
        }

        private void ShowManageLibraryPackageDialog(object sender, EventArgs e)
        {
            ShowManageLibraryPackageDialog();
        }

        private static void ShowManageLibraryPackageDialog()
        {
            DialogWindow window = new PackageManagerWindow();
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
        
        private void BeforeQueryStatusForAddPackageDialog(object sender, EventArgs args)
        {
            OleMenuCommand command = (OleMenuCommand)sender;
            command.Visible = _solutionManager.IsSolutionOpen && !IsIDEInDebuggingOrBuildingContext();
            command.Enabled = true;
            if (command.Visible)
            {
                command.Text = Resources.ManagePackageLabel;
            }
        }

        private void BeforeQueryStatusForAddPackageForSolutionDialog(object sender, EventArgs args)
        {
            OleMenuCommand command = (OleMenuCommand)sender;
            command.Visible = _solutionManager.IsSolutionOpen && !IsIDEInDebuggingOrBuildingContext();
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
            ShowOptionPageSafe(typeof(FeedOptionsPage));
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
    }
}
