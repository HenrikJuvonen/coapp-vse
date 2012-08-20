using System;
using System.Globalization;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CoApp.Packaging.Common;
using CoApp.VSE.Core.Packaging;
using CoApp.VSE.Core.Utility;
using VSLangProj;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Construction;
using Project = EnvDTE.Project;

namespace CoApp.VSE.Core.Extensions
{    
    public static class ProjectExtensions
    {
        private static readonly HashSet<string> SupportedProjectTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            VsConstants.CsharpProjectTypeGuid,
            VsConstants.VbProjectTypeGuid,
            VsConstants.JsProjectTypeGuid,
            VsConstants.FsharpProjectTypeGuid,
            VsConstants.VcProjectTypeGuid
        };

        public static string GetName(this Project project)
        {
            string name = project.Name;
            if (project.IsJavaScriptProject())
            {
                const string suffix = " (loading...)";
                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - suffix.Length);
                }
            }
            return name;
        }

        public static bool IsJavaScriptProject(this Project project)
        {
            return project != null && VsConstants.JsProjectTypeGuid.Equals(project.Kind, StringComparison.OrdinalIgnoreCase);
        }
        
        public static string GetDirectory(this Project project)
        {
            return Path.GetDirectoryName(project.FullName);
        }

        public static bool IsVcProject(this Project project)
        {
            return project.Kind != null && project.Kind.Equals(VsConstants.VcProjectTypeGuid, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsNetProject(this Project project)
        {
            return project.IsSupported();
        }

        public static bool IsSupported(this Project project)
        {
            return project.Kind != null && SupportedProjectTypes.Contains(project.Kind);
        }
        
        public static string GetTargetFramework(this Project project)
        {
            return project.AsMsBuildProject().GetPropertyValue("TargetFrameworkMoniker") ?? string.Empty;
        }

        public static double GetTargetFrameworkVersion(this Project project, string targetFramework = null)
        {
            targetFramework = targetFramework ?? project.GetTargetFramework();

            var split = targetFramework.Split(',');
            var versionStr = split.Count() > 1 ? split[1].Trim().Substring(split[1].IndexOf('=') + 2) : string.Empty;
            
            double version;
            double.TryParse(versionStr, NumberStyles.Any, CultureInfo.InvariantCulture, out version);

            return version;
        }

        /// <summary>
        /// Checks if project is compatible with the package.
        /// </summary>
        public static bool IsCompatible(this Project project, IPackage package)
        {
            if (package == null)
                return true;

            var targetFramework = project.GetTargetFramework();
            var targetFrameworkVersion = project.GetTargetFrameworkVersion(targetFramework);

            var targetsNetFramework = targetFramework.Contains(".NETFramework") || targetFramework.Contains("Silverlight");

            // VC-compatibility
            var compatible = ((package.GetDeveloperLibraryType() == DeveloperLibraryType.VcInclude ||
                               (package.GetDeveloperLibraryType() == DeveloperLibraryType.VcLibrary && project.IsCompatibleArchitecture(package.Architecture)))
                               && project.IsVcProject());

            // NET-compatibility
            compatible = compatible || (package.GetDeveloperLibraryType() == DeveloperLibraryType.Net && project.IsNetProject() && 
                (
                (package.Flavor == "" && targetsNetFramework) ||
                (package.Flavor == "[net20]" && (targetsNetFramework && targetFrameworkVersion >= 2.0)) ||
                (package.Flavor == "[net35]" && (targetsNetFramework && targetFrameworkVersion >= 3.5)) ||
                (package.Flavor == "[net40]" && (targetsNetFramework && targetFrameworkVersion >= 4.0)) ||
                (package.Flavor == "[net45]" && (targetsNetFramework && targetFrameworkVersion >= 4.5)) ||
                (package.Flavor == "[silverlight]" && targetsNetFramework)
                ));
            
            return compatible;
        }
        
        /// <summary>
        /// Checks if VC-project supports specified architecture.
        /// </summary>
        public static bool IsCompatibleArchitecture(this Project project, string architecture)
        {
            var configurations = project.GetCompatibleConfigurations(architecture);

            return configurations.Any();
        }
        
        public static Microsoft.Build.Evaluation.Project AsMsBuildProject(this Project project)
        {
            return ProjectCollection.GlobalProjectCollection.GetLoadedProjects(project.FullName).FirstOrDefault() ??
                   ProjectCollection.GlobalProjectCollection.LoadProject(project.FullName);
        }

        /// <summary>
        /// Get compatible configurations of VC-project. E.g. Debug|Win32, Release|Win32, ... depending on architecture
        /// </summary>
        public static IEnumerable<string> GetCompatibleConfigurations(this Project project, string architecture)
        {
            var buildProject = project.AsMsBuildProject();

            var result = new List<string>();

            if (buildProject != null)
            {
                var xml = buildProject.Xml;
                var itemDefinitionGroups = xml.ItemDefinitionGroups;

                foreach (var itemDefinitionGroup in itemDefinitionGroups)
                {
                    string condition = itemDefinitionGroup.Condition;
                    string configuration = condition.Substring(33, condition.LastIndexOf("'", StringComparison.InvariantCulture) - 33);

                    // 1. Platform string
                    string platform = configuration.Split('|')[1].ToLowerInvariant();

                    // 2. TargetMachine definition
                    var machine = project.GetDefinitionValue(configuration, "TargetMachine");

                    if (((platform == "win32" || platform == "x86") && architecture == "x86") ||
                        ((platform == "win64" || platform == "x64") && architecture == "x64") ||
                        machine.ToLowerInvariant().Contains(architecture))
                    {
                        result.Add(configuration);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Get configurations of VC-project. E.g. Debug|Win32, Release|Win32, ...
        /// </summary>
        public static IEnumerable<string> GetConfigurations(this Project project)
        {
            var buildProject = project.AsMsBuildProject();

            var result = new List<string>();

            if (buildProject != null)
            {
                var xml = buildProject.Xml;
                var itemDefinitionGroups = xml.ItemDefinitionGroups;

                foreach (var itemDefinitionGroup in itemDefinitionGroups)
                {
                    string condition = itemDefinitionGroup.Condition;
                    result.Add(condition.Substring(33, condition.LastIndexOf("'", StringComparison.InvariantCulture) - 33));
                }
            }

            return result;
        }

        /// <summary>
        /// Get definition in VC-project.
        /// </summary>
        private static string GetDefinitionValue(this Project project, string configuration, string elementName)
        {
            var buildProject = project.AsMsBuildProject();

            if (buildProject != null)
            {
                foreach (var itemDefinitionGroup in buildProject.Xml.ItemDefinitionGroups)
                {
                    if (itemDefinitionGroup.Condition.Contains(configuration))
                    {
                        foreach (ProjectItemDefinitionElement element in itemDefinitionGroup.Children)
                        {
                            switch (element.ItemType)
                            {
                                case "ClCompile":
                                case "Link":
                                    foreach (ProjectMetadataElement subElement in element.Children)
                                    {
                                        if (subElement.Name == elementName)
                                        {
                                            return subElement.Value;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Set definition in VC-project.
        /// </summary>
        private static void SetDefinitionValue(this Project project, string configuration, string elementName, string elementValue)
        {
            var buildProject = project.AsMsBuildProject();

            if (buildProject != null)
            {
                foreach (var itemDefinitionGroup in buildProject.Xml.ItemDefinitionGroups)
                {
                    if (itemDefinitionGroup.Condition.Contains(configuration))
                    {
                        foreach (ProjectItemDefinitionElement element in itemDefinitionGroup.Children)
                        {
                            switch (element.ItemType)
                            {
                                case "ClCompile":
                                case "Link":
                                    foreach (ProjectMetadataElement subElement in element.Children)
                                    {
                                        if (subElement.Name == elementName)
                                        {
                                            // subelement found
                                            subElement.Value = elementValue;
                                            return;
                                        }
                                    }
                                    break;
                            }

                            // subelement not found, create new
                            if ((element.ItemType == "ClCompile" && elementName == "AdditionalIncludeDirectories") || 
                                (element.ItemType == "Link" && (elementName == "AdditionalLibraryDirectories" || elementName == "AdditionalDependencies")))
                            {
                                element.AddMetadata(elementName, elementValue);
                            }
                        }
                    }
                }
            }
        }
        
        private static IEnumerable<Tuple<ProjectItem, AssemblyName>> GetAssemblyReferences(this Microsoft.Build.Evaluation.Project project)
        {
            foreach (var referenceProjectItem in project.GetItems("Reference"))
            {
                AssemblyName assemblyName = null;
                try
                {
                    assemblyName = new AssemblyName(referenceProjectItem.EvaluatedInclude);
                }
                catch
                {
                }

                // We can't yield from within the try so we do it out here if everything was successful
                if (assemblyName != null)
                {
                    yield return Tuple.Create(referenceProjectItem, assemblyName);
                }
            }
        }
        
        /// <summary>
        /// Add or remove references in .NET-project.
        /// </summary>
        public static void ManageReferences(this Project project, PackageReference packageReference, IEnumerable<LibraryReference> libraries)
        {
            // TODO: This will be changed to "referenceassemblies\\flavor\\arch\\simplename-version\\" in 1.2.0.444+
            string path = string.Format(@"{0}\ReferenceAssemblies\{1}\{2}{3}-{4}\",
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                packageReference.CanonicalName.Architecture, packageReference.CanonicalName.Name, packageReference.CanonicalName.Flavor, packageReference.CanonicalName.Version);

            var buildProject = project.AsMsBuildProject();
            var vsProject = (VSProject)project.Object;

            if (buildProject != null && vsProject != null)
            {
                foreach (var lib in libraries)
                {
                    var assemblyName = Path.GetFileNameWithoutExtension(lib.Name);
                    var assemblyPath = path + lib.Name;

                    Reference reference = null;

                    // Remove old references
                    do
                    {
                        try
                        {
                            reference = vsProject.References.Find(assemblyName);
                        }
                        catch { }

                        if (reference != null)
                        {
                            reference.Remove();
                        }
                    }
                    while (reference != null);

                    // Add new reference
                    if (lib.IsChecked)
                    {
                        vsProject.References.Add(assemblyPath);
                        
                        var references = buildProject.GetAssemblyReferences();
                        var referenceItem = references.First(n => n.Item2.Name == assemblyName).Item1;

                        referenceItem.SetMetadataValue("HintPath", assemblyPath);
                    }
                }
            }
        }
        
        /// <summary>
        /// Add or remove library directories and linker dependencies in VC-project.
        /// </summary>
        public static void ManageLinkerDefinitions(this Project project, PackageReference packageReference, IEnumerable<Project> projects, IEnumerable<LibraryReference> libraries)
        {
            var path = string.Format(@"{0}\lib\{1}\",
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                packageReference.CanonicalName.Architecture);

            foreach (var configuration in project.GetConfigurations())
            {
                var value = project.GetDefinitionValue(configuration, "AdditionalLibraryDirectories");

                var paths = new HashSet<string>(value.Split(';'));

                if (projects.Any(n => n.FullName == project.FullName))
                {
                    paths.Add(path);
                }
                else
                {
                    paths.Remove(path);
                }

                project.SetDefinitionValue(configuration, "AdditionalLibraryDirectories", string.Join(";", paths));

                var configLibraries = libraries.Where(lib => lib.ConfigurationName == configuration);

                value = project.GetDefinitionValue(configuration, "AdditionalDependencies");

                var current = value.Split(';');

                var removed = configLibraries.Where(n => !n.IsChecked)
                                             .Select(n => Path.GetFileNameWithoutExtension(n.Name) + "-" + packageReference.CanonicalName.Version + ".lib");

                var added = configLibraries.Where(n => n.IsChecked)
                                           .Select(n => Path.GetFileNameWithoutExtension(n.Name) + "-" + packageReference.CanonicalName.Version + ".lib");

                var result = current.Except(removed)
                                    .Union(added);

                project.SetDefinitionValue(configuration, "AdditionalDependencies", string.Join(";", result));
            }
        }

        /// <summary>
        /// Add or remove include directories in VC-project.
        /// </summary>
        public static void ManageIncludeDirectories(this Project project, PackageReference packageReference, IEnumerable<Project> projects)
        {
            var path = string.Format(@"{0}\include\{1}-{2}\",
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                packageReference.CanonicalName.Name.Split('-')[0],
                packageReference.CanonicalName.Version);

            foreach (var configuration in project.GetConfigurations())
            {
                var value = project.GetDefinitionValue(configuration, "AdditionalIncludeDirectories");

                var paths = new HashSet<string>(value.Split(';'));

                if (projects.Any(n => n.FullName == project.FullName))
                {
                    paths.Add(path);
                }
                else
                {
                    paths.Remove(path);
                }

                project.SetDefinitionValue(configuration, "AdditionalIncludeDirectories", string.Join(";", paths));
            }
        }

        public static bool HasPackage(this Project project, IPackage package)
        {
            var packageReferenceFile = new PackageReferenceFile(project.GetDirectory() + "/coapp.packages.config");

            return packageReferenceFile.FindEntry(package.CanonicalName, true) != null;
        }
    }
}