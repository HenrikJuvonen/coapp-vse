using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using CoApp.VisualStudio.Dialog.Providers;
using CoApp.VisualStudio.VsCore;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;

namespace CoApp.VisualStudio.Dialog
{
    public partial class PackageManagerWindow
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
            var providerServices = new ProviderServices();
            var solutionManager = ServiceLocator.GetInstance<ISolutionManager>();

            var solutionProvider = new SolutionProvider(Resources, providerServices, solutionManager);
            var installedProvider = new InstalledProvider(Resources, providerServices, solutionManager);
            var onlineProvider = new OnlineProvider(Resources, providerServices);
            var updatesProvider = new UpdatesProvider(Resources, providerServices);

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
                MessageHelper.ShowErrorMessage(exception, Dialog.Resources.Dialog_MessageBoxTitle);
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

        private void ExecutePackageOperation(RoutedEventArgs e, Action<PackageItem, PackagesProviderBase> action)
        {
            if (OperationCoordinator.IsBusy)
            {
                return;
            }

            var control = e.Source as VSExtensionsExplorerCtl;
            if (control == null)
            {
                return;
            }

            var selectedItem = control.SelectedExtension as PackageItem;
            if (selectedItem == null)
            {
                return;
            }

            var provider = control.SelectedProvider as PackagesProviderBase;
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
            _optionsPageActivator.ActivatePage("General", OnActivated);
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

        private void ExecuteSetFocusOnSearchBox(object sender, ExecutedRoutedEventArgs e)
        {
            explorer.SetFocusOnSearchBox();
        }

        private void OnCategorySelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedNode = explorer.SelectedExtensionTreeNode as PackagesTreeNodeBase;
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
            var selectedNode = explorer.SelectedExtensionTreeNode as PackagesTreeNodeBase;
            if (selectedNode != null)
            {
                selectedNode.RefreshSelectedPackage();
            }
        }

        private void OnDialogWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CoAppWrapper.SaveFilterStates();

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
            var fxCombo = FindComboBox("cmb_Fx");

            if (fxCombo == null)
                return;

            CoAppWrapper.ResetFilterStates();

            if (!fxCombo.HasItems)
            {
                fxCombo.Items.Add(new Label { Content = "Filters", Visibility = Visibility.Collapsed });
                fxCombo.Items.Add(new CheckBox { Name = "Highest", Content = "Version: Highest only" });
                fxCombo.Items.Add(new CheckBox { Name = "Stable", Content = "Version: Stable only" });
                fxCombo.Items.Add(new CheckBox { Name = "Compatible", Content = "Flavor: Compatible only" });
                fxCombo.Items.Add(new CheckBox { Name = "any", Content = "Architecture: any" });
                fxCombo.Items.Add(new CheckBox { Name = "x64", Content = "Architecture: x64" });
                fxCombo.Items.Add(new CheckBox { Name = "x86", Content = "Architecture: x86" });
                fxCombo.Items.Add(new CheckBox { Name = "Application", Content = "Role: Application" });
                fxCombo.Items.Add(new CheckBox { Name = "Assembly", Content = "Role: Assembly" });
                fxCombo.Items.Add(new CheckBox { Name = "DeveloperLibrary", Content = "Role: DeveloperLibrary" });

                fxCombo.SelectedIndex = 0;
                fxCombo.SelectionChanged += OnFxComboSelectionChanged;
            }

            foreach (var obj in fxCombo.Items)
            {
                var checkbox = obj is CheckBox ? (CheckBox)obj : null;

                if (checkbox != null)
                {
                    FilterType filterType;
                    Enum.TryParse(checkbox.Name, true, out filterType);

                    checkbox.IsChecked = CoAppWrapper.GetFilterState(filterType);
                    checkbox.Checked += OnFilterChecked;
                    checkbox.Unchecked += OnFilterChecked;
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

            FilterType filterType;
            Enum.TryParse(checkbox.Name, true, out filterType);

            CoAppWrapper.SetFilterState(filterType, checkbox.IsChecked == true);

            var selectedProvider = explorer.SelectedProvider;

            if (selectedProvider != null)
            {
                foreach (PackagesTreeNodeBase node in selectedProvider.ExtensionsTree.Nodes)
                {
                    node.CurrentPage = 1;
                    node.Refresh();
                }
            }
        }

        private ComboBox FindComboBox(string name)
        {
            var grid = LogicalTreeHelper.FindLogicalNode(explorer, "resGrid") as Grid;
            if (grid != null)
            {
                return FindChildElementByNameOrType(grid, name, typeof(SortCombo)) as ComboBox;
            }

            return null;
        }

        private static UIElement FindChildElementByNameOrType(Grid parent, string childName, Type childType)
        {
            var element = parent.FindName(childName) as UIElement;
            return element ?? parent.Children.Cast<UIElement>().FirstOrDefault(childType.IsInstanceOfType);
        }
    }
}
