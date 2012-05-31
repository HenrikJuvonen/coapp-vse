using System;
using EnvDTE;
using CoApp.Packaging.Client;

namespace CoApp.VisualStudio.Dialog.PackageManagerUI
{
    public interface IUserNotifierServices
    {
        bool? ShowRemoveDependenciesWindow(string message);
        object[] ShowProjectSelectorWindow(
            string instructionText,
            PackageReference packageReference);
    }
}