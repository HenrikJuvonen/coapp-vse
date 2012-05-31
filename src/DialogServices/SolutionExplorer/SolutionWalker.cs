using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EnvDTE;
using CoApp.VisualStudio.VsCore;
using CoApp.Packaging.Client;

namespace CoApp.VisualStudio.Dialog
{
    internal static class SolutionWalker
    {
        public static FolderNode Walk(
            Solution solution,
            PackageReference packageReference)
        {
            if (solution == null)
            {
                throw new ArgumentNullException("solution");
            }

            if (!solution.IsOpen)
            {
                return null;
            }

            var children = CreateProjectNode(solution.Projects.OfType<Project>(), packageReference).ToArray();
            Array.Sort(children, ProjectNodeComparer.Default);

            return new SolutionNode(
                null,
                String.Format(CultureInfo.CurrentCulture, Resources.Dialog_SolutionNode, solution.GetName()),
                children);
        }

        private static IEnumerable<ViewModelNodeBase> CreateProjectNode(
            IEnumerable<Project> projects,
            PackageReference packageReference)
        {
            foreach (var project in projects)
            {
                if (project.IsSupported() && project.IsCompatible(packageReference))
                {
                    IList<ViewModelNodeBase> children;

                    if (packageReference.Type == "vc,lib")
                        children = CreateConfigurationNode(
                            project,
                            packageReference
                        ).ToList();
                    else
                        children = new List<ViewModelNodeBase>();

                    bool allChildrenSelected = children.Any() ? children.All(n => n.IsSelected == true) : false;
                    
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
                            packageReference
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
            Array configurations = (Array)project.ConfigurationManager.ConfigurationRowNames;

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
            string[] files = Directory.GetFiles(packageReference.Path + "lib", "*.lib");

            foreach (string file in files)
            {
                string filename = file.Substring(file.LastIndexOf('\\') + 1);

                yield return new LibraryNode(project, filename)
                {
                    IsSelected = DetermineCheckState(packageReference, project, config, filename)
                };
            }
        }

        private static bool? DetermineCheckState(PackageReference packageReference, Project project, string config, string lib)
        {
            PackageReferenceFile packageReferenceFile = new PackageReferenceFile(Path.GetDirectoryName(project.FullName) + "/coapp.config");

            IEnumerable<PackageReference> packageReferences = packageReferenceFile.GetPackageReferences();

            bool hasLibraries = false;
            bool projectHasPackage = false;

            foreach (PackageReference p in packageReferences)
            {
                if (p.Name != packageReference.Name)
                    continue;

                projectHasPackage = true;

                if (p.Libraries != null && p.Libraries.Any())
                    hasLibraries = true;

                foreach (Library l in p.Libraries)
                {
                    if (l.ConfigurationName == config && l.Name == lib)
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
                if (first is FolderNode && second is ProjectNode)
                {
                    return -1;
                }
                else if (first is ProjectNode && second is FolderNode)
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