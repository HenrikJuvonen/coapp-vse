using System.Threading;
using System.Threading.Tasks;
using CoApp.Packaging.Client;
using CoApp.Packaging.Common;
using CoApp.Packaging.Common.Exceptions;
using CoApp.Toolkit.Configuration;
using CoApp.Toolkit.Extensions;
using CoApp.Toolkit.Linq;
using CoApp.Toolkit.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using CoApp.Toolkit.Text;
using CoApp.VSE.Core.Extensions;
using CoApp.VSE.Core.ViewModel;
using EnvDTE;
using CoApp.VSE.Core.Model;
using System.Windows;

namespace CoApp.VSE.Core.Packaging
{
    public class PackageManager
    {
        private PackagesViewModel _packagesViewModel; 

        public bool IsQuerying { get; private set; }
        public bool IsQueryCompleted { get { return _packagesViewModel != null; } }

        private readonly Dictionary<Package, List<Mark>> _marks = new Dictionary<Package, List<Mark>>();
        public Dictionary<string, List<string>> Filters = new Dictionary<string, List<string>>();

        private bool _initialUpdateCheckCompleted;
        
        public Package GetHighestInstalledPackage(Package package)
        {
            var packages = PackagesViewModel.Packages.Where(m => m.Name == package.Name && m.Flavor == package.Flavor && m.Architecture == package.Architecture);

            var highestInstalled = packages.FirstOrDefault(n => n.IsHighestInstalled);

            return highestInstalled != null ? highestInstalled.PackageIdentity : null;
        }

        public PackagesViewModel QueryPackages()
        {
            return PackagesViewModel;
        }

        public PackagesViewModel PackagesViewModel
        {
            get
            {
                if (_packagesViewModel != null)
                    return _packagesViewModel;

                IsQuerying = true;

                SetNewCancellationTokenSource();

                _packagesViewModel = new PackagesViewModel(GetPackagesFromAllFeeds());

                if (!_initialUpdateCheckCompleted)
                {
                    GetPackages("updatable");
                    _initialUpdateCheckCompleted = true;
                }

                IsQuerying = false;
                
                return _packagesViewModel;
            }
        }

        public void Reset()
        {
            if (IsQueryCompleted)
            {
                _packagesViewModel = null;
                IsQuerying = false;
                PackagesInFeeds.Clear();
                _marks.Clear();
            }
        }

        public bool IsAnyUpdates
        {
            get { return PackagesViewModel.Packages.Any(n => n.IsUpdate); }
        }

        public bool IsAnyMarked
        {
            get { return _marks.Any(); }
        }

        public void ClearVisualStudioMarks()
        {
            var marks = VisualStudioPlan.ToArray();

            foreach (var package in marks)
            {
                _marks.Remove(package);

                foreach (var packageItem in PackagesViewModel.Packages.Where(n => n.PackageIdentity == package))
                {
                    packageItem.SetStatus();
                    packageItem.ItemBackground = null;

                    packageItem.InSolution = Module.IsSolutionOpen && Module.DTE.Solution.Projects.OfType<Project>().Any(m => m.IsSupported() && m.HasPackage(packageItem.PackageIdentity));
                }
            }
        }

        public void ClearMarks(bool forceClearInSolution)
        {
            _marks.Clear();

            foreach (var packageItem in PackagesViewModel.Packages)
            {
                packageItem.SetStatus();
                packageItem.ItemBackground = null;

                packageItem.InSolution = !forceClearInSolution && Module.IsSolutionOpen && Module.DTE.Solution.Projects.OfType<Project>().Any(m => m.IsSupported() && m.HasPackage(packageItem.PackageIdentity));
            }
        }

        public void AddMarks(IEnumerable<Package> packages, Mark mark)
        {
            foreach (var package in packages)
            {
                AddMark(package, mark);
            }
        }

        public void RemoveMarks(IEnumerable<Package> packages, Mark mark)
        {
            foreach (var package in packages)
            {
                RemoveMark(package, mark);
            }
        }

        public void AddMark(Package package, Mark mark)
        {
            if (_marks.ContainsKey(package))
            {
                _marks[package].Add(mark);
            }
            else
            {
                _marks.Add(package, new List<Mark> { mark });
            }
        }

        public bool RemoveMark(Package package, Mark mark)
        {
            if (_marks.ContainsKey(package) && _marks[package].Contains(mark))
            {
                _marks[package].Remove(mark);

                if (!_marks[package].Any())
                {
                    _marks.Remove(package);
                }

                return true;
            }
            return false;
        }

        public IEnumerable<Package> RemovePlan
        {
            get
            {
                return _marks
                    .Where(n => n.Value.Contains(Mark.DirectReinstall) ||
                                n.Value.Contains(Mark.DirectUpgrade) ||
                                n.Value.Contains(Mark.DirectRemove) ||
                                n.Value.Contains(Mark.DirectCompletelyRemove) ||
                                n.Value.Contains(Mark.IndirectRemove) ||
                                n.Value.Contains(Mark.IndirectCompletelyRemove))
                    .Select(n => n.Key)
                    .Where(package => !(package.Name == "coapp" && package.IsActive) && !package.PackageState.HasFlag(PackageState.DoNotChange)).ToArray();
            }
        }

        public IEnumerable<Package> TrimPlan
        {
            get
            {
                return PackagesInFeeds.SelectMany(n => n.Value).Where(n => n.IsTrimable && n.IsInstalled).Except(DoNotTrimPlan).ToArray();
            }
        }

        private IEnumerable<Package> DoNotTrimPlan
        {
            get
            {
                return (from package in InstallPlan select IdentifyOwnDependencies(package))
                       .SelectMany(n => n)
                       .Union(InstallPlan);
            }
        }

        public IEnumerable<Package> InstallPlan
        {
            get
            {
                return _marks
                    .Where(n => n.Value.Contains(Mark.DirectInstall) ||
                                n.Value.Contains(Mark.DirectReinstall) ||
                                n.Value.Contains(Mark.IndirectInstall) ||
                                n.Value.Contains(Mark.IndirectReinstall) ||
                                n.Value.Contains(Mark.IndirectUpdate) ||
                                n.Value.Contains(Mark.IndirectUpgrade))
                    .Select(n => n.Key)
                    .Where(package => !package.IsBlocked && (!package.IsInstalled || (package.IsInstalled && RemovePlan.Contains(package)))).ToArray();
            }
        }

        public IEnumerable<Package> UpdatePlan
        {
            get
            {
                return _marks
                    .Where(n => n.Value.Contains(Mark.DirectUpdate))
                    .Select(n => n.Key).ToArray();
            }
        }

        public IEnumerable<Package> VisualStudioPlan
        {
            get
            {
                return _marks
                    .Where(n => n.Value.Contains(Mark.DirectVisualStudio) ||
                                n.Value.Contains(Mark.IndirectVisualStudio))
                    .Select(n => n.Key).ToArray();
            }
        }

        private readonly CoApp.Packaging.Client.PackageManager _pkm = new CoApp.Packaging.Client.PackageManager();

        public readonly RegistryView Settings = RegistryView.CoAppUser["coapp_vse"];

        private CancellationTokenSource CancellationTokenSource { get; set; }

        public event Action Elevated = delegate { };
        public event Action FeedsUpdated = delegate { };
        public event EventHandler<UpdatesAvailableEventArgs> UpdatesAvailable = delegate { };
        public event EventHandler<ProgressEventArgs> ProgressAvailable = delegate { };

        public event EventHandler<LogEventArgs> Message = delegate { };
        public event EventHandler<LogEventArgs> Warning = delegate { };
        public event EventHandler<LogEventArgs> Error = delegate { };

        public Dictionary<string, IList<Package>> PackagesInFeeds = new Dictionary<string, IList<Package>>();

        public readonly List<string> Downloads = new List<string>();

        public PackageManager()
        {
            CurrentTask.Events += new PackageInstallProgress((name, progress, overall) =>
                ProgressAvailable(null, new ProgressEventArgs(name, "Installing", 50 + progress / 2)));

            CurrentTask.Events += new PackageInstalled(name =>
                ProgressAvailable(null, new ProgressEventArgs(name, "Installed", 100)));

            CurrentTask.Events += new PackageRemoveProgress((name, progress) =>
                ProgressAvailable(null, new ProgressEventArgs(name, "Removing", progress)));

            CurrentTask.Events += new PackageRemoved(name =>
                ProgressAvailable(null, new ProgressEventArgs(name, "Removed", 100)));

            CurrentTask.Events += new UnableToDownloadPackage(name =>
                Warning(null, new LogEventArgs("Unable to download package (" + name + ")")));

            CurrentTask.Events += new DownloadProgress((remoteLocation, location, progress) =>
                {
                    var decodedUrl = remoteLocation.UrlDecode();

                    try
                    {
                        CanonicalName result = new CanonicalName(decodedUrl);
                        ProgressAvailable(null, new ProgressEventArgs(result, "Downloading", progress / 2));
                    }
                    catch
                    {
                        if (!Downloads.Contains(decodedUrl))
                        {
                            Downloads.Add(decodedUrl);
                            Message(null, new LogEventArgs("Downloading " + decodedUrl));
                        }
                    }
                });

            CurrentTask.Events += new DownloadCompleted((remoteLocation, locallocation) =>
                {
                    var decodedUrl = remoteLocation.UrlDecode();

                    try
                    {
                        CanonicalName result = new CanonicalName(decodedUrl);
                        ProgressAvailable(null, new ProgressEventArgs(result, "Downloaded", 50));
                    }
                    catch
                    {
                        if (Downloads.Contains(decodedUrl))
                        {
                            Downloads.Remove(decodedUrl);
                        }
                        Message(null, new LogEventArgs("Downloaded " + decodedUrl));
                    }
                });

        }

