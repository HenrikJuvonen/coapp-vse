namespace CoApp.VisualStudio.Dialog.PackageManagerUI
{
    using System;
    using System.Globalization;
    using System.Windows.Threading;
    using EnvDTE;
    using CoApp.VisualStudio.VsCore;
    using CoApp.Packaging.Client;
    using CoApp.Toolkit.Win32;
    using CoApp.VisualStudio.Dialog;

    public class UserNotifierServices
    {
        private readonly Dispatcher _uiDispatcher;

        public UserNotifierServices()
        {
            _uiDispatcher = Dispatcher.CurrentDispatcher;
        }

        public bool? ShowQueryMessage(string message)
        {
            if (!_uiDispatcher.CheckAccess())
            {
                object result = _uiDispatcher.Invoke(
                    new Func<string, bool?>(ShowQueryMessage),
                    message);
                return (bool?)result;
            }

            return MessageHelper.ShowQueryMessage(message, null, true);
        }

        public void ShowInfoMessage(string message)
        {
            if (!_uiDispatcher.CheckAccess())
            {
                object result = _uiDispatcher.Invoke(
                    new Action<string>(ShowInfoMessage),
                    message);
                return;
            }

            MessageHelper.ShowInfoMessage(message, null);
        }

        public void ShowErrorMessage(string message)
        {
            if (!_uiDispatcher.CheckAccess())
            {
                object result = _uiDispatcher.Invoke(
                    new Action<string>(ShowErrorMessage),
                    message);
                return;
            }

            MessageHelper.ShowErrorMessage(message, null);
        }

        public void ShowWarningMessage(string message)
        {
            if (!_uiDispatcher.CheckAccess())
            {
                object result = _uiDispatcher.Invoke(
                    new Action<string>(ShowWarningMessage),
                    message);
                return;
            }

            MessageHelper.ShowWarningMessage(message, null);
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