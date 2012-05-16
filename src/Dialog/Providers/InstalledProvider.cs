using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using CoApp.Toolkit.Engine.Client;
using System.Threading;
using CoGet.VisualStudio;
using CoGet.Dialog.PackageManagerUI;

namespace CoGet.Dialog.Providers
{
    class InstalledProvider : PackagesProviderBase
    {
        private readonly IUserNotifierServices _userNotifierServices;
        private readonly ISolutionManager _solutionManager;

        private static readonly Dictionary<string, bool> _checkStateCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

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

        public override bool CanExecute(PackageItem item)
        {
            return item.Name != "CoApp.Toolkit";
        }

        private static bool DetermineProjectCheckState(Project project)
        {
            bool checkState;
            if (String.IsNullOrEmpty(project.UniqueName) ||
                !_checkStateCache.TryGetValue(project.UniqueName, out checkState))
            {
                checkState = true;
            }
            return checkState;
        }

        private void SaveProjectCheckStates(IList<Project> selectedProjects)
        {
            var selectedProjectSet = new HashSet<Project>(selectedProjects);

            foreach (Project project in _solutionManager.GetProjects())
            {
                if (!String.IsNullOrEmpty(project.UniqueName))
                {
                    bool checkState = selectedProjectSet.Contains(project);
                    _checkStateCache[project.UniqueName] = checkState;
                }
            }
        }

        protected override bool ExecuteCore2(PackageItem item)
        {
            IList<Project> selectedProjectsList;

            var selectedProjects = _userNotifierServices.ShowProjectSelectorWindow(
                Resources.Dialog_OnlineSolutionInstruction,
                item.PackageIdentity,
                DetermineProjectCheckState,
                ignored => true);

            if (selectedProjects == null)
            {
                // user presses Cancel button on the Solution dialog
                return false;
            }

            selectedProjectsList = selectedProjects.ToList();
            if (selectedProjectsList.Count == 0)
            {
                return false;
            }

            // save the checked state of projects so that we can restore them the next time
            SaveProjectCheckStates(selectedProjectsList);

            // Add package to selected projects here
            // TODO

            // C++
            if (item.Name.Contains("[vc10]") || item.Name.Contains("-common"))
            {
                if (item.Name.Contains("[vc10]"))
                {
                    // lib
                    // $COAPP_LIB = "c:/.apps/lib/"
                    // add $COAPP_LIB to lib-path
                    
                    // ask which libs are added to linker deps
                    // add <package.architecture>/name.lib etc to linker deps
                }
                else if (item.Name.Contains("-common"))
                {
                    // include
                    // $COAPP_INCLUDE += "c:/.apps/Program Files <package.architecture>/Outercurve Foundation/<package.canonicalname>/include
                    // add $COAPP_INCLUDE to include-path
                }
            }

            // C#
            if (false)
            {
                // lib
                // ask which lib-references are added
                // add references
            }

            // add package to packages.config
            // 

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
            ShowProgressWindow();
            UninstallPackage(item, (bool)removeDependencies);
            HideProgressWindow();
            return true;
        }


        protected bool? AskRemoveDependency(Package package, bool checkDependents)
        {
            if (checkDependents)
            {
                // check if there is any other package depends on this package.
                // if there is, throw to cancel the uninstallation

                var dependents = Proxy.GetDependents(package);

                if (!dependents.IsEmpty())
                {
                    ShowProgressWindow();
                    throw new Exception("Uninstall depending packages first:\n" + String.Join("\n", dependents.Select(pkg => pkg.CanonicalName)));
                }
            }

            //var uninstallPackageNames = package.Dependencies.Where(name => !name.Contains("coapp.toolkit"));

            bool? removeDependencies = false;

            /*
            if (uninstallPackageNames.Count() > 0)
            {
                // show each dependency package on one line
                String packageNames = String.Join(Environment.NewLine, uninstallPackageNames);
                String message = String.Format(Resources.Dialog_RemoveDependencyMessage, package)
                        + Environment.NewLine
                        + Environment.NewLine
                        + packageNames;

                removeDependencies = _userNotifierServices.ShowRemoveDependenciesWindow(message);
            }
            */

            return removeDependencies;
        }

        protected void UninstallPackage(PackageItem item, bool removeDependencies)
        {
            Proxy.UninstallPackage(item.PackageIdentity, removeDependencies);
        }

        public override IVsExtension CreateExtension(Package package)
        {
            return new PackageItem(this, package)
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

        protected override string GetProgressMessage(Package package)
        {
            return Resources.Dialog_UninstallProgress + package.ToString();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We want to suppress all errors to show an empty node.")]
        protected override void FillRootNodes()
        {
            RootNode.Nodes.Add(CreateTreeNodeForPackages("installed"));
            RootNode.Nodes.Add(CreateTreeNodeForPackages("installed,dev"));
        }
    }
}