        public void InitializeSettings()
        {
            if (Settings["#update"].Value == null)
                Settings["#update"].IntValue = 2;

            if (Settings["#restore"].Value == null)
                Settings["#restore"].IntValue = 2;

            if (Settings["#rememberFilters"].Value == null)
                Settings["#rememberFilters"].BoolValue = false;

            if (Settings["#autoEnd"].Value == null)
                Settings["#autoEnd"].BoolValue = false;

            if (Settings["#autoTrim"].Value == null)
                Settings["#autoTrim"].BoolValue = false;

            if (Settings["#showConsole"].Value == null)
                Settings["#showConsole"].BoolValue = false;

            if (Settings["#showTrayIcon"].Value == null)
                Settings["#showTrayIcon"].BoolValue = false;

            if (Settings["#startInTray"].Value == null)
                Settings["#startInTray"].BoolValue = false;

            if (Settings["#showNotifications"].Value == null)
                Settings["#showNotifications"].BoolValue = true;

            if (Settings["#closeToTray"].Value == null)
                Settings["#closeToTray"].BoolValue = false;

            if (Settings["#theme"].Value == null)
                Settings["#theme"].StringValue = "Light";
        }

        public bool Elevate()
        {
            var isElevated = false;

            ContinueTask(_pkm.Elevate().Continue(() => { Elevated(); isElevated = true;}).ContinueOnCanceled(() => Error(this, new LogEventArgs("Not elevated."))));

            return isElevated;
        }

        public void Cancel()
        {
            CancellationTokenSource.Cancel();
        }

        public bool IsPackageInFeed(Package package, string feedLocation)
        {
            return PackagesInFeeds.ContainsKey(feedLocation) && PackagesInFeeds[feedLocation].Contains(package);
        }

        public void SetAllFeedsStale()
        {
            _pkm.SetAllFeedsStale().Wait();
        }

        public void SetNewCancellationTokenSource()
        {
            CancellationTokenSource = new CancellationTokenSource();
        }

        public void SetTelemetry(bool state)
        {
            _pkm.SetTelemetry(state);
        }

        public bool GetTelemetry()
        {
            return _pkm.GetTelemetry().Result;
        }

        public void SaveFilters(Dictionary<string, List<string>> filters)
        {
            if (!Settings["#rememberFilters"].BoolValue)
                return;

            foreach (var key in Settings["filters"].Subkeys)
            {
                Settings["filters"].DeleteSubkey(key);
            }

            foreach (var key in filters.Keys.Where(key => key != "Search"))
            {
                if (filters[key].IsNullOrEmpty())
                {
                    Settings["filters"][key].Value = null;
                }
                else
                {
                    Settings["filters"][key].DeleteValues();

                    foreach (var value in filters[key])
                    {
                        var index = String.Format("{0}", filters[key].IndexOf(value));
                        Settings["filters"][key, index].StringValue = value;
                    }
                }
            }
        }

