namespace CoApp.VisualStudio
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;
    using CoApp.Packaging.Client;
    using CoApp.Packaging.Common;
    using CoApp.Toolkit.Extensions;
    using CoApp.Toolkit.Linq;
    using CoApp.Toolkit.Tasks;
    using CoApp.Toolkit.Win32;

    /// <summary>
    /// Interface between the GUI and CoApp.
    /// </summary>
    public static class CoAppWrapper
    {
        private static Architecture architecture = Architecture.Auto;

        private static readonly List<Task> preCommandTasks = new List<Task>();
        private static readonly List<string> activeDownloads = new List<string>();
        private static readonly PackageManager _packageManager = new PackageManager();
        
        private static readonly ProgressProvider _progressProvider = new ProgressProvider();

        public static CancellationTokenSource CancellationTokenSource { get; set; }

        public static ProgressProvider ProgressProvider { get { return _progressProvider; } }

        /// <summary>
        /// Initializes the CoAppWrapper.
        /// </summary>
        public static void Initialize()
        {
            CancellationTokenSource = new CancellationTokenSource();

            CurrentTask.Events += new DownloadProgress((remoteLocation, location, progress) =>
            {
                if (!activeDownloads.Contains(remoteLocation))
                {
                    activeDownloads.Add(remoteLocation);
                }
                UpdateProgress("Downloading " + remoteLocation.UrlDecode(), progress);
            });

            CurrentTask.Events += new DownloadCompleted((remoteLocation, locallocation) =>
            {
                if (activeDownloads.Contains(remoteLocation))
                {
                    activeDownloads.Remove(remoteLocation);
                }
            });
        }

        /// <summary>
        /// Used for filtering packages by architecture in PackageManagerWindow.
        /// </summary>
        public static void SetArchitecture(string arch)
        {
            architecture = Architecture.Parse(arch);
        }

        /// <summary>
        /// Used for setting package states in PackageManagerWindow.
        /// </summary>
        public static void SetPackageState(IPackage package, string state)
        {
            Task task = preCommandTasks.Continue(() =>
            {
                switch (state)
                {
                    case "wanted":
                        if (package.IsWanted)
                            _packageManager.SetPackageWanted(package.CanonicalName, false);
                        else
                            _packageManager.SetPackageWanted(package.CanonicalName, true);
                        break;
                    case "updatable":
                        _packageManager.SetGeneralPackageInformation(50, package.CanonicalName, "state", PackageState.Updatable.ToString());
                        break;
                    case "upgradable":
                        _packageManager.SetGeneralPackageInformation(50, package.CanonicalName, "state", PackageState.Upgradable.ToString());
                        break;
                    case "blocked":
                        _packageManager.SetGeneralPackageInformation(50, package.CanonicalName, "state", PackageState.Blocked.ToString());
                        break;
                    case "locked":
                        _packageManager.SetGeneralPackageInformation(50, package.CanonicalName, "state", PackageState.DoNotChange.ToString());
                        break;
                }
                
            });

            ContinueTask(task);
        }

        /// <summary>
        /// Used for listing feeds in PackagesSourcesOptionsControl.
        /// </summary>
        public static IEnumerable<Feed> GetFeeds()
        {
            Console.WriteLine("Fetching feed list...");

            IEnumerable<Feed> feeds = null;

            Task task = preCommandTasks.Continue(() => _packageManager.Feeds.Continue(fds => feeds = fds));

            ContinueTask(task);

            return feeds;
        }

        /// <summary>
        /// Used for adding a feed in PackagesSourcesOptionsControl.
        /// </summary>
        public static void AddFeed(string feedLocation)
        {
            Console.WriteLine("Adding feed: " + feedLocation);

            Task task = preCommandTasks.Continue(() => _packageManager.AddSystemFeed(feedLocation));

            try
            {
                ContinueTask(task);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Unwrap().Message);
            }
        }

        /// <summary>
        /// Used for removing a feed in PackagesSourcesOptionsControl.
        /// </summary>
        public static void RemoveFeed(string feedLocation)
        {
            Console.WriteLine("Removing feed: " + feedLocation);

            Task task = preCommandTasks.Continue(() => _packageManager.RemoveSystemFeed(feedLocation));

            try
            {
                ContinueTask(task);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Unwrap().Message);
            }
        }

        /// <summary>
        /// Used for getting packages in SimpleTreeNode.
        /// </summary>
        public static IEnumerable<IPackage> GetPackages(string type, int vsMajorVersion = 0)
        {
            IEnumerable<IPackage> packages = null;
            Filter<IPackage> pkgFilter = null;

            switch (type)
            {
                case "all":
                case "all,dev":
                    packages = QueryPackages(new string[] { "*" }, pkgFilter, null);
                    break;
                case "installed":
                case "installed,dev":
                    pkgFilter = Package.Properties.Installed.Is(true);
                    packages = QueryPackages(new string[] { "*" }, pkgFilter, null);                        
                    break;
                case "updatable":
                case "updatable,dev":
                    packages = Enumerable.Empty<IPackage>();
                    break;
            }

            return FilterPackages(packages, type, vsMajorVersion);
        }

        /// <summary>
        /// Used for getting packages in CoAppWrapperTest.
        /// </summary>
        public static IEnumerable<IPackage> GetPackages(string[] parameters)
        {
            return QueryPackages(parameters, null, null);
        }

        /// <summary>
        /// Used for installing packages in OnlineProvider.
        /// </summary>
        public static void InstallPackage(IPackage package)
        {
            UpdateProgress("Installing packages...", 0);
            Console.Write("Installing packages...");

            try
            {
                Task task = preCommandTasks.Continue(() => InstallPackage(package.CanonicalName));
                ContinueTask(task);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                CancellationTokenSource.Cancel();
            }
        }

        /// <summary>
        /// Used for getting dependents in InstalledProvider.
        /// </summary>
        public static IEnumerable<IPackage> GetDependents(IPackage package)
        {
            return GetPackages("installed").Where(pkg => pkg.Dependencies.Contains(package));
        }

        /// <summary>
        /// Used for refreshing a package in PackagesTreeNodeBase.
        /// </summary>
        public static IPackage GetPackage(CanonicalName canonicalName)
        {
            return QueryPackages(new string[] { canonicalName.PackageName }, null, null).FirstOrDefault();
        }

        /// <summary>
        /// Used for querying packages.
        /// </summary>
        private static IEnumerable<IPackage> QueryPackages(string[] queries,
                                                           Filter<IPackage> pkgFilter,
                                                           Expression<Func<IEnumerable<IPackage>, IEnumerable<IPackage>>> collectionFilter)
        {
            if (!queries.Any() || queries[0] == "*")
            {
                collectionFilter = collectionFilter.Then(p => p.HighestPackages());
            }

            IEnumerable<IPackage> pkgs = null;

            try
            {
                Console.Write("Querying packages...");

                Task task = preCommandTasks.Continue(() => _packageManager.QueryPackages(queries, pkgFilter, collectionFilter, null).Continue(p => pkgs = p));

                ContinueTask(task);
            }
            catch (OperationCanceledException)
            {
                return Enumerable.Empty<IPackage>();
            }

            return pkgs;
        }

        /// <summary>
        /// Used for querying updatable packages.
        /// </summary>
        private static IEnumerable<IPackage> QueryUpdatablePackages(string[] queries,
                                                                   Filter<IPackage> pkgFilter,
                                                                   Expression<Func<IEnumerable<IPackage>, IEnumerable<IPackage>>> collectionFilter)
        {
            if (!queries.Any() || queries[0] == "*")
            {
                collectionFilter = collectionFilter.Then(p => p.HighestPackages());
            }

            IEnumerable<IPackage> pkgs = null;

            try
            {
                Console.Write("Querying packages...");

                Task task = preCommandTasks.Continue(() => _packageManager.QueryPackages(queries, pkgFilter, collectionFilter, null)).Continue(p => pkgs = p);

                ContinueTask(task);
            }
            catch (OperationCanceledException)
            {
                return Enumerable.Empty<IPackage>();
            }
                        
            return pkgs;
        }

        /// <summary>
        /// Used for removing packages in InstalledProvider.
        /// </summary>
        public static void RemovePackage(IPackage package, bool removeDependencies = false)
        {
            UpdateProgress("Uninstalling packages...", 0);
            Console.Write("Removing packages...");

            try
            {
                IEnumerable<CanonicalName> canonicalNames = new List<CanonicalName>() { package.CanonicalName };
                if (removeDependencies)
                {
                    canonicalNames = canonicalNames.Concat(package.Dependencies.Where(p => !(p.Name == "coapp" && p.IsActive)).Select(p => p.CanonicalName));
                }
                Task task = preCommandTasks.Continue(() => RemovePackages(canonicalNames));
                ContinueTask(task);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                CancellationTokenSource.Cancel();
            }
        }

        /// <summary>
        /// Used for installing packages.
        /// </summary>
        private static Task InstallPackage(CanonicalName canonicalName)
        {
            CurrentTask.Events += new PackageInstallProgress((name, progress, overall) => UpdateProgress("Installing " + name + "...", progress));

            return _packageManager.Install(canonicalName);
        }

        /// <summary>
        /// Used for removing packages.
        /// </summary>
        private static Task RemovePackages(IEnumerable<CanonicalName> canonicalNames)
        {
            CurrentTask.Events += new PackageRemoveProgress((name, progress) => UpdateProgress("Uninstalling " + name + "...", progress));

            return _packageManager.RemovePackages(canonicalNames, true);
        }

        /// <summary>
        /// Used for filtering packages.
        /// </summary>
        private static IEnumerable<IPackage> FilterPackages(IEnumerable<IPackage> packages, string type, int vsMajorVersion)
        {
            if (type.Contains("dev"))
            {
                packages = packages.Where(package => package.Name.Contains("-dev"))
                                   .Where(package => package.Name.Contains("vc") ?
                                                     package.Name.Contains("vc" + vsMajorVersion) : true);
            }

            if (architecture != Architecture.Auto)
            {
                packages = packages.Where(package => package.Architecture == architecture);
            }

            return packages;
        }

        /// <summary>
        /// Used for updating progress in ProgressDialog.
        /// </summary>
        private static void UpdateProgress(string message, int progress)
        {
            ProgressProvider.OnProgressAvailable(message, progress);
        }

        private static void ContinueTask(Task task)
        {
            task.ContinueOnCanceled(() =>
            {
                // the task was cancelled, and presumably dealt with.
                Console.WriteLine("Operation Canceled.");
            });

            task.ContinueOnFail((exception) =>
            {
                exception = exception.Unwrap();
                if (!(exception is OperationCanceledException))
                {
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
