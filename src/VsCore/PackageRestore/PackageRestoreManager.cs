using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using CoApp.Toolkit.Extensions;
using CoApp.Packaging.Common;

namespace CoApp.VisualStudio.VsCore
{
    [Export(typeof(IPackageRestoreManager))]
    internal class PackageRestoreManager : IPackageRestoreManager
    {
        private readonly ISolutionManager _solutionManager;
        private readonly DTE _dte;

        private readonly WaitDialog _waitDialog;

        private readonly ISettings _settings;
        private string _level;

        private bool _fromActivation;

        [ImportingConstructor]
        public PackageRestoreManager(
            ISolutionManager solutionManager) :
            this(ServiceLocator.GetInstance<DTE>(),
                 solutionManager)
        {
        }

        internal PackageRestoreManager(
            DTE dte,
            ISolutionManager solutionManager)
        {
            Debug.Assert(solutionManager != null);
            _dte = dte;
            _solutionManager = solutionManager;
            _solutionManager.SolutionOpened += OnSolutionOpened;

            _waitDialog = new WaitDialog();

            _settings = new Settings();
        }

        private void LoadSettings()
        {
            _level = _settings.GetValue("coapp", "restore");
        }

        private void OnRunWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            _waitDialog.Show(VsResources.PackageRestoreCheckingMessage);

            e.Cancel = !CheckForMissingPackages();

            if (!e.Cancel && !_waitDialog.IsCanceled)
            {
                if (_level == "notify" && !_fromActivation)
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
                        _waitDialog.IsCanceled = true;
                        return;
                    }
                }

                _waitDialog.Update(VsResources.PackageRestoreInstallingMessage);
                RestoreMissingPackages();
            }
        }
        
        private void OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            CoAppWrapper.ProgressProvider.ProgressAvailable -= _waitDialog.OnProgressAvailable;

            _waitDialog.Hide();

            if (_waitDialog.IsCanceled)
                return;

            if (e.Cancelled)
            {
                if (_fromActivation)
                {
                    MessageHelper.ShowInfoMessage(VsResources.PackageRestoreNoMissingPackages, null);
                }
            }
            else
            {
                // after we're done with restoring packages, do the check again
                if (CheckForMissingPackages())
                {
                    string message = VsResources.PackageRestoreFollowingPackages + Environment.NewLine +
                                        string.Join(Environment.NewLine, GetMissingPackages().Select(n => n.CanonicalName.PackageName));

                    if (_fromActivation || _level != "nothing")
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

        public void BeginRestore(bool fromActivation)
        {
            if (!_solutionManager.IsSolutionOpen)
            {
                throw new InvalidOperationException("solution not available");
            }

            CoAppWrapper.ProgressProvider.ProgressAvailable += _waitDialog.OnProgressAvailable;
            CoAppWrapper.CancellationTokenSource = new System.Threading.CancellationTokenSource();

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
        }

        private IEnumerable<IPackage> GetMissingPackages()
        {
            IEnumerable<IPackage> packages = CoAppWrapper.GetPackages(null, null, VsVersionHelper.VsMajorVersion, false);
            ISet<IPackage> resultPackages = new HashSet<IPackage>();

            foreach (Project p in _solutionManager.GetProjects())
            {
                PackageReferenceFile packageReferenceFile = new PackageReferenceFile(Path.GetDirectoryName(p.FullName) + "/coapp.packages.config");

                IEnumerable<PackageReference> packageReferences = packageReferenceFile.GetPackageReferences();

                foreach (PackageReference package in packageReferences)
                {
                    try
                    {
                        resultPackages.Add(packages.First(pkg => pkg.Name == package.Name &&
                                                                 pkg.Flavor == package.Flavor &&
                                                                 pkg.Version == package.Version &&
                                                                 pkg.Architecture == package.Architecture));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

            }
            return resultPackages.Where(n => !n.IsInstalled);
        }
        
        private void OnSolutionOpened(object sender, EventArgs e)
        {
            LoadSettings();

            if (_level != "nothing")
            {
                BeginRestore(fromActivation: false);
            }
        }
    }
}
