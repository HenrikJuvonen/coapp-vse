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
using CoApp.Toolkit.Win32;
using CoApp.Toolkit.Tasks;

namespace CoApp.VisualStudio
{
    public class CoAppWrapper
    {
        private static bool? _force = null;

        private static string _location = null;
        private static bool? _autoUpgrade = null;

        private static Architecture architecture = Architecture.Auto;

        private static readonly List<Task> preCommandTasks = new List<Task>();

        private static List<string> activeDownloads = new List<string>();

        private static readonly PackageManager _packageManager = new PackageManager();
        
        private static readonly ProgressProvider _progressProvider = new ProgressProvider();

        public static ProgressProvider ProgressProvider
        {
            get
            {
                return _progressProvider;
            }
        }

        public static CancellationTokenSource CancellationTokenSource { get; set; }

        private static void UpdateProgress(string message, long progress)
        {
            ProgressProvider.OnProgressAvailable(message, (int)progress);
        }

        public static void Initialize()
        {
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
                    Console.WriteLine();
                    activeDownloads.Remove(remoteLocation);
                }
            });
        }

        public static void SetArchitecture(string arch)
        {
            architecture = Architecture.Parse(arch);
        }

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

        public static IEnumerable<Feed> ListFeeds()
        {
            Console.WriteLine("Fetching feed list...");

            IEnumerable<Feed> feeds = null;

            Task task = preCommandTasks.Continue(() => _packageManager.Feeds.Continue(fds => feeds = fds));

            ContinueTask(task);

            return feeds;
        }

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
                case "updateable":
                case "updateable,dev":
                    packages = Enumerable.Empty<IPackage>();
                    break;
            }

            return FilterPackages(packages, type, vsMajorVersion);
        }

        public static IEnumerable<IPackage> GetPackages(string[] parameters)
        {
            return QueryPackages(parameters, null, null);
        }

        public static IPackage GetPackage(CanonicalName canonicalName)
        {
            return QueryPackages(new string[] { canonicalName.PackageName }, null, null).FirstOrDefault();
        }

        private static IEnumerable<IPackage> QueryPackages(string[] queries,
                                                           Filter<IPackage> pkgFilter,
                                                           Expression<Func<IEnumerable<IPackage>, IEnumerable<IPackage>>> collectionFilter)
        {
            if (!queries.Any() || queries[0] == "*")
            {
                collectionFilter = collectionFilter.Then(p => p.HighestPackages());
            }

            IEnumerable<IPackage> pkgs = null;

            Task task = preCommandTasks.Continue(() => _packageManager.QueryPackages(queries, pkgFilter, collectionFilter, _location).Continue(p => pkgs = p));

            if (CancellationTokenSource.IsCancellationRequested) return Enumerable.Empty<IPackage>();

            ContinueTask(task);
            return pkgs;
        }

        private static IEnumerable<IPackage> QueryUpdateablePackages(string[] queries,
                                                                   Filter<IPackage> pkgFilter,
                                                                   Expression<Func<IEnumerable<IPackage>, IEnumerable<IPackage>>> collectionFilter)
        {
            if (!queries.Any() || queries[0] == "*")
            {
                collectionFilter = collectionFilter.Then(p => p.HighestPackages());
            }

            IEnumerable<IPackage> pkgs = null;

            Task task = preCommandTasks.Continue(() => _packageManager.QueryPackages(queries, pkgFilter, collectionFilter, _location)).Continue(p => pkgs = p);

            if (CancellationTokenSource.IsCancellationRequested) return Enumerable.Empty<IPackage>();

            ContinueTask(task);
            return pkgs;
        }

        public static void RemovePackage(IPackage package, bool removeDependencies = false)
        {
            UpdateProgress("Uninstalling packages...", 0);

            try
            {
                IEnumerable<CanonicalName> canonicalNames = new List<CanonicalName>() { package.CanonicalName };
                if (removeDependencies)
                {
                    canonicalNames = canonicalNames.Concat(package.Dependencies.Where(p => p.Name != "CoApp.Toolkit").Select(p => p.CanonicalName));
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

        public static void InstallPackage(IPackage package)
        {
            UpdateProgress("Installing packages...", 0);

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

        private static Task InstallPackage(CanonicalName canonicalName)
        {
            CurrentTask.Events += new PackageInstallProgress((name, progress, overall) => UpdateProgress("Installing " + name + "...", progress));

            return _packageManager.Install(canonicalName, _autoUpgrade == true, _force == true);
        }

        private static Task RemovePackages(IEnumerable<CanonicalName> canonicalNames)
        {
            CurrentTask.Events += new PackageRemoveProgress((name, progress) => UpdateProgress("Uninstalling " + name + "...", progress));

            return _packageManager.RemovePackages(canonicalNames, true);
        }

        public static IEnumerable<IPackage> GetDependents(IPackage package)
        {
            return GetPackages("installed").Where(pkg => pkg.Dependencies.Contains(package));
        }

        private static IEnumerable<IPackage> FilterPackages(IEnumerable<IPackage> packages, string type, int vsMajorVersion)
        {
            if (type.Contains("dev"))
            {
                packages = from package in packages
                           where package.Name.Contains("-dev")
                           where package.Name.Contains("vc") ? package.Name.Contains("vc" + vsMajorVersion) : true
                           select package;
            }

            if (architecture != Architecture.Auto)
            {
                packages = packages.Where(package => package.Architecture == architecture);
            }

            return packages;
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
