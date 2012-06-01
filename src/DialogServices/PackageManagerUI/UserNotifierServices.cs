using System;
using System.Globalization;
using System.Windows.Threading;
using EnvDTE;
using CoApp.VisualStudio.VsCore;
using CoApp.Packaging.Client;
using CoApp.Toolkit.Win32;
using CoApp.VisualStudio.Dialog;

namespace CoApp.VisualStudio.Dialog.PackageManagerUI
{
    public class UserNotifierServices
    {
        private readonly Dispatcher _uiDispatcher;

        public UserNotifierServices()
        {
            _uiDispatcher = Dispatcher.CurrentDispatcher;
        }

        public bool? ShowRemoveDependenciesWindow(string message)
        {
            if (!_uiDispatcher.CheckAccess())
            {
                object result = _uiDispatcher.Invoke(
                    new Func<string, bool?>(ShowRemoveDependenciesWindow),
                    message);
                return (bool?)result;
            }

            return MessageHelper.ShowQueryMessage(message, title: null, showCancelButton: true);
        }

        public object[] ShowProjectSelectorWindow(
            string instructionText,
            PackageReference packageReference)
        {
            if (!_uiDispatcher.CheckAccess())
            {
                // Use Invoke() here to block the worker thread
                object result = _uiDispatcher.Invoke(
                    new Func<string, PackageReference, object[]>(ShowProjectSelectorWindow),
                    instructionText,
                    packageReference);

                return (object[])result;
            }

            var viewModel = new SolutionExplorerViewModel(
                ServiceLocator.GetInstance<DTE>().Solution,
                packageReference);

            // only show the solution explorer window if there is at least one compatible project
            if (viewModel.HasProjects)
            {
                var window = new SolutionExplorer()
                {
                    DataContext = viewModel
                };
                window.InstructionText.Text = instructionText;

                bool? dialogResult = window.ShowModal();
                if (dialogResult ?? false)
                {
                    return new object[] { viewModel.GetSelectedProjects(), viewModel.GetLibraries() };
                }
                else
                {
                    return null;
                }
            }
            else
            {
                string errorMessage = 
                    String.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Dialog_NoCompatibleProjectNoFrameworkNames,
                        packageReference.Name);

                // if there is no project compatible with the selected package, show an error message and return
                MessageHelper.ShowWarningMessage(errorMessage, title: null);
                return null;
            }
        }
    }
}