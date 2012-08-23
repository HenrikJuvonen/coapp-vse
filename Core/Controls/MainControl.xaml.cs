using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using CoApp.Packaging.Client;
using CoApp.Packaging.Common;
using CoApp.Toolkit.Extensions;

namespace CoApp.VSE.Core.Controls
{
    using Extensions;
    using Model;
    using Utility;
    using System.Windows.Controls.Primitives;

    public partial class MainControl
    {
        private SortDescription[] sortDescriptions;

        public MainControl()
        {
            InitializeComponent();
            
            Module.PackageManager.FeedsUpdated += OnFeedsUpdated;

            if (Module.IsDTELoaded)
            {
                var action = new Action<bool>(b =>
                {
                    Module.PackageManager.ClearMarks(b);
                    Reload();
                });

                Module.DTE.Events.SolutionEvents.AfterClosing += () => action.Invoke(true);
                Module.DTE.Events.SolutionEvents.ProjectAdded += project => action.Invoke(false);
                Module.DTE.Events.SolutionEvents.ProjectRemoved += project => action.Invoke(false);
                Module.DTE.Events.SolutionEvents.ProjectRenamed += (project, name) => action.Invoke(false);

                InSolutionColumn.Visibility = Visibility.Visible;
            }

            if (Module.PackageManager.Settings["#showConsole"].BoolValue)
            {
                ConsoleControl.Visibility = Visibility.Visible;
            }
        }

