using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Threading;
using EnvDTE;
using CoGet.VisualStudio;
using System.Runtime.Versioning;
using CoApp.Toolkit.Engine.Client;

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
    }
}