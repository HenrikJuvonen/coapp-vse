extern alias dialog;
extern alias dialog10;

using System;
using System.ComponentModel.Design;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using CoApp.VisualStudio.Options;
using CoApp.VisualStudio.VsCore;

using PackageManagerDialog = dialog::CoApp.VisualStudio.Dialog.PackageManagerWindow;
using VS10PackageManagerDialog = dialog10::CoApp.VisualStudio.Dialog.PackageManagerWindow;

namespace CoApp.VisualStudio.Tools
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(FeedOptionsPage), "CoApp Package Manager", "Package Feeds", 113, 114, true)]
    [ProvideOptionPage(typeof(GeneralOptionsPage), "CoApp Package Manager", "General", 113, 115, true)]
    [ProvideBindingPath] // Definition dll needs to be on VS binding path
    [ProvideAutoLoad(UIContextGuids80.NoSolution)]
    
    [Guid(GuidList.guidVSPkgString)]
    public sealed class VsPackage : Package
    {
        private uint _debuggingContextCookie, _solutionBuildingContextCookie;
        private IVsMonitorSelection _vsMonitorSelection;
        private ISolutionManager _solutionManager;
        private IPackageRestoreManager _packageRestoreManager;
        private bool _isUpdateNotifierShown;

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            
            CoAppWrapper.Initialize();

            if (CoAppWrapper.Settings["#update"].IntValue == 0)
                CoAppWrapper.UpdatesAvailable += OnUpdatesAvailable;

            // get the UI context cookie for the debugging mode
            _vsMonitorSelection = (IVsMonitorSelection)GetService(typeof(IVsMonitorSelection));
            // get debugging context cookie
            var debuggingContextGuid = VSConstants.UICONTEXT_Debugging;
            _vsMonitorSelection.GetCmdUIContextCookie(ref debuggingContextGuid, out _debuggingContextCookie);

            // get the solution building cookie
            var solutionBuildingContextGuid = VSConstants.UICONTEXT_SolutionBuilding;
            _vsMonitorSelection.GetCmdUIContextCookie(ref solutionBuildingContextGuid, out _solutionBuildingContextCookie);

            _solutionManager = ServiceLocator.GetInstance<ISolutionManager>();
            _packageRestoreManager = ServiceLocator.GetInstance<IPackageRestoreManager>();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            AddMenuCommandHandlers();

            _solutionManager.SolutionOpened += (sender, args) =>
            {
                // Delayed check for updates
                var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                dispatcherTimer.Tick += (o, a) => CoAppWrapper.GetPackages("updatable");
                dispatcherTimer.Interval = new TimeSpan(0, 0, 15);
                dispatcherTimer.Start();
            };
        }

        private void OnUpdatesAvailable(object sender, EventArgs e)
        {
            if (_isUpdateNotifierShown)
                return;

            _isUpdateNotifierShown = true;

            var notifier = new NotifyIcon
            {
                Icon = Resources.CoApp,
                BalloonTipIcon = ToolTipIcon.Info,
                BalloonTipTitle = Resources.NotifierTitle,
                BalloonTipText = Resources.NotifierText
            };

            notifier.BalloonTipClicked += ShowManageLibraryPackageDialogUpdates;
            notifier.BalloonTipClicked += (o, args) => { notifier.Visible = false; };
            notifier.BalloonTipClosed += (o, args) => { notifier.Visible = false; };
            notifier.Visible = true;
            notifier.ShowBalloonTip(10000);
        }

        private void AddMenuCommandHandlers()
        {
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // menu command for opening Manage CoApp packages dialog
                var managePackageDialogCommandID = new CommandID(GuidList.guidVSDialogCmdSet, PkgCmdIDList.cmdidAddPackageDialog);
                var managePackageDialogCommand = new OleMenuCommand(ShowManageLibraryPackageDialog, null, QueryStatusEnable, managePackageDialogCommandID);
                mcs.AddCommand(managePackageDialogCommand);

                // menu command for opening Package feed settings options page
                var settingsCommandID = new CommandID(GuidList.guidVSConsoleCmdSet, PkgCmdIDList.cmdidSourceSettings);
                var settingsMenuCommand = new OleMenuCommand(ShowPackageSourcesOptionPage, settingsCommandID);
                mcs.AddCommand(settingsMenuCommand);

                // menu command for opening General options page
                var generalSettingsCommandID = new CommandID(GuidList.guidVSToolsGroupCmdSet, PkgCmdIDList.cmdIdGeneralSettings);
                var generalSettingsCommand = new OleMenuCommand(ShowGeneralSettingsOptionPage, generalSettingsCommandID);
                mcs.AddCommand(generalSettingsCommand);

                // menu command for Package Restore command
                var restorePackagesCommandID = new CommandID(GuidList.guidVSPackagesRestoreCmdSet, PkgCmdIDList.cmdidRestorePackages);
                var restorePackagesCommand = new OleMenuCommand(EnablePackagesRestore, null, QueryStatusEnable, restorePackagesCommandID);
                mcs.AddCommand(restorePackagesCommand);
            }
        }

        private void EnablePackagesRestore(object sender, EventArgs args)
        {
            _packageRestoreManager.BeginRestore(fromActivation: true);
        }

        private void QueryStatusEnable(object sender, EventArgs args)
        {
            var command = (OleMenuCommand)sender;
            command.Visible = _solutionManager.IsSolutionOpen && !IsIDEInDebuggingOrBuildingContext();
            command.Enabled = true;
        }

        private void ShowManageLibraryPackageDialog(object sender, EventArgs e)
        {
            var window = VsVersionHelper.IsVisualStudio2010 ?
                GetVS10PackageManagerWindow():
                GetPackageManagerWindow();

            // do not notify after the dialog is opened
            _isUpdateNotifierShown = true;

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

        private void ShowManageLibraryPackageDialogUpdates(object sender, EventArgs e)
        {
            _packageRestoreManager.IsSuspended = true;

            var window = VsVersionHelper.IsVisualStudio2010 ?
                GetVS10PackageManagerWindow(true) :
                GetPackageManagerWindow(true);

            window.Closed += (o, args) => _packageRestoreManager.IsSuspended = false;

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
        private static DialogWindow GetVS10PackageManagerWindow(bool updatesOnly = false)
        {
            return new VS10PackageManagerDialog(updatesOnly);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static DialogWindow GetPackageManagerWindow(bool updatesOnly = false)
        {
            return new PackageManagerDialog(updatesOnly);
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
            ShowOptionPageSafe(typeof(GeneralOptionsPage));
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
