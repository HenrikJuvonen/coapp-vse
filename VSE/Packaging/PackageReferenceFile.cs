using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using CoApp.Packaging.Common;
using CoApp.VSE.Extensions;

namespace CoApp.VSE.Packaging
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
                throw new ArgumentException("path");
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
                List<LibraryReference> libraries = new List<LibraryReference>();

                string canonicalName = e.GetOptionalAttributeValue("canonicalName");
                string devtype = e.GetOptionalAttributeValue("devtype");

                if (string.IsNullOrEmpty(canonicalName))
                {
                    // If the canonicalName is empty, ignore the record.
                    continue;
                }

                if (e.Elements("configuration").Any())
                {
                    foreach (var ce in e.Elements("configuration"))
                    {
                        string config = ce.GetOptionalAttributeValue("name");

                        if (String.IsNullOrEmpty(config))
                        {
                            // If the config is empty, ignore the record.
                            continue;
                        }

                        foreach (var le in ce.Elements("lib"))
                        {
                            string lib = le.GetOptionalAttributeValue("name");

                            if (String.IsNullOrEmpty(lib))
                            {
                                // If the lib is empty, ignore the record.
                                continue;
                            }

                            libraries.Add(new LibraryReference(lib, "", config, true));
                        }
                    }
                }
                else
                {
                    foreach (var le in e.Elements("lib"))
                    {
                        string lib = le.GetOptionalAttributeValue("name");

                        if (String.IsNullOrEmpty(lib))
                        {
                            // If the lib is empty, ignore the record.
                            continue;
                        }

                        libraries.Add(new LibraryReference(lib, "", null, true));
                    }
                }

                DeveloperLibraryType developerLibraryType;
                Enum.TryParse(devtype, true, out developerLibraryType);

                yield return new PackageReference(canonicalName, null, libraries, developerLibraryType);
            }
        }

        /// <summary>
        /// Deletes an entry from the file with matching id and version. Returns true if the file was deleted.
        /// </summary>
        public bool DeleteEntry(CanonicalName canonicalName)
        {
            XDocument document = GetDocument();

            if (document == null)
            {
                return false;
            }

            return DeleteEntry(document, canonicalName);
        }

        public void AddEntry(CanonicalName canonicalName, IEnumerable<LibraryReference> libraries, DeveloperLibraryType devtype)
        {
            XDocument document = GetDocument(createIfNotExists: true);

            AddEntry(document, canonicalName, libraries, devtype);
        }

        private void AddEntry(XDocument document, CanonicalName canonicalName, IEnumerable<LibraryReference> libraries, DeveloperLibraryType devtype)
        {
            XElement element = FindEntry(document, canonicalName);

            if (element != null)
            {
                element.Remove();
            }

            var newElement = new XElement("package",
                                  new XAttribute("canonicalName", canonicalName),
                                  new XAttribute("devtype", devtype));

            var configs = libraries.Select(n => n.ConfigurationName)
                                   .Where(n => !string.IsNullOrEmpty(n))
                                   .Distinct();

            if (configs.Any())
            {
                foreach (string config in configs)
                {
                    var configElement = new XElement("configuration",
                                            new XAttribute("name", config));

                    foreach (LibraryReference library in libraries)
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
                foreach (LibraryReference library in libraries)
                {
                    var libraryElement = new XElement("lib",
                                            new XAttribute("name", library.Name));

                    newElement.Add(libraryElement);
                }
            }
            
            document.Root.Add(newElement);

            SaveDocument(document);
        }

        public XElement FindEntry(CanonicalName canonicalName, bool isExact = false)
        {
            XDocument document = GetDocument();

            return FindEntry(document, canonicalName);
        }

        private static XElement FindEntry(XDocument document, CanonicalName canonicalName, bool isExact = false)
        {
            if (document == null || String.IsNullOrEmpty(canonicalName))
            {
                return null;
            }

            return (from e in document.Root.Elements("package")
                    let entrycanonicalName = e.GetOptionalAttributeValue("canonicalName")
                    where entrycanonicalName != null && (canonicalName == entrycanonicalName || (isExact && canonicalName.DiffersOnlyByVersion(entrycanonicalName)))
                    select e).FirstOrDefault();
        }

        private void SaveDocument(XDocument document)
        {
            // Sort the elements by package id and only take valid entries
            var packageElements = (from e in document.Root.Elements("package")
                                   let canonicalName = e.GetOptionalAttributeValue("canonicalName")
                                   where !String.IsNullOrEmpty(canonicalName)
                                   orderby canonicalName
                                   select e).ToList();

            // Remove all elements
            document.Root.RemoveAll();

            // Re-add them sorted
            document.Root.Add(packageElements);

            document.Save(_path);
        }

        private bool DeleteEntry(XDocument document, CanonicalName canonicalName)
        {
            XElement element = FindEntry(document, canonicalName);

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
