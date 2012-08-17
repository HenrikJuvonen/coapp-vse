namespace CoApp.VSE.Core
{
    public enum DeveloperLibraryType
    {
        None,
        VcInclude,
        VcLibrary,
        Net
    }

    public enum PackageItemStatus
    {
        MarkedForInstallation,
        MarkedForReinstallation,
        MarkedForUpdate,
        MarkedForUpgrade,
        MarkedForRemoval,
        MarkedForCompleteRemoval,
        NotInstalled,
        NotInstalledBlocked,
        Installed,
        InstalledHasUpdate,
        InstalledLocked,
        Broken,
        Update,
        MarkedForVisualStudio,
        Unmark
    }
    
    public enum Mark
    {
        DirectReinstall,
        DirectInstall,
        DirectUpdate,
        DirectUpgrade,
        DirectRemove,
        DirectCompletelyRemove,
        IndirectReinstall,
        IndirectInstall,
        IndirectUpdate,
        IndirectUpgrade,
        IndirectRemove,
        IndirectCompletelyRemove,
        DirectVisualStudio,
        IndirectVisualStudio
    }
}
