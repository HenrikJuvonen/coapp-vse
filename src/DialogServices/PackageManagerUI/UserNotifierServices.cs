using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Threading;
using EnvDTE;
using CoGet.VisualStudio;
using System.Runtime.Versioning;
using CoApp.Toolkit.Engine.Client;
using CoApp.Toolkit.Win32;

namespace CoGet.Dialog.PackageManagerUI
{
    internal class UserNotifierServices : IUserNotifierServices
    {
        private readonly Dispatcher _uiDispatcher;

        public UserNotifierServices()
        {
            _uiDispatcher = Dispatcher.CurrentDispatcher;
        }

        public void ShowSummaryWindow(object failedProjects)
        {
            if (!_uiDispatcher.CheckAccess())
            {
                _uiDispatcher.Invoke(new Action<object>(ShowSummaryWindow), failedProjects);
                return;
            }

            var window = new SummaryWindow()
            {
                DataContext = failedProjects
            };

            window.ShowModal();
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
            Package package,
            Func<Package, Project, string, string, bool?> checkedStateSelector,
            Predicate<Project> enabledStateSelector,
            string type)
        {
            if (!_uiDispatcher.CheckAccess())
            {
                // Use Invoke() here to block the worker thread
                object result = _uiDispatcher.Invoke(
                    new Func<string, Package, Func<Package, Project, string, string, bool?>, Predicate<Project>, string, object[]>(ShowProjectSelectorWindow),
                    instructionText,
                    package,
                    checkedStateSelector,
                    enabledStateSelector,
                    type);

                return (object[])result;
            }

            var viewModel = new SolutionExplorerViewModel(
                ServiceLocator.GetInstance<DTE>().Solution,
                package,
                checkedStateSelector,
                enabledStateSelector,
                type);

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
                Architecture architecture = package.Architecture;

                string errorMessage = architecture.Equals("x86") ?
                    String.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Dialog_NoCompatibleProject,
                        package.Name,
                        Environment.NewLine + String.Join(Environment.NewLine, architecture)) :
                    String.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Dialog_NoCompatibleProjectNoFrameworkNames,
                        package.Name);

                // if there is no project compatible with the selected package, show an error message and return
                MessageHelper.ShowWarningMessage(errorMessage, title: null);
                return null;
            }
        }
    }
}