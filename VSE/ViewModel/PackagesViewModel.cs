using CoApp.Packaging.Client;
using CoApp.VSE.Extensions;
using EnvDTE;

namespace CoApp.VSE.ViewModel
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Data;
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

        public PackagesViewModel(IEnumerable<Package> packages = null)
        {
            Packages = new ObservableCollection<PackageItem>();
            
            BuildPackageCollection(packages);
            
            View = CollectionViewSource.GetDefaultView(Packages);
            View.Filter = FilterOut;
            View.SortDescriptions.Add(new SortDescription("PackageIdentity.CanonicalName", ListSortDirection.Ascending));
        }

        private void BuildPackageCollection(IEnumerable<Package> packages)
        {
            if (packages == null)
                return;

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
            var boolean = filters.ContainsKey("Boolean") ? filters["Boolean"] : null;

            var result = true;

            if (filters.ContainsKey("Name"))
                result = filters["Name"].Any(m => n.Name != string.Empty && n.Name.Contains(m));

            if (filters.ContainsKey("Flavor"))
                result = result && filters["Flavor"].Any(m => n.Flavor != string.Empty && m.Contains(n.Flavor));

            if (filters.ContainsKey("Architecture"))
                result = result && filters["Architecture"].Contains(n.Architecture);

            if (filters.ContainsKey("Role"))
                result = result && filters["Role"].Any(m => n.PackageIdentity.Roles.Select(k => k.PackageRole.ToString()).Contains(m));

            if (boolean != null)
            {
                if (boolean.Contains("Is Stable"))
                    result = result && n.PackageIdentity.PackageDetails.Stability == 0;

                if (boolean.Contains("Is Dependency"))
                    result = result && n.PackageIdentity.IsDependency;

                if (boolean.Contains("Is Blocked"))
                    result = result && n.PackageIdentity.IsBlocked;

                if (boolean.Contains("Is Installed"))
                    result = result && n.PackageIdentity.IsInstalled;

                if (boolean.Contains("Is Update"))
                    result = result && n.IsNewVersion;

                if (boolean.Contains("Is Wanted"))
                    result = result && n.PackageIdentity.IsWanted;

                if (boolean.Contains("Is Safe For Work"))
                    result = result && !n.PackageIdentity.PackageDetails.IsNsfw;

                if (boolean.Contains("Is Highest"))
                {
                    var allVersions = View.Cast<PackageItem>().Where(m => m.Name == n.Name && m.Flavor == n.Flavor && m.Architecture == n.Architecture);

                    if (allVersions.Any())
                        result = result && n.Version == allVersions.Max(m => m.PackageIdentity.Version);
                }

                if (Module.IsSolutionOpen)
                {
                    if (boolean.Contains("In Solution"))
                    {
                        result = result && Module.DTE.Solution.Projects.OfType<Project>().Any(m => m.IsSupported() && m.HasPackage(n.PackageIdentity));
                    }

                    if (boolean.Contains("For Development"))
                    {
                        result = result && n.PackageIdentity.GetDeveloperLibraryType() != DeveloperLibraryType.None;
                    }
                }
            }

            if (filters.ContainsKey("Feed URL"))
            {
                var isHighest = false;
                var isInFeeds = false;

                foreach (var feedLocation in filters["Feed URL"].Where(m => m != null))
                {
                    isInFeeds = isInFeeds || Module.PackageManager.IsPackageInFeed(n.PackageIdentity, feedLocation);

                    if (boolean != null && boolean.Contains("Is Highest"))
                    {
                        var allVersions = Module.PackageManager.PackagesInFeeds[feedLocation].Where(m => m.Name == n.Name && m.Flavor == n.Flavor && m.Architecture == n.Architecture);

                        if (allVersions.Any())
                            isHighest = isHighest || n.Version == allVersions.Max(m => m.Version);
                    }
                }

                result = result && isInFeeds;

                if (boolean != null && boolean.Contains("Is Highest"))
                    result = result && isHighest;
            }
            else
            {
                if (boolean != null && boolean.Contains("Is Highest"))
                {
                    var allVersions = Packages.Where(m => m.Name == n.Name && m.Flavor == n.Flavor && m.Architecture == n.Architecture);

                    if (allVersions.Any())
                        result = result && n.Version == allVersions.Max(m => m.PackageIdentity.Version);
                }
            }

            if (filters.ContainsKey("Project"))
            {
                var isInProjects = false;

                foreach (var projectName in filters["Project"].Where(m => m != null))
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
                    result = result && n.PackageIdentity.CanonicalName.PackageName.Contains(searchText);
            }

            return result;
        }

        public void Sort(ListSortDirection sortDirection)
        {
            View.SortDescriptions.Clear();
            View.SortDescriptions.Add(new SortDescription("PackageIdentity.CanonicalName", sortDirection));
        }

        public void Refresh()
        {
            View.Refresh();
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
