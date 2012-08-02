using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CoApp.Packaging.Client;
using CoApp.VSE.Extensions;
using CoApp.VSE.Model;
using CoApp.VSE.Packaging;
using EnvDTE;

namespace CoApp.VSE.Utility
{
    public static class SolutionWalker
    {
        private static readonly object SpinLock = new object();

        public static SolutionNode Walk(Package package)
        {
            if (!Module.IsSolutionOpen)
                return null;

            var children = CreateProjectNodes(Module.DTE.Solution.Projects.OfType<Project>().Where(n => !string.IsNullOrEmpty(n.FullName)), package).ToArray();

            return children.Any() ? new SolutionNode(Module.DTE.Solution.Properties.Item("Name").Value, children) : null;
        }

        private static IEnumerable<ProjectNode> CreateProjectNodes(IEnumerable<Project> projects, Package package)
        {
            var result = new List<ProjectNode>();

            Parallel.ForEach(projects, project =>
            {
                if (project.IsSupported() && project.IsCompatible(package))
                {
                    var children = new List<TreeNodeBase>();

                    switch (package.GetDeveloperLibraryType())
                    {
                        case DeveloperLibraryType.VcLibrary:
                            children = CreateConfigurationNode(project, package).ToList();
                            break;
                        case DeveloperLibraryType.Net:
                            children = CreateAssemblyNode(project, package).ToList();
                            break;
                    }

                    var projectNode = new ProjectNode(project, children);

                    if (!children.Any())
                        projectNode.IsChecked = DetermineCheckState(package, project, null, null);

                    lock (SpinLock)
                    {
                        result.Add(projectNode);
                    }
                }
            });

            return result;
        }

        private static IEnumerable<TreeNodeBase> CreateConfigurationNode(Project project, Package package)
        {
            var configurations = project.GetCompatibleConfigurations(package.CanonicalName.Architecture);

            foreach (var configuration in configurations)
            {
                var children = CreateLibraryNode(project, package, configuration).ToArray();

                yield return new ConfigurationNode(configuration, children);
            }
        }

        private static IEnumerable<TreeNodeBase> CreateLibraryNode(Project project, Package package, string configuration)
        {
            var path = package.GetPackageDirectory() + "lib";

            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.lib");

                foreach (var filename in files.Select(Path.GetFileName))
                {
                    yield return new LibraryNode(filename)
                    {
                        IsChecked = DetermineCheckState(package, project, configuration, filename)
                    };
                }
            }
        }

        private static IEnumerable<TreeNodeBase> CreateAssemblyNode(Project project, Package package)
        {
            var path = package.GetPackageDirectory() + "ReferenceAssemblies";

            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.dll");

                foreach (var filename in files.Select(Path.GetFileName))
                {
                    yield return new AssemblyNode(filename)
                    {
                        IsChecked = DetermineCheckState(package, project, null, filename)
                    };
                }
            }
        }

        private static bool? DetermineCheckState(Package package, Project project, string config, string filename)
        {
            var packageReferenceFile = new PackageReferenceFile(project.GetDirectory() + "/coapp.packages.config");

            foreach (var p in packageReferenceFile.GetPackageReferences().Where(p => p.CanonicalName.Name == package.CanonicalName.Name &&
                                                                                     p.CanonicalName.Flavor == package.CanonicalName.Flavor &&
                                                                                     p.CanonicalName.Architecture == package.CanonicalName.Architecture))
            {
                if (filename == null)
                    return true;

                if (config == null && p.Libraries.Any(l => l.Name == filename))
                    return true;

                if (p.Libraries.Any(l => l.ConfigurationName == config && l.Name == filename))
                    return true;
            }

            return false;
        }
    }
}
