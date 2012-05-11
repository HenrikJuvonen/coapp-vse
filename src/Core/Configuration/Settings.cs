﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using CoGet.Resources;

namespace CoGet
{
    public class Settings : ISettings
    {
        private readonly XDocument _config;
        private readonly IFileSystem _fileSystem;

        public Settings(IFileSystem fileSystem)
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException("fileSystem");
            }
            _fileSystem = fileSystem;
            _config = XmlUtility.GetOrCreateDocument("configuration", _fileSystem, Constants.SettingsFileName);
        }

        public string ConfigFilePath
        {
            get { return Path.Combine(_fileSystem.Root, Constants.SettingsFileName); }
        }

        public static ISettings LoadDefaultSettings()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!String.IsNullOrEmpty(appDataPath))
            {
                var defaultSettingsPath = Path.Combine(appDataPath, "CoGet");
                var fileSystem = new PhysicalFileSystem(defaultSettingsPath);
                try
                {
                    return new Settings(fileSystem);
                }
                catch (XmlException)
                {
                    // Work Item 1531: If the config file is malformed and the constructor throws, CoGet fails to load in VS. 
                    // Returning a null instance prevents us from silently failing.
                }
            }

            // If there is no AppData folder, use a null file system to make the Settings object do nothing
            return NullSettings.Instance;
        }

        public string GetValue(string section, string key)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }

            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "key");
            }

            // Get the section and return null if it doesn't exist
            var sectionElement = GetSection(_config.Root, section);
            if (sectionElement == null)
            {
                return null;
            }
           
            // Get the add element that matches the key and return null if it doesn't exist
            var element = FindElementByKey(sectionElement, key);
            if (element == null)
            {
                return null;
            }

            // Return the optional value which if not there will be null;
            return element.GetOptionalAttributeValue("value");
        }

        public IList<KeyValuePair<string, string>> GetValues(string section)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }

            var sectionElement = GetSection(_config.Root, section);
            if (sectionElement == null)
            {
                return EmptyList();
            }

            return sectionElement.Elements("add")
                                 .Select(ReadValue)
                                 .ToList()
                                 .AsReadOnly();
        }

        public IList<KeyValuePair<string, string>> GetNestedValues(string section, string key)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }

            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "key");
            }

            var sectionElement = GetSection(_config.Root, section);
            if (sectionElement == null)
            {
                return EmptyList();
            }
            var subSection = GetSection(sectionElement, key);
            if (subSection == null)
            {
                return EmptyList();
            }

            return subSection.Elements("add")
                             .Select(ReadValue)
                             .ToList()
                             .AsReadOnly();
        }

        public void SetValue(string section, string key, string value)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }
            var sectionElement = GetOrCreateSection(_config.Root, section);
            SetValueInternal(sectionElement, key, value);
            Save();
        }

        public void SetValues(string section, IList<KeyValuePair<string, string>> values)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            var sectionElement = GetOrCreateSection(_config.Root, section);
            foreach (var kvp in values)
            {
                SetValueInternal(sectionElement, kvp.Key, kvp.Value);
            }
            Save();
        }

        public void SetNestedValues(string section, string key, IList<KeyValuePair<string, string>> values)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            var sectionElement = GetOrCreateSection(_config.Root, section);
            var element = GetOrCreateSection(sectionElement, key);

            foreach (var kvp in values)
            {
                SetValueInternal(element, kvp.Key, kvp.Value);
            }
            Save();
        }

        private void SetValueInternal(XElement sectionElement, string key, string value)
        {
            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "key");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            var element = FindElementByKey(sectionElement, key);
            if (element != null)
            {
                element.SetAttributeValue("value", value);
                Save();
            }
            else
            {
                sectionElement.Add(new XElement("add", 
                                                    new XAttribute("key", key), 
                                                    new XAttribute("value", value)));
            }
        }

        public bool DeleteValue(string section, string key)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }
            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "key");
            }

            var sectionElement = GetSection(_config.Root, section);
            if (sectionElement == null)
            {
                return false;
            }

            var elementToDelete = FindElementByKey(sectionElement, key);
            if (elementToDelete == null)
            {
                return false;
            }
            elementToDelete.Remove();
            Save();
            return true;
        }

        public bool DeleteSection(string section)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }

            var sectionElement = GetSection(_config.Root, section);
            if (sectionElement == null)
            {
                return false;
            }

            sectionElement.Remove();
            Save();
            return true;
        }

        private void Save()
        {
            _fileSystem.AddFile(Constants.SettingsFileName, _config.Save);
        }

        private KeyValuePair<string, string> ReadValue(XElement element)
        {
            var keyAttribute = element.Attribute("key");
            var valueAttribute = element.Attribute("value");

            if (keyAttribute == null || String.IsNullOrEmpty(keyAttribute.Value) || valueAttribute == null)
            {
                throw new InvalidDataException(String.Format(CultureInfo.CurrentCulture, CoGetResources.UserSettings_UnableToParseConfigFile, ConfigFilePath));
            }

            return new KeyValuePair<string, string>(keyAttribute.Value, valueAttribute.Value);
        }

        private static XElement GetSection(XElement parentElement, string section)
        {
            section = XmlConvert.EncodeLocalName(section);
            return parentElement.Element(section);
        }

        private static XElement GetOrCreateSection(XElement parentElement, string sectionName)
        {
            sectionName = XmlConvert.EncodeLocalName(sectionName);
            var section = parentElement.Element(sectionName);
            if (section == null)
            {
                section = new XElement(sectionName);
                parentElement.Add(section);
            }
            return section;
        }

        private static XElement FindElementByKey(XElement sectionElement, string key)
        {
            return sectionElement.Elements("add")
                                        .FirstOrDefault(s => key.Equals(s.GetOptionalAttributeValue("key"), StringComparison.OrdinalIgnoreCase));
        }

        private static IList<KeyValuePair<string, string>> EmptyList()
        {
            return new KeyValuePair<string, string>[0];
        }
    }
}