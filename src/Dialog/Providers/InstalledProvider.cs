using System;
using System.IO;
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
using Microsoft.VisualStudio.VCProjectEngine;

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

        private static bool? DetermineCheckState(Package package, Project project, string config, string lib)
        {
            PackageReferenceFile packageReferenceFile = new PackageReferenceFile(Path.GetDirectoryName(project.FullName) + "/packages.config");

            IEnumerable<PackageReference> packageReferences = packageReferenceFile.GetPackageReferences();

            bool hasLibraries = false;
            bool projectHasPackage = false;

            foreach (PackageReference p in packageReferences)
            {
                if (p.Name != package.Name)
                    continue;

                projectHasPackage = true;

                if (p.Libraries != null && !p.Libraries.IsEmpty())
                    hasLibraries = true;

                foreach (Library l in p.Libraries)
                {
                    if (l.Configuration == config && l.Name == lib)
                    {
                        return true;
                    }
                }
            }

            if (config == null && lib == null && projectHasPackage)
            {
                if (hasLibraries)
                {
                    return null;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        protected override bool ExecuteCore2(PackageItem item)
        {
            string type = item.Name.Contains("[vc10]") ? "vc,lib" :
                item.Name.Contains("-common") ? "vc" : "";

            var selected = _userNotifierServices.ShowProjectSelectorWindow(
                Resources.Dialog_OnlineSolutionInstruction,
                item.PackageIdentity,
                DetermineCheckState,
                ignored => true,
                type);

            if (selected == null)
            {
                // user presses Cancel button on the Solution dialog
                return false;
            }
            IEnumerable<Project> projects = (IEnumerable<Project>)selected[0];
            IEnumerable<Library> libraries = (IEnumerable<Library>)selected[1];

            // C++
            switch (type)
            {
                case "vc,lib":
                    {
                        // vc,lib

                        VCProject vcProject;
                        IVCCollection configs;
                        VCConfiguration config;
                        VCLinkerTool linker;

                        foreach (Project p in _solutionManager.GetProjects())
                        {
                            if (!p.GetProjectTypeGuids().Contains(VsConstants.CppProjectTypeGuid))
                                break;

                            vcProject = (VCProject)p.Object;
                            configs = vcProject.Configurations;

                            IEnumerable<Library> projectLibraries = libraries.Where(lib => lib.Project == p.Name);

                            foreach (string configName in (Array)p.ConfigurationManager.ConfigurationRowNames)
                            {
                                IEnumerable<Library> configLibraries = projectLibraries.Where(lib => lib.Configuration == configName);

                                config = configs.Item(configName);
                                linker = config.Tools.Item("Linker Tool");
                                
                                string dir = @"c:\apps\lib\";
                                /*
                                IList<string> dirs = linker.AdditionalLibraryDirectories.Split(';').Where(n => !n.IsEmpty()).ToList();

                                if (!dirs.Contains(dir) && projects.Contains(p))
                                {
                                    dirs.Add(dir);
                                }
                                else if (projects.IsEmpty())
                                {
                                    dirs.Remove(dir);
                                }

                                linker.AdditionalLibraryDirectories = string.Join(";", dirs);*/

                                // List of current deps
                                List<string> deps = linker.AdditionalDependencies.Split(' ').Where(n => !n.IsEmpty()).ToList();

                                // List of removed deps
                                List<string> removed = configLibraries.Where(n => !n.IsSelected).Select(n => dir + item.Architecture + "\\" + n.Name).ToList();

                                // List of added deps
                                List<string> added = configLibraries.Where(n => n.IsSelected).Select(n => dir + item.Architecture + "\\" + n.Name).ToList();

                                List<string> result = deps.Except(removed).Union(added).ToList();

                                linker.AdditionalDependencies = string.Join(" ", result);
                            }

                            PackageReferenceFile packageReferenceFile = new PackageReferenceFile(Path.GetDirectoryName(p.FullName) + "/packages.config");

                            if (projects.Contains(p))
                            {
                                packageReferenceFile.AddEntry(item.Name, item.Version, item.Architecture, projectLibraries.Where(n => n.IsSelected));
                            }
                            else
                            {
                                packageReferenceFile.DeleteEntry(item.Name, item.Version, item.Architecture);
                            }
                        }
                        break;
                    }
                case "vc":
                    {
                        // vc,include

                        VCProject vcProject;
                        IVCCollection configs;
                        VCConfiguration config;
                        VCCLCompilerTool compiler;

                        foreach (Project p in _solutionManager.GetProjects())
                        {
                            if (!p.GetProjectTypeGuids().Contains(VsConstants.CppProjectTypeGuid))
                                break;

                            vcProject = (VCProject)p.Object;
                            configs = vcProject.Configurations;

                            foreach (string configName in (Array)p.ConfigurationManager.ConfigurationRowNames)
                            {
                                config = configs.Item(configName);
                                compiler = config.Tools.Item("VCCLCompilerTool");

                                string dir = @"c:\apps\Program Files\Outercurve Foundation\" + item.PackageIdentity.CanonicalName + @"\include\";

                                IList<string> dirs = compiler.AdditionalIncludeDirectories.Split(';').Where(n => !n.IsEmpty()).ToList();

                                if (!dirs.Contains(dir) && projects.Contains(p))
                                {
                                    dirs.Add(dir);
                                }
                                else if (!projects.Contains(p))
                                {
                                    dirs.Remove(dir);
                                }

                                compiler.AdditionalIncludeDirectories = string.Join(";", dirs);
                            }

                            PackageReferenceFile packageReferenceFile = new PackageReferenceFile(Path.GetDirectoryName(p.FullName) + "/packages.config");

                            if (projects.Contains(p))
                            {
                                packageReferenceFile.AddEntry(item.Name, item.Version, item.Architecture, Enumerable.Empty<Library>());
                            }
                            else
                            {
                                packageReferenceFile.DeleteEntry(item.Name, item.Version, item.Architecture);
                            }

                        }
                        break;
                    }
                case ".net":
                    {
                        // lib
                        // ask which references are to be added
                        // add references
                        break;
                    }
            }

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
