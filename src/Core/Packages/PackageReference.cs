using System.Collections.Generic;

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
    }
}
