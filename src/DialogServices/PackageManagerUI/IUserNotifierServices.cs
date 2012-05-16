using System;
using System.Collections.Generic;
using EnvDTE;
using CoApp.Toolkit.Engine.Client;

namespace CoGet.Dialog.PackageManagerUI
{
    public interface IUserNotifierServices
    {
        void ShowSummaryWindow(object failedProjects);
        bool? ShowRemoveDependenciesWindow(string message);
        IEnumerable<Project> ShowProjectSelectorWindow(
            string instructionText,
            Package package,
            Predicate<Project> checkedStateSelector,
            Predicate<Project> enabledStateSelector);
    }
}