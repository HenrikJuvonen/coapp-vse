using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CoApp.VSE.VSPackage
{
    using Core;

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution)]

    [Guid(GuidList.guidVsPkgString)]
    public sealed class VsPackage : Package
    {
        private uint _debuggingContextCookie, _solutionBuildingContextCookie, _solutionEventsCookie;
        private IVsMonitorSelection _vsMonitorSelection;

        private DTEEvents DTEEvents;
        
        protected override void Initialize()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CoAppApplication.ResolveAssembly);

            base.Initialize();
            
            // get the UI context cookie for the debugging mode
            _vsMonitorSelection = (IVsMonitorSelection)GetService(typeof(IVsMonitorSelection));

            // get debugging context cookie
            var debuggingContextGuid = VSConstants.UICONTEXT_Debugging;
            _vsMonitorSelection.GetCmdUIContextCookie(ref debuggingContextGuid, out _debuggingContextCookie);

            // get the solution building cookie
            var solutionBuildingContextGuid = VSConstants.UICONTEXT_SolutionBuilding;
            _vsMonitorSelection.GetCmdUIContextCookie(ref solutionBuildingContextGuid, out _solutionBuildingContextCookie);

            // Add our command handlers for menu (commands must exist in the .vsct file)
            AddMenuCommandHandlers();

            Module.DTE = (DTE)GetService(typeof(DTE));

            var solution = (IVsSolution) GetService(typeof (SVsSolution));

            var solutionEvents = new SolutionEvents();
            solution.AdviseSolutionEvents(solutionEvents, out _solutionEventsCookie);
            
            Module.SolutionEvents = Module.DTE.Events.SolutionEvents;
            Module.BuildEvents = Module.DTE.Events.BuildEvents;
            Module.SolutionEvents.BeforeClosing += Module.HideVisualStudioControl;
            Module.BuildEvents.OnBuildBegin += (scope, action) => Module.HideVisualStudioControl();
            solutionEvents.AfterOpenProject += (sender, args) => Module.RestoreMissingPackages();
            Module.SolutionEvents.Opened += Module.UpdateInSolutionStatus;

            Module.Initialize();
            Module.OnStartup(null, null);

            MessageFilter.Register();

            DTEEvents = Module.DTE.Events.DTEEvents;

            DTEEvents.OnBeginShutdown += () =>
            {
                MessageFilter.Revoke();
                Module.OnExit(null, null);
                solution.UnadviseSolutionEvents(_solutionEventsCookie);
            };
        }

        private void AddMenuCommandHandlers()
        {
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // menu command for showing Main Control
                var showMainCommand = new OleMenuCommand(ExecuteShowMain, new CommandID(GuidList.guidVsCmdSet, PkgCmdIDList.cmdidShowMain));
                mcs.AddCommand(showMainCommand);

                // menu command for showing Options Control
                var showOptionsCommand = new OleMenuCommand(ExecuteShowOptions, new CommandID(GuidList.guidVsCmdSet, PkgCmdIDList.cmdidShowOptions));
                mcs.AddCommand(showOptionsCommand);

                // menu command for restoring packages
                var restorePackagesCommand = new OleMenuCommand(ExecuteRestorePackages, null, QueryStatusEnable, new CommandID(GuidList.guidVsCmdSet, PkgCmdIDList.cmdidRestorePackages));
                mcs.AddCommand(restorePackagesCommand);
            }
        }

        private void QueryStatusEnable(object sender, EventArgs args)
        {
            var command = (OleMenuCommand)sender;
            command.Visible = !IsIDEInDebuggingOrBuildingContext() && Module.IsSolutionOpen;
            command.Enabled = true;
        }

        private void ExecuteShowMain(object sender, EventArgs e)
        {
            Module.ShowMainControl();
            Module.ShowMainWindow();
        }

        private void ExecuteShowOptions(object sender, EventArgs e)
        {
            Module.ShowOptionsControl();
            Module.ShowMainWindow();
        }

        private void ExecuteRestorePackages(object sender, EventArgs e)
        {
            Module.RestoreMissingPackages(true);
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
    }
}
