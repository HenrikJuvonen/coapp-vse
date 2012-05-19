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
        object[] ShowProjectSelectorWindow(
            string instructionText,
            Package package,
            Func<Package, Project, string, string, bool?> checkedStateSelector,
            Predicate<Project> enabledStateSelector,
            string type);
    }
}