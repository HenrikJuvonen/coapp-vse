using System;
using EnvDTE;
using CoApp.Toolkit.Engine.Client;

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