using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
using CoApp.Packaging.Common;
using CoApp.VisualStudio.VsCore;
using CoApp.VisualStudio.Dialog.PackageManagerUI;

namespace CoApp.VisualStudio.Dialog.Providers
{
    class InstalledProvider : PackagesProviderBase
    {
        private readonly UserNotifierServices _userNotifierServices;
        private readonly ISolutionManager _solutionManager;

        public InstalledProvider(ResourceDictionary resources,
                                ProviderServices providerServices,
                                ISolutionManager solutionManager)
            : base(resources, providerServices)
        {
            _userNotifierServices = providerServices.UserNotifierServices;
            _solutionManager = solutionManager;
        }

        public override string Name
        {
            get
            {
                return Resources.Dialog_InstalledProvider;
            }
        }

        public override float SortOrder
        {
            get
            {
                return 1.0f;
            }
        }

        public override bool CanExecuteCore(PackageItem item)
        {
            return !(item.Name == "coapp" && item.PackageIdentity.IsActive);
        }

        public override bool CanExecuteManage(PackageItem item)
        {
            return item.PackageIdentity.GetDeveloperPackageType() != DeveloperPackageType.None;
        }

        protected override bool ExecuteManage(PackageItem item)
        {
            var replacePackages = AskReplacePackages(item.PackageIdentity);

            if (replacePackages == null)
            {
                // user presses Cancel
                return false;
            }

            var package = item.PackageIdentity;
            var packageReference = new PackageReference(package.Name, package.Flavor, package.Version, package.Architecture, package.GetPackageDirectory(), null, package.GetDeveloperPackageType());

            var selected = _userNotifierServices.ShowProjectSelectorWindow(
                Resources.Dialog_OnlineSolutionInstruction,
                packageReference,
                replacePackages == true);

            if (selected == null)
            {
                // user presses Cancel button on the Solution dialog
                return false;
            }

            var projects = (IEnumerable<Project>)selected[0];
            var libraries = (IEnumerable<Library>)selected[1];

            if (replacePackages == true)
            {
                var differentPackages = GetDifferentPackages(item.PackageIdentity);
                foreach (var pkg in differentPackages)
                {
                    RemovePackagesFromSolution(pkg);
                }
            }

            _solutionManager.ManagePackage(packageReference, projects, libraries);

            return true;
        }

        protected override bool ExecuteCore(PackageItem item)
        {
            var removeFromSolution = AskRemovePackagesFromSolution(item.PackageIdentity);

            switch (removeFromSolution)
            {
                case true:
                    RemovePackagesFromSolution(item.PackageIdentity);
                    break;
                case null:
                    return false;
            }

            ShowWaitDialog();
            CoAppWrapper.RemovePackage(item.PackageIdentity);

            return true;
        }

        private IEnumerable<IPackage> GetDifferentPackages(IPackage package)
        {
            var differentPackages = Enumerable.Empty<PackageReference>();

            foreach (var p in _solutionManager.GetProjects())
            {
                var packageReferenceFile = new PackageReferenceFile(p.GetDirectory() + "/coapp.packages.config");

                differentPackages = packageReferenceFile.GetPackageReferences().Where(pkg => pkg.Name == package.Name &&
                                                                                             pkg.Flavor == package.Flavor &&
                                                                                             pkg.Version != package.Version &&
                                                                                             pkg.Architecture == package.Architecture);
            }

            return CoAppWrapper.GetPackages(differentPackages.Distinct());
        }
        
        private bool? AskReplacePackages(IPackage package)
        {
            var differentPackages = GetDifferentPackages(package);

            if (differentPackages.Any())
            {
                var packageNames = String.Join(Environment.NewLine, differentPackages.Select(p => p.GetPackageNameWithoutPublicKeyToken()));
                var message = String.Format(Resources.Dialog_ReplacePackage, package.CanonicalName.PackageName)
                    + Environment.NewLine
                    + Environment.NewLine
                    + packageNames;

                return _userNotifierServices.ShowQueryMessage(message);
            }

            return true;
        }

        private void RemovePackagesFromSolution(IPackage package)
        {
            var packageReference = new PackageReference(package.Name, package.Flavor, package.Version, package.Architecture, package.GetPackageDirectory(), null, package.GetDeveloperPackageType());

            var viewModel = new SolutionExplorerViewModel(
                ServiceLocator.GetInstance<DTE>().Solution,
                packageReference);

            var projects = viewModel.GetSelectedProjects();
            var libraries = viewModel.GetLibraries();

            var resultLibraries = libraries.Select(lib => new Library(lib.Name, lib.ProjectName, lib.ConfigurationName, false));

            _solutionManager.ManagePackage(packageReference, projects, resultLibraries);
        }

        private bool? AskRemovePackagesFromSolution(IPackage package)
        {
            var projects = from p in _solutionManager.GetProjects()
                           let packageReferenceFile = new PackageReferenceFile(p.GetDirectory() + "/coapp.packages.config")
                           where packageReferenceFile.GetPackageReferences().Any(pkg => pkg.Equals(package))
                           select p;

            bool? remove = false;

            if (projects.Any())
            {
                // show each project on one line
                var projectNames = String.Join(Environment.NewLine, projects.Select(p => p.GetName()));
                var message = String.Format(Resources.Dialog_RemoveFromSolutionMessage, package.CanonicalName.PackageName)
                    + Environment.NewLine
                    + Environment.NewLine
                    + projectNames;

                remove = _userNotifierServices.ShowQueryMessage(message);
            }

            return remove;
        }

        private IEnumerable<Project> GetReferenceProjects(IPackage package)
        {
            var projects = _solutionManager.GetProjects();

            return from project in projects
                   let packageReferenceFile = new PackageReferenceFile(project.GetDirectory() + "/coapp.packages.config")
                   let packageReference = packageReferenceFile.GetPackageReferences().FirstOrDefault(pkg => pkg.Equals(package))
                   where packageReference != null
                   select project;
        }

        public override IVsExtension CreateExtension(IPackage package)
        {
            return new PackageItem(this, package, GetReferenceProjects(package))
            {
                CommandName = Resources.Dialog_UninstallButton
            };
        }

        public override string NoItemsMessage
        {
            get
            {
                return Resources.Dialog_InstalledProviderNoItem;
            }
        }

        public override string ProgressWindowTitle
        {
            get
            {
                return Resources.Dialog_UninstallProgress;
            }
        }

        protected override void FillRootNodes()
        {
            FillRootNodes(installed: true);
        }
    }
}
