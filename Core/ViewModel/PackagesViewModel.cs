using CoApp.Packaging.Client;
using CoApp.VSE.Core.Extensions;
using EnvDTE;

namespace CoApp.VSE.Core.ViewModel
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Data;
    using Model;
    using System;

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
            
            Sort(ListSortDirection.Ascending);
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
                    result = result && n.IsUpdate;

                if (boolean.Contains("Is Wanted"))
                    result = result && n.PackageIdentity.IsWanted;
                
                if (boolean.Contains("Is Latest Version"))
                {
                    var allVersions = Packages.Where(m => m.Name == n.Name && m.Flavor == n.Flavor && m.Architecture == n.Architecture);

                    if (allVersions.Any())
                        result = result && n.Version == allVersions.Max(m => m.PackageIdentity.Version);
                }

                if (boolean.Contains("Is Used In Projects"))
                    result = result && Module.IsSolutionOpen && Module.DTE.Solution.Projects.OfType<Project>().Any(m => m.IsSupported() && m.HasPackage(n.PackageIdentity));

                if (boolean.Contains("Is Development Package"))
                    result = result && n.PackageIdentity.GetDeveloperLibraryType() != DeveloperLibraryType.None;
            }

            if (filters.ContainsKey("Feed URL"))
            {
                var isInFeeds = false;

                foreach (var feedLocation in filters["Feed URL"].Where(m => m != null))
                {
                    isInFeeds = isInFeeds || Module.PackageManager.IsPackageInFeed(n.PackageIdentity, feedLocation);
                }

                result = result && isInFeeds;
            }

            if (filters.ContainsKey("Projects"))
            {
                var isInProjects = false;

                foreach (var projectName in filters["Projects"].Where(m => m != null))
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

        public void Sort(ListSortDirection sortDirection)
        {
            SelectedPackage = null;

            View = CollectionViewSource.GetDefaultView(Packages);
            View.Filter = FilterOut;

            var view = (ListCollectionView)View;
            view.CustomSort = sortDirection == ListSortDirection.Ascending ? new SortAscending() : (IComparer)new SortDescending();
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

    public class SortAscending : IComparer
    {
        public int Compare(object x, object y)
        {
            var a = ((PackageItem)x).PackageIdentity;
            var b = ((PackageItem)y).PackageIdentity;

            if (a == null || b == null)
                return 0;

            var aN = a.CanonicalName.PackageName;
            var bN = b.CanonicalName.PackageName;

            return string.Compare(aN, 0, bN, 0, aN.Length + bN.Length);
        }
    }

    public class SortDescending : IComparer
    {
        public int Compare(object x, object y)
        {
            var a = ((PackageItem)y).PackageIdentity;
            var b = ((PackageItem)x).PackageIdentity;

            if (a == null || b == null)
                return 0;

            var aN = a.CanonicalName.PackageName;
            var bN = b.CanonicalName.PackageName;

            return string.Compare(aN, 0, bN, 0, aN.Length + bN.Length);
        }
    }
}
