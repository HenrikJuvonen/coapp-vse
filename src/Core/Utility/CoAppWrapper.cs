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

        private static readonly PackageManager _packageManager = new PackageManager();
        
        private static readonly IProgressProvider _progressProvider = new ProgressProvider();

        public static IProgressProvider ProgressProvider
        {
            get
            {
                return _progressProvider;
            }
        }

        public static CancellationTokenSource CancellationTokenSource { get; set; }

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

        public static IEnumerable<IPackage> GetPackages(string type, int vsMajorVersion)
        {
            IEnumerable<IPackage> pkgs = null;
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
                        pkgs = new List<IPackage>();
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
                
        public static IEnumerable<IPackage> GetAllPackages()
        {
            return ListPackages(new string[] { "*" }, null, null);
        }

        public static IEnumerable<IPackage> GetPackages(string[] parameters)
        {
            return ListPackages(parameters, null, null);
        }

        public static IEnumerable<IPackage> GetUpdateablePackages()
        {
            return Enumerable.Empty<IPackage>(); //ListUpdateablePackages(new string[] { "*" }, null, null);
        }

        public static IEnumerable<IPackage> GetInstalledPackages()
        {
            var pkgFilter = Package.Properties.Installed.Is(true);
            return ListPackages(new string[] { "*" }, pkgFilter, null);
        }

        private static IEnumerable<IPackage> ListUpdateablePackages(string[] parameters, 
                                                                   Filter<IPackage> pkgFilter,
                                                                   Expression<Func<IEnumerable<IPackage>, IEnumerable<IPackage>>> collectionFilter)
        {
            if (!parameters.Any() || parameters[0] == "*")
            {
                collectionFilter = collectionFilter.Then(p => p.HighestPackages());
            }

            IEnumerable<IPackage> pkgs = null;
            
            Task task = preCommandTasks.Continue(() => _packageManager.QueryPackages(parameters, pkgFilter, collectionFilter, _location)).Continue(p => pkgs = p);

            if (CancellationTokenSource.IsCancellationRequested) return Enumerable.Empty<IPackage>();

            ContinueTask(task);
            return pkgs;
        }

        private static IEnumerable<IPackage> ListPackages(string[] parameters,
                                                         Filter<IPackage> pkgFilter,
                                                         Expression<Func<IEnumerable<IPackage>, IEnumerable<IPackage>>> collectionFilter)
        {
            if (!parameters.Any() || parameters[0] == "*")
            {
                collectionFilter = collectionFilter.Then(p => p.HighestPackages());
            }

            IEnumerable<IPackage> pkgs = null;

            Task task = preCommandTasks.Continue(() => _packageManager.QueryPackages(parameters, pkgFilter, collectionFilter, _location).Continue(p => pkgs = p));

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
