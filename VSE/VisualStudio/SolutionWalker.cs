using System.Collections.Generic;
using System.IO;
using System.Linq;
using CoApp.VSE.Extensions;
using CoApp.VSE.Model;
using CoApp.VSE.Packaging;
using EnvDTE;

namespace CoApp.VSE.VisualStudio
{
    public static class SolutionWalker
    {
        public static SolutionNode Walk(PackageReference packageReference)
        {
            if (!Module.IsSolutionOpen)
                return null;

            var children = CreateProjectNodes(Module.DTE.Solution.Projects.OfType<Project>().Where(n => !string.IsNullOrEmpty(n.FullName)), packageReference).ToArray();

            if (children.Any())
                return new SolutionNode(Module.DTE.Solution.Properties.Item("Name").Value, children);

            return null;
        }

        private static IEnumerable<ProjectNode> CreateProjectNodes(IEnumerable<Project> projects, PackageReference packageReference)
        {
            foreach (var project in projects)
            {
                if (project.IsSupported() && project.IsCompatible(packageReference))
                {
                    var children = new List<TreeNodeBase>();

                    switch (packageReference.Type)
                    {
                        case DeveloperLibraryType.VcLibrary:
                            children = CreateConfigurationNode(project, packageReference).ToList();
                            break;
                        case DeveloperLibraryType.Net:
                            children = CreateAssemblyNode(project, packageReference).ToList();
                            break;
                    }

                    var projectNode = new ProjectNode(project, children);

                    if (!children.Any())
                        projectNode.IsChecked = DetermineCheckState(packageReference, project, null, null);

                    yield return projectNode;
                }
            }
        }

        private static IEnumerable<TreeNodeBase> CreateConfigurationNode(Project project, PackageReference packageReference)
        {
            var configurations = project.GetCompatibleConfigurations(packageReference.CanonicalName.Architecture);

            foreach (var configuration in configurations)
            {
                var children = CreateLibraryNode(project, packageReference, configuration).ToArray();

                yield return new ConfigurationNode(configuration, children);
            }
        }

        private static IEnumerable<TreeNodeBase> CreateLibraryNode(Project project, PackageReference packageReference, string configuration)
        {
            var path = packageReference.PackageDirectory + "lib";

            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.lib");

                foreach (var filename in files.Select(Path.GetFileName))
                {
                    yield return new LibraryNode(filename)
                    {
                        IsChecked = DetermineCheckState(packageReference, project, configuration, filename)
                    };
                }
            }
        }

        private static IEnumerable<TreeNodeBase> CreateAssemblyNode(Project project, PackageReference packageReference)
        {
            var path = packageReference.PackageDirectory + "ReferenceAssemblies";

            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.dll");

                foreach (var filename in files.Select(Path.GetFileName))
                {
                    yield return new AssemblyNode(filename)
                    {
                        IsChecked = DetermineCheckState(packageReference, project, null, filename)
                    };
                }
            }
        }

        private static bool? DetermineCheckState(PackageReference packageReference, Project project, string config, string filename)
        {
            var packageReferenceFile = new PackageReferenceFile(project.GetDirectory() + "/coapp.packages.config");
            
            foreach (var p in packageReferenceFile.GetPackageReferences().Where(p => p.CanonicalName.Name == packageReference.CanonicalName.Name &&
                                                                                     p.CanonicalName.Flavor == packageReference.CanonicalName.Flavor &&
                                                                                     p.CanonicalName.Architecture == packageReference.CanonicalName.Architecture))
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
