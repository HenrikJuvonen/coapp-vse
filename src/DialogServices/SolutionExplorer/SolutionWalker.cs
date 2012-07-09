using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EnvDTE;
using CoApp.VisualStudio.VsCore;

namespace CoApp.VisualStudio.Dialog
{
    internal static class SolutionWalker
    {
        public static SolutionNode Walk(
            Solution solution,
            PackageReference packageReference,
            bool replacePackage)
        {
            if (solution == null)
            {
                throw new ArgumentNullException("solution");
            }

            if (!solution.IsOpen)
            {
                return null;
            }

            var children = CreateProjectNode(solution.Projects.OfType<Project>(), packageReference, replacePackage).ToArray();

            return new SolutionNode(
                null,
                String.Format(CultureInfo.CurrentCulture, Resources.Dialog_SolutionNode, solution.GetName()),
                children);
        }

        private static IEnumerable<ViewModelNodeBase> CreateProjectNode(
            IEnumerable<Project> projects,
            PackageReference packageReference,
            bool replacePackage)
        {
            foreach (var project in projects)
            {
                if (project.IsSupported() && project.IsCompatible(packageReference) &&
                    (replacePackage || !project.IsOtherSimilarPackageAdded(packageReference)))
                {
                    var children = new List<ViewModelNodeBase>();

                    switch (packageReference.Type)
                    {
                        case DeveloperPackageType.VcLibrary:
                            children = CreateConfigurationNode(
                                project,
                                packageReference
                                ).ToList();
                            break;
                        case DeveloperPackageType.Net:
                            children = CreateAssemblyNode(
                                project,
                                packageReference
                                ).ToList();
                            break;
                    }

                    bool allChildrenSelected = children.Any() && children.All(n => n.IsSelected == true);
                    
                    yield return new ProjectNode(project, children)
                    {
                        IsSelected = allChildrenSelected ? true : DetermineCheckState(packageReference, project, null, null)
                    };
                }
                else if (project.IsSolutionFolder())
                {
                    if (project.ProjectItems != null)
                    {
                        var children = CreateProjectNode(
                            project.ProjectItems.
                                OfType<ProjectItem>().
                                Where(p => p.SubProject != null).
                                Select(p => p.SubProject),
                            packageReference,
                            replacePackage
                        ).ToArray();

                        if (children.Length > 0)
                        {
                            // only create a folder node if it has at least one child
                            yield return new SolutionNode(project, project.Name, children);
                        }
                    }
                }
            }
        }

        private static IEnumerable<ViewModelNodeBase> CreateConfigurationNode(
            Project project,
            PackageReference packageReference)
        {
            var configurations = project.GetCompatibleConfigurations(packageReference.Architecture);

            foreach (var config in configurations)
            {
                var children = CreateLibraryNode(
                                project,
                                packageReference,
                                config
                            ).ToArray();

                yield return new ConfigurationNode(project, config, children);
            }
        }

        private static IEnumerable<ViewModelNodeBase> CreateLibraryNode(
            Project project,
            PackageReference packageReference,
            string config)
        {
            var path = packageReference.PackageDirectory + "lib";

            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.lib");

                foreach (var filename in files.Select(Path.GetFileName))
                {
                    yield return new LibraryNode(project, filename)
                    {
                        IsSelected = DetermineCheckState(packageReference, project, config, filename)
                    };
                }
            }
        }

        private static IEnumerable<ViewModelNodeBase> CreateAssemblyNode(
            Project project,
            PackageReference packageReference)
        {
            var path = packageReference.PackageDirectory + "ReferenceAssemblies";

            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.dll");

                foreach (var filename in files.Select(Path.GetFileName))
                {
                    yield return new LibraryNode(project, filename)
                    {
                        IsSelected = DetermineCheckState(packageReference, project, null, filename)
                    };
                }
            }
        }

        /// <summary>
        /// Checks if there is already a package added with same name, but different flavor/version/architecture.
        /// </summary>
        /// <returns></returns>
        private static bool IsOtherSimilarPackageAdded(this Project project, PackageReference packageReference)
        {
            var packageReferenceFile = new PackageReferenceFile(project.GetDirectory() + "/coapp.packages.config");

            var packageReferences = packageReferenceFile.GetPackageReferences();

            return packageReferences.Any(n => n.Name == packageReference.Name && (n.Flavor != packageReference.Flavor ||
                                                                                  n.Version != packageReference.Version ||
                                                                                  n.Architecture != packageReference.Architecture));
        }

        private static bool? DetermineCheckState(PackageReference packageReference, Project project, string config, string filename)
        {
            var packageReferenceFile = new PackageReferenceFile(project.GetDirectory() + "/coapp.packages.config");
            
            bool hasLibraries = false;
            bool projectHasPackage = false;

            foreach (var p in packageReferenceFile.GetPackageReferences().Where(p => p.Equals(packageReference)))
            {
                projectHasPackage = true;

                if (p.Libraries != null && p.Libraries.Any())
                    hasLibraries = true;
                else
                    continue;

                if (p.Libraries.Any(l => config == null ? l.Name == filename : l.ConfigurationName == config && l.Name == filename))
                    return true;
            }

            if (config == null && filename == null && projectHasPackage)
            {
                if (hasLibraries)
                    return null;

                return true;
            }

            return false;
        }
    }
}