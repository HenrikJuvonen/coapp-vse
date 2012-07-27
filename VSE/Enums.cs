namespace CoApp.VSE
{
    using System;

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
        InstalledHasNewVersion,
        InstalledBlocked,
        Broken,
        NewVersion,
        MarkedForVisualStudio,
        Unmark
    }
    
    [Flags]
    public enum Mark
    {
        None = 0,
        DirectReinstall = 1 << 0,
        DirectInstall = 1 << 1,
        DirectUpdate = 1 << 2,
        DirectUpgrade = 1 << 3,
        DirectRemove = 1 << 4,
        DirectCompletelyRemove = 1 << 5,
        IndirectReinstall = 1 << 6,
        IndirectInstall = 1 << 7,
        IndirectUpdate = 1 << 8,
        IndirectUpgrade = 1 << 9,
        IndirectRemove = 1 << 10,
        IndirectCompletelyRemove = 1 << 11,
        DirectVisualStudio = 1 << 12,
        IndirectVisualStudio = 1 << 13
    }
}
