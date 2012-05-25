using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media.Imaging;
using CoApp.Packaging.Client;
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

        public string Name
        {
            get
            {
                return PackageIdentity.Name;
            }
        }

        public string Description
        {
            get
            {
                return PackageIdentity.PackageDetails.Description;
            }
        }

        public bool ProviderIsOnlineProvider
        {
            get
            {
                return _provider is OnlineProvider;
            }
        }

        // Returns "vc" or "vc,lib" or "net"
        public string Type
        {
            get
            {
                string flavor = PackageIdentity.CanonicalName.Flavor;

                return flavor == "common" ? "vc" :
                       flavor.Contains("vc") ? "vc,lib" :
                       flavor.Contains("net") ? "net" : "";
            }
        }

        public string Path
        {
            get
            {
                return @"c:\apps\Program Files (" + PackageIdentity.Architecture + @")\Outercurve Foundation\" + PackageIdentity.CanonicalName + @"\";
            }
        }
        
        public bool IsDev
        {
            get
            {
                return PackageIdentity.Name.Contains("-dev");
            }
        }

        public string Tags
        {
            get
            {
                if (PackageIdentity.PackageDetails.Tags.IsEmpty())
                    return null;

                return String.Join(", ", _packageIdentity.PackageDetails.Tags);
            }
        }
        
        public IEnumerable<string> Dependencies
        {
            get
            {
                return PackageIdentity.Dependencies.Select(dependency => 
                    string.Format("{0}-{1}-{2}", dependency.Name, dependency.Version, dependency.Architecture));
            }
        }

        public ICollection<Project> ReferenceProjects
        {
            get
            {
                return _referenceProjectNames;
            }
        }

        public Uri IconUri
        {
            get
            {
                return PackageIdentity.PackageDetails.Icons.FirstOrDefault();
            }
        }
        
        public string CommandName
        {
            get;
            set;
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

        public bool IsCoreEnabled
        {
            get
            {
                return _provider.CanExecuteCore(this);
            }
        }

        public bool IsManageEnabled
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
        public string Id
        {
            get { return Name; }
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