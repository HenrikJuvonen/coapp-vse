using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using CoApp.VisualStudio.Dialog.Providers;
using CoApp.VisualStudio.VsCore;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using Microsoft.VisualStudio.PlatformUI;

namespace CoApp.VisualStudio.Dialog
{
    public partial class PackageManagerWindow : DialogWindow
    {
        internal static PackageManagerWindow CurrentInstance;

        private readonly IOptionsPageActivator _optionsPageActivator;

        public PackageManagerWindow()
        {
            InitializeComponent();

#if !VS10
            // set unique search guid for VS11
            explorer.SearchCategory = new Guid("{AEE3218E-8A86-49D3-B684-670F4D80E784}");
#endif

            _optionsPageActivator = ServiceLocator.GetInstance<IOptionsPageActivator>();

            SetupFilters();
            SetupProviders();
        }

        private void SetupProviders()
        {
            ProviderServices providerServices = new ProviderServices();
            ISolutionManager solutionManager = ServiceLocator.GetInstance<ISolutionManager>();

            SolutionProvider solutionProvider = new SolutionProvider(Resources, providerServices, solutionManager);
            InstalledProvider installedProvider = new InstalledProvider(Resources, providerServices, solutionManager);
            OnlineProvider onlineProvider = new OnlineProvider(Resources, providerServices);
            UpdatesProvider updatesProvider = new UpdatesProvider(Resources, providerServices);

            explorer.Providers.Add(solutionProvider);
            explorer.Providers.Add(installedProvider);
            explorer.Providers.Add(onlineProvider);
            explorer.Providers.Add(updatesProvider);

            explorer.SelectedProvider = explorer.Providers[0];
        }

        /// <summary>
        /// Called when coming back from the Options dialog
        /// </summary>
        private static void OnActivated()
        {
            var window = new PackageManagerWindow();
            try
            {
                window.ShowModal();
            }
            catch (TargetInvocationException exception)
            {
                MessageHelper.ShowErrorMessage(exception, CoApp.VisualStudio.Dialog.Resources.Dialog_MessageBoxTitle);
                ExceptionHelper.WriteToActivityLog(exception);
            }
        }

        private void CanExecuteCommandOnPackage(object sender, CanExecuteRoutedEventArgs e)
        {
            ExecutePackageOperation(e, (selectedItem, n) =>
            {
                try
                {
                    e.CanExecute = selectedItem.IsCoreEnabled;
                }
                catch (Exception)
                {
                    e.CanExecute = false;
                }
            });
        }

        private void CanExecuteManageOnPackage(object sender, CanExecuteRoutedEventArgs e)
        {
            ExecutePackageOperation(e, (selectedItem, n) =>
            {
                try
                {
                    e.CanExecute = selectedItem.IsManageEnabled;
                }
                catch (Exception)
                {
                    e.CanExecute = false;
                }
            });
        }

        private void ExecutePackageOperationCore(object sender, ExecutedRoutedEventArgs e)
        {
            ExecutePackageOperation(e, (selectedItem, provider) =>
                provider.Execute(selectedItem, "core"));
        }

        private void ExecutePackageOperationManage(object sender, ExecutedRoutedEventArgs e)
        {
            ExecutePackageOperation(e, (selectedItem, provider) =>
                provider.Execute(selectedItem, "manage"));
        }

        private void ExecutePackageOperationSetWanted(object sender, ExecutedRoutedEventArgs e)
        {
            ExecutePackageOperation(e, (selectedItem, provider) =>
                CoAppWrapper.SetPackageState(selectedItem.PackageIdentity, "wanted"));

            RefreshSelectedNode();
        }

        private void ExecutePackageOperationSetBlocked(object sender, ExecutedRoutedEventArgs e)
        {
            ExecutePackageOperation(e, (selectedItem, provider) =>
                CoAppWrapper.SetPackageState(selectedItem.PackageIdentity, "blocked"));

            RefreshSelectedNode();
        }

        private void ExecutePackageOperationSetLocked(object sender, ExecutedRoutedEventArgs e)
        {
            ExecutePackageOperation(e, (selectedItem, provider) =>
                CoAppWrapper.SetPackageState(selectedItem.PackageIdentity, "locked"));

            RefreshSelectedNode();
        }

