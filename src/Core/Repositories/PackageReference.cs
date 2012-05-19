using System;
using System.Collections.Generic;

namespace CoGet
{
    public class PackageReference : IEquatable<PackageReference>
    {
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
        public IEnumerable<Library> Libraries { get; private set; }

        public override bool Equals(object obj)
        {
            var reference = obj as PackageReference;
            if (reference != null)
            {
                return Equals(reference);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() * 3137 + Version.GetHashCode() * 2135 + Architecture.GetHashCode() * 123;
        }

        public override string ToString()
        {
            return Name + " " + Version + " " + Architecture;
        }

        public bool Equals(PackageReference other)
        {
            return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) &&
                   Version == other.Version && Architecture == other.Architecture;
        }
    }
}
