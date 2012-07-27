using System.Collections.Generic;
using CoApp.VSE.Extensions;
using CoApp.Packaging.Common;

namespace CoApp.VSE.Packaging
{
    /// <summary>
    /// Represents a package.
    /// </summary>
    public class PackageReference
    {
        public PackageReference(IPackage package)
        {
            CanonicalName = package.CanonicalName;
            PackageDirectory = package.GetPackageDirectory();
            Type = package.GetDeveloperLibraryType();
        }

        public PackageReference(IPackage package, IEnumerable<LibraryReference> libraries)
            : this(package)
        {
            Libraries = libraries;
        }

        public PackageReference(CanonicalName canonicalName, string packageDirectory,
            IEnumerable<LibraryReference> libraries, DeveloperLibraryType type)
        {
            CanonicalName = canonicalName;
            PackageDirectory = packageDirectory;
            Libraries = libraries;
            Type = type;
        }
        
        public CanonicalName CanonicalName { get; private set; }
        public string PackageDirectory { get; private set; }
        public IEnumerable<LibraryReference> Libraries { get; private set; }
        public DeveloperLibraryType Type { get; private set; }

        public override bool Equals(object obj)
        {
            if (obj is PackageReference)
            {
                var other = obj as PackageReference;
                return 
                    CanonicalName.Name == other.CanonicalName.Name &&
                    CanonicalName.Flavor == other.CanonicalName.Flavor &&
                    CanonicalName.Version == other.CanonicalName.Version &&
                    CanonicalName.Architecture == other.CanonicalName.Architecture;
            }

            if (obj is IPackage)
            {
                var other = obj as IPackage;
                return
                    CanonicalName.Name == other.CanonicalName.Name &&
                    CanonicalName.Flavor == other.CanonicalName.Flavor &&
                    CanonicalName.Version == other.CanonicalName.Version &&
                    CanonicalName.Architecture == other.CanonicalName.Architecture;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
