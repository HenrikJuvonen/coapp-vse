using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media.Imaging;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
using CoApp.Toolkit.Engine.Client;

namespace CoGet.Dialog.Providers
{
    internal class PackageItem : IVsExtension, INotifyPropertyChanged
    {
        private readonly PackagesProviderBase _provider;
        private readonly Package _packageIdentity;
        private readonly bool _isUpdateItem, _isPrerelease;
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
            _isPrerelease = false;
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

        public bool IsPrerelease
        {
            get
            {
                return _isPrerelease;
            }
        }

        public string CommandName
        {
            get;
            set;
        }

        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "This property is data-bound in XAML.")]
        public string PublisherName
        {
            get
            {
                if (_packageIdentity.PublisherName.IsEmpty())
                    return null;

                return _packageIdentity.PublisherName;
            }
        }

        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "This property is data-bound in XAML.")]
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
                return _provider.CanExecute(this);
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