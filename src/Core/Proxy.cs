using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoApp.Toolkit.Engine.Client;
using CoApp.Toolkit.Extensions;
using CoApp.Toolkit.Win32;
using System.Threading;

namespace CoApp.VsExtension
{
    public class Proxy
    {
        private FourPartVersion? _minVersion = null;
        private FourPartVersion? _maxVersion = null;

        private bool? _installed = null;
        private bool? _active = null;
        private bool? _blocked = null;
        private bool? _latest = null;

        private string _location = null;

        private bool? _x64 = null;
        private bool? _x86 = null;
        private bool? _cpuany = null;

        private bool IsFiltering { get { return (true == _x64) || (true == _x86) || (true == _cpuany); } }

        private readonly List<Task> preCommandTasks = new List<Task>();

        private static List<string> activeDownloads = new List<string>();

        private static IEnumerable<Package> allPackages, updateablePackages, installedPackages;

        private CancellationTokenSource cts;

        public Proxy()
        {
        }

        private readonly EasyPackageManager _easyPackageManager = new EasyPackageManager((itemUri, localLocation, progress) =>
        {
            if (!activeDownloads.Contains(itemUri))
            {
                activeDownloads.Add(itemUri);
            }
        }, (itemUrl, localLocation) =>
        {
            if (activeDownloads.Contains(itemUrl))
            {
                Console.WriteLine();
                activeDownloads.Remove(itemUrl);
            }
        });

        public IEnumerable<Feed> ListFeeds()
        {
            Console.WriteLine("Fetching feed list...");

            IEnumerable<Feed> fds = null;

            Task task = preCommandTasks.Continue(() => _easyPackageManager.Feeds.Continue(feeds => fds = feeds));

            ContinueTask(task);

            return fds;
        }

        public IEnumerable<Package> GetAllPackages()
        {
            if (allPackages == null || allPackages.IsEmpty())
            {
                allPackages = ListPackages(new string[] { "*" });
            }

            List<Package> pl = allPackages.ToList();
            for (int i = 10; i < pl.Count; i++)
            {
                pl[i] = GetDetailedPackage(pl[i]);
            }

            return allPackages;
        }

        public IEnumerable<Package> GetUpdateablePackages()
        {
            if (updateablePackages == null || updateablePackages.IsEmpty())
            {
                updateablePackages = ListUpdateablePackages(new string[] { "*" });
            }

            return updateablePackages;
        }

        public IEnumerable<Package> GetInstalledPackages()
        {
            if (installedPackages == null || installedPackages.IsEmpty())
            {
                _installed = true;
                installedPackages = ListPackages(new string[] { "*" });
                _installed = null;
            }

            return installedPackages;
        }

        public IEnumerable<Package> ListUpdateablePackages(string[] parameters)
        {
            Console.WriteLine("Fetching package list...");

            IEnumerable<Package> pkgs = null;

            if (cts.IsCancellationRequested)
                return new List<Package>();

            Task task = preCommandTasks.Continue(() => _easyPackageManager.GetUpdatablePackages(parameters)).Continue(p => pkgs = p);

            if (cts.IsCancellationRequested)
                return new List<Package>();

            ContinueTask(task);
            return pkgs;
        }
        
        public IEnumerable<Package> ListPackages(string[] parameters)
        {
            if (!parameters.Any() || parameters[0] == "*")
            {
                _latest = true;
            }

            Console.WriteLine("Fetching package list...");

            IEnumerable<Package> pkgs = null;

            if (cts.IsCancellationRequested)
                return new List<Package>();

            Task task = preCommandTasks.Continue(() => _easyPackageManager.GetPackages(parameters, _minVersion, _maxVersion, false, _installed, _active, null, _blocked, _latest, _location)
                .Continue(p => pkgs = p));

            if (cts.IsCancellationRequested)
                return new List<Package>();

            ContinueTask(task);

            List<Package> pl = pkgs.ToList();
            for (int i = 0; i < pl.Count && i < 10; i++)
            {
                pl[i] = GetDetailedPackage(pl[i]);
            }

            if (cts.IsCancellationRequested)
                return new List<Package>();

            return pl;
        }

        public Package GetDetailedPackage(Package package)
        {
            Task task = _easyPackageManager.GetPackageDetails(package.CanonicalName).Continue(detailedPackage =>
            {
                package = detailedPackage;
            });

            ContinueTask(task);

            return package;
        }

        public IEnumerable<Package> GetDetailedPackages(IEnumerable<Package> packages)
        {
            IEnumerable<Package> pkgs = null;

            packages.Select(package => _easyPackageManager.GetPackageDetails(package.CanonicalName)).ToArray().Continue(detailedPackages =>
            {
                if (pkgs == null)
                    pkgs = detailedPackages;
                else
                    pkgs = pkgs.Concat(detailedPackages);
            });

            return packages;
        }

        public void SetCancellationTokenSource(CancellationTokenSource cts)
        {
            this.cts = cts;
        }
        
        public int ContinueTask(Task task)
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
