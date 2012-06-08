using System;
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
        private readonly IVsThreadedWaitDialogFactory _waitDialogFactory;
        private readonly DTE _dte;

        [ImportingConstructor]
        public PackageRestoreManager(
            ISolutionManager solutionManager) :
            this(ServiceLocator.GetInstance<DTE>(),
                 solutionManager,
                 ServiceLocator.GetGlobalService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>())
        {
        }

        internal PackageRestoreManager(
            DTE dte,
            ISolutionManager solutionManager,
            IVsThreadedWaitDialogFactory waitDialogFactory)
        {
            Debug.Assert(solutionManager != null);
            _dte = dte;
            _solutionManager = solutionManager;
            _waitDialogFactory = waitDialogFactory;
            _solutionManager.ProjectAdded += OnProjectAdded;
            _solutionManager.SolutionOpened += OnSolutionOpened;
        }
        
        public void BeginRestore(bool fromActivation)
        {
            if (!_solutionManager.IsSolutionOpen)
            {
                throw new InvalidOperationException("solution not available");
            }

            if (!CheckForMissingPackages())
                return;

            Exception exception = null;

            IVsThreadedWaitDialog2 waitDialog;
            _waitDialogFactory.CreateInstance(out waitDialog);
            try
            {
                waitDialog.StartWaitDialog(
                    VsResources.DialogTitle,
                    VsResources.PackageRestoreWaitMessage,
                    String.Empty, 
                    varStatusBmpAnim: null, 
                    szStatusBarText: null,
                    iDelayToShowDialog: 0,
                    fIsCancelable: false,
                    fShowMarqueeProgress: true);

                RestoreMissingPackages();

                // after we're done with restoring packages, do the check again
                if (!CheckForMissingPackages())
                {
                    throw new Exception("Some packages were not restored.");
                }
            }
            catch (Exception ex)
            {
                exception = ex;
                ExceptionHelper.WriteToActivityLog(exception);
            }
            finally
            {
                int canceled;
                waitDialog.EndWaitDialog(out canceled);
            }

            if (fromActivation)
            {
                if (exception != null)
                {
                    // show error message
                    MessageHelper.ShowErrorMessage(
                        VsResources.PackageRestoreErrorMessage +
                            Environment.NewLine +
                            Environment.NewLine +
                            exception.Unwrap().Message,
                        VsResources.DialogTitle);
                }
                else
                {
                    // show success message
                    MessageHelper.ShowInfoMessage(
                        VsResources.PackageRestoreCompleted,
                        VsResources.DialogTitle);
                }
            }
        }

        public event EventHandler<PackagesMissingStatusEventArgs> PackagesMissingStatusChanged = delegate { };

        public bool CheckForMissingPackages()
        {
            bool missing = GetMissingPackages().Any();
            PackagesMissingStatusChanged(this, new PackagesMissingStatusEventArgs(missing));
            return missing;
        }

        public void RestoreMissingPackages()
        {
            var packages = GetMissingPackages();

            foreach (var package in packages)
            {
                CoAppWrapper.InstallPackage(package);
            }
        }

        private IEnumerable<IPackage> GetMissingPackages()
        {
            IEnumerable<IPackage> packages = CoAppWrapper.GetPackages("online", null, VsVersionHelper.VsMajorVersion);
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
            BeginRestore(fromActivation: false);
        }

        private void OnProjectAdded(object sender, ProjectEventArgs e)
        {
            CheckForMissingPackages();
        }
    }
}
