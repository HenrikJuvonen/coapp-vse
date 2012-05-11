using System;
using System.IO;
using CoApp.Toolkit.Engine.Client;

namespace CoGet
{
    public class DefaultPackagePathResolver : IPackagePathResolver
    {
        private readonly IFileSystem _fileSystem;

        public DefaultPackagePathResolver(string path)
            : this(new PhysicalFileSystem(path))
        {
        }

        public DefaultPackagePathResolver(IFileSystem fileSystem)
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException("fileSystem");
            }
            _fileSystem = fileSystem;
        }

        public virtual string GetInstallPath(Package package)
        {
            return Path.Combine(_fileSystem.Root, GetPackageDirectory(package));
        }

        public virtual string GetPackageDirectory(Package package)
        {
            return GetPackageDirectory(package.CanonicalName);
        }

        public virtual string GetPackageFileName(Package package)
        {
            return GetPackageFileName(package.CanonicalName);
        }

        public virtual string GetPackageDirectory(string canonicalName)
        {
            return canonicalName;
        }

        public virtual string GetPackageFileName(string canonicalName)
        {
            return canonicalName + ".msi";
        }
    }
}