        private void ExecutePackageOperationSetUpdatable(object sender, ExecutedRoutedEventArgs e)
        {
            ExecutePackageOperation(e, (selectedItem, provider) =>
                CoAppWrapper.SetPackageState(selectedItem.PackageIdentity, "updatable"));

            RefreshSelectedNode();
        }

        private void ExecutePackageOperationSetUpgradable(object sender, ExecutedRoutedEventArgs e)
        {
            ExecutePackageOperation(e, (selectedItem, provider) =>
                CoAppWrapper.SetPackageState(selectedItem.PackageIdentity, "upgradable"));

            RefreshSelectedNode();
        }

        private void ExecutePackageOperationSetActive(object sender, ExecutedRoutedEventArgs e)
        {
        }

        private void ExecutePackageOperationSetRequired(object sender, ExecutedRoutedEventArgs e)
        {
        }

        private void ExecutePackageOperation(RoutedEventArgs e, Action<PackageItem, PackagesProviderBase> action)
        {
            if (OperationCoordinator.IsBusy)
            {
                return;
            }

            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;
            if (control == null)
            {
                return;
            }

            PackageItem selectedItem = control.SelectedExtension as PackageItem;
            if (selectedItem == null)
            {
                return;
            }

            PackagesProviderBase provider = control.SelectedProvider as PackagesProviderBase;
            if (provider != null)
            {
                action(selectedItem, provider);
            }
        }

        private void CanExecuteClose(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !OperationCoordinator.IsBusy;
            e.Handled = true;
        }

        private void ExecuteClose(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void ExecuteShowOptionsPage(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
            _optionsPageActivator.ActivatePage(
                "General",
                () => OnActivated());
        }

        private void ExecuteOpenLicenseLink(object sender, ExecutedRoutedEventArgs e)
        {
            Hyperlink hyperlink = e.OriginalSource as Hyperlink;
            if (hyperlink != null && hyperlink.NavigateUri != null)
            {
                UriHelper.OpenExternalLink(hyperlink.NavigateUri);
                e.Handled = true;
            }
        }

        private void ExecuteSetFocusOnSearchBox(object sender, ExecutedRoutedEventArgs e)
        {
            explorer.SetFocusOnSearchBox();
        }

        private void OnCategorySelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            PackagesTreeNodeBase selectedNode = explorer.SelectedExtensionTreeNode as PackagesTreeNodeBase;
            if (selectedNode != null)
            {
                // notify the selected node that it is opened.
                selectedNode.OnOpened();

                ComboBox fxCombo = FindComboBox("cmb_Fx");
                fxCombo.IsEnabled = !(explorer.SelectedProvider is SolutionProvider);
            }
        }

        private void RefreshSelectedNode()
        {
            PackagesTreeNodeBase selectedNode = explorer.SelectedExtensionTreeNode as PackagesTreeNodeBase;
            if (selectedNode != null)
            {
                selectedNode.RefreshSelectedPackage();
            }
        }

        private void OnDialogWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // don't allow the dialog to be closed if an operation is pending
            if (OperationCoordinator.IsBusy)
            {
                e.Cancel = true;
            }
        }

        private void OnDialogWindowClosed(object sender, EventArgs e)
        {
            explorer.Providers.Clear();

            CurrentInstance = null;
        }

        private void OnDialogWindowLoaded(object sender, RoutedEventArgs e)
        {
            CurrentInstance = this;
        }

