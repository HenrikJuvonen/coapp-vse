namespace CoApp.VisualStudio
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Packaging.Client;
    using Packaging.Common;
    using Packaging.Common.Model;
    using Toolkit.Configuration;
    using Toolkit.Exceptions;
    using Toolkit.Extensions;
    using Toolkit.Linq;
    using Toolkit.Tasks;
    using Toolkit.Win32;

    /// <summary>
    /// Interface between the GUI and CoApp.
    /// </summary>
    public static class CoAppWrapper
    {
        private static readonly ISet<Architecture> ArchitectureFilters = new HashSet<Architecture>();
        private static readonly ISet<PackageRole> RoleFilters = new HashSet<PackageRole>();
        private static bool _onlyHighestVersions = true;
        private static bool _onlyStableVersions = true;
        private static bool _onlyCompatibleFlavors = true;

        private static readonly List<string> ActiveDownloads = new List<string>();
        private static readonly PackageManager PackageManager = new PackageManager();

        public static readonly RegistryView Settings = RegistryView.CoAppUser["coapp_vse"];
        public static readonly ProgressProvider ProgressProvider = new ProgressProvider();

        public static CancellationTokenSource CancellationTokenSource { get; private set; }

        public static string ErrorMessage { get; private set; }

        public static event EventHandler<EventArgs> UpdatesAvailable = delegate { };

        /// <summary>
        /// Initializes the CoAppWrapper.
        /// </summary>
        public static void Initialize()
        {
            ResetFilterStates();
            SaveFilterStates();

            if (Settings["#itemsOnPage"].Value == null)
                Settings["#itemsOnPage"].IntValue = 8;

            if (Settings["#update"].Value == null)
                Settings["#update"].IntValue = 1;

            if (Settings["#restore"].Value == null)
                Settings["#restore"].IntValue = 2;

            if (Settings["#rememberFilters"].Value == null)
                Settings["#rememberFilters"].BoolValue = false;

            CurrentTask.Events += new PackageInstallProgress((name, progress, overall) =>
                ProgressProvider.Update("Installing", name, progress));

            CurrentTask.Events += new PackageRemoveProgress((name, progress) =>
                ProgressProvider.Update("Uninstalling", name, progress));
            
            CurrentTask.Events += new DownloadProgress((remoteLocation, location, progress) =>
            {
                if (!ActiveDownloads.Contains(remoteLocation))
                {
                    ActiveDownloads.Add(remoteLocation);
                }
                ProgressProvider.Update("Downloading", remoteLocation.UrlDecode(), progress);
            });

            CurrentTask.Events += new DownloadCompleted((remoteLocation, locallocation) =>
            {
                if (ActiveDownloads.Contains(remoteLocation))
                {
                    ActiveDownloads.Remove(remoteLocation);
                }
            });

            PackageManager.Elevate().Wait();
        }

        public static void SetNewCancellationTokenSource()
        {
            CancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Set telemetry enabled/disabled.
        /// </summary>
        public static void SetTelemetry(bool state)
        {
            PackageManager.SetTelemetry(state);
        }

        /// <summary>
        /// Get telemetry.
        /// </summary>
        public static bool GetTelemetry()
        {
            return PackageManager.GetTelemetry().Result;
        }

        /// <summary>
        /// Reset filter states to defaults or if they are remembered, get states from settings-file.
        /// </summary>
        public static void ResetFilterStates()
        {
            bool rememberFilters = Settings["#rememberFilters"].BoolValue;

            if (rememberFilters)
            {
                var valueNames = Settings["filters"].ValueNames;

                foreach (var name in valueNames)
                {
                    FilterType filterType;
                    Enum.TryParse(name, true, out filterType);

                    SetFilterState(filterType, Settings["filters", name].BoolValue);
                }
            }
            else
            {
                ArchitectureFilters.Add(Architecture.Any);
                ArchitectureFilters.Add(Architecture.x64);
                ArchitectureFilters.Add(Architecture.x86);

                RoleFilters.Add(PackageRole.Application);
                RoleFilters.Add(PackageRole.Assembly);
                RoleFilters.Add(PackageRole.DeveloperLibrary);

                _onlyHighestVersions = true;
                _onlyStableVersions = true;
                _onlyCompatibleFlavors = false;
            }
        }

        /// <summary>
        /// Save filter states to settings-file.
        /// </summary>
        public static void SaveFilterStates()
        {
            foreach (var filterType in Enum.GetNames(typeof(FilterType)))
            {
                Settings["filters", filterType].BoolValue = GetFilterState((FilterType)Enum.Parse(typeof(FilterType), filterType));
            }
        }

        /// <summary>
        /// Gets filter states.
        /// </summary>
        public static bool GetFilterState(FilterType type)
        {
            switch (type)
            {
                case FilterType.Highest: return _onlyHighestVersions;
                case FilterType.Stable: return _onlyStableVersions;
                case FilterType.Compatible: return _onlyCompatibleFlavors;
                case FilterType.Any: return ArchitectureFilters.Contains(Architecture.Any);
                case FilterType.X64: return ArchitectureFilters.Contains(Architecture.x64);
                case FilterType.X86: return ArchitectureFilters.Contains(Architecture.x86);
                case FilterType.Application: return RoleFilters.Contains(PackageRole.Application);
                case FilterType.Assembly: return RoleFilters.Contains(PackageRole.Assembly);
                case FilterType.DeveloperLibrary: return RoleFilters.Contains(PackageRole.DeveloperLibrary);
            }

            return false;
        }

        /// <summary>
        /// Sets filter states.
        /// </summary>
        public static void SetFilterState(FilterType type, bool state)
        {
            switch (type)
            {
                case FilterType.Highest:
                case FilterType.Stable:
                    SetVersionFilterState(type, state);
                    break;
                case FilterType.Compatible:
                    SetFlavorFilterState(type, state);
                    break;
                case FilterType.Any:
                case FilterType.X64:
                case FilterType.X86:
                    SetArchitectureFilterState(type, state);
                    break;
                case FilterType.Application:
                case FilterType.Assembly:
                case FilterType.DeveloperLibrary:
                    SetRoleFilterState(type, state);
                    break;
            }
        }

        /// <summary>
        /// Sets package states. (wanted, updatable, upgradable, blocked, locked)
        /// </summary>
        public static void SetPackageState(IPackage package, string state)
        {
            Task task = Task.Factory.StartNew(() =>
            {
                switch (state)
                {
                    case "wanted":
                        PackageManager.SetPackageWanted(package.CanonicalName, package.IsWanted);
                        break;
                }
                
            });

            ContinueTask(task);
        }

        /// <summary>
        /// Used for listing feeds in FeedOptionsControl.
        /// </summary>
        public static IEnumerable<string> GetFeedLocations()
        {
            var feeds = PackageManagerSettings.PerFeedSettings.Subkeys;

            return feeds.Select(Toolkit.Text.HttpUtility.UrlDecode);
        }

        /// <summary>
        /// Used for adding a feed in FeedOptionsControl.
        /// </summary>
        public static void AddFeed(string feedLocation)
        {
            if (!feedLocation.IsWebUri())
            {
                feedLocation = feedLocation.CanonicalizePathWithWildcards();
            }

            PackageManagerSettings.PerFeedSettings[feedLocation.UrlEncodeJustBackslashes(), "state"].SetEnumValue(FeedState.Active);
        }

        /// <summary>
        /// Used for removing a feed in FeedOptionsControl.
        /// </summary>
        public static void RemoveFeed(string feedLocation)
        {
            CancellationTokenSource = new CancellationTokenSource();

            Task task = PackageManager.RemoveSystemFeed(feedLocation);

            ContinueTask(task);
        }

        public static IEnumerable<IPackage> GetPackages(IEnumerable<PackageReference> packageReferences)
        {
            var packages = GetPackages();
            var result = new List<IPackage>();

            foreach (var packageReference in packageReferences)
            {
                result.AddRange(packages.Where(n => packageReference.Equals(n)));
            }

            return result;
        }

        /// <summary>
        /// Used for getting packages in SimpleTreeNode.
        /// </summary>
        public static IEnumerable<IPackage> GetPackages(string type = null, string location = null)
        {
            Filter<IPackage> pkgFilter = null;

            if (type == "installed")
                pkgFilter = Package.Filters.InstalledPackages;
            else if (type == "updatable")
                pkgFilter = Package.Filters.PackagesWithUpdateAvailable & Package.Filters.InstalledPackages;

            var packages = QueryPackages(new[] { "*" }, pkgFilter, location);

            if (type == "updatable")
            {
                packages = packages.Select(package => package.AvailableNewestUpdate).Distinct();
                UpdatesAvailable(null, EventArgs.Empty);
            }

            return packages;
        }

        /// <summary>
        /// Used for getting packages in CoAppWrapperTest.
        /// </summary>
        public static IEnumerable<IPackage> GetPackages(string[] parameters)
        {
            return QueryPackages(parameters, null, null);
        }

        /// <summary>
        /// Used for refreshing a package in PackagesTreeNodeBase.
        /// </summary>
        public static IPackage GetPackage(CanonicalName canonicalName)
        {
            return QueryPackages(new[] { canonicalName.PackageName }, null, null).FirstOrDefault();
        }

        /// <summary>
        /// Used for getting dependents in InstalledProvider.
        /// </summary>
        public static IEnumerable<IPackage> GetDependents(IPackage package)
        {
            return GetPackages("installed").Where(pkg => pkg.Dependencies.Contains(package));
        }

        /// <summary>
        /// Used for querying packages.
        /// </summary>
        private static IEnumerable<IPackage> QueryPackages(IEnumerable<string> queries,
                                                           Filter<IPackage> pkgFilter,
                                                           string location)
        {
            ErrorMessage = null;

            IEnumerable<IPackage> packages = null;
            
            Task task = Task.Factory.StartNew(() =>
            {
                var queryTask = PackageManager.QueryPackages(queries, pkgFilter, null, location);

                ContinueTask(queryTask);

                try
                {
                    packages = queryTask.Result;
                }
                catch
                {
                }
            });
            ContinueTask(task);

            return packages ?? Enumerable.Empty<IPackage>();
        }

        /// <summary>
        /// Used for installing packages in OnlineProvider.
        /// </summary>
        public static void InstallPackage(IPackage package)
        {
            InstallPackages(new[] { package });
        }

        /// <summary>
        /// Used for installing packages in OnlineProvider.
        /// </summary>
        public static void InstallPackages(IEnumerable<IPackage> packages)
        {
            ErrorMessage = null;

            Task task = Task.Factory.StartNew(() =>
            {
                IEnumerable<Package> plan = Enumerable.Empty<Package>();

                var packageList = new List<Package>();
                foreach (var p in packages)
                {
                    packageList.Add((Package)p);

                    ProgressProvider.Update("Waiting", p.CanonicalName);
                }

                if (!CancellationTokenSource.IsCancellationRequested)
                {
                    var planTask = PackageManager.IdentifyPackageAndDependenciesToInstall(packageList, false, true);

                    ContinueTask(planTask);

                    try
                    {
                        plan = planTask.Result;
                    }
                    catch
                    {
                    }
                }

                foreach (var p in plan)
                {
                    if (!CancellationTokenSource.IsCancellationRequested)
                    {
                        var installTask = PackageManager.Install(p.CanonicalName, false);

                        ContinueTask(installTask);
                    }
                }
            });

            ContinueTask(task, allowCancellation: false);
        }

        /// <summary>
        /// Used for removing packages in InstalledProvider.
        /// </summary>
        public static void RemovePackage(IPackage package, bool removeDependencies = false)
        {
            ErrorMessage = null;

            IEnumerable<CanonicalName> canonicalNames = new List<CanonicalName> { package.CanonicalName };

            if (removeDependencies)
            {
                canonicalNames = canonicalNames.Concat(package.Dependencies.Where(p => !(p.Name == "coapp" && p.IsActive)).Select(p => p.CanonicalName));
            }

            Task task = Task.Factory.StartNew(() =>
                {
                    if (!CancellationTokenSource.IsCancellationRequested)
                    {
                        ProgressProvider.Update("Waiting", package.CanonicalName);

                        var uninstallTask = PackageManager.RemovePackages(canonicalNames, true);

                        ContinueTask(uninstallTask);
                    }
                });

            ContinueTask(task, allowCancellation: false);
        }

        /// <summary>
        /// Used for filtering packages.
        /// </summary>
        public static IEnumerable<IPackage> FilterPackages(IEnumerable<IPackage> packages, int vsMajorVersion)
        {
            packages = packages.Where(package => package.Roles.Any(n => RoleFilters.Contains(n.PackageRole)));

            if (RoleFilters.Contains(PackageRole.DeveloperLibrary))
            {
                packages = packages.Where(package => package.Roles.Any(n => RoleFilters.Contains(n.PackageRole)));

                if (_onlyCompatibleFlavors)
                    packages = packages.Where(package => !package.Flavor.IsWildcardMatch("*vc*") || package.Flavor.IsWildcardMatch("*vc" + vsMajorVersion + "*"));
            }

            if (_onlyHighestVersions)
            {
                var highestPackages = new List<IPackage>(packages);

                foreach (var package in packages)
                {
                    if (highestPackages.Where(n => n.Name == package.Name && n.Flavor == package.Flavor).Any(n => n.Version > package.Version))
                    {
                        highestPackages.Remove(package);
                    }
                }

                packages = highestPackages;
            }

            if (_onlyStableVersions)
                packages = packages.Where(package => package.PackageDetails.Stability == 0);

            packages = packages.Where(package => ArchitectureFilters.Contains(package.Architecture));

            return packages;
        }


        /// <summary>
        /// Sets architecture filter states. (type: Any, X64, X86)
        /// </summary>
        private static void SetArchitectureFilterState(FilterType type, bool state)
        {
            var architecture = Architecture.Parse(Enum.GetName(typeof(FilterType), type));

            if (state)
                ArchitectureFilters.Add(architecture);
            else
                ArchitectureFilters.Remove(architecture);
        }

        /// <summary>
        /// Sets role filter states. (type: Application, Assembly, DeveloperLibrary)
        /// </summary>
        private static void SetRoleFilterState(FilterType type, bool state)
        {
            PackageRole role = PackageRole.Application;

            switch (type)
            {
                case FilterType.Application:
                    role = PackageRole.Application;
                    break;
                case FilterType.Assembly:
                    role = PackageRole.Assembly;
                    break;
                case FilterType.DeveloperLibrary:
                    role = PackageRole.DeveloperLibrary;
                    break;
            }

            if (state)
                RoleFilters.Add(role);
            else
                RoleFilters.Remove(role);
        }

        /// <summary>
        /// Sets version filter states. (type: Highest, Stable)
        /// </summary>
        private static void SetVersionFilterState(FilterType type, bool state)
        {
            switch (type)
            {
                case FilterType.Highest:
                    _onlyHighestVersions = state;
                    break;
                case FilterType.Stable:
                    _onlyStableVersions = state;
                    break;
            }
        }

        /// <summary>
        /// Sets flavor filter states. (type: Compatible)
        /// </summary>
        private static void SetFlavorFilterState(FilterType type, bool state)
        {
            switch (type)
            {
                case FilterType.Compatible:
                    _onlyCompatibleFlavors = state;
                    break;
            }
        }
        
        private static void ContinueTask(Task task, bool allowCancellation = true)
        {
            try
            {
                task.ContinueOnFail(exception =>
                    {
                        if (exception is CoAppException)
                        {
                            ErrorMessage = exception.Unwrap().Message;
                            ProgressProvider.Update("Error", ErrorMessage);
                        }
                    });

                if (allowCancellation)
                {
                    task.Wait(CancellationTokenSource.Token);
                }
                else
                {
                    task.Wait();
                }
            }
            catch
            {
            }
        }
    }
}
