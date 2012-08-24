using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using CoApp.Packaging.Client;
using CoApp.Packaging.Common;
using EnvDTE;

namespace CoApp.VSE.Core.ViewModel
{
    using Extensions;
    using Model;

    public class PackagesViewModel : INotifyPropertyChanged
    {
        private PackageItem _selectedPackage;
        public PackageItem SelectedPackage
        {
            get { return _selectedPackage; }
            set
            {
                if (_selectedPackage != value)
                {
                    _selectedPackage = value;
                    OnPropertyChanged("SelectedPackage");
                }
            }
        }

        public ObservableCollection<PackageItem> Packages { get; private set; }

        public ICollectionView View { get; private set; }

        public PackagesViewModel(IEnumerable<Package> packages)
        {
            Packages = new ObservableCollection<PackageItem>();
            
            BuildPackageCollection(packages);

            Refresh();
        }

        private void BuildPackageCollection(IEnumerable<Package> packages)
        {
            foreach (var package in packages)
            {
                if (package != null)
                    Packages.Add(new PackageItem(package));
            }
        }

        public bool FilterOut(object o)
        {
            var n = (PackageItem) o;
            var filters = Module.PackageManager.Filters;
            var boolean = filters.ContainsKey(Resources.Filter_Boolean) ? filters[Resources.Filter_Boolean] : null;

            var result = true;

            if (filters.ContainsKey(Resources.Filter_Name))
                result = filters[Resources.Filter_Name].Any(m => n.Name != string.Empty && n.Name.Contains(m));

            if (filters.ContainsKey(Resources.Filter_Flavor))
                result = result && filters[Resources.Filter_Flavor].Any(m => n.Flavor != string.Empty && m.Contains(n.Flavor));

            if (filters.ContainsKey(Resources.Filter_Architecture))
                result = result && filters[Resources.Filter_Architecture].Contains(n.Architecture);

            if (filters.ContainsKey(Resources.Filter_Role))
                result = result && filters[Resources.Filter_Role].Any(m => n.PackageIdentity.Roles.Select(k => k.PackageRole.ToString()).Contains(m));

            if (boolean != null)
            {
                if (boolean.Contains(Resources.Filter_Boolean_Stable))
                    result = result && n.PackageIdentity.PackageDetails.Stability == 0;

                if (boolean.Contains(Resources.Filter_Boolean_Active))
                    result = result && n.PackageIdentity.IsActive;

                if (boolean.Contains(Resources.Filter_Boolean_Trimable))
                    result = result && n.PackageIdentity.IsTrimable;

                if (boolean.Contains(Resources.Filter_Boolean_Dependency))
                    result = result && n.PackageIdentity.IsDependency;

                if (boolean.Contains(Resources.Filter_Boolean_Blocked))
                    result = result && n.PackageIdentity.IsBlocked;

                if (boolean.Contains(Resources.Filter_Boolean_Installed))
                    result = result && n.PackageIdentity.IsInstalled;

                if (boolean.Contains(Resources.Filter_Boolean_Update))
                    result = result && n.IsUpdate;

                if (boolean.Contains(Resources.Filter_Boolean_Wanted))
                    result = result && n.PackageIdentity.IsWanted;
                
                if (boolean.Contains(Resources.Filter_Boolean_Latest))
                {
                    var allVersions = Packages.Where(m => m.Name == n.Name && m.Flavor == n.Flavor && m.Architecture == n.Architecture);

                    if (allVersions.Any())
                        result = result && n.Version == allVersions.Max(m => m.PackageIdentity.Version);
                }

                if (boolean.Contains(Resources.Filter_Boolean_Locked))
                    result = result && n.PackageIdentity.PackageState.HasFlag(PackageState.DoNotChange);

                if (boolean.Contains(Resources.Filter_Boolean_UsedInProjects))
                    result = result && Module.IsSolutionOpen && Module.DTE.Solution.Projects.OfType<Project>().Any(m => m.IsSupported() && m.HasPackage(n.PackageIdentity));

                if (boolean.Contains(Resources.Filter_Boolean_Devel))
                    result = result && n.PackageIdentity.GetDeveloperLibraryType() != DeveloperLibraryType.None;
            }

            if (filters.ContainsKey(Resources.Filter_FeedUrl))
            {
                var isInFeeds = false;

                foreach (var feedLocation in filters[Resources.Filter_FeedUrl].Where(m => m != null))
                {
                    isInFeeds = isInFeeds || Module.PackageManager.IsPackageInFeed(n.PackageIdentity, feedLocation);
                }

                result = result && isInFeeds;
            }

            if (filters.ContainsKey(Resources.Filter_Project) && Module.IsSolutionOpen)
            {
                var isInProjects = false;

                foreach (var projectName in filters[Resources.Filter_Project].Where(m => m != null))
                {
                    var project = Module.DTE.Solution.Projects.OfType<Project>().FirstOrDefault(m => m.Name == projectName);

                    isInProjects = isInProjects || project.HasPackage(n.PackageIdentity);
                }

                result = result && isInProjects;
            }
            
            if (filters.ContainsKey("Search"))
            {
                var searchText = filters["Search"].FirstOrDefault();

                if (!string.IsNullOrEmpty(searchText))
                {
                    var split = searchText.Split(' ');
                    var matches = false;
                    foreach (var text in split)
                    {
                        if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text))
                            continue;

                        if (n.PackageIdentity.CanonicalName.PackageName.Contains(text) || n.PackageIdentity.PackageDetails.Tags.Contains(text))
                            matches = true;
                    }

                    result = result && matches;
                }
            }

            return result;
        }

        public void Refresh()
        {
            SelectedPackage = null;

            View = CollectionViewSource.GetDefaultView(Packages);
            View.Filter = FilterOut;
        }

        internal void ReplacePackage(Package package, Package newPackage)
        {
            var packageItem = Packages.First(n => n.PackageIdentity == package);
            packageItem.PackageIdentity = newPackage;
            packageItem.SetStatus();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