        private void SetupFilters()
        {
            ComboBox fxCombo = FindComboBox("cmb_Fx");
            if (fxCombo != null)
            {
                CoAppWrapper.ResetFilters();

                Label label = new Label(); label.Content = "Filters"; label.Visibility = Visibility.Collapsed;

                CheckBox verHigh = new CheckBox(); verHigh.Content = "Version: Highest only";
                CheckBox verStab = new CheckBox(); verStab.Content = "Version: Stable only";
                CheckBox flavSup = new CheckBox(); flavSup.Content = "Flavor: Compatible only";
                CheckBox archAny = new CheckBox(); archAny.Content = "Architecture: any";
                CheckBox archX64 = new CheckBox(); archX64.Content = "Architecture: x64";
                CheckBox archX86 = new CheckBox(); archX86.Content = "Architecture: x86";
                CheckBox roleApp = new CheckBox(); roleApp.Content = "Role: Application";
                CheckBox roleAsy = new CheckBox(); roleAsy.Content = "Role: Assembly";
                CheckBox roleDev = new CheckBox(); roleDev.Content = "Role: DeveloperLibrary";
                
                fxCombo.Items.Clear();
                fxCombo.Items.Add(label);
                fxCombo.Items.Add(verHigh);
                fxCombo.Items.Add(verStab);
                fxCombo.Items.Add(flavSup);
                fxCombo.Items.Add(archAny);
                fxCombo.Items.Add(archX64);
                fxCombo.Items.Add(archX86);
                fxCombo.Items.Add(roleApp);
                fxCombo.Items.Add(roleAsy);
                fxCombo.Items.Add(roleDev);

                fxCombo.SelectedIndex = 0;
                fxCombo.SelectionChanged += OnFxComboSelectionChanged;

                foreach (object obj in fxCombo.Items)
                {
                    CheckBox c = obj is CheckBox ? (CheckBox)obj : null;

                    if (c != null)
                    {
                        c.IsChecked = true;
                        c.Checked += OnFilterChecked;
                        c.Unchecked += OnFilterChecked;
                    }
                }
            }
        }

        private void OnFxComboSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = (ComboBox)sender;
            combo.SelectedIndex = 0;
        }

        private void OnFilterChecked(object sender, RoutedEventArgs e)
        {
            var checkbox = (CheckBox)sender;

            switch ((string)checkbox.Content)
            {
                case "Version: Highest only":
                    CoAppWrapper.SetVersionFilter("Highest", checkbox.IsChecked == true);
                    break;
                case "Version: Stable only":
                    CoAppWrapper.SetVersionFilter("Stable", checkbox.IsChecked == true);
                    break;
                case "Flavor: Compatible only":
                    CoAppWrapper.SetFlavorFilter("Compatible", checkbox.IsChecked == true);
                    break;
                case "Architecture: any":
                    CoAppWrapper.SetArchitectureFilter("any", checkbox.IsChecked == true);
                    break;
                case "Architecture: x64":
                    CoAppWrapper.SetArchitectureFilter("x64", checkbox.IsChecked == true);
                    break;
                case "Architecture: x86":
                    CoAppWrapper.SetArchitectureFilter("x86", checkbox.IsChecked == true);
                    break;
                case "Role: Application":
                    CoAppWrapper.SetRoleFilter("Application", checkbox.IsChecked == true);
                    break;
                case "Role: Assembly":
                    CoAppWrapper.SetRoleFilter("Assembly", checkbox.IsChecked == true);
                    break;
                case "Role: DeveloperLibrary":
                    CoAppWrapper.SetRoleFilter("DeveloperLibrary", checkbox.IsChecked == true);
                    break;
            }

            var selectedTreeNode = explorer.SelectedExtensionTreeNode as PackagesTreeNodeBase;
            if (selectedTreeNode != null)
            {
                selectedTreeNode.Refresh(resetQueryBeforeRefresh: true);
            }
        }

        private ComboBox FindComboBox(string name)
        {
            Grid grid = LogicalTreeHelper.FindLogicalNode(explorer, "resGrid") as Grid;
            if (grid != null)
            {
                return FindChildElementByNameOrType(grid, name, typeof(SortCombo)) as ComboBox;
            }

            return null;
        }

        private static UIElement FindChildElementByNameOrType(Grid parent, string childName, Type childType)
        {
            UIElement element = parent.FindName(childName) as UIElement;
            if (element != null)
            {
                return element;
            }
            else
            {
                foreach (UIElement child in parent.Children)
                {
                    if (childType.IsInstanceOfType(child))
                    {
                        return child;
                    }
                }
                return null;
            }
        }
    }
}