        public Dictionary<string, List<string>> LoadFilters()
        {
            var result = new Dictionary<string, List<string>>();

            if (Settings["#rememberFilters"].BoolValue)
            {
                if (Settings.Subkeys.Contains("filters"))
                {
                    foreach (var key in Settings["filters"].Subkeys)
                    {
                        var list = new List<string>();

                        foreach (var index in Settings["filters"][key].ValueNames)
                        {
                            var value = Settings["filters"][key, index].StringValue;

                            if (!String.IsNullOrEmpty(value))
                                list.Add(value);
                        }

                        result.Add(key, list);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Sets package states. (blocked, locked, wanted)
        /// </summary>
        public void SetPackageState(Package package, string state)
        {
            ContinueTask(Task.Factory.StartNew(() =>
            {
                switch (state)
                {
                    case "Wanted":
                        _pkm.SetPackageWanted(package.CanonicalName, !package.IsWanted);
                        RefreshPackage(package);
                        break;
                }

            }));
        }

        private Package GetPackage(CanonicalName canonicalName)
        {
            return QueryPackages(new[] { canonicalName.PackageName }, null, null).FirstOrDefault();
        }

        private void RefreshPackage(Package package)
        {
            var newPackage = GetPackage(package.CanonicalName);

            foreach (var list in PackagesInFeeds.Select(n => n.Value))
            {
                list.Remove(package);
                list.Add(newPackage);
            }

            PackagesViewModel.ReplacePackage(package, newPackage);
        }


        public IEnumerable<string> GetFeedLocations()
        {
            var feeds = PackageManagerSettings.PerFeedSettings.Subkeys;

            return feeds.Select(HttpUtility.UrlDecode);
        }

        public void AddFeed(string feedLocation)
        {
            if (!Elevate())
                return;

            CancellationTokenSource.Cancel();
            CancellationTokenSource = new CancellationTokenSource();

            ContinueTask(_pkm.AddSystemFeed(feedLocation));

            FeedsUpdated();
        }

        public void RemoveFeed(string feedLocation)
        {
            if (!Elevate())
                return;

            CancellationTokenSource.Cancel();
            CancellationTokenSource = new CancellationTokenSource();

            ContinueTask(_pkm.RemoveSystemFeed(feedLocation));

            // Add default feeds if removed all feeds
            if (GetFeedLocations().IsNullOrEmpty())
                AddFeed(null);

            FeedsUpdated();
        }

        public bool IsPackageHighestInstalled(Package package)
        {
            return package.NewerPackages.Any() ? package.NewerPackages.Any(m => m.IsInstalled && m.Version == package.NewerPackages.Max(k => k.Version)) : package.IsInstalled;
        }

        public IEnumerable<Package> IdentifyDependencies(Package package)
        {
            return IdentifyPackageAndDependencies(package).Except(new[] { package });
        }

        public IEnumerable<Package> IdentifyPackageAndDependencies(Package package)
        {
            var dependencies = new List<Package> { package };

            try
            {
                Parallel.ForEach(package.Dependencies, dependency =>
                {
                    if (dependency != null && PackagesInFeeds.Any(n => n.Value.Any(m => m.CanonicalName == dependency.CanonicalName)))
                    {
                        var subdependencies = IdentifyPackageAndDependencies((Package) dependency);
                        lock (this)
                        {
                            dependencies.AddRange(subdependencies);
                        }
                    }
                });
            }
            catch
            {
            }

            return dependencies.Distinct();
        }

        public IEnumerable<Package> IdentifyDependents(Package package)
        {
            var dependents = new List<Package>();

            try
            {
                Parallel.ForEach(GetPackagesFromAllFeeds(), n =>
                {
                    if (n != package && n.Dependencies.Any(m => m.CanonicalName == package.CanonicalName))
                    {
                        lock (this)
                        {
                            dependents.Add((Package)n);
                        }
                    }
                });
            }
            catch
            {
            }

            return dependents;
        }

        public IEnumerable<Package> IdentifyOwnDependencies(Package package)
        {
            var dependencies = new List<Package>();

            try
            {
                Parallel.ForEach(package.Dependencies, dependency =>
                {
                    if (dependency != null && PackagesInFeeds.Any(n => n.Value.Any(m => m.CanonicalName == dependency.CanonicalName)))
                    {
                        lock (this)
                        {
                            dependencies.Add((Package)dependency);
                        }
                    }
                });
            }
            catch
            {
            }

            return dependencies.Where(n => n != null);
        }

        private IEnumerable<Package> GetPackagesFromAllFeeds()
        {
            var packages = new List<Package>();

            Parallel.Invoke(
                () => Parallel.ForEach(GetFeedLocations(), feedLocation =>
                {
                    var pkgs = GetPackages(location: feedLocation);
                    lock (this)
                    {
                        packages.AddRange(pkgs);
                    }
                }),
                () =>
                {
                    var pkgs = GetPackages("installed");
                    lock (this)
                    {
                        packages.AddRange(pkgs);
                    }
                });

            return packages.Distinct();
        }

        public IEnumerable<Package> GetPackages(string type = null, string location = null)
        {
            Filter<IPackage> pkgFilter = null;

            if (type == "installed")
                pkgFilter = Package.Filters.InstalledPackages;
            else if (type == "updatable")
                pkgFilter = Package.Filters.PackagesWithUpdateAvailable & Package.Filters.InstalledPackages;

            IEnumerable<Package> packages = QueryPackages(new[] { "*" }, pkgFilter, location).ToArray();

            if (type == "installed")
            {
                lock (this)
                {
                    if (PackagesInFeeds.ContainsKey(""))
                        PackagesInFeeds.Remove("");

                    PackagesInFeeds.Add("", packages.ToList());
                }
            }

            if (type == "updatable")
            {
                packages = packages.Select(package => package.AvailableNewestUpdate as Package).Distinct();

                if (!packages.IsNullOrEmpty())
                    UpdatesAvailable(null, new UpdatesAvailableEventArgs(packages.Count()));
            }

            if (location != null)
            {
                lock (this)
                {
                    if (PackagesInFeeds.ContainsKey(location))
                        PackagesInFeeds.Remove(location);

                    PackagesInFeeds.Add(location, packages.ToList());
                }
            }

            if (packages.Contains(null) || packages.IsNullOrEmpty())
                return Enumerable.Empty<Package>();

            return packages;
        }
        
        private IEnumerable<Package> QueryPackages(IEnumerable<string> queries, Filter<IPackage> pkgFilter, string location)
        {
            IEnumerable<Package> packages = null;

            ContinueTask(Task.Factory.StartNew(() => 
                ContinueTask(_pkm.QueryPackages(queries, pkgFilter, null, location).Continue(n => packages = n))));

            return packages ?? Enumerable.Empty<Package>();
        }

        private void DownloadPackages(IEnumerable<Package> packages)
        {
            ContinueTask(Task.Factory.StartNew(() =>
            {
                Message(null, new LogEventArgs("Downloading packages..."));

                foreach (var package in packages)
                {
                    if (CancellationTokenSource.IsCancellationRequested)
                        return;

                    ContinueTask(DownloadPackage(package));
                }
            }));
        }

        private Task DownloadPackage(IPackage package)
        {
            var impl = new PackageManagerResponseImpl();

            return Task.Factory.StartNew(() => 
                impl.RequireRemoteFile(package.CanonicalName, package.RemoteLocations, PackageManagerSettings.CoAppCacheDirectory + "\\packages", false));
        }

        public void InstallPackages(IEnumerable<Package> packages)
        {
            if (!packages.Any())
                return;

            DownloadPackages(packages);

            if (CancellationTokenSource.IsCancellationRequested)
                return;

            ContinueTask(Task.Factory.StartNew(() =>
            {
                Message(null, new LogEventArgs("Installing packages..."));

                foreach (var p in packages)
                {
                    if (CancellationTokenSource.IsCancellationRequested)
                        return;

                    ContinueTask(_pkm.Install(p.CanonicalName, false, false, false));
                }
            }), false);
        }

        public void RemovePackages(IEnumerable<Package> packages)
        {
            if (!packages.Any())
                return;
            
            ContinueTask(Task.Factory.StartNew(() =>
            {
                Message(null, new LogEventArgs("Removing packages..."));

                if (CancellationTokenSource.IsCancellationRequested)
                    return;
                foreach (var p in packages)
                {
                    if (CancellationTokenSource.IsCancellationRequested)
                        return;

                    ContinueTask(_pkm.RemovePackage(p.CanonicalName, true));
                }
            }), false);
        }

        private void ContinueTask(Task task, bool allowCancellation = true)
        {
            try
            {
                task.ContinueOnFail(exception =>
                {
                    if (exception is FailedPackageRemoveException)
                    {
                        var e = (FailedPackageRemoveException)exception;
                        Error(null, new LogEventArgs(e.Reason + "(" + e.CanonicalName + ")"));
                    }
                    else if (exception is AggregateException)
                    {
                        var unwrapped = UnwrapException(exception);
                        string lastMessage = null;

                        foreach (var ex in unwrapped)
                        {
                            string message;

                            if (ex is RequiresPermissionException)
                            {
                                var e = (RequiresPermissionException) ex;
                                message = "Permission required: " + e.PolicyName + ".";
                            }
                            else
                            {
                                message = exception.Message;
                            }

                            if (lastMessage != message)
                            {
                                Error(null, new LogEventArgs(message));
                                lastMessage = message;
                            }
                        }
                    }
                    else
                    {
                        Error(null, new LogEventArgs(exception.Message));
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
                // nothing to do here
            }
        }

        private IEnumerable<Exception> UnwrapException(Exception exception)
        {
            var exceptions = new List<Exception>();

            var aggregate = exception as AggregateException;

            if (aggregate == null)
            {
                exceptions.Add(exception);
            }
            else
            {
                foreach (var ex in aggregate.InnerExceptions)
                {
                    exceptions.AddRange(UnwrapException(ex));
                }
            }

            return exceptions;
        }
    }
}
