using CoApp.Packaging.Client;
using EnvDTE;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;

namespace CoApp.VSE.Core
{
    using Extensions;
    using Packaging;
    using System.Windows;

    public static class Module
    {
        private static bool _isLoaded;

        public static DTE DTE;

        public static event Action SolutionChanged = delegate { };
        public static event Action SolutionOpened = delegate { };
        public static event Action SolutionClosed = delegate { };

        public static NotifyIcon TrayIcon;
        public static PackageManager PackageManager;
        public static MainWindow MainWindow;
        public static bool IsShutdownForced { get; private set; }

        public static bool IsDTELoaded { get { return DTE != null; } }
        public static bool IsSolutionOpen { get; private set; }
        public static bool IsProjectsOpen { get; private set; }
        
        private static bool _isApplying;
        private static bool _isRestoring;

        public static bool IsApplying
        {
            get { return _isApplying; }
            set
            {
                _isApplying = value;
                foreach (MenuItem menuItem in TrayIcon.ContextMenu.MenuItems)
                {
                    menuItem.Enabled = !_isApplying;
                }
            }
        }

        public static void ReloadMainControl()
        {
            MainWindow.MainControl.Reload();
        }

        public static void ShowMainWindow()
        {
            if (!_isLoaded)
            {
                _isLoaded = true;

                if (IsDTELoaded)
                {
                    MainWindow.Owner = (Window)HwndSource.FromHwnd(new IntPtr(DTE.MainWindow.HWnd)).RootVisual;
                }

                ReloadMainControl();
                
                MainWindow.ShowDialog();
            }
            else
            {
                MainWindow.WindowState = WindowState.Normal;

                if (!MainWindow.IsVisible)
                    MainWindow.ShowDialog();
            }
        }
                
        public static void ShowInformationControl()
        {
            MainWindow.GoTo("Info");
            MainWindow.InfoControl.DataContext = PackageManager.PackagesViewModel.SelectedPackage;
        }

        public static void ShowMainControl()
        {
            MainWindow.GoTo("Main");
            _isRestoring = false;
        }

        public static void ShowOptionsControl()
        {
            MainWindow.GoTo("Options");
        }

        public static void ShowVisualStudioControl()
        {
            MainWindow.GoTo("VisualStudio");
            MainWindow.VisualStudioControl.Initialize();
        }

        public static void HideVisualStudioControl()
        {
            PackageManager.ClearVisualStudioMarks();
            MainWindow.MainControl.ApplyButton.IsEnabled = PackageManager.IsAnyMarked;
            ShowMainControl();
        }

        public static void ShowSummaryControl()
        {
            MainWindow.GoTo("Summary");
            MainWindow.SummaryControl.Initialize();
        }

        public static void ShowProgressControl()
        {
            MainWindow.GoTo("Progress");
            MainWindow.ProgressControl.Initialize();
        }

        public static void Initialize()
        {
            PackageManager = new PackageManager();

            PackageManager.UpdatesAvailable += OnUpdatesAvailable;

            TrayIcon = new NotifyIcon
            {
                Icon = Resources.CoApp
            };

            TrayIcon.BalloonTipClicked += OnTrayIconBalloonTipClosed;
            TrayIcon.BalloonTipClosed += OnTrayIconBalloonTipClosed;
            TrayIcon.DoubleClick += OnTrayIconDoubleClicked;

            TrayIcon.ContextMenu = new ContextMenu();
            TrayIcon.ContextMenu.MenuItems.Add("Package Manager...", (sender, args) => { ShowMainControl(); ShowMainWindow(); });
            TrayIcon.ContextMenu.MenuItems.Add("Options...", (sender, args) => { ShowOptionsControl(); ShowMainWindow(); });

            if (!IsDTELoaded)
                TrayIcon.ContextMenu.MenuItems.Add("Exit", (sender, args) => { IsShutdownForced = true; MainWindow.Close(); });

            // Check for updates once in an hour
            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += (sender, args) =>
            {
                if (PackageManager.Settings["#update"].IntValue == 2)
                    return;

                PackageManager.SetAllFeedsStale();
                PackageManager.GetPackages("updatable");
            };
            dispatcherTimer.Interval = new TimeSpan(0, 1, 0, 0);
            dispatcherTimer.Start();
        }

        private static void OnTrayIconDoubleClicked(object sender, EventArgs e)
        {
            if (MainWindow.IsVisible)
            {
                MainWindow.WindowState = WindowState.Normal;

                if (PackageManager.Settings["#closeToTray"].BoolValue)
                {
                    MainWindow.Close();
                }
                else
                {
                    MainWindow.Hide();
                }
            }
            else
            {
                if (!PackageManager.Settings["#showTrayIcon"].BoolValue)
                {
                    TrayIcon.Visible = false;
                }
                ShowMainWindow();
            }
        }

        private static void OnTrayIconBalloonTipClosed(object sender, EventArgs e)
        {
            if (!PackageManager.Settings["#showTrayIcon"].BoolValue)
            {
                TrayIcon.Visible = false;
            }

            TrayIcon.BalloonTipClicked -= OnTrayIconBalloonTipUpdatesClosed;
            TrayIcon.BalloonTipClicked -= OnTrayIconBalloonTipRestoreClosed;
        }

