using System;
using EnvDTE;
using CoApp.Packaging.Client;

namespace CoApp.VisualStudio.Dialog.PackageManagerUI
{
    public interface IUserNotifierServices
    {
        void ShowSummaryWindow(object failedProjects);
        bool? ShowRemoveDependenciesWindow(string message);
        object[] ShowProjectSelectorWindow(
            string instructionText,
            PackageReference packageReference);
    }
}