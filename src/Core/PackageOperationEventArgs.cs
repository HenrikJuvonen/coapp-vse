using System.ComponentModel;
using CoApp.Toolkit.Engine.Client;

namespace CoGet
{
    public class PackageOperationEventArgs : CancelEventArgs
    {
        public PackageOperationEventArgs(Package package, IFileSystem fileSystem, string installPath) 
        {
            Package = package;
            InstallPath = installPath;
            FileSystem = fileSystem;
        }

        public string InstallPath { get; private set; }
        public Package Package { get; private set; }
        public IFileSystem FileSystem { get; private set; }
    }
}