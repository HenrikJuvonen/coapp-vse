using System;
using System.Collections.Generic;
using System.Globalization;
using CoGet.Resources;

namespace CoGet
{
    public class NullSettings : ISettings
    {
        private static readonly NullSettings _settings = new NullSettings();

        public static NullSettings Instance
        {
            get { return _settings; }
        }

        public string GetValue(string section, string key)
        {
            return String.Empty;
        }

        public IList<KeyValuePair<string, string>> GetValues(string section)
        {
            return new List<KeyValuePair<string, string>>().AsReadOnly();
        }

        public IList<KeyValuePair<string, string>> GetNestedValues(string section, string key)
        {
            return new List<KeyValuePair<string, string>>().AsReadOnly();
        }

        public void SetValue(string section, string key, string value)
        {
            throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, CoGetResources.InvalidNullSettingsOperation, "SetValue"));
        }

        public void SetValues(string section, IList<KeyValuePair<string, string>> values)
        {
            throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, CoGetResources.InvalidNullSettingsOperation, "SetValues"));
        }

        public void SetNestedValues(string section, string key, IList<KeyValuePair<string, string>> values)
        {
            throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, CoGetResources.InvalidNullSettingsOperation, "SetNestedValues"));
        }

        public bool DeleteValue(string section, string key)
        {
            throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, CoGetResources.InvalidNullSettingsOperation, "DeleteValue"));
        }

        public bool DeleteSection(string section)
        {
            throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, CoGetResources.InvalidNullSettingsOperation, "DeleteSection"));
        }
    }
}
