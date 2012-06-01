using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using EnvDTE;
using CoApp.VisualStudio.VsCore;
using CoApp.VisualStudio.Dialog.Providers;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;

namespace CoApp.VisualStudio.Dialog
{
    public partial class PackageManagerWindow : DialogWindow
    {
        internal static PackageManagerWindow CurrentInstance;

        private readonly IOptionsPageActivator _optionsPageActivator;
        private readonly Project _activeProject;

        private ComboBox _archComboBox;

        public PackageManagerWindow(Project project) :
            this(project,
                 ServiceLocator.GetInstance<DTE>(),
                 ServiceLocator.GetInstance<IOptionsPageActivator>(),
                 ServiceLocator.GetInstance<ISolutionManager>())
        {
        }

        public PackageManagerWindow(Project project,
                                    DTE dte,
                                    IOptionsPageActivator optionPageActivator,
                                    ISolutionManager solutionManager)
        {
            InitializeComponent();

            _activeProject = project;
            _optionsPageActivator = optionPageActivator;

            PrepareArchitectureComboBox();

            ProviderServices providerServices = new ProviderServices();

            SetupProviders(providerServices,
                           _activeProject,
                           dte,
                           solutionManager);
        }
       
        private void SetupProviders(ProviderServices providerServices,
                                    Project activeProject,
                                    DTE dte,
                                    ISolutionManager solutionManager)
        {
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
        private static void OnActivated(Project project)
        {
            var window = new PackageManagerWindow(project);
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
                "Package Feeds",
                () => OnActivated(_activeProject));
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

        private void PrepareArchitectureComboBox()
        {
            ComboBox fxCombo = FindComboBox("cmb_Fx");
            if (fxCombo != null)
            {
                fxCombo.Items.Clear();
                fxCombo.Items.Add("Architecture: All");
                fxCombo.Items.Add("Architecture: Any");
                fxCombo.Items.Add("Architecture: x64");
                fxCombo.Items.Add("Architecture: x86");
                fxCombo.SelectedIndex = 0;
                fxCombo.SelectionChanged += OnFxComboBoxSelectionChanged;

                _archComboBox = fxCombo;
            }
        }

        private void OnFxComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = (ComboBox)sender;
            if (combo.SelectedIndex == -1)
            {
                return;
            }

            string arch = combo.SelectedIndex == 0 ? "All" :
                          combo.SelectedIndex == 1 ? "Any" :
                          combo.SelectedIndex == 2 ? "x64" : "x86";

            CoAppWrapper.SetArchitecture(arch);

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
