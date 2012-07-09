using System.Collections.Generic;
using CoApp.Packaging.Common;

namespace CoApp.VisualStudio
{
    /// <summary>
    /// Represents a package.
    /// </summary>
    public class PackageReference
    {
        public PackageReference(string name, string flavor, string version, string architecture, string packageDirectory,
            IEnumerable<Library> libraries, DeveloperPackageType type)
        {
            Name = name;
            Flavor = flavor;
            Version = version;
            Architecture = architecture;
            PackageDirectory = packageDirectory;
            Libraries = libraries;
            Type = type;
        }
        
        public string Name { get; private set; }
        public string Flavor { get; private set; }
        public string Version { get; private set; }
        public string Architecture { get; private set; }
        public string PackageDirectory { get; private set; }
        public IEnumerable<Library> Libraries { get; private set; }
        public DeveloperPackageType Type { get; private set; }

        public override bool Equals(object obj)
        {
            if (obj is PackageReference)
            {
                var other = obj as PackageReference;
                return Name == other.Name && Flavor == other.Flavor && Version == other.Version && Architecture == other.Architecture;
            }

            if (obj is IPackage)
            {
                var other = obj as IPackage;
                return Name == other.Name && Flavor == other.Flavor && Version == other.Version && Architecture == other.Architecture;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
