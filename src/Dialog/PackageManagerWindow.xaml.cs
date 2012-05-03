using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.VisualStudio.PlatformUI;
using System.Diagnostics.CodeAnalysis;
using EnvDTE;
using CoApp.Toolkit.Engine.Client;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
//using CoApp.VsExtension.VisualStudio;

namespace CoApp.VsExtension.Dialog
{
    using Providers;

    /// <summary>
    /// Interaction logic for PkmWindow.xaml
    /// </summary>
    public partial class PackageManagerWindow : DialogWindow
    {
        public PackageManagerWindow()
        {
            InitializeComponent();
            SetupProviders(null, null);
        }
        
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        private void SetupProviders(Project activeProject, DTE dte)
        {
            OnlineProvider onlineProvider = new OnlineProvider();
            UpdatesProvider updatesProvider = new UpdatesProvider();

            explorer.Providers.Add(onlineProvider);
            explorer.Providers.Add(updatesProvider);

            explorer.SelectedProvider = explorer.Providers[0];
        }


        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We don't care about exception handling here.")]
        private void CanExecuteCommandOnPackage(object sender, CanExecuteRoutedEventArgs e)
        {
            if (OperationCoordinator.IsBusy)
            {
                e.CanExecute = false;
                return;
            }

            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;
            if (control == null)
            {
                e.CanExecute = false;
                return;
            }

            PackageItem selectedItem = control.SelectedExtension as PackageItem;
            if (selectedItem == null)
            {
                e.CanExecute = false;
                return;
            }

            try
            {
                e.CanExecute = selectedItem.IsEnabled;
            }
            catch (Exception)
            {
                e.CanExecute = false;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We don't care about exception handling here.")]
        private void ExecutedPackageCommand(object sender, ExecutedRoutedEventArgs e)
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
                provider.Execute(selectedItem);
            }
        }

        private void CanExecuteClose(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !OperationCoordinator.IsBusy;
            e.Handled = true;
        }

        private void ExecutedClose(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void OnDialogWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // don't allow the dialog to be closed if an operation is pending
            if (OperationCoordinator.IsBusy)
            {
                e.Cancel = true;
            }
        }
        
    }
}