        private static void OnTrayIconBalloonTipUpdatesClosed(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (PackageManager.Settings["#update"].IntValue == 1)
                {
                    MainWindow.MainControl.ExecuteMarkUpdates(null, null);
                    ShowSummaryControl();
                }

                ShowMainWindow();
            }));
        }

        private static void OnTrayIconBalloonTipRestoreClosed(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (PackageManager.Settings["#restore"].IntValue == 1)
                {
                    var packages = new List<Package>();
                    foreach (var package in PackageManager.GetMissingPackages())
                    {
                        packages.AddRange(PackageManager.IdentifyDependencies(package).Where(n => !n.IsInstalled));
                        PackageManager.AddMark(package, Mark.DirectInstall);
                    }
                    PackageManager.AddMarks(packages, Mark.IndirectInstall);
                    _isRestoring = true;
                    ShowSummaryControl();
                }

                ShowMainWindow();
            }));
        }

        public static void OnUpdatesAvailable(object sender, UpdatesAvailableEventArgs e)
        {
            if (PackageManager.Settings["#update"].IntValue == 2)
                return;

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                TrayIcon.Visible = true;

                string tooltipText;

                if (PackageManager.Settings["#update"].IntValue == 0)
                {
                    tooltipText = e.Count == 1
                        ? Resources.Update_Installing
                        : string.Format(Resources.Update_InstallingMany, e.Count);


                    MainWindow.MainControl.ExecuteMarkUpdates(null, null);
                    ShowProgressControl();
                }
                else
                {
                    tooltipText = e.Count == 1
                        ? Resources.Update_Available
                        : string.Format(Resources.Update_AvailableMany, e.Count);
                }

                TrayIcon.BalloonTipClicked += OnTrayIconBalloonTipUpdatesClosed;
                ShowBalloonTip(tooltipText);
            }));
        }

        public static void OnStartup(object sender, StartupEventArgs e)
        {
            MainWindow = new MainWindow();

            if (PackageManager.Settings["#theme"].StringValue == "Dark")
                Utility.ThemeManager.ChangeTheme(MainWindow, MahApps.Metro.Theme.Dark);
            else
                Utility.ThemeManager.ChangeTheme(MainWindow, MahApps.Metro.Theme.Light);

            if (PackageManager.Settings["#showTrayIcon"].BoolValue)
            {
                TrayIcon.Visible = true;
            }

            var worker = new BackgroundWorker();
            worker.DoWork += (o, args) => PackageManager.QueryPackages();
            worker.RunWorkerAsync();
            
            if (IsDTELoaded)
            {
                MainWindow.Title = "CoApp for Visual Studio";
                TrayIcon.Text = MainWindow.Title;
                MainWindow.ShowMinButton = false;
                MainWindow.ShowInTaskbar = false;
                MainWindow.MinWidth += 15;
            }
            else
            {
                MainWindow.Title = "CoApp.VSE";
                TrayIcon.Text = MainWindow.Title;

                if (!(PackageManager.Settings["#showTrayIcon"].BoolValue && PackageManager.Settings["#startInTray"].BoolValue))
                    ShowMainWindow();
            }
        }

        public static void OnExit(object sender, ExitEventArgs e)
        {
            TrayIcon.Visible = false;
        }

        public static void ShowBalloonTip(string message)
        {
            if (PackageManager.Settings["#showNotifications"].BoolValue)
                TrayIcon.ShowBalloonTip(2000, MainWindow.Title, message, ToolTipIcon.Info);
        }

        public static void HideBalloonTip()
        {
            TrayIcon.Visible = false;
            TrayIcon.Visible = PackageManager.Settings["#showTrayIcon"].BoolValue;
        }

        public static void InvokeSolutionChanged()
        {
            IsProjectsOpen = DTE.Solution.Projects.OfType<Project>().Any(m => m.IsSupported());
            SolutionChanged();
        }

        public static void InvokeSolutionOpened()
        {
            var worker = new BackgroundWorker();
            worker.DoWork += (o, a) =>
            {
                foreach (var packageItem in PackageManager.PackagesViewModel.Packages)
                {
                    packageItem.InSolution = DTE.Solution.Projects.OfType<Project>().Any(m => m.IsSupported() && m.HasPackage(packageItem.PackageIdentity));
                }

                IsProjectsOpen = DTE.Solution.Projects.OfType<Project>().Any(m => m.IsSupported());

                RestoreMissingPackages();
            };
            worker.RunWorkerCompleted += (o, a) =>
            {
                IsSolutionOpen = true;
                SolutionOpened();
                ReloadMainControl();
            };
            worker.RunWorkerAsync();
        }

        public static void InvokeSolutionClosed()
        {
            Module.HideVisualStudioControl();
            IsSolutionOpen = false;
            IsProjectsOpen = false;
            SolutionClosed();
        }

        public static void ManagePackage(PackageReference packageReference, IEnumerable<Project> checkedProjects, IEnumerable<LibraryReference> libraries)
        {
            if (!IsSolutionOpen)
                return;

            foreach (var project in DTE.Solution.Projects.OfType<Project>().Where(n => n.IsSupported()))
            {
                var resultLibraries = Enumerable.Empty<LibraryReference>();

                var projectLibraries = libraries.Where(n => n.ProjectName == project.GetName());

                switch (packageReference.Type)
                {
                    case DeveloperLibraryType.VcInclude:
                        project.ManageIncludeDirectories(packageReference, checkedProjects);
                        break;
                    case DeveloperLibraryType.VcLibrary:
                        project.ManageLinkerDefinitions(packageReference, checkedProjects, projectLibraries);
                        resultLibraries = projectLibraries.Where(n => n.IsChecked);
                        break;
                    case DeveloperLibraryType.Net:
                        project.ManageReferences(packageReference, projectLibraries);
                        resultLibraries = projectLibraries.Where(n => n.IsChecked);
                        break;
                }

                var packageReferenceFile = new PackageReferenceFile(project.GetDirectory() + "/coapp.packages.config");

                if (checkedProjects.Any(n => n.FullName == project.FullName))
                {
                    packageReferenceFile.AddEntry(packageReference.CanonicalName, resultLibraries, packageReference.Type);
                }
                else
                {
                    packageReferenceFile.DeleteEntry(packageReference.CanonicalName);
                }

                project.Save(project.FullName);
            }
        }

        public static void RemoveExistingSimilarPackages(PackageReference packageReference, IEnumerable<Project> checkedProjects)
        {
            foreach (var project in DTE.Solution.Projects.OfType<Project>().Where(n => n.IsSupported()))
            {
                if (!checkedProjects.Any(n => n.Name == project.Name))
                    continue;

                var packageReferenceFile = new PackageReferenceFile(project.GetDirectory() + "/coapp.packages.config");

                var packageReferences = packageReferenceFile.GetPackageReferences();

                foreach (var packageRef in packageReferences)
                {
                    if (packageRef.CanonicalName.Name != packageReference.CanonicalName.Name ||
                        packageRef.CanonicalName.Architecture != packageReference.CanonicalName.Architecture)
                        continue;

                    var removedLibraries = new List<LibraryReference>();

                    foreach (var lib in packageRef.Libraries)
                    {
                        removedLibraries.Add(new LibraryReference(lib.Name, project.GetName(), lib.ConfigurationName, false));
                    }

                    ManagePackage(packageRef, new Project[0], removedLibraries);
                }
            }
        }

        public static void RestoreMissingPackages(bool force = false)
        {
            if (!IsSolutionOpen)
                return;

            if (PackageManager.Settings["#restore"].IntValue == 2 && !force)
                return;

            var missingPackages = PackageManager.GetMissingPackages();

            if (!missingPackages.Any())
            {
                if (force)
                    ShowBalloonTip(Resources.Restore_None);

                return;
            }

            string tooltipText;

            if (PackageManager.Settings["#restore"].IntValue == 0 || force)
            {
                tooltipText = missingPackages.Count() == 1 ? Resources.Restore_Installing : string.Format(Resources.Restore_InstallingMany, missingPackages.Count());
                var packages = new List<Package>();
                foreach (var package in missingPackages)
                {
                    packages.AddRange(PackageManager.IdentifyDependencies(package).Where(n => !n.IsInstalled));
                    PackageManager.AddMark(package, Mark.DirectInstall);
                }
                PackageManager.AddMarks(packages, Mark.IndirectInstall);
                _isRestoring = true;
                ShowProgressControl();
                
                if (force)
                    ShowMainWindow();
            }
            else
            {
                tooltipText = missingPackages.Count() == 1 ? Resources.Restore_Available : string.Format(Resources.Restore_AvailableMany, missingPackages.Count());
                TrayIcon.BalloonTipClicked += OnTrayIconBalloonTipRestoreClosed;
            }

            if (!force)
                ShowBalloonTip(tooltipText);
        }

        public static void FinishPackageRestore()
        {
            if (!_isRestoring)
                return;

            foreach (var project in DTE.Solution.Projects.OfType<Project>().Where(n => n.IsSupported()))
            {
                var packageReferenceFile = new PackageReferenceFile(project.GetDirectory() + "/coapp.packages.config");

                var packageReferences = packageReferenceFile.GetPackageReferences();

                foreach (var packageReference in packageReferences)
                {
                    var removedLibraries = new List<LibraryReference>();
                    var addedLibraries = new List<LibraryReference>();

                    foreach (var lib in packageReference.Libraries)
                    {
                        removedLibraries.Add(new LibraryReference(lib.Name, project.GetName(), lib.ConfigurationName, false));
                        addedLibraries.Add(new LibraryReference(lib.Name, project.GetName(), lib.ConfigurationName, lib.IsChecked));
                    }

                    ManagePackage(packageReference, new[] { project }, removedLibraries);
                    ManagePackage(packageReference, new[] { project }, addedLibraries);
                }
            }
        }
    }
}
