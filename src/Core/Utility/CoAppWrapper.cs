﻿namespace CoApp.VisualStudio
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
    using CoApp.Packaging.Common.Exceptions;
    using CoApp.Toolkit.Extensions;
    using CoApp.Toolkit.Linq;
    using CoApp.Toolkit.Tasks;
    using CoApp.Toolkit.Win32;

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

        public static CancellationTokenSource CancellationTokenSource { get; set; }

        public static ProgressProvider ProgressProvider { get { return progressProvider; } }

        /// <summary>
        /// Initializes the CoAppWrapper.
        /// </summary>
        public static void Initialize()
        {
            CancellationTokenSource = new CancellationTokenSource();

            ResetFilters();

            packageManager.Elevate().Wait();

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
        }

        public static void ResetFilters()
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

        /// <summary>
        /// Used for filtering packages by architecture in PackageManagerWindow.
        /// </summary>
        /// <param name="enabled">
        /// If true: packages of architectureName are displayed.
        /// </param>
        public static void SetArchitectureFilter(string architectureName, bool enabled)
        {
            Architecture architecture = Architecture.Parse(architectureName);

            if (enabled)
                architectureFilters.Add(architecture);
            else
                architectureFilters.Remove(architecture);
        }

        /// <summary>
        /// Used for filtering packages by role in PackageManagerWindow.
        /// </summary>
        /// <param name="enabled">
        /// If true: packages of roleName are displayed.
        /// </param>
        public static void SetRoleFilter(string roleName, bool enabled)
        {
            PackageRole role = PackageRole.Application;

            switch(roleName)
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

            if (enabled)
                roleFilters.Add(role);
            else
                roleFilters.Remove(role);
        }

        /// <summary>
        /// Used for filtering packages by version in PackageManagerWindow.
        /// </summary>
        /// <param name="enabled">
        /// If true: packages of versionName are displayed.
        /// </param>
        public static void SetVersionFilter(string versionName, bool enabled)
        {
            switch (versionName)
            {
                case "Highest":
                    onlyHighestVersions = enabled;
                    break;
                case "Stable":
                    onlyStableVersions = enabled;
                    break;
            }
        }

        /// <summary>
        /// Used for filtering packages by flavor in PackageManagerWindow.
        /// </summary>
        /// <param name="enabled">
        /// If true: packages of flavorName are displayed.
        /// </param>
        public static void SetFlavorFilter(string flavorName, bool enabled)
        {
            switch (flavorName)
            {
                case "Compatible":
                    onlyCompatibleFlavors = enabled;
                    break;
            }
        }
        
        /// <summary>
        /// Used for setting package states in PackageManagerWindow.
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
        /// Used for listing feeds in FeedsOptionsControl.
        /// </summary>
        public static IEnumerable<Feed> GetFeeds()
        {
            Console.WriteLine("Fetching feed list...");

            IEnumerable<Feed> feeds = null;

            try
            {
                Task task = tasks.Continue(() => packageManager.Feeds.Continue(fds => feeds = fds));

                ContinueTask(task);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Unwrap().Message);
            }

            return feeds;
        }

        /// <summary>
        /// Used for adding a feed in FeedsOptionsControl.
        /// </summary>
        public static void AddFeed(string feedLocation)
        {
            Console.WriteLine("Adding feed: " + feedLocation);

            try
            {
                Task task = tasks.Continue(() => packageManager.AddSystemFeed(feedLocation));

                ContinueTask(task);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Unwrap().Message);
            }
        }

        /// <summary>
        /// Used for removing a feed in FeedsOptionsControl.
        /// </summary>
        public static void RemoveFeed(string feedLocation)
        {
            Console.WriteLine("Removing feed: " + feedLocation);

            try
            {
                Task task = tasks.Continue(() => packageManager.RemoveSystemFeed(feedLocation));

                ContinueTask(task);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Unwrap().Message);
            }
        }

        public static IEnumerable<IPackage> GetPackages(IEnumerable<PackageReference> packageReferences)
        {
            var packages = GetPackages(useFilters: false);
            var result = new List<IPackage>();

            foreach (var pkg in packageReferences)
            {
                result.AddRange(packages.Where(n => n.Name == pkg.Name &&
                                               n.Version.ToString() == pkg.Version &&
                                               n.Architecture.ToString() == pkg.Architecture));
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

            if (onlyHighestVersions)
                collectionFilter = collectionFilter.Then(p => p.HighestPackages());

            packages = QueryPackages(new string[] { "*" }, pkgFilter, collectionFilter, location);

            if (type == "updatable")
            {
                packages = packages.Select(package => package.AvailableNewestUpdate);
            }

            return FilterPackages(packages, vsMajorVersion);
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
            IEnumerable<IPackage> pkgs = null;

            try
            {
                Console.Write("Querying packages...");

                Task task = tasks.Continue(() => packageManager.QueryPackages(queries, pkgFilter, collectionFilter, location).Continue(p => pkgs = p));

                ContinueTask(task);
            }
            catch (OperationCanceledException)
            {
                return Enumerable.Empty<IPackage>();
            }

            return pkgs;
        }

        /// <summary>
        /// Used for installing packages in OnlineProvider.
        /// </summary>
        public static void InstallPackage(IPackage package)
        {
            Console.Write("Installing packages...");
            
            try
            {
                Task task = tasks.Continue(() =>
                {
                    var pkgs = new List<Package>() { (Package)package };

                    var planTask = packageManager.IdentifyPackageAndDependenciesToInstall(pkgs, false, true);

                    planTask.Continue(allPackages =>
                    {
                        foreach (var pkg in allPackages)
                        {
                            try
                            {
                                packageManager.Install(pkg.CanonicalName, false, true, true).Wait();
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Unwrap().Message);
                            }
                        }
                    });
                }
                );
                ContinueTask(task);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                CancellationTokenSource.Cancel();
            }
        }

        /// <summary>
        /// Used for removing packages in InstalledProvider.
        /// </summary>
        public static void RemovePackage(IPackage package, bool removeDependencies = false)
        {
            Console.Write("Removing packages...");

            try
            {
                IEnumerable<CanonicalName> canonicalNames = new List<CanonicalName>() { package.CanonicalName };
                if (removeDependencies)
                {
                    canonicalNames = canonicalNames.Concat(package.Dependencies.Where(p => !(p.Name == "coapp" && p.IsActive)).Select(p => p.CanonicalName));
                }
                Task task = tasks.Continue(() => packageManager.RemovePackages(canonicalNames, true));

                task.ContinueOnFail(exception =>
                {
                    if (exception is OperationCanceledException)
                    {
                        // it's been dealt with.
                        return;
                    }
                    var ae = exception as AggregateException;
                    IEnumerable<FailedPackageRemoveException> fpres = (exception as FailedPackageRemoveException).SingleItemAsEnumerable();
                    if (ae != null)
                    {
                        fpres = from each in ae.InnerExceptions let fpre = each as FailedPackageRemoveException where fpre != null select fpre;
                    }

                    if (!fpres.IsNullOrEmpty())
                    {
                        string message = "The following packages failed to remove:\n";
                        foreach (var failedPackage in fpres)
                        {
                            message += string.Format("   {0}\n", failedPackage.CanonicalName);
                            failedPackage.Cancel();
                        }
                        Console.Write(message);
                        ProgressProvider.Update("Error", message);
                    }
                });

                task.Continue(() =>
                {
                    Console.WriteLine("Done.");
                }).Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                CancellationTokenSource.Cancel();
            }
        }

        /// <summary>
        /// Used for filtering packages.
        /// </summary>
        private static IEnumerable<IPackage> FilterPackages(IEnumerable<IPackage> packages, int vsMajorVersion)
        {
            packages = packages.Where(package => package.Roles.Any(n => roleFilters.Contains(n.PackageRole)));

            if (roleFilters.Contains(PackageRole.DeveloperLibrary))
            {
                packages = packages.Where(package => package.Roles.Any(n => roleFilters.Contains(n.PackageRole)));

                if (onlyCompatibleFlavors)
                    packages = packages.Where(package => package.Flavor.IsWildcardMatch("*vc*") ?
                                                         package.Flavor.IsWildcardMatch("*vc" + vsMajorVersion + "*") : true);
            }

            if (onlyStableVersions)
                packages = packages.Where(package => package.PackageDetails.Stability == 0);

            packages = packages.Where(package => architectureFilters.Contains(package.Architecture));

            return packages;
        }

        private static void ContinueTask(Task task)
        {
            task.ContinueOnCanceled(() =>
            {
                // the task was cancelled, and presumably dealt with.
                ProgressProvider.Update("Error", "Operation Canceled.");
                Console.WriteLine("Operation Canceled.");
            });

            task.ContinueOnFail((exception) =>
            {
                exception = exception.Unwrap();
                if (!(exception is OperationCanceledException))
                {
                    ProgressProvider.Update("Error", exception.Message);
                    Console.WriteLine("Error (???): {0}\r\n\r\n{1}", exception.Message, exception.StackTrace);
                }
                // it's all been handled then.
            });

            task.Continue(() =>
            {
                Console.WriteLine("Done.");
            }).Wait();
        }
    }
}
