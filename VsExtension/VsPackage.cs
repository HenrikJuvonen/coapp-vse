using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Windows.Threading;
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
        private uint _solutionExistsAndNotBuildingAndNotDebuggingContextCookie, _solutionExistsAndFullyLoadedContextCookie;
        private IVsMonitorSelection _vsMonitorSelection;

        private DTEEvents DTEEvents;
        private Dictionary<Project, string> Projects;

        private bool _isSolutionOpen;

        protected override void Initialize()
        {
            base.Initialize();

            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CoAppApplication.ResolveAssembly);
                        
            _vsMonitorSelection = (IVsMonitorSelection)GetService(typeof(IVsMonitorSelection));
            Module.DTE = (DTE)GetService(typeof(DTE));

            // get the solution exists/not building/not debugging cookie
            var solutionExistsAndNotBuildingAndNotDebuggingContextGuid = VSConstants.UICONTEXT.SolutionExistsAndNotBuildingAndNotDebugging_guid;
            _vsMonitorSelection.GetCmdUIContextCookie(ref solutionExistsAndNotBuildingAndNotDebuggingContextGuid, out _solutionExistsAndNotBuildingAndNotDebuggingContextCookie);
            
            // get the solution exists and fully loaded cookie
            var solutionExistsAndFullyLoadedContextGuid = VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_guid;
            _vsMonitorSelection.GetCmdUIContextCookie(ref solutionExistsAndFullyLoadedContextGuid, out _solutionExistsAndFullyLoadedContextCookie);
                                    
            // Add our command handlers for menu (commands must exist in the .vsct file)
            AddMenuCommandHandlers();
            
            Module.Initialize();
            Module.OnStartup(null, null);

            DTEEvents = Module.DTE.Events.DTEEvents;
            DTEEvents.OnBeginShutdown += () => Module.OnExit(null, null);

            var timer = new DispatcherTimer();
            timer.Tick += (o, a) => TrackSolution();
            timer.Interval += new TimeSpan(0, 0, 0, 1);
            timer.Start();
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
            command.Visible = IsIDENotDebuggingAndNotBuilding();
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

        private bool IsSolutionFullyLoaded()
        {
            int pfActive;
            int result = _vsMonitorSelection.IsCmdUIContextActive(_solutionExistsAndFullyLoadedContextCookie, out pfActive);
            return result == VSConstants.S_OK && pfActive > 0;
        }

        private bool IsIDENotDebuggingAndNotBuilding()
        {
            int pfActive;
            int result = _vsMonitorSelection.IsCmdUIContextActive(_solutionExistsAndNotBuildingAndNotDebuggingContextCookie, out pfActive);
            return result == VSConstants.S_OK && pfActive > 0;
        }
        
        /// <summary>
        /// Tracks changes in solution: is solution opened/closed, is project added/removed, is project name changed.
        /// </summary>
        private void TrackSolution()
        {
            if (IsSolutionFullyLoaded())
            {
                if (!_isSolutionOpen)
                {
                    _isSolutionOpen = true;
                    Module.InvokeSolutionOpened();
                }
            }
            else
            {
                if (_isSolutionOpen)
                {
                    _isSolutionOpen = false;
                    Module.InvokeSolutionClosed();
                    Projects = null;
                }
            }

            if (_isSolutionOpen)
            {
                if (Projects == null)
                {
                    Projects = Module.DTE.Solution.Projects.OfType<Project>().ToDictionary(n => n, n => n.Name);
                }
                else
                {
                    var projects = Module.DTE.Solution.Projects.OfType<Project>().ToDictionary(n => n, n => n.Name);

                    bool differs = false;

                    foreach (var a in projects)
                    {
                        foreach (var b in Projects)
                        {
                            if (a.Key == b.Key && a.Value != b.Value)
                                differs = true;
                        }
                    }

                    if (projects.Count != Projects.Count)
                        differs = true;

                    if (differs)
                    {
                        Projects = projects;
                        Module.InvokeSolutionChanged();
                    }
                }
            }
        }
    }
}
