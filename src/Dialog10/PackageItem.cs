using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media.Imaging;
using CoApp.Packaging.Common;
using CoApp.Packaging.Common.Model;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace CoApp.VisualStudio.Dialog.Providers
{
    internal class PackageItem : IVsExtension, INotifyPropertyChanged
    {
        private readonly PackagesProviderBase _provider;
        private readonly IPackage _packageIdentity;
        private bool _isSelected;
        private readonly ObservableCollection<Project> _referenceProjectNames;

        public PackageItem(PackagesProviderBase provider, IPackage package) :
            this(provider, package, new Project[0])
        {
        }

        public PackageItem(PackagesProviderBase provider, IPackage package, IEnumerable<Project> referenceProjectNames)
        {
            _provider = provider;
            _packageIdentity = package;
            _referenceProjectNames = new ObservableCollection<Project>(referenceProjectNames);
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public IPackage PackageIdentity
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

        public string Title
        {
            get
            {
                return Name + FlavorVersionArchitecture;
            }
        }

        public string FlavorVersionArchitecture
        {
            get
            {
                return string.Format("{0}-{1}-{2}", PackageIdentity.Flavor, PackageIdentity.Version, PackageIdentity.Architecture);
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
        
        public bool IsDev
        {
            get
            {
                return PackageIdentity.Roles.Any(n => n.PackageRole == PackageRole.DeveloperLibrary);
            }
        }

        public string Tags
        {
            get
            {
                if (!PackageIdentity.PackageDetails.Tags.Any())
                    return null;

                return String.Join(", ", _packageIdentity.PackageDetails.Tags);
            }
        }

        public bool IsLocked
        {
            get
            {
                return PackageIdentity.PackageState.HasFlag(PackageState.DoNotChange);
            }
        }

        public bool IsUpdatable
        {
            get
            {
                return PackageIdentity.PackageState.HasFlag(PackageState.Updatable);
            }
        }

        public bool IsUpgradable
        {
            get
            {
                return PackageIdentity.PackageState.HasFlag(PackageState.Upgradable);
            }
        }
        
        public IEnumerable<string> Dependencies
        {
            get
            {
                return PackageIdentity.Dependencies.Select(dependency => 
                    string.Format("{0}{1}-{2}-{3}", dependency.Name, dependency.Flavor, dependency.Version, dependency.Architecture));
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
                return _provider.CanExecuteManage(this) && PackageIdentity.IsInstalled && IsDev && IsSelected;
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