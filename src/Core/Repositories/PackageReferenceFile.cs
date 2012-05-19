using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using CoGet.Resources;

namespace CoGet
{
    public class PackageReferenceFile
    {
        private readonly string _path;
        private readonly Dictionary<string, string> _constraints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public PackageReferenceFile(string path) :
            this(new PhysicalFileSystem(Path.GetDirectoryName(path)),
                                        Path.GetFileName(path))
        {
        }

        public PackageReferenceFile(IFileSystem fileSystem, string path)
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException("fileSystem");
            }
            if (String.IsNullOrEmpty(path))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "path");
            }

            FileSystem = fileSystem;
            _path = path;
        }

        private IFileSystem FileSystem { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This might be expensive")]
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

                string name = e.GetOptionalAttributeValue("name");
                string version = e.GetOptionalAttributeValue("version");
                string architecture = e.GetOptionalAttributeValue("architecture");

                if (String.IsNullOrEmpty(name) || String.IsNullOrEmpty(version) || String.IsNullOrEmpty(architecture))
                {
                    // If the name, version or architecture is empty, ignore the record.
                    continue;
                }

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

                        libraries.Add(new Library(lib,
                                                  "",
                                                  config,
                                                  true));
                    }
                }

                yield return new PackageReference(name, version, architecture, libraries);
            }
        }

        /// <summary>
        /// Deletes an entry from the file with matching id and version. Returns true if the file was deleted.
        /// </summary>
        public bool DeleteEntry(string name, string version, string architecture)
        {
            XDocument document = GetDocument();

            if (document == null)
            {
                return false;
            }

            return DeleteEntry(document, name, version, architecture);
        }

        public bool EntryExists(string name, string version, string architecture)
        {
            XDocument document = GetDocument();
            if (document == null)
            {
                return false;
            }

            return FindEntry(document, name, version, architecture) != null;
        }

        public void AddEntry(string name, string version, string architecture, IEnumerable<Library> libraries)
        {
            XDocument document = GetDocument(createIfNotExists: true);

            AddEntry(document, name, version, architecture, libraries);
        }

        private void AddEntry(XDocument document, string name, string version, string architecture, IEnumerable<Library> libraries)
        {
            XElement element = FindEntry(document, name, version, architecture);

            if (element != null)
            {
                element.Remove();
            }

            var newElement = new XElement("package",
                                  new XAttribute("name", name),
                                  new XAttribute("version", version),
                                  new XAttribute("architecture", architecture));

            IEnumerable<string> configs = libraries.Select(n => n.Configuration).Distinct();

            foreach (string config in configs)
            {
                var configElement = new XElement("configuration",
                                    new XAttribute("name", config));

                foreach (Library library in libraries)
                {
                    if (library.Configuration == config)
                    {
                        var libraryElement = new XElement("lib",
                                             new XAttribute("name", library.Name));

                        configElement.Add(libraryElement);
                    }
                }

                newElement.Add(configElement);
            }
            
            document.Root.Add(newElement);

            SaveDocument(document);
        }

        private static XElement FindEntry(XDocument document, string name, string version, string architecture)
        {
            if (String.IsNullOrEmpty(name))
            {
                return null;
            }

            return (from e in document.Root.Elements("package")
                    let entryName = e.GetOptionalAttributeValue("name")
                    let entryVersion = e.GetOptionalAttributeValue("version")
                    let entryArchitecture = e.GetOptionalAttributeValue("architecture")
                    where entryName != null && entryVersion != null && entryArchitecture != null
                    where name.Equals(entryName, StringComparison.OrdinalIgnoreCase) && (version == null || entryVersion.Equals(version))
                    select e).FirstOrDefault();
        }

        private void SaveDocument(XDocument document)
        {
            // Sort the elements by package id and only take valid entries (one with both id and version)
            var packageElements = (from e in document.Root.Elements("package")
                                   let name = e.GetOptionalAttributeValue("name")
                                   let version = e.GetOptionalAttributeValue("version")
                                   let architecture = e.GetOptionalAttributeValue("architecture")
                                   where !String.IsNullOrEmpty(name) && !String.IsNullOrEmpty(version) && !String.IsNullOrEmpty(architecture)
                                   orderby name
                                   select e).ToList();

            // Remove all elements
            document.Root.RemoveAll();

            // Re-add them sorted
            document.Root.Add(packageElements);

            FileSystem.AddFile(_path, document.Save);
        }

        private bool DeleteEntry(XDocument document, string name, string version, string architecture)
        {
            XElement element = FindEntry(document, name, version, architecture);

            if (element != null)
            {
                // Preserve the allowedVersions attribute for this package id (if any defined)
                var versionConstraint = element.GetOptionalAttributeValue("allowedVersions");

                if (!String.IsNullOrEmpty(versionConstraint))
                {
                    _constraints[name] = versionConstraint;
                }

                // Remove the element from the xml dom
                element.Remove();

                // Always try and save the document, this works around a source control issue for solution-level packages.config.
                SaveDocument(document);

                if (!document.Root.HasElements)
                {
                    // Remove the file if there are no more elements
                    FileSystem.DeleteFile(_path);

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
                if (FileSystem.FileExists(_path))
                {
                    using (Stream stream = FileSystem.OpenFile(_path))
                    {
                        return XDocument.Load(stream);
                    }
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
                    String.Format(CultureInfo.CurrentCulture, CoGetResources.ErrorReadingFile, FileSystem.GetFullPath(_path)), e);
            }
        }
    }
}
