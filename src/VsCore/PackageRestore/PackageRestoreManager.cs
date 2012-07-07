using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EnvDTE;
using CoApp.Packaging.Common;
using CoApp.Toolkit.Configuration;

namespace CoApp.VisualStudio.VsCore
{
    [Export(typeof(IPackageRestoreManager))]
    internal class PackageRestoreManager : IPackageRestoreManager
    {
        private readonly ISolutionManager _solutionManager;
        private readonly WaitDialog _waitDialog;
        private readonly RegistryView _settings = RegistryView.CoAppUser["coapp_vse"];
        
        /// <summary>
        /// 0 = automatic restore
        /// 1 = notify
        /// 2 = nothing
        /// </summary>
        private int _level;

        private bool _fromActivation;

        [ImportingConstructor]
        public PackageRestoreManager(ISolutionManager solutionManager)
        {
            Debug.Assert(solutionManager != null);
            _solutionManager = solutionManager;
            _solutionManager.SolutionOpened += OnSolutionOpened;

            _waitDialog = new WaitDialog();
        }

        private void OnRunWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            _waitDialog.Show(VsResources.PackageRestoreCheckingMessage);

            e.Cancel = !CheckForMissingPackages();

            if (!e.Cancel && !_waitDialog.IsCancelled)
            {
                if (_level == 1 && !_fromActivation)
                {
                    string missingPackages = string.Join(Environment.NewLine, GetMissingPackages().Select(n => n.CanonicalName.PackageName));

                    bool? result = _waitDialog.ShowQueryDialog(
                        VsResources.PackageRestoreMissingPackagesFound
                        + Environment.NewLine
                        + Environment.NewLine
                        + missingPackages,
                        false);

                    if (result == false)
                    {
                        _waitDialog.IsCancelled = true;
                        return;
                    }
                }

                _waitDialog.Update(VsResources.PackageRestoreInstallingMessage);
                RestoreMissingPackages();
            }
        }
        
        private void OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _waitDialog.Hide();

            CoAppWrapper.ProgressProvider.ProgressAvailable -= _waitDialog.OnProgressAvailable;

            string errorMessage = CoAppWrapper.ErrorMessage;

            if (!_waitDialog.IsCancelled)
            {
                if (e.Cancelled)
                {
                    if (_fromActivation)
                    {
                        var unrecoverable = GetUnrecoverablePackages();

                        if (unrecoverable.Any())
                        {
                            string message = 
                                VsResources.PackageRestoreFollowingPackages + Environment.NewLine +
                                string.Join(Environment.NewLine, GetMissingPackages().Select(n => n.CanonicalName.PackageName).Union(GetUnrecoverablePackages()));

                            MessageHelper.ShowErrorMessage(message, null);
                        }
                        else
                        {
                            MessageHelper.ShowInfoMessage(VsResources.PackageRestoreNoMissingPackages, null);
                        }
                    }
                }
                else
                {
                    // after we're done with restoring packages, do the check again
                    if (CheckForMissingPackages())
                    {
                        string message = errorMessage + (errorMessage != null ? Environment.NewLine + Environment.NewLine : null) +
                            VsResources.PackageRestoreFollowingPackages + Environment.NewLine +
                            string.Join(Environment.NewLine, GetMissingPackages().Select(n => n.CanonicalName.PackageName).Union(GetUnrecoverablePackages()));

                        if (_fromActivation || _level != 2)
                        {
                            MessageHelper.ShowErrorMessage(message, null);
                        }
                    }
                    else
                    {
                        if (_fromActivation)
                        {
                            MessageHelper.ShowInfoMessage(VsResources.PackageRestoreCompleted, null);
                        }
                    }
                }
            }
        }

        public void BeginRestore(bool fromActivation)
        {
            if (!_solutionManager.IsSolutionOpen)
            {
                throw new InvalidOperationException("solution not available");
            }

            CoAppWrapper.SetNewCancellationTokenSource();

            CoAppWrapper.ProgressProvider.ProgressAvailable += _waitDialog.OnProgressAvailable;

            _fromActivation = fromActivation;

            var worker = new BackgroundWorker();
            worker.DoWork += OnRunWorkerDoWork;
            worker.RunWorkerCompleted += OnRunWorkerCompleted;
            worker.RunWorkerAsync();
        }

        public bool CheckForMissingPackages()
        {
            return GetMissingPackages().Any();
        }

        public void RestoreMissingPackages()
        {
            var packages = GetMissingPackages();

            CoAppWrapper.InstallPackages(packages);

            foreach (var p in _solutionManager.GetProjects())
            {
                var packageReferenceFile = new PackageReferenceFile(Path.GetDirectoryName(p.FullName) + "/coapp.packages.config");

                var packageReferences = packageReferenceFile.GetPackageReferences();

                foreach (var packageReference in packageReferences)
                {
                    var removedLibraries = new List<Library>();
                    var addedLibraries = new List<Library>();

                    foreach (var lib in packageReference.Libraries)
                    {
                        removedLibraries.Add(new Library(lib.Name, p.GetName(), lib.ConfigurationName, false));
                        addedLibraries.Add(new Library(lib.Name, p.GetName(), lib.ConfigurationName, lib.IsSelected));
                    }

                    _solutionManager.ManagePackage(packageReference, new[] { p }, removedLibraries);
                    _solutionManager.ManagePackage(packageReference, new[] { p }, addedLibraries);
                }
            }
        }

        private IEnumerable<IPackage> GetMissingPackages()
        {
            var packages = CoAppWrapper.GetPackages(useFilters: false);
            var resultPackages = new HashSet<IPackage>();

            foreach (var p in _solutionManager.GetProjects())
            {
                var packageReferenceFile = new PackageReferenceFile(Path.GetDirectoryName(p.FullName) + "/coapp.packages.config");

                foreach (var packageReference in packageReferenceFile.GetPackageReferences())
                {
                    var pkg = packages.FirstOrDefault(package => package.Name == packageReference.Name &&
                                                                 package.Flavor == packageReference.Flavor &&
                                                                 package.Version == packageReference.Version &&
                                                                 package.Architecture == packageReference.Architecture);

                    if (pkg != null)
                    {
                        resultPackages.Add(pkg);
                    }
                }

            }
            return resultPackages.Where(n => !n.IsInstalled);
        }

        private IEnumerable<string> GetUnrecoverablePackages()
        {
            var packages = CoAppWrapper.GetPackages(null, null, false);
            var unrecoverable = new HashSet<string>();

            foreach (var p in _solutionManager.GetProjects())
            {
                var packageReferenceFile = new PackageReferenceFile(Path.GetDirectoryName(p.FullName) + "/coapp.packages.config");

                foreach (var packageReference in packageReferenceFile.GetPackageReferences())
                {
                    if (!packages.Any(package => package.Name == packageReference.Name &&
                                                 package.Flavor == packageReference.Flavor &&
                                                 package.Version == packageReference.Version &&
                                                 package.Architecture == packageReference.Architecture))
                    {
                        unrecoverable.Add(string.Format("{0}{1}-{2}-{3}", packageReference.Name, packageReference.Flavor, packageReference.Version, packageReference.Architecture));
                    }
                }

            }

            return unrecoverable;
        }
        
        private void OnSolutionOpened(object sender, EventArgs e)
        {
            _level = _settings["#restore"].IntValue;

            if (_level != 2)
            {
                BeginRestore(fromActivation: false);
            }
        }
    }
}
