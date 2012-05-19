using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EnvDTE;
using CoGet.VisualStudio;
using CoApp.Toolkit.Engine.Client;

namespace CoGet.Dialog
{
    internal static class SolutionWalker
    {
        public static FolderNode Walk(
            Solution solution,
            Package package,
            Func<Package, Project, string, string, bool?> checkedStateSelector,
            Predicate<Project> enabledStateSelector,
            string type)
        {
            if (solution == null)
            {
                throw new ArgumentNullException("solution");
            }

            if (!solution.IsOpen)
            {
                return null;
            }

            if (checkedStateSelector == null)
            {
                checkedStateSelector = (p,q,r,s) => false;
            }

            if (enabledStateSelector == null)
            {
                enabledStateSelector = p => true;
            }

            var children = CreateProjectNode(solution.Projects.OfType<Project>(), package, checkedStateSelector, enabledStateSelector, type).ToArray();
            Array.Sort(children, ProjectNodeComparer.Default);

            return new FolderNode(
                null,
                String.Format(CultureInfo.CurrentCulture, Resources.Dialog_SolutionNode, solution.GetName()),
                children);
        }

        private static IEnumerable<ProjectNodeBase> CreateConfigurationNode(
            Project project,
            Package package,
            Func<Package, Project, string, string, bool?> checkedStateSelector,
            Predicate<Project> enabledStateSelector)
        {
            Array configurations = (Array)project.ConfigurationManager.ConfigurationRowNames;

            foreach (string config in configurations)
            {
                var children = CreateLibraryNode(
                                project,
                                package,
                                checkedStateSelector,
                                enabledStateSelector,
                                config
                            ).ToArray();

                yield return new ConfigurationNode(project, config, children);
            }
        }

        private static IEnumerable<ProjectNodeBase> CreateLibraryNode(
            Project project,
            Package package,
            Func<Package, Project, string, string, bool?> checkedStateSelector,
            Predicate<Project> enabledStateSelector,
            string config)
        {
            string dir = @"c:\apps\Program Files (" + package.Architecture.ToString() + @")\Outercurve Foundation\" + package.CanonicalName + @"\lib\";

            string[] files = Directory.GetFiles(dir, "*.lib");

            foreach(string file in files)
            {
                string filename = file.Substring(file.LastIndexOf('\\') + 1);

                yield return new LibraryNode(project, filename)
                {
                    // default checked state of this node will be determined by the passed-in selector
                    IsSelected = checkedStateSelector(package, project, config, filename),
                    IsEnabled = enabledStateSelector(project)
                };
            }
        }

        private static IEnumerable<ProjectNodeBase> CreateProjectNode(
            IEnumerable<Project> projects,
            Package package,
            Func<Package, Project, string, string, bool?> checkedStateSelector,
            Predicate<Project> enabledStateSelector,
            string type)
        {

            foreach (var project in projects)
            {
                if (project.IsSupported() && project.IsCompatible(package))
                {
                    IList<ProjectNodeBase> children;

                    if (type == "vc,lib")
                        children = CreateConfigurationNode(
                            project, 
                            package,
                            checkedStateSelector,
                            enabledStateSelector
                        ).ToList();
                    else
                        children = Enumerable.Empty<ProjectNodeBase>().ToList();

                    bool allChildrenSelected = children.All(n => n.IsSelected == true);
                    
                    yield return new ProjectNode(project, children)
                    {
                        // default checked state of this node will be determined by the passed-in selector
                        IsSelected = allChildrenSelected ? true : checkedStateSelector(package, project, null, null),
                        IsEnabled = enabledStateSelector(project)
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
                            package,
                            checkedStateSelector,
                            enabledStateSelector,
                            type
                        ).ToArray();

                        if (children.Length > 0)
                        {
                            Array.Sort(children, ProjectNodeComparer.Default);
                            // only create a folder node if it has at least one child
                            yield return new FolderNode(project, project.Name, children);
                        }
                    }
                }
            }
        }

        private class ProjectNodeComparer : IComparer<ProjectNodeBase>
        {
            public static readonly ProjectNodeComparer Default = new ProjectNodeComparer();

            private ProjectNodeComparer()
            {
            }

            public int Compare(ProjectNodeBase first, ProjectNodeBase second)
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