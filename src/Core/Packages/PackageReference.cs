using System;
using System.Collections.Generic;

namespace CoApp.VisualStudio
{
    public class PackageReference
    {
        public PackageReference(string name, string version, string architecture, string type, string path) :
            this(name, version, architecture, null)
        {
            Type = type;
            Path = path;
        }

        public PackageReference(string name, string version, string architecture, IEnumerable<Library> libraries)
        {
            Name = name;
            Version = version;
            Architecture = architecture;
            Libraries = libraries;
        }

        public string Name { get; private set; }
        public string Version { get; private set; }
        public string Architecture { get; private set; }
        public string Type { get; private set; }
        public string Path { get; private set; }
        public IEnumerable<Library> Libraries { get; private set; }
    }
}
