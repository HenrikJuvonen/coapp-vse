using System;
using System.IO;
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
        protected readonly UserNotifierServices _userNotifierServices;
        protected readonly ISolutionManager _solutionManager;

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

        public override bool RefreshOnNodeSelection
        {
            get
            {
                // only refresh if the current node doesn't have any extensions
                return (SelectedNode == null || SelectedNode.Extensions.Count == 0);
            }
        }

        public override bool CanExecuteCore(PackageItem item)
        {
            return !(item.Name == "coapp" && item.PackageIdentity.IsActive);
        }

        public override bool CanExecuteManage(PackageItem item)
        {
            return item.Name.Contains("common") ||
                   item.PackageIdentity.Flavor.IsWildcardMatch("*vc" + VsVersionHelper.VsMajorVersion + "*") ||
                   item.PackageIdentity.Flavor.IsWildcardMatch("*net*") ||
                   item.Name == "coapp" ||
                   item.Name == "coapp.devtools";
        }

        protected override bool ExecuteManage(PackageItem item)
        {
            string type = item.Type;

            PackageReference packageReference = new PackageReference(item.Name, item.PackageIdentity.Version, item.PackageIdentity.Architecture, type, item.Path);

            var selected = _userNotifierServices.ShowProjectSelectorWindow(
                Resources.Dialog_OnlineSolutionInstruction,
                packageReference);

            if (selected == null)
            {
                // user presses Cancel button on the Solution dialog
                return false;
            }

            IEnumerable<Project> projects = (IEnumerable<Project>)selected[0];
            IEnumerable<Library> libraries = (IEnumerable<Library>)selected[1];

            _solutionManager.ManagePackage(packageReference, projects, libraries);

            return true;
        }

        protected override bool ExecuteCore(PackageItem item)
        {
            bool? removeDependencies = AskRemoveDependency(item.PackageIdentity, checkDependents: true);

            if (removeDependencies == null)
            {
                // user presses Cancel
                return false;
            }

            bool? removeFromSolution = AskRemovePackagesFromSolution(item.PackageIdentity);

            if (removeFromSolution == true)
            {
                // user presses Yes

                PackageReference packageReference = new PackageReference(item.Name, item.PackageIdentity.Version, item.PackageIdentity.Architecture, item.Type, item.Path);

                var viewModel = new SolutionExplorerViewModel(
                    ServiceLocator.GetInstance<DTE>().Solution,
                    packageReference);

                IEnumerable<Project> projects = viewModel.GetSelectedProjects();
                IEnumerable<Library> libraries = viewModel.GetLibraries();

                foreach (Library lib in libraries)
                {
                    lib.IsSelected = false;
                }

                _solutionManager.ManagePackage(packageReference, projects, libraries);
            }
            else if (removeFromSolution == null)
            {
                // user presses Cancel
                return false;
            }

            ShowProgressWindow();
            RemovePackage(item, (bool)removeDependencies);
            HideProgressWindow();
            return true;
        }

        public bool? AskRemovePackagesFromSolution(IPackage package)
        {
            IEnumerable<IPackage> installedPackages = CoAppWrapper.GetPackages("installed");
            List<Project> projects = new List<Project>();

            foreach (Project p in _solutionManager.GetProjects())
            {
                PackageReferenceFile packageReferenceFile = new PackageReferenceFile(p.GetDirectory() + "/coapp.config");

                if (packageReferenceFile.GetPackageReferences().Any(pkg => pkg.Name == package.Name &&
                                                                           pkg.Version == package.Version &&
                                                                           pkg.Architecture == package.Architecture))
                {
                    projects.Add(p);
                }

            }

            bool? remove = false;

            if (projects.Any())
            {
                // show each project on one line
                String projectNames = String.Join(Environment.NewLine, projects.Select(p => p.GetName()));
                String message = String.Format(Resources.Dialog_RemoveFromSolutionMessage, package.CanonicalName.PackageName)
                        + Environment.NewLine
                        + Environment.NewLine
                        + projectNames;

                remove = _userNotifierServices.ShowQuestionWindow(message);
            }

            return remove;
            
        }

        protected bool? AskRemoveDependency(IPackage package, bool checkDependents)
        {
            if (checkDependents)
            {
                // check if there is any other package depends on this package.
                // if there is, throw to cancel the uninstallation

                var dependents = CoAppWrapper.GetDependents(package);

                if (dependents.Any())
                {
                    ShowProgressWindow();
                    throw new Exception("Uninstall depending packages first:\n" + String.Join("\n", dependents.Select(pkg => pkg.CanonicalName.PackageName)));
                }
            }

            //var uninstallPackageNames = package.Dependencies.Where(name => name != "coapp");

            bool? removeDependencies = false;

            /*
            if (uninstallPackageNames.Count() > 0)
            {
                // show each dependency package on one line
                String packageNames = String.Join(Environment.NewLine, uninstallPackageNames);
                String message = String.Format(Resources.Dialog_RemoveDependencyMessage, package.CanonicalName.PackageName)
                        + Environment.NewLine
                        + Environment.NewLine
                        + packageNames;

                removeDependencies = _userNotifierServices.ShowQuestionWindow(message);
            }
            */

            return removeDependencies;
        }

        protected void RemovePackage(PackageItem item, bool removeDependencies)
        {
            CoAppWrapper.RemovePackage(item.PackageIdentity, removeDependencies);
        }

        public IEnumerable<Project> GetReferenceProjects(IPackage package)
        {
            var projects = _solutionManager.GetProjects();

            List<Project> result = new List<Project>();

            foreach (Project project in projects)
            {
                PackageReferenceFile packageReferenceFile = new PackageReferenceFile(Path.GetDirectoryName(project.FullName) + "/coapp.config");

                PackageReference packageReference = packageReferenceFile.GetPackageReferences()
                    .FirstOrDefault(pkg => pkg.Name == package.Name &&
                                    pkg.Version == package.Version.ToString() &&
                                    pkg.Architecture == package.Architecture.ToString());

                if (packageReference != null)
                {
                    result.Add(project);
                }
            }

            return result;
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

        protected override string GetProgressMessage(IPackage package)
        {
            return Resources.Dialog_UninstallProgress + package.ToString();
        }

        protected override void FillRootNodes()
        {
            RootNode.Nodes.Add(CreateTreeNodeForPackages("installed"));
            RootNode.Nodes.Add(CreateTreeNodeForPackages("installed,dev"));
        }
    }
}
