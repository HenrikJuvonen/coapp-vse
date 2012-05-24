using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media.Imaging;
using CoApp.Toolkit.Engine.Client;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace CoApp.VisualStudio.Dialog.Providers
{
    internal class PackageItem : IVsExtension, INotifyPropertyChanged
    {
        private readonly PackagesProviderBase _provider;
        private readonly Package _packageIdentity;
        private readonly bool _isUpdateItem;
        private bool _isSelected;
        private readonly ObservableCollection<Project> _referenceProjectNames;

        public PackageItem(PackagesProviderBase provider, Package package, bool isUpdateItem = false) :
            this(provider, package, new Project[0], isUpdateItem)
        {
        }

        public PackageItem(PackagesProviderBase provider, Package package, IEnumerable<Project> referenceProjectNames, bool isUpdateItem = false)
        {
            _provider = provider;
            _packageIdentity = package;
            _isUpdateItem = isUpdateItem;
            _referenceProjectNames = new ObservableCollection<Project>(referenceProjectNames);
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public Package PackageIdentity
        {
            get { return _packageIdentity; }
        }

        public string Id
        {
            get { return _packageIdentity.CanonicalName; }
        }

        public string Name
        {
            get
            {
                return String.IsNullOrEmpty(_packageIdentity.Name) ? _packageIdentity.CanonicalName : _packageIdentity.Name;
            }
        }

        public string Version
        {
            get
            {
                return _packageIdentity.Version.ToString();
            }
        }

        public string Architecture
        {
            get
            {
                return _packageIdentity.Architecture.ToString();
            }
        }

        public string PublishDate
        {
            get
            {
                var date = DateTime.FromFileTime(long.Parse(_packageIdentity.PublishDate));
                return date.ToShortDateString();
            }
        }

        public bool ProviderIsOnlineProvider
        {
            get
            {
                return _provider is OnlineProvider;
            }
        }

        // E.g. "common" or "vc10-ts" or "net40"
        public string Flavor
        {
            get
            {
                string canonicalName = PackageIdentity.CanonicalName;

                if (canonicalName.Contains("common"))
                    return "common";

                int a = canonicalName.IndexOf('[') + 1;
                int b = canonicalName.LastIndexOf(']');

                if (a == -1 || b == -1)
                    return "";

                return canonicalName.Substring(a, b - a);
            }
        }

        // Returns "vc" or "vc,lib" or "net"
        public string Type
        {
            get
            {
                return Flavor == "common" ? "vc" :
                       Flavor.Contains("vc") ? "vc,lib" :
                       Flavor.Contains("net") ? "net" : "";
            }
        }

        public string Path
        {
            get
            {
                return @"c:\apps\Program Files (" + Architecture + @")\Outercurve Foundation\" + PackageIdentity.CanonicalName + @"\";
            }
        }

        public string License
        {
            get
            {
                return _packageIdentity.License.ToString();
            }
        }

        public bool IsInstalled
        {
            get
            {
                return _packageIdentity.IsInstalled;
            }
        }

        public bool IsDev
        {
            get
            {
                return _packageIdentity.Name.Contains("-dev");
            }
        }

        public bool IsUpdateItem
        {
            get
            {
                return _isUpdateItem;
            }
        }

        public string Tags
        {
            get
            {
                if (_packageIdentity.Tags.IsEmpty())
                    return null;

                return String.Join(", ", _packageIdentity.Tags);
            }
        }

        public string Description
        {
            get
            {
                if (_isUpdateItem && !String.IsNullOrEmpty(_packageIdentity.Description))
                {
                    return _packageIdentity.Description;
                }

                return _packageIdentity.Description;
            }
        }

        public string Summary
        {
            get
            {
                return String.IsNullOrEmpty(_packageIdentity.Summary) ? _packageIdentity.Description : _packageIdentity.Summary;
            }
        }

        public IEnumerable<string> Dependencies
        {
            get
            {
                List<string> deps = _packageIdentity.Dependencies.ToList();

                for (int i = 0; i < deps.Count; i++)
                {
                    deps[i] = deps[i].Substring(0, deps[i].LastIndexOf('-'));
                }

                return deps;
            }
        }

        public ICollection<Project> ReferenceProjects
        {
            get
            {
                return _referenceProjectNames;
            }
        }
        
        public string CommandName
        {
            get;
            set;
        }

        public string PublisherName
        {
            get
            {
                if (_packageIdentity.PublisherName.IsEmpty())
                    return null;

                return _packageIdentity.PublisherName;
            }
        }

        public bool RequireLicenseAcceptance
        {
            get
            {
                return false;
            }
        }

        public string LicenseUrl
        {
            get
            {
                return _packageIdentity.LicenseUrl;
            }
        }

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                OnNotifyPropertyChanged("IsSelected");
            }
        }

        public bool IsEnabled
        {
            get
            {
                return _provider.CanExecuteCore(this);
            }
        }

        public bool IsEnabled2
        {
            get
            {
                return _provider.CanExecuteManage(this);
            }
        }

        internal void UpdateEnabledStatus()
        {
            OnNotifyPropertyChanged("IsEnabled");
        }

        private void OnNotifyPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        // Not used but required by the interface IVsExtension.
        public float Priority
        {
            get { return 0; }
        }

        // Not used but required by the interface IVsExtension.
        public BitmapSource MediumThumbnailImage
        {
            get { return null; }
        }

        // Not used but required by the interface IVsExtension.
        public BitmapSource SmallThumbnailImage
        {
            get { return null; }
        }

        // Not used but required by the interface IVsExtension.
        public BitmapSource PreviewImage
        {
            get { return null; }
        }
    }
}