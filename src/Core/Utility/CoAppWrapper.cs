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

        private static bool? _x64 = null;
        private static bool? _x86 = null;
        private static bool? _cpuany = null;

        private static readonly List<Task> preCommandTasks = new List<Task>();

        private static List<string> activeDownloads = new List<string>();
        private static Expression<Func<IEnumerable<IPackage>, IEnumerable<IPackage>>> collectionFilter;

        private static readonly PackageManager _packageManager = new PackageManager();

        private static IEnumerable<Package> allPackages, updateablePackages, installedPackages, subPackages;
        private static DateTime allRetrievalTime, updateableRetrievalTime, installedRetrievalTime, subRetrievalTime;

        private static readonly IProgressProvider _progressProvider = new ProgressProvider();

        public static IProgressProvider ProgressProvider
        {
            get
            {
                return _progressProvider;
            }
        }

        public static CancellationTokenSource CancellationTokenSource
        {
            get;
            set;
        }

        public static void UpdateProgress(string message, long progress)
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

        public static void SetArchitecture(string architecture)
        {
            _cpuany = null;
            _x64 = null;
            _x86 = null;

            switch (architecture)
            {
                case "Any":
                    _cpuany = true;
                    break;
                case "x64":
                    _x64 = true;
                    break;
                case "x86":
                    _x86 = true;
                    break;
            }
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
            catch (AggregateException ae)
            {
                foreach (Exception e in ae.InnerExceptions)
                {
                    Console.Error.WriteLine(e.Message);
                }
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
            catch (AggregateException ae)
            {
                foreach (Exception e in ae.InnerExceptions)
                {
                    Console.Error.WriteLine(e.Message);
                }
            }
        }

        public static IEnumerable<Package> GetPackages(string type, int vsMajorVersion)
        {
            IEnumerable<Package> pkgs = null;
            switch (type)
            {
                case "all":
                case "all,dev":
                    {
                        pkgs = GetAllPackages();
                        break;
                    }
                case "installed":
                case "installed,dev":
                    {
                        pkgs = GetInstalledPackages();
                        break;
                    }
                case "updateable":
                case "updateable,dev":
                    {
                        pkgs = GetUpdateablePackages();
                        break;
                    }
                default:
                    {
                        pkgs = new List<Package>();
                        break;
                    }
            }

            if (type.Contains("dev"))
            {
                pkgs = pkgs.Where(pkg => pkg.Name.Contains("-dev"))
                           .Where(pkg => pkg.Name.Contains("vc") ?
                                         pkg.Name.Contains("vc" + vsMajorVersion) : true);
            }

            if (true == _cpuany)
            {
                pkgs = pkgs.Where(pkg => pkg.Architecture == Architecture.Any);
            }
            else if (true == _x64)
            {
                pkgs = pkgs.Where(pkg => pkg.Architecture == Architecture.x64);
            }
            else if (true == _x86)
            {
                pkgs = pkgs.Where(pkg => pkg.Architecture == Architecture.x86);
            }

            return pkgs;
        }
                
        public static IEnumerable<Package> GetAllPackages()
        {
            if (allPackages == null || allPackages.IsEmpty() || DateTime.Compare(DateTime.Now, allRetrievalTime.AddSeconds(30)) > 0)
            {
                allPackages = ListPackages(new string[] { "*" }, null);
                allRetrievalTime = DateTime.Now;
            }

            List<Package> pl = allPackages.ToList();
            for (int i = 10; i < pl.Count; i++)
            {
                pl[i] = GetDetailedPackage(pl[i]);
            }

            return allPackages;
        }

        public static IEnumerable<Package> GetPackages(string[] parameters)
        {
            if (subPackages == null || subPackages.IsEmpty() || DateTime.Compare(DateTime.Now, subRetrievalTime.AddSeconds(30)) > 0)
            {
                subPackages = ListPackages(parameters, null);
                subRetrievalTime = DateTime.Now;
            }

            return subPackages;
        }

        public static IEnumerable<Package> GetUpdateablePackages()
        {
            if (updateablePackages == null || updateablePackages.IsEmpty() || DateTime.Compare(DateTime.Now, updateableRetrievalTime.AddSeconds(30)) > 0)
            {
                updateablePackages = ListUpdateablePackages(new string[] { "*" }, null);
                updateableRetrievalTime = DateTime.Now;
            }

            return updateablePackages;
        }

        public static IEnumerable<Package> GetInstalledPackages()
        {
            if (installedPackages == null || installedPackages.IsEmpty() || DateTime.Compare(DateTime.Now, installedRetrievalTime.AddSeconds(30)) > 0)
            {
                Filter<IPackage> pkgFilter = Package.Properties.Installed.Is(true);
                installedPackages = ListPackages(new string[] { "*" }, pkgFilter);
                installedRetrievalTime = DateTime.Now;
            }

            List<Package> pl = installedPackages.ToList();
            for (int i = 10; i < pl.Count; i++)
            {
                pl[i] = GetDetailedPackage(pl[i]);
            }

            return installedPackages;
        }

        private static IEnumerable<Package> ListUpdateablePackages(string[] parameters, Filter<IPackage> pkgFilter)
        {
            IEnumerable<Package> pkgs = null;

            if (CancellationTokenSource.IsCancellationRequested)
                return new List<Package>();

            Task task = preCommandTasks.Continue(() => _packageManager.QueryPackages(parameters, pkgFilter, collectionFilter, _location)).Continue(p => pkgs = p);

            if (CancellationTokenSource.IsCancellationRequested)
                return new List<Package>();

            ContinueTask(task);
            return pkgs;
        }

        private static IEnumerable<Package> ListPackages(string[] parameters, Filter<IPackage> pkgFilter)
        {
            if (!parameters.Any() || parameters[0] == "*")
            {
                collectionFilter = collectionFilter.Then(p => p.HighestPackages());
            }

            IEnumerable<Package> pkgs = null;

            if (CancellationTokenSource != null && CancellationTokenSource.IsCancellationRequested)
                return new List<Package>();

            Task task = preCommandTasks.Continue(() => _packageManager.QueryPackages(parameters, pkgFilter, collectionFilter, _location)
                .Continue(p => pkgs = p));

            if (CancellationTokenSource != null && CancellationTokenSource.IsCancellationRequested)
                return new List<Package>();

            ContinueTask(task);

            List<Package> pl = pkgs.ToList();
            for (int i = 0; i < pl.Count && i < 10; i++)
            {
                pl[i] = GetDetailedPackage(pl[i]);
            }

            if (CancellationTokenSource != null && CancellationTokenSource.IsCancellationRequested)
                return new List<Package>();

            return pl;
        }

        public static Package GetDetailedPackage(Package package)
        {
            Task task = _packageManager.GetPackageDetails(package.CanonicalName).Continue(detailedPackage =>
            {
                package = detailedPackage;
            });

            ContinueTask(task);

            return package;
        }

        public static IEnumerable<Package> GetDetailedPackages(IEnumerable<Package> packages)
        {
            IEnumerable<Package> pkgs = null;

            packages.Select(package => _packageManager.GetPackageDetails(package.CanonicalName)).ToArray().Continue(detailedPackages =>
            {
                if (pkgs == null)
                    pkgs = detailedPackages;
                else
                    pkgs = pkgs.Concat(detailedPackages);
            });

            return packages;
        }

        public static void RemovePackage(Package package, bool removeDependencies = false)
        {
            UpdateProgress("Removing packages...", 0);

            try
            {
                IEnumerable<CanonicalName> canonicalNames = new List<CanonicalName>() { package.CanonicalName };
                if (removeDependencies)
                {
                    canonicalNames = canonicalNames.Concat(package.Dependencies.Where(p => p.Name != "CoApp.Toolkit").Select(p => p.CanonicalName));
                }
                Task task = preCommandTasks.Continue(() => RemovePackages(canonicalNames));
                ContinueTask(task);
                
                installedPackages = null;
                updateablePackages = null;
                allPackages = null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                CancellationTokenSource.Cancel();
            }
            
        }

        public static void InstallPackage(Package package)
        {
            UpdateProgress("Installing packages...", 0);

            try
            {
                Task task = preCommandTasks.Continue(() => InstallPackage(package.CanonicalName));
                ContinueTask(task);
                installedPackages = null;
                allPackages = null;
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

            return _packageManager.InstallPackage(canonicalName, _force == true);
        }

        private static Task RemovePackage(CanonicalName canonicalName)
        {
            CurrentTask.Events += new PackageRemoveProgress((name, progress) => UpdateProgress("Removing " + name + "...", progress));

            return _packageManager.RemovePackage(canonicalName, true);// (name, progress) => UpdateProgress("Uninstalling " + name + "...", progress));
        }

        private static Task RemovePackages(IEnumerable<CanonicalName> canonicalNames)
        {
            CurrentTask.Events += new PackageRemoveProgress((name, progress) => UpdateProgress("Removing " + name + "...", progress));

            return _packageManager.RemovePackages(canonicalNames, true);
        }

        public static IEnumerable<Package> GetDependents(Package package)
        {
            return GetInstalledPackages().Where(pkg => pkg.Dependencies.Contains(package));
        }

        public static int ContinueTask(Task task)
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

            return 0;
        }

    }
}
