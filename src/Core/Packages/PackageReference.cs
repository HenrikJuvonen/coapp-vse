using System;
using System.Linq;
using System.Collections.Generic;

namespace CoApp.VisualStudio
{
    /// <summary>
    /// Represents a package.
    /// </summary>
    public class PackageReference
    {
        private string _type;

        public PackageReference(string name, string flavor, string version, string architecture, string path, IEnumerable<Library> libraries)
        {
            Name = name;
            Flavor = flavor;
            Version = version;
            Architecture = architecture;
            Path = path;
            Libraries = libraries;
        }
        
        public string Name { get; private set; }
        public string Flavor { get; private set; }
        public string Version { get; private set; }
        public string Architecture { get; private set; }
        public string Path { get; private set; }
        public IEnumerable<Library> Libraries { get; private set; }

        public string Type
        {
            get
            {
                if (_type == null)
                {
                    if (Name.Contains("-common"))
                    {
                        _type = "vc";
                    }
                    else if (Flavor.Contains("vc"))
                    {
                        _type = "vc,lib";
                    }
                    else if (Flavor.Contains("net") || Flavor.Contains("silverlight"))
                    {
                        _type = "net";
                    }
                    else
                    {
                        var package = CoAppWrapper.GetPackages(new[] { this }).FirstOrDefault();

                        if (package != null)
                        {
                            _type = package.GetDevType();
                        }
                        else
                        {
                            _type = string.Empty;
                        }
                    }
                }

                return _type; 
            }
        }
    }
}
