using System;
using System.Collections.Generic;

namespace CoApp.VisualStudio
{
    /// <summary>
    /// Represents a package.
    /// </summary>
    public class PackageReference
    {
        public PackageReference(string name, string flavor, string version, string architecture, string type, string path, IEnumerable<Library> libraries)
        {
            Name = name;
            Flavor = flavor;
            Version = version;
            Architecture = architecture;
            Type = type;
            Path = path;
            Libraries = libraries;
        }
        
        public string Name { get; private set; }
        public string Flavor { get; private set; }
        public string Version { get; private set; }
        public string Architecture { get; private set; }
        public string Type { get; private set; }
        public string Path { get; private set; }
        public IEnumerable<Library> Libraries { get; private set; }
    }
}