        public void OnFeedsUpdated()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Module.PackageManager.Reset();
                FilterItemsControl.Clear();
                Reload();
            }));
        }

        public void Reload()
        {
            if (Module.PackageManager.IsQuerying)
            {
                var timer = new DispatcherTimer();
                timer.Tick += (o, a) => { timer.Stop(); Reload(); };
                timer.Interval = new TimeSpan(0, 0, 1);
                timer.Start();
                return;
            }

            MarkAllUpgradesButton.IsEnabled = false;
            MarkAllUpdatesButton.IsEnabled = false;
            ApplyButton.IsEnabled = false;
            NoItemsPane.Visibility = Visibility.Collapsed;
            PackageDetailsPanel.Visibility = Visibility.Collapsed;
            PackagesDataGrid.Visibility = Visibility.Collapsed;
            ProgressPane.Visibility = Visibility.Visible;

            sortDescriptions = PackagesDataGrid.Items.SortDescriptions.ToArray();
            DataContext = null;

            Module.PackageManager.Filters.Clear();

            foreach (FilterControl item in FilterItemsControl.FilterBox.Items)
            {
                if (!Module.PackageManager.Filters.ContainsKey(item.Caption))
                    Module.PackageManager.Filters.Add(item.Caption, item.Details);
                else
                    Module.PackageManager.Filters[item.Caption].AddRange(item.Details);
            }

            if (Module.PackageManager.Filters.ContainsKey("Search"))
                Module.PackageManager.Filters.Remove("Search");

            Module.PackageManager.Filters.Add("Search", new List<string> { SearchBox.Text.ToLowerInvariant() });

            var worker = new BackgroundWorker();
            worker.DoWork += (sender, args) => Module.PackageManager.QueryPackages();
            worker.DoWork += RefreshView;
            worker.RunWorkerCompleted += FinishReload;
            worker.RunWorkerAsync();
        }

        private int _counter;
        private void RefreshView(object sender, DoWorkEventArgs e)
        {
            _counter++;

            Thread.Sleep(200);

            _counter--;

            if (_counter != 0)
            {
                e.Cancel = true;
                return;
            }
            
            lock (Module.PackageManager.PackagesViewModel)
            {
                Module.PackageManager.PackagesViewModel.Refresh();
            }

            if (!Module.PackageManager.PackagesViewModel.View.IsEmpty)
            {
                Thread.Sleep(400);
            }

            if (_counter != 0)
            {
                e.Cancel = true;
            }
        }

        private void FinishReload(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
                return;

            lock (Module.PackageManager.PackagesViewModel)
            {
                DataContext = Module.PackageManager.PackagesViewModel;
            }

            if (!Module.PackageManager.PackagesViewModel.View.IsEmpty)
            {
                PackageDetailsPanel.Visibility = Visibility.Visible;
                PackagesDataGrid.Visibility = Visibility.Visible;
                RestoreSortDescriptions();
                PackagesDataGrid.SelectedIndex = 0;
            }
            else
            {
                NoItemsPane.Visibility = Visibility.Visible;
            }

            ProgressPane.Visibility = Visibility.Collapsed;

            MarkAllUpdatesButton.IsEnabled = Module.PackageManager.IsAnyUpdates;
            MarkAllUpgradesButton.IsEnabled = Module.PackageManager.IsAnyUpdates;
            ApplyButton.IsEnabled = Module.PackageManager.IsAnyMarked;
        }

        private void RestoreSortDescriptions()
        {
            foreach (var column in PackagesDataGrid.Columns)
            {
                var sortDescription = sortDescriptions.FirstOrDefault(n => n.PropertyName == column.SortMemberPath);
                column.SortDirection = sortDescription != null ? sortDescription.Direction : ListSortDirection.Ascending;
            }

            if (sortDescriptions.Any())
            {
                foreach (var sortDescription in sortDescriptions)
                {
                    PackagesDataGrid.Items.SortDescriptions.Add(sortDescription);
                }
            }
            else
            {
                PackagesDataGrid.Items.SortDescriptions.Add(new SortDescription("SortByName", ListSortDirection.Ascending));
            }
        }
        
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (SearchBox.IsFocused)
                {
                    if (SearchBox.Text.IsNullOrEmpty())
                    {
                        FocusManager.SetFocusedElement(Module.MainWindow, Module.MainWindow);
                    }
                    else
                    {
                        SearchBox.Clear();
                    }
                    e.Handled = true;
                }
            }

            if (e.Key == Key.Space)
            {
                if (sender is DataGrid)
                {
                    OnStatusCheckBoxChanged(null, null);
                    e.Handled = true;
                }
            }
        }

        private void OnSearchBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            Reload();
        }

        public void ExecuteShowOptions(object sender = null, ExecutedRoutedEventArgs e = null)
        {
            Module.ShowOptionsControl();
        }
        
        private void ExecuteApply(object sender, ExecutedRoutedEventArgs e)
        {
            if (Module.PackageManager.VisualStudioPlan.Any())
                Module.ShowVisualStudioControl();
            else
                Module.ShowSummaryControl();
        }

        private void ExecuteBrowse(object sender, ExecutedRoutedEventArgs e)
        {
            var worker = new BackgroundWorker();
            worker.DoWork += (o, a) =>
            {
                try
                {
                    var directory = Module.PackageManager.PackagesViewModel.SelectedPackage.PackageIdentity.GetPackageDirectory();
                    System.Diagnostics.Process.Start(directory);
                }
                catch { }
            };
            worker.RunWorkerAsync();
        }

        private void CanExecuteBrowse(object sender, CanExecuteRoutedEventArgs e)
        {
            if (PackagesDataGrid != null)
                e.CanExecute = PackagesDataGrid.IsVisible;
        }

        private void ExecuteReload(object sender, ExecutedRoutedEventArgs e)
        {
            Module.PackageManager.Reset();
            Reload();
        }

        private void ExecuteMoreInformation(object sender, ExecutedRoutedEventArgs e)
        {
            Module.ShowInformationControl();
        }

        public void ExecuteMarkUpdates(object sender = null, ExecutedRoutedEventArgs e = null)
        {
            foreach (var item in Module.PackageManager.PackagesViewModel.Packages.Where(n => n.Status == PackageItemStatus.InstalledHasUpdate))
            {
                item.SetStatus(PackageItemStatus.MarkedForUpdate);

                UpdateMarkLists(item);
            }
        }

        private void ExecuteMarkUpgrades(object sender, ExecutedRoutedEventArgs e)
        {
            foreach (var item in Module.PackageManager.PackagesViewModel.Packages.Where(n => n.Status == PackageItemStatus.InstalledHasUpdate))
            {
                item.SetStatus(PackageItemStatus.MarkedForUpgrade);

                UpdateMarkLists(item);
            }
        }

        private void ExecuteOpenLicenseLink(object sender, ExecutedRoutedEventArgs e)
        {
            var hyperlink = e.OriginalSource as Hyperlink;
            if (hyperlink != null && hyperlink.NavigateUri != null)
            {
                UriHelper.OpenExternalLink(hyperlink.NavigateUri);
                e.Handled = true;
            }
        }

        private void ExecuteClearSearch(object sender, ExecutedRoutedEventArgs e)
        {
            SearchBox.Clear();
        }

        private void OnStatusContextMenuItemClick(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;
            var packageItem = (PackageItem)PackagesDataGrid.SelectedItem;

            var statusText = (string)menuItem.Header;
            statusText = statusText.Replace("Mark for ", "MarkedFor").Replace(" ", "");

            var status = (PackageItemStatus)Enum.Parse(typeof(PackageItemStatus), statusText);
            
            packageItem.SetStatus(status);

            UpdateMarkLists(packageItem);
        }

        private void OnStatusContextMenuItemClick2(object sender, RoutedEventArgs e)
        {
            var packageItem = (PackageItem)PackagesDataGrid.SelectedItem;
            
            var worker = new BackgroundWorker();
            worker.DoWork += (o, a) => Module.PackageManager.SetPackageState(packageItem.PackageIdentity, "Wanted");
            worker.RunWorkerCompleted += (o, a) => UpdateMarkLists(packageItem);
            worker.RunWorkerAsync();            
        }

        private void OnDataGridMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            dynamic src = e.OriginalSource;

            if (e.OriginalSource is Rectangle || src.TemplatedParent is DataGridColumnHeader)
                return;

            if (PackagesDataGrid.CurrentColumn != null && PackagesDataGrid.CurrentColumn.DisplayIndex == 0)
                return;

            OnStatusCheckBoxChanged(null, null);
        }

        private void OnStatusCheckBoxChanged(object sender, EventArgs e)
        {
            var packageItem = (PackageItem)PackagesDataGrid.SelectedItem;

            if (packageItem == null)
                return;

            if (packageItem.PackageIdentity.IsInstalled)
            {
                if (packageItem.Status == PackageItemStatus.MarkedForInstallation ||
                    packageItem.Status == PackageItemStatus.MarkedForReinstallation ||
                    packageItem.Status == PackageItemStatus.MarkedForUpdate ||
                    packageItem.Status == PackageItemStatus.MarkedForUpgrade ||
                    packageItem.Status == PackageItemStatus.MarkedForRemoval ||
                    packageItem.Status == PackageItemStatus.MarkedForCompleteRemoval ||
                    packageItem.Status == PackageItemStatus.MarkedForVisualStudio)
                {
                    packageItem.SetStatus();
                }
                else
                {

                    if (Module.IsSolutionOpen && packageItem.PackageIdentity.GetDeveloperLibraryType() != DeveloperLibraryType.None)
                        packageItem.SetStatus(PackageItemStatus.MarkedForVisualStudio);
                    else
                    {
                        if ((packageItem.Name == "coapp" && packageItem.PackageIdentity.IsActive) || packageItem.PackageIdentity.PackageState.HasFlag(PackageState.DoNotChange))
                            return;
                        
                        packageItem.SetStatus(PackageItemStatus.MarkedForRemoval);
                    }
                }
            }
            else
            {
                if (packageItem.Status == PackageItemStatus.MarkedForInstallation)
                {
                    packageItem.SetStatus();
                }
                else
                {
                    if (packageItem.PackageIdentity.IsBlocked)
                        return;

                    packageItem.SetStatus(PackageItemStatus.MarkedForInstallation);
                }
            }

            UpdateMarkLists(packageItem);
        }

        internal void UpdateMarkLists(PackageItem packageItem)
        {
            IEnumerable<Package> packages = null;

            switch (packageItem.Status)
            {
                case PackageItemStatus.MarkedForVisualStudio:
                    packages = Module.PackageManager.IdentifyOwnDependencies(packageItem.PackageIdentity).Where(n => n.IsInstalled && n.Name.StartsWith(packageItem.Name));
                    Module.PackageManager.AddMark(packageItem.PackageIdentity, Mark.DirectVisualStudio);
                    Module.PackageManager.AddMarks(packages, Mark.IndirectVisualStudio);
                    break;
                case PackageItemStatus.MarkedForInstallation:
                    packages = Module.PackageManager.IdentifyDependencies(packageItem.PackageIdentity).Where(n => !n.IsInstalled);
                    Module.PackageManager.AddMark(packageItem.PackageIdentity, Mark.DirectInstall);
                    Module.PackageManager.AddMarks(packages, Mark.IndirectInstall);
                    break;
                case PackageItemStatus.MarkedForReinstallation:
                    packages = Module.PackageManager.IdentifyDependencies(packageItem.PackageIdentity).Where(n => !n.IsInstalled);
                    Module.PackageManager.AddMark(packageItem.PackageIdentity, Mark.DirectReinstall);
                    Module.PackageManager.AddMarks(packages, Mark.IndirectReinstall);
                    break;
                case PackageItemStatus.MarkedForUpdate:
                    packages = Module.PackageManager.IdentifyPackageAndDependencies((Package)packageItem.PackageIdentity.AvailableNewestUpdate);
                    Module.PackageManager.AddMark(packageItem.PackageIdentity, Mark.DirectUpdate);
                    Module.PackageManager.AddMarks(packages, Mark.IndirectUpdate);
                    break;
                case PackageItemStatus.MarkedForUpgrade:
                    packages = Module.PackageManager.IdentifyPackageAndDependencies((Package)packageItem.PackageIdentity.AvailableNewestUpdate);
                    Module.PackageManager.AddMark(packageItem.PackageIdentity, Mark.DirectUpgrade);
                    Module.PackageManager.AddMarks(packages, Mark.IndirectUpgrade);
                    break;
                case PackageItemStatus.MarkedForRemoval:
                    Module.PackageManager.AddMark(packageItem.PackageIdentity, Mark.DirectRemove);
                    break;
                case PackageItemStatus.MarkedForCompleteRemoval:
                    packages = Module.PackageManager.IdentifyDependencies(packageItem.PackageIdentity).Where(package => !(package.Name == "coapp" && package.IsActive)).Where(n => n.IsInstalled);
                    Module.PackageManager.AddMark(packageItem.PackageIdentity, Mark.DirectCompletelyRemove);
                    Module.PackageManager.AddMarks(packages, Mark.IndirectCompletelyRemove);
                    break;
                default:
                    Module.PackageManager.RemoveMark(packageItem.PackageIdentity, Mark.DirectRemove);

                    if (Module.PackageManager.RemoveMark(packageItem.PackageIdentity, Mark.DirectVisualStudio))
                    {
                        packages = Module.PackageManager.IdentifyOwnDependencies(packageItem.PackageIdentity).Where(n => n.IsInstalled && n.Name.StartsWith(packageItem.Name));
                        Module.PackageManager.RemoveMarks(packages, Mark.IndirectVisualStudio);
                    }
                    else if (Module.PackageManager.RemoveMark(packageItem.PackageIdentity, Mark.DirectInstall))
                    {
                        packages = Module.PackageManager.IdentifyDependencies(packageItem.PackageIdentity).Where(n => !n.IsInstalled);
                        Module.PackageManager.RemoveMarks(packages, Mark.IndirectInstall);
                    }
                    else if (Module.PackageManager.RemoveMark(packageItem.PackageIdentity, Mark.DirectCompletelyRemove))
                    {
                        packages = Module.PackageManager.IdentifyDependencies(packageItem.PackageIdentity).Where(n => n.IsInstalled);
                        Module.PackageManager.RemoveMarks(packages, Mark.IndirectCompletelyRemove);
                    }
                    else if (Module.PackageManager.RemoveMark(packageItem.PackageIdentity, Mark.DirectUpdate))
                    {
                        packages = Module.PackageManager.IdentifyPackageAndDependencies((Package)packageItem.PackageIdentity.AvailableNewestUpdate);
                        Module.PackageManager.RemoveMarks(packages, Mark.IndirectUpdate);
                    }
                    else if (Module.PackageManager.RemoveMark(packageItem.PackageIdentity, Mark.DirectUpgrade))
                    {
                        packages = Module.PackageManager.IdentifyPackageAndDependencies((Package)packageItem.PackageIdentity.AvailableNewestUpdate);
                        Module.PackageManager.RemoveMarks(packages, Mark.IndirectUpgrade);
                    }
                    else if (Module.PackageManager.RemoveMark(packageItem.PackageIdentity, Mark.DirectReinstall))
                    {
                        packages = Module.PackageManager.IdentifyDependencies(packageItem.PackageIdentity).Where(n => n.IsInstalled);
                        Module.PackageManager.RemoveMarks(packages, Mark.IndirectReinstall);
                    }
                    break;
            }

            packages = packages == null ? new[] {packageItem.PackageIdentity} : packages.Union(new[] {packageItem.PackageIdentity});
            
            ApplyButton.IsEnabled = Module.PackageManager.IsAnyMarked;

            UpdatePackageItemBackgrounds(packages);
        }

        private void UpdatePackageItemBackgrounds(IEnumerable<Package> packages)
        {
            foreach (var item in Module.PackageManager.PackagesViewModel.Packages.Where(n => packages.Contains(n.PackageIdentity)))
            {
                UpdatePackageItemBackground(item);
            }
        }

        private void UpdatePackageItemBackground(PackageItem packageItem)
        {
            if (Module.PackageManager.VisualStudioPlan.Contains(packageItem.PackageIdentity))
                packageItem.ItemBackground = "Blue";
            else if (Module.PackageManager.UpdatePlan.Contains(packageItem.PackageIdentity))
                packageItem.ItemBackground = "Yellow";
            else if (Module.PackageManager.InstallPlan.Contains(packageItem.PackageIdentity))
                packageItem.ItemBackground = "Green";
            else if (Module.PackageManager.RemovePlan.Contains(packageItem.PackageIdentity))
                packageItem.ItemBackground = "Red";
            else
                packageItem.ItemBackground = null;
        }

        private void OnStatusContextMenuOpened(object sender, RoutedEventArgs e)
        {
            var menu = (ContextMenu)sender;
            var packageItem = (PackageItem)PackagesDataGrid.SelectedItem;

            var unmark = ((MenuItem)menu.Items[0]);

            if (!Module.IsDTELoaded)
                ((MenuItem)menu.Items[7]).Visibility = Visibility.Collapsed;

            unmark.IsEnabled = packageItem.Status == PackageItemStatus.MarkedForInstallation ||
                               packageItem.Status == PackageItemStatus.MarkedForReinstallation ||
                               packageItem.Status == PackageItemStatus.MarkedForUpdate ||
                               packageItem.Status == PackageItemStatus.MarkedForUpgrade ||
                               packageItem.Status == PackageItemStatus.MarkedForRemoval ||
                               packageItem.Status == PackageItemStatus.MarkedForCompleteRemoval ||
                               packageItem.Status == PackageItemStatus.MarkedForVisualStudio;

            if (packageItem.PackageIdentity.IsInstalled)
            {
                ((MenuItem)menu.Items[1]).IsEnabled = false;
                ((MenuItem)menu.Items[2]).IsEnabled = !unmark.IsEnabled && !packageItem.PackageIdentity.PackageState.HasFlag(PackageState.DoNotChange) &&
                    !(packageItem.Name == "coapp" && packageItem.PackageIdentity.IsActive);
                ((MenuItem)menu.Items[3]).IsEnabled = !unmark.IsEnabled && !packageItem.IsHighestInstalled;
                ((MenuItem)menu.Items[4]).IsEnabled = !unmark.IsEnabled && !packageItem.IsHighestInstalled;
                ((MenuItem)menu.Items[5]).IsEnabled = !unmark.IsEnabled && !packageItem.PackageIdentity.PackageState.HasFlag(PackageState.DoNotChange) &&
                    !(packageItem.Name == "coapp" && packageItem.PackageIdentity.IsActive);
                ((MenuItem)menu.Items[6]).IsEnabled = !unmark.IsEnabled && !packageItem.PackageIdentity.PackageState.HasFlag(PackageState.DoNotChange) &&
                    !(packageItem.Name == "coapp" && packageItem.PackageIdentity.IsActive);
                ((MenuItem)menu.Items[7]).IsEnabled = !unmark.IsEnabled && Module.IsSolutionOpen &&
                    packageItem.PackageIdentity.GetDeveloperLibraryType() != DeveloperLibraryType.None;
            }
            else
            {
                ((MenuItem)menu.Items[1]).IsEnabled = packageItem.Status != PackageItemStatus.MarkedForInstallation && !packageItem.PackageIdentity.IsBlocked;
                ((MenuItem)menu.Items[2]).IsEnabled = false;
                ((MenuItem)menu.Items[3]).IsEnabled = false;
                ((MenuItem)menu.Items[4]).IsEnabled = false;
                ((MenuItem)menu.Items[5]).IsEnabled = false;
                ((MenuItem)menu.Items[6]).IsEnabled = false;
                ((MenuItem)menu.Items[7]).IsEnabled = false;
            }


            var wrap = GetWrapPanelForPackageStateHeader(packageItem.PackageIdentity.IsWanted, Brushes.Black);
            wrap.Children.Add(new TextBlock { Text = "Wanted" });
            ((MenuItem)menu.Items[9]).Header = wrap;

            wrap = GetWrapPanelForPackageStateHeader(packageItem.PackageIdentity.IsBlocked, Brushes.Gray);
            wrap.Children.Add(new TextBlock { Text = "Blocked" });
            ((MenuItem)menu.Items[10]).Header = wrap;

            wrap = GetWrapPanelForPackageStateHeader(packageItem.PackageIdentity.PackageState.HasFlag(PackageState.DoNotChange), Brushes.Gray);
            wrap.Children.Add(new TextBlock { Text = "Locked" });
            ((MenuItem)menu.Items[11]).Header = wrap;

            wrap = GetWrapPanelForPackageStateHeader(packageItem.PackageIdentity.IsTrimable, Brushes.Gray);
            wrap.Children.Add(new TextBlock { Text = "Trimable" });
            ((MenuItem)menu.Items[12]).Header = wrap;

            wrap = GetWrapPanelForPackageStateHeader(packageItem.PackageIdentity.IsActive, Brushes.Gray);
            wrap.Children.Add(new TextBlock { Text = "Active" });
            ((MenuItem)menu.Items[13]).Header = wrap;
        }

        private WrapPanel GetWrapPanelForPackageStateHeader(bool state, Brush checkBrush)
        {
            var border = new Border();

            if (state)
            {
                border.Child = new TextBlock { Text = "a", FontFamily = new FontFamily("Marlett"), FontSize = 14, HorizontalAlignment = HorizontalAlignment.Left };
            }
            border.HorizontalAlignment = HorizontalAlignment.Left;
            border.VerticalAlignment = VerticalAlignment.Center;
            border.Width = 12;
            border.Height = 14;
            border.Margin = new Thickness(-20, 0, 1, 0);
            
            var wrap = new WrapPanel();
            wrap.Children.Add(border);
            
            return wrap;
        }
    }
}
