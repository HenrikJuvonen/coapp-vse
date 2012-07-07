﻿using System;
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
            Array.Sort(children, ProjectNodeComparer.Default);

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
                    IList<ViewModelNodeBase> children;

                    if (packageReference.Type == DeveloperPackageType.VcLibrary)
                    {
                        children = CreateConfigurationNode(
                            project,
                            packageReference
                        ).ToList();
                    }
                    else if (packageReference.Type == DeveloperPackageType.Net)
                    {
                        children = CreateAssemblyNode(
                            project,
                            packageReference
                        ).ToList();
                    }
                    else
                    {
                        children = new List<ViewModelNodeBase>();
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
                            Array.Sort(children, ProjectNodeComparer.Default);
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

            foreach (string config in configurations)
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
            string path = packageReference.PackageDirectory + "lib";

            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path, "*.lib");

                foreach (string file in files)
                {
                    string filename = Path.GetFileName(file);

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
            string path = packageReference.PackageDirectory + "ReferenceAssemblies";

            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path, "*.dll");

                foreach (string file in files)
                {
                    string filename = Path.GetFileName(file);

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

            IEnumerable<PackageReference> packageReferences = packageReferenceFile.GetPackageReferences();

            return packageReferences.Any(n => n.Name == packageReference.Name && (n.Flavor != packageReference.Flavor ||
                                                                                  n.Version != packageReference.Version ||
                                                                                  n.Architecture != packageReference.Architecture));
        }

        private static bool? DetermineCheckState(PackageReference packageReference, Project project, string config, string filename)
        {
            var packageReferenceFile = new PackageReferenceFile(project.GetDirectory() + "/coapp.packages.config");
            
            bool hasLibraries = false;
            bool projectHasPackage = false;

            foreach (PackageReference p in packageReferenceFile.GetPackageReferences())
            {
                if (p.Name != packageReference.Name || p.Flavor != packageReference.Flavor)
                    continue;

                projectHasPackage = true;

                if (p.Libraries != null && p.Libraries.Any())
                    hasLibraries = true;

                foreach (Library l in p.Libraries)
                {
                    if (config == null ? l.Name == filename : l.ConfigurationName == config && l.Name == filename)
                    {
                        return true;
                    }
                }
            }

            if (config == null && filename == null && projectHasPackage)
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

        private class ProjectNodeComparer : IComparer<ViewModelNodeBase>
        {
            public static readonly ProjectNodeComparer Default = new ProjectNodeComparer();

            private ProjectNodeComparer()
            {
            }

            public int Compare(ViewModelNodeBase first, ViewModelNodeBase second)
            {
                if (first == null && second == null)
                {
                    return 0;
                }
                else if (first == null)
                {
                    return -1;
                }
                else if (second == null)
                {
                    return 1;
                }

                // solution folder goes before projects
                if (first is SolutionNode && second is ProjectNode)
                {
                    return -1;
                }
                else if (first is ProjectNode && second is SolutionNode)
                {
                    return 1;
                }
                else
                {
                    // if the two nodes are of the same kinds, compare by their names
                    return StringComparer.CurrentCultureIgnoreCase.Compare(first.Name, second.Name);
                }
            }
        }
    }
}