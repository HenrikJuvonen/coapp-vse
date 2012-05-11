using System.Collections.Generic;
using CoApp.Toolkit.Engine.Client;
using CoApp.Toolkit.Win32;

namespace CoGet.VisualStudio
{
    internal class VsPackageMetadata : IVsPackageMetadata
    {
        private readonly Package _package;
        private readonly string _installPath;
        private readonly IFileSystem _fileSystem;

        public VsPackageMetadata(Package package, string installPath) :
            this(package, installPath, fileSystem: null)
        {
        }

        public VsPackageMetadata(Package package, string installPath, IFileSystem fileSystem)
        {
            _package = package;
            _installPath = installPath;
            _fileSystem = fileSystem;
        }

        public string Id
        {
            get { return _package.Name; }
        }

        public FourPartVersion Version
        {
            get { return _package.Version; }
        }

        public string Title
        {
            get { return _package.Name; }
        }

        public string PublisherName
        {
            get { return _package.PublisherName; }
        }

        public string Description
        {
            get { return _package.Description; }
        }

        public string InstallPath
        {
            get { return _installPath; }
        }

        public IFileSystem FileSystem
        {
            get { return _fileSystem; }
        }
    }
}
