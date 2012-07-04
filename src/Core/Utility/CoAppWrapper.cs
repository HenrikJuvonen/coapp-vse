namespace CoApp.VisualStudio
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;
    using CoApp.Toolkit.Collections;
    using CoApp.Packaging.Client;
    using CoApp.Packaging.Common;
    using CoApp.Packaging.Common.Model;
    using CoApp.Toolkit.Exceptions;
    using CoApp.Toolkit.Extensions;
    using CoApp.Toolkit.Linq;
    using CoApp.Toolkit.Tasks;
    using CoApp.Toolkit.Win32;
    using CoApp.Toolkit.Configuration;

    /// <summary>
    /// Interface between the GUI and CoApp.
    /// </summary>
    public static class CoAppWrapper
    {
        private static ISet<Architecture> architectureFilters = new HashSet<Architecture>();
        private static ISet<PackageRole> roleFilters = new HashSet<PackageRole>();
        private static bool onlyHighestVersions = true;
        private static bool onlyStableVersions = true;
        private static bool onlyCompatibleFlavors = true;
        
        private static readonly List<Task> tasks = new List<Task>();
        private static readonly List<string> activeDownloads = new List<string>();
        private static readonly PackageManager packageManager = new PackageManager();
        
        private static readonly ProgressProvider progressProvider = new ProgressProvider();

        private static readonly RegistryView _settings = RegistryView.CoAppUser["coapp_vse"];

        public static CancellationTokenSource CancellationTokenSource { get; private set; }

        public static ProgressProvider ProgressProvider { get { return progressProvider; } }

        public static string ErrorMessage { get; private set; }

        /// <summary>
        /// Initializes the CoAppWrapper.
        /// </summary>
        public static void Initialize()
        {
            ResetFilterStates();
            SaveFilterStates();

            if (_settings["#itemsOnPage"].Value == null)
            {
                _settings["#itemsOnPage"].IntValue = 8;
            }

            if (_settings["#update"].Value == null)
            {
                _settings["#update"].IntValue = 2;
            }

            if (_settings["#restore"].Value == null)
            {
                _settings["#restore"].IntValue = 2;
            }

            if (_settings["#rememberFilters"].Value == null)
            {
                _settings["#rememberFilters"].BoolValue = false;
            }

            CurrentTask.Events += new PackageInstallProgress((name, progress, overall) =>
                ProgressProvider.Update("Installing", name, progress));

            CurrentTask.Events += new PackageRemoveProgress((name, progress) =>
                ProgressProvider.Update("Uninstalling", name, progress));
            
            CurrentTask.Events += new DownloadProgress((remoteLocation, location, progress) =>
            {
                if (!activeDownloads.Contains(remoteLocation))
                {
                    activeDownloads.Add(remoteLocation);
                }
                ProgressProvider.Update("Downloading", remoteLocation.UrlDecode(), progress);
            });

            CurrentTask.Events += new DownloadCompleted((remoteLocation, locallocation) =>
            {
                if (activeDownloads.Contains(remoteLocation))
                {
                    activeDownloads.Remove(remoteLocation);
                }
            });

            packageManager.Elevate().Wait();
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
            packageManager.SetTelemetry(state);
        }

        /// <summary>
        /// Get telemetry.
        /// </summary>
        public static bool GetTelemetry()
        {
            return packageManager.GetTelemetry().Result;
        }

        /// <summary>
        /// Reset filter states to defaults or if they are remembered, get states from settings-file.
        /// </summary>
        public static void ResetFilterStates()
        {
            bool rememberFilters = _settings["#rememberFilters"].BoolValue;

            if (rememberFilters)
            {
                var valueNames = _settings["filters"].ValueNames;

                foreach (var name in valueNames)
                {
                    SetFilterState(name, _settings["filters", name].BoolValue);
                }
            }
            else
            {
                architectureFilters.Add(Architecture.Any);
                architectureFilters.Add(Architecture.x64);
                architectureFilters.Add(Architecture.x86);

                roleFilters.Add(PackageRole.Application);
                roleFilters.Add(PackageRole.Assembly);
                roleFilters.Add(PackageRole.DeveloperLibrary);

                onlyHighestVersions = true;
                onlyStableVersions = true;
                onlyCompatibleFlavors = true;
            }
        }

        /// <summary>
        /// Save filter states to settings-file.
        /// </summary>
        public static void SaveFilterStates()
        {
            _settings["filters", "Highest"].BoolValue = GetFilterState("Highest");
            _settings["filters", "Stable"].BoolValue = GetFilterState("Stable");
            _settings["filters", "Compatible"].BoolValue = GetFilterState("Compatible");
            _settings["filters", "any"].BoolValue = GetFilterState("any");
            _settings["filters", "x86"].BoolValue = GetFilterState("x86");
            _settings["filters", "x64"].BoolValue = GetFilterState("x64");
            _settings["filters", "Application"].BoolValue = GetFilterState("Application");
            _settings["filters", "Assembly"].BoolValue = GetFilterState("Assembly");
            _settings["filters", "DeveloperLibrary"].BoolValue = GetFilterState("DeveloperLibrary");
        }

        /// <summary>
        /// Gets filter states.
        /// </summary>
        /// <param name="name">
        /// Highest, Stable, Compatible, any, x64, x86, Application, Assembly, DeveloperLibrary
        /// </param>
        public static bool GetFilterState(string name)
        {
            switch (name)
            {
                case "Highest": return onlyHighestVersions;
                case "Stable": return onlyStableVersions;
                case "Compatible": return onlyCompatibleFlavors;
                case "any": return architectureFilters.Contains(Architecture.Any);
                case "x64": return architectureFilters.Contains(Architecture.x64);
                case "x86": return architectureFilters.Contains(Architecture.x86);
                case "Application": return roleFilters.Contains(PackageRole.Application);
                case "Assembly": return roleFilters.Contains(PackageRole.Assembly);
                case "DeveloperLibrary": return roleFilters.Contains(PackageRole.DeveloperLibrary);
            }

            return false;
        }

        /// <summary>
        /// Sets filter states.
        /// </summary>
        /// <param name="name">
        /// Highest, Stable, Compatible, any, x64, x86, Application, Assembly, DeveloperLibrary
        /// </param>
        public static void SetFilterState(string name, bool state)
        {
            switch (name)
            {
                case "Highest":
                case "Stable":
                    SetVersionFilterState(name, state);
                    break;
                case "Compatible":
                    SetFlavorFilterState(name, state);
                    break;
                case "any":
                case "x64":
                case "x86":
                    SetArchitectureFilterState(name, state);
                    break;
                case "Application":
                case "Assembly":
                case "DeveloperLibrary":
                    SetRoleFilterState(name, state);
                    break;
            }
        }

        /// <summary>
        /// Sets package states. (wanted, updatable, upgradable, blocked, locked)
        /// </summary>
        public static void SetPackageState(IPackage package, string state)
        {
            Task task = tasks.Continue(() =>
            {
                switch (state)
                {
                    case "wanted":
                        if (package.IsWanted)
                            packageManager.SetPackageWanted(package.CanonicalName, false);
                        else
                            packageManager.SetPackageWanted(package.CanonicalName, true);
                        break;
                    case "updatable":
                        packageManager.SetGeneralPackageInformation(50, package.CanonicalName, "state", PackageState.Updatable.ToString());
                        break;
                    case "upgradable":
                        packageManager.SetGeneralPackageInformation(50, package.CanonicalName, "state", PackageState.Upgradable.ToString());
                        break;
                    case "blocked":
                        packageManager.SetGeneralPackageInformation(50, package.CanonicalName, "state", PackageState.Blocked.ToString());
                        break;
                    case "locked":
                        packageManager.SetGeneralPackageInformation(50, package.CanonicalName, "state", PackageState.DoNotChange.ToString());
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
            IEnumerable<string> feeds = PackageManagerSettings.PerFeedSettings.Subkeys;

            var decodedFeeds = new List<string>();

            foreach (var f in feeds)
            {
                decodedFeeds.Add(CoApp.Toolkit.Text.HttpUtility.UrlDecode(f));
            }

            return decodedFeeds ?? Enumerable.Empty<string>();
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

            Task task = packageManager.RemoveSystemFeed(feedLocation);

            ContinueTask(task);
        }

        public static IEnumerable<IPackage> GetPackages(IEnumerable<PackageReference> packageReferences)
        {
            var packages = GetPackages(useFilters: false);
            var result = new List<IPackage>();

            foreach (var packageReference in packageReferences)
            {
                result.AddRange(packages.Where(n => n.Name == packageReference.Name &&
                                                    n.Flavor == packageReference.Flavor &&
                                                    n.Version == packageReference.Version &&
                                                    n.Architecture == packageReference.Architecture));
            }

            return result;
        }

        /// <summary>
        /// Used for getting packages in SimpleTreeNode.
        /// </summary>
        public static IEnumerable<IPackage> GetPackages(string type = null, string location = null, int vsMajorVersion = 0, bool useFilters = true)
        {
            IEnumerable<IPackage> packages = null;
            Filter<IPackage> pkgFilter = null;
            XList<Expression<Func<IEnumerable<IPackage>, IEnumerable<IPackage>>>> collectionFilter = null;

            if (type == "installed")
                pkgFilter = Package.Properties.Installed.Is(true);
            else if (type == "updatable")
                pkgFilter = !Package.Properties.AvailableNewestUpdate.Is(null) & Package.Properties.Installed.Is(true);

            if (!useFilters)
            {
                return QueryPackages(new string[] { "*" }, pkgFilter, collectionFilter, location);
            }

            packages = QueryPackages(new string[] { "*" }, pkgFilter, collectionFilter, location);

            if (type == "updatable")
            {
                packages = packages.Select(package => package.AvailableNewestUpdate);
            }

            return packages;
        }

        /// <summary>
        /// Used for getting packages in CoAppWrapperTest.
        /// </summary>
        public static IEnumerable<IPackage> GetPackages(string[] parameters)
        {
            return QueryPackages(parameters, null, null, null);
        }

        /// <summary>
        /// Used for refreshing a package in PackagesTreeNodeBase.
        /// </summary>
        public static IPackage GetPackage(CanonicalName canonicalName)
        {
            return QueryPackages(new string[] { canonicalName.PackageName }, null, null, null).FirstOrDefault();
        }

        /// <summary>
        /// Used for getting dependents in InstalledProvider.
        /// </summary>
        public static IEnumerable<IPackage> GetDependents(IPackage package)
        {
            return GetPackages("installed", null, 0, false).Where(pkg => pkg.Dependencies.Contains(package));
        }

        /// <summary>
        /// Used for querying packages.
        /// </summary>
        private static IEnumerable<IPackage> QueryPackages(string[] queries,
                                                           Filter<IPackage> pkgFilter,
                                                           XList<Expression<Func<IEnumerable<IPackage>, IEnumerable<IPackage>>>> collectionFilter,
                                                           string location)
        {
            ErrorMessage = null;

            IEnumerable<IPackage> packages = null;
            
            Task task = tasks.Continue(() =>
            {
                var queryTask = packageManager.QueryPackages(queries, pkgFilter, collectionFilter, location);

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

            Task task = tasks.Continue(() =>
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
                    var planTask = packageManager.IdentifyPackageAndDependenciesToInstall(packageList, false, true);

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
                        var installTask = packageManager.Install(p.CanonicalName, false);

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

            IEnumerable<CanonicalName> canonicalNames = new List<CanonicalName>() { package.CanonicalName };

            if (removeDependencies)
            {
                canonicalNames = canonicalNames.Concat(package.Dependencies.Where(p => !(p.Name == "coapp" && p.IsActive)).Select(p => p.CanonicalName));
            }

            Task task = tasks.Continue(() =>
                {
                    if (!CancellationTokenSource.IsCancellationRequested)
                    {
                        ProgressProvider.Update("Waiting", package.CanonicalName);

                        var uninstallTask = packageManager.RemovePackages(canonicalNames, true);

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
            packages = packages.Where(package => package.Roles.Any(n => roleFilters.Contains(n.PackageRole)));

            if (roleFilters.Contains(PackageRole.DeveloperLibrary))
            {
                packages = packages.Where(package => package.Roles.Any(n => roleFilters.Contains(n.PackageRole)));

                if (onlyCompatibleFlavors)
                    packages = packages.Where(package => package.Flavor.IsWildcardMatch("*vc*") ?
                                                         package.Flavor.IsWildcardMatch("*vc" + vsMajorVersion + "*") : true);
            }

            if (onlyHighestVersions)
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

            if (onlyStableVersions)
                packages = packages.Where(package => package.PackageDetails.Stability == 0);

            packages = packages.Where(package => architectureFilters.Contains(package.Architecture));

            return packages;
        }


        /// <summary>
        /// Sets architecture filter states. (names: any, x64, x86)
        /// </summary>
        /// <param name="state">
        /// If true: packages of architectureName are displayed.
        /// </param>
        private static void SetArchitectureFilterState(string architectureName, bool state)
        {
            Architecture architecture = Architecture.Parse(architectureName);

            if (state)
                architectureFilters.Add(architecture);
            else
                architectureFilters.Remove(architecture);
        }

        /// <summary>
        /// Sets role filter states. (names: Application, Assembly, DeveloperLibrary)
        /// </summary>
        /// <param name="state">
        /// If true: packages of roleName are displayed.
        /// </param>
        private static void SetRoleFilterState(string roleName, bool state)
        {
            PackageRole role = PackageRole.Application;

            switch (roleName)
            {
                case "Application":
                    role = PackageRole.Application;
                    break;
                case "Assembly":
                    role = PackageRole.Assembly;
                    break;
                case "DeveloperLibrary":
                    role = PackageRole.DeveloperLibrary;
                    break;
            }

            if (state)
                roleFilters.Add(role);
            else
                roleFilters.Remove(role);
        }

        /// <summary>
        /// Sets version filter states. (names: Highest, Stable)
        /// </summary>
        /// <param name="state">
        /// If true: packages of versionName are displayed.
        /// </param>
        private static void SetVersionFilterState(string versionName, bool state)
        {
            switch (versionName)
            {
                case "Highest":
                    onlyHighestVersions = state;
                    break;
                case "Stable":
                    onlyStableVersions = state;
                    break;
            }
        }

        /// <summary>
        /// Sets flavor filter states. (names: Compatible)
        /// </summary>
        /// <param name="state">
        /// If true: packages of flavorName are displayed.
        /// </param>
        private static void SetFlavorFilterState(string flavorName, bool state)
        {
            switch (flavorName)
            {
                case "Compatible":
                    onlyCompatibleFlavors = state;
                    break;
            }
        }
        
        private static void ContinueTask(Task task, bool allowCancellation = true)
        {
            try
            {
                task.ContinueOnFail(exception =>
                    {
                        if (exception is CoAppException ||
                            exception is OperationCompletedBeforeResultException)
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
