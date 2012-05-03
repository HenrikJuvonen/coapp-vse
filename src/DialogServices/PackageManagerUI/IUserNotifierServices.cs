using System;
using System.Collections.Generic;
using EnvDTE;
using CoApp.Toolkit.Engine.Client;

namespace CoApp.VsExtension.Dialog.PackageManagerUI
{
    public interface IUserNotifierServices
    {
        bool ShowLicenseWindow(IEnumerable<Package> packages);
        IEnumerable<Project> ShowProjectSelectorWindow(
            string instructionText,
            Package package,
            Predicate<Project> checkedStateSelector,
            Predicate<Project> enabledStateSelector);
        void ShowSummaryWindow(object failedProjects);
        bool? ShowRemoveDependenciesWindow(string message);
    }
}