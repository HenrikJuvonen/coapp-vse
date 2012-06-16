using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace CoApp.VisualStudio
{
    /// <summary>
    /// Represents an XML-document containing information about added packages
    /// </summary>
    public class PackageReferenceFile
    {
        private readonly string _path;

        public PackageReferenceFile(string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "path");
            }

            _path = path;
        }
        
        public IEnumerable<PackageReference> GetPackageReferences()
        {
            XDocument document = GetDocument();

            if (document == null)
            {
                yield break;
            }

            foreach (var e in document.Root.Elements("package"))
            {
                List<Library> libraries = new List<Library>();

                string name = e.Attribute("name").Value;
                string flavor = e.Attribute("flavor").Value;
                string version = e.Attribute("version").Value;
                string architecture = e.Attribute("architecture").Value;

                if (String.IsNullOrEmpty(name) || String.IsNullOrEmpty(version) || String.IsNullOrEmpty(architecture))
                {
                    // If the name, version or architecture is empty, ignore the record.
                    continue;
                }

                if (e.Elements("configuration").Any())
                {
                    foreach (var ce in e.Elements("configuration"))
                    {
                        string config = ce.Attribute("name").Value;

                        if (String.IsNullOrEmpty(config))
                        {
                            // If the config is empty, ignore the record.
                            continue;
                        }

                        foreach (var le in ce.Elements("lib"))
                        {
                            string lib = le.Attribute("name").Value;

                            if (String.IsNullOrEmpty(lib))
                            {
                                // If the lib is empty, ignore the record.
                                continue;
                            }

                            libraries.Add(new Library(lib, "", config, true));
                        }
                    }
                }
                else
                {
                    foreach (var le in e.Elements("lib"))
                    {
                        string lib = le.Attribute("name").Value;

                        if (String.IsNullOrEmpty(lib))
                        {
                            // If the lib is empty, ignore the record.
                            continue;
                        }

                        libraries.Add(new Library(lib, "", null,true));
                    }
                }

                yield return new PackageReference(name, flavor, version, architecture, null, null, libraries);
            }
        }

        /// <summary>
        /// Deletes an entry from the file with matching id and version. Returns true if the file was deleted.
        /// </summary>
        public bool DeleteEntry(string name, string flavor, string version, string architecture)
        {
            XDocument document = GetDocument();

            if (document == null)
            {
                return false;
            }

            return DeleteEntry(document, name, flavor, version, architecture);
        }

        public void AddEntry(string name, string flavor, string version, string architecture, IEnumerable<Library> libraries)
        {
            XDocument document = GetDocument(createIfNotExists: true);

            AddEntry(document, name, flavor, version, architecture, libraries);
        }

        private void AddEntry(XDocument document, string name, string flavor, string version, string architecture, IEnumerable<Library> libraries)
        {
            XElement element = FindEntry(document, name, flavor, version, architecture);

            if (element != null)
            {
                element.Remove();
            }

            var newElement = new XElement("package",
                                  new XAttribute("name", name),
                                  new XAttribute("flavor", flavor),
                                  new XAttribute("version", version),
                                  new XAttribute("architecture", architecture));

            IEnumerable<string> configs = libraries.Select(n => n.ConfigurationName)
                                                   .Where(n => !string.IsNullOrEmpty(n))
                                                   .Distinct();

            if (configs.Any())
            {
                foreach (string config in configs)
                {
                    var configElement = new XElement("configuration",
                                            new XAttribute("name", config));

                    foreach (Library library in libraries)
                    {
                        if (library.ConfigurationName == config)
                        {
                            var libraryElement = new XElement("lib",
                                                     new XAttribute("name", library.Name));

                            configElement.Add(libraryElement);
                        }
                    }

                    newElement.Add(configElement);
                }
            }
            else
            {
                foreach (Library library in libraries)
                {
                    var libraryElement = new XElement("lib",
                                            new XAttribute("name", library.Name));

                    newElement.Add(libraryElement);
                }
            }
            
            document.Root.Add(newElement);

            SaveDocument(document);
        }

        private static XElement FindEntry(XDocument document, string name, string flavor, string version, string architecture)
        {
            if (String.IsNullOrEmpty(name))
            {
                return null;
            }

            return (from e in document.Root.Elements("package")
                    let entryName = e.Attribute("name").Value
                    let entryFlavor = e.Attribute("flavor").Value
                    let entryVersion = e.Attribute("version").Value
                    let entryArchitecture = e.Attribute("architecture").Value
                    where entryName != null && entryFlavor != null && entryVersion != null && entryArchitecture != null
                    where name.Equals(entryName, StringComparison.OrdinalIgnoreCase) &&
                          (flavor == null || flavor.Equals(entryFlavor)) && 
                          (version == null || entryVersion.Equals(version)) &&
                          (architecture == null || entryArchitecture.Equals(architecture))
                    select e).FirstOrDefault();
        }

        private void SaveDocument(XDocument document)
        {
            // Sort the elements by package id and only take valid entries (one with both id and version)
            var packageElements = (from e in document.Root.Elements("package")
                                   let name = e.Attribute("name").Value
                                   let flavor = e.Attribute("flavor").Value
                                   let version = e.Attribute("version").Value
                                   let architecture = e.Attribute("architecture").Value
                                   where !String.IsNullOrEmpty(name) && !String.IsNullOrEmpty(version) && !String.IsNullOrEmpty(architecture)
                                   orderby name
                                   select e).ToList();

            // Remove all elements
            document.Root.RemoveAll();

            // Re-add them sorted
            document.Root.Add(packageElements);

            document.Save(_path);
        }

        private bool DeleteEntry(XDocument document, string name, string flavor, string version, string architecture)
        {
            XElement element = FindEntry(document, name, flavor, version, architecture);

            if (element != null)
            {
                // Remove the element from the xml dom
                element.Remove();

                // Always try and save the document, this works around a source control issue for solution-level coapp.packages.config.
                SaveDocument(document);

                if (!document.Root.HasElements)
                {
                    // Remove the file if there are no more elements
                    File.Delete(_path);

                    return true;
                }
            }

            return false;
        }

        private XDocument GetDocument(bool createIfNotExists = false)
        {
            try
            {
                // If the file exists then open and return it
                if (File.Exists(_path))
                {
                    return XDocument.Load(_path);
                }

                // If it doesn't exist and we're creating a new file then return a
                // document with an empty packages node
                if (createIfNotExists)
                {
                    return new XDocument(new XElement("packages"));
                }

                return null;
            }
            catch (XmlException e)
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture, "Error reading '{0}'.", _path), e);
            }
        }
    }
}
