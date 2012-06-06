using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Project = EnvDTE.Project;
using ProjectItem = EnvDTE.ProjectItem;
using Microsoft.VisualStudio.VCProjectEngine;
using VSLangProj;

namespace CoApp.VisualStudio.VsCore
{
    public static class ProjectExtensions
    {
        private const string WebConfig = "web.config";
        private const string AppConfig = "app.config";
        private const string BinFolder = "Bin";

        private static readonly Dictionary<string, string> _knownNestedFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "web.debug.config", "web.config" },
            { "web.release.config", "web.config" }
        };

        private static readonly HashSet<string> _supportedProjectTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                                                                          VsConstants.WebSiteProjectTypeGuid, 
                                                                          VsConstants.CsharpProjectTypeGuid, 
                                                                          VsConstants.VbProjectTypeGuid,
                                                                          VsConstants.JsProjectTypeGuid,
                                                                          VsConstants.FsharpProjectTypeGuid,
                                                                          VsConstants.NemerleProjectTypeGuid,
                                                                          VsConstants.WixProjectTypeGuid,
                                                                          VsConstants.VcProjectTypeGuid
                                                                        };

        private static readonly HashSet<string> _unsupportedProjectTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                                                                            VsConstants.LightSwitchProjectTypeGuid,
                                                                            VsConstants.InstallShieldLimitedEditionTypeGuid
                                                                        };

        private static readonly IEnumerable<string> _fileKinds = new[] { VsConstants.VsProjectItemKindPhysicalFile, VsConstants.VsProjectItemKindSolutionItem };
        private static readonly IEnumerable<string> _folderKinds = new[] { VsConstants.VsProjectItemKindPhysicalFolder };

        // List of project types that cannot have references added to them
        private static readonly string[] _unsupportedProjectTypesForAddingReferences = new[] { VsConstants.WixProjectTypeGuid };
        // List of project types that cannot have binding redirects added
        private static readonly string[] _unsupportedProjectTypesForBindingRedirects = new[] { VsConstants.WixProjectTypeGuid, VsConstants.JsProjectTypeGuid, VsConstants.NemerleProjectTypeGuid };

        private static readonly char[] PathSeparatorChars = new[] { Path.DirectorySeparatorChar };
        
        // Get the ProjectItems for a folder path
        public static ProjectItems GetProjectItems(this Project project, string folderPath, bool createIfNotExists = false)
        {
            if (String.IsNullOrEmpty(folderPath))
            {
                return project.ProjectItems;
            }

            // Traverse the path to get at the directory
            string[] pathParts = folderPath.Split(PathSeparatorChars, StringSplitOptions.RemoveEmptyEntries);

            // 'cursor' can contain a reference to either a Project instance or ProjectItem instance. 
            // Both types have the ProjectItems property that we want to access.
            dynamic cursor = project;

            string fullPath = project.GetDirectory();
            string folderRelativePath = String.Empty;

            foreach (string part in pathParts)
            {
                fullPath = Path.Combine(fullPath, part);
                folderRelativePath = Path.Combine(folderRelativePath, part);

                cursor = GetOrCreateFolder(project, cursor, fullPath, folderRelativePath, part, createIfNotExists);
                if (cursor == null)
                {
                    return null;
                }
            }

            return cursor.ProjectItems;
        }

        public static ProjectItem GetProjectItem(this Project project, string path)
        {
            string folderPath = Path.GetDirectoryName(path);
            string itemName = Path.GetFileName(path);

            ProjectItems container = GetProjectItems(project, folderPath);

            ProjectItem projectItem;
            // If we couldn't get the folder, or the child item doesn't exist, return null
            if (container == null ||
                (!container.TryGetFile(itemName, out projectItem) &&
                 !container.TryGetFolder(itemName, out projectItem)))
            {
                return null;
            }

            return projectItem;
        }

        /// <summary>
        /// Recursively retrieves all supported child projects of a virtual folder.
        /// </summary>
        /// <param name="project">The root container project</param>
        public static IEnumerable<Project> GetSupportedChildProjects(this Project project)
        {
            if (!project.IsSolutionFolder())
            {
                yield break;
            }

            var containerProjects = new Queue<Project>();
            containerProjects.Enqueue(project);

            while (containerProjects.Any())
            {
                var containerProject = containerProjects.Dequeue();
                foreach (ProjectItem item in containerProject.ProjectItems)
                {
                    var nestedProject = item.SubProject;
                    if (nestedProject == null)
                    {
                        continue;
                    }
                    else if (nestedProject.IsSupported())
                    {
                        yield return nestedProject;
                    }
                    else if (nestedProject.IsSolutionFolder())
                    {
                        containerProjects.Enqueue(nestedProject);
                    }
                }
            }
        }

        public static bool DeleteProjectItem(this Project project, string path)
        {
            ProjectItem projectItem = GetProjectItem(project, path);
            if (projectItem == null)
            {
                return false;
            }

            projectItem.Delete();
            return true;
        }

        public static bool TryGetFolder(this ProjectItems projectItems, string name, out ProjectItem projectItem)
        {
            projectItem = GetProjectItem(projectItems, name, _folderKinds);

            return projectItem != null;
        }

        public static bool TryGetFile(this ProjectItems projectItems, string name, out ProjectItem projectItem)
        {
            projectItem = GetProjectItem(projectItems, name, _fileKinds);

            if (projectItem == null)
            {
                // Try to get the nested project item
                return TryGetFileNestedFile(projectItems, name, out projectItem);
            }

            return projectItem != null;
        }

        /// <summary>
        /// // If we didn't find the project item at the top level, then we look one more level down.
        /// In VS files can have other nested files like foo.aspx and foo.aspx.cs or web.config and web.debug.config. 
        /// These are actually top level files in the file system but are represented as nested project items in VS.            
        /// </summary>
        private static bool TryGetFileNestedFile(ProjectItems projectItems, string name, out ProjectItem projectItem)
        {
            string parentFileName;
            if (!_knownNestedFiles.TryGetValue(name, out parentFileName))
            {
                parentFileName = Path.GetFileNameWithoutExtension(name);
            }

            // If it's not one of the known nested files then we're going to look up prefixes backwards
            // i.e. if we're looking for foo.aspx.cs then we look for foo.aspx then foo.aspx.cs as a nested file
            ProjectItem parentProjectItem = GetProjectItem(projectItems, parentFileName, _fileKinds);

            if (parentProjectItem != null)
            {
                // Now try to find the nested file
                projectItem = GetProjectItem(parentProjectItem.ProjectItems, name, _fileKinds);
            }
            else
            {
                projectItem = null;
            }

            return projectItem != null;
        }
        
        public static string GetName(this Project project)
        {
            string name = project.Name;
            if (project.IsJavaScriptProject())
            {
                // The JavaScript project initially returns a "(loading..)" suffix to the project Name.
                // Need to get rid of it for the rest of CoApp.VisualStudio to work properly.
                // TODO: Follow up with the VS team to see if this will be fixed eventually
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
        
        private static ProjectItem GetProjectItem(this ProjectItems projectItems, string name, IEnumerable<string> allowedItemKinds)
        {
            try
            {
                ProjectItem projectItem = projectItems.Item(name);
                if (projectItem != null && allowedItemKinds.Contains(projectItem.Kind, StringComparer.OrdinalIgnoreCase))
                {
                    return projectItem;
                }
            }
            catch
            {
            }

            return null;
        }

        public static string GetDirectory(this Project project)
        {
            Property path = null;// project.Properties.Item("Path");

            if (path != null)
                return Path.GetDirectoryName(path.Value);

            return Path.GetDirectoryName(project.FullName);
        }

        public static T GetPropertyValue<T>(this Project project, string propertyName)
        {
            try
            {
                Property property = project.Properties.Item(propertyName);
                if (property != null)
                {
                    // REVIEW: Should this cast or convert?
                    return (T)property.Value;
                }
            }
            catch (ArgumentException)
            {

            }
            return default(T);
        }
        
        // 'parentItem' can be either a Project or ProjectItem
        private static ProjectItem GetOrCreateFolder(
            Project project,
            dynamic parentItem,
            string fullPath,
            string folderRelativePath,
            string folderName,
            bool createIfNotExists)
        {
            if (parentItem == null)
            {
                return null;
            }

            ProjectItem subFolder;

            ProjectItems projectItems = parentItem.ProjectItems;
            if (projectItems.TryGetFolder(folderName, out subFolder))
            {
                // Get the sub folder
                return subFolder;
            }
            else if (createIfNotExists)
            {
                // The JS Metro project system has a bug whereby calling AddFolder() to an existing folder that
                // does not belong to the project will throw. To work around that, we have to manually include 
                // it into our project.
                if (project.IsJavaScriptProject() && Directory.Exists(fullPath))
                {
                    bool succeeded = IncludeExistingFolderToProject(project, folderRelativePath);
                    if (succeeded)
                    {
                        // IMPORTANT: after including the folder into project, we need to get 
                        // a new ProjectItems snapshot from the parent item. Otheriwse, reusing 
                        // the old snapshot from above won't have access to the added folder.
                        projectItems = parentItem.ProjectItems;
                        if (projectItems.TryGetFolder(folderName, out subFolder))
                        {
                            // Get the sub folder
                            return subFolder;
                        }
                    }
                    return null;
                }

                try
                {
                    return projectItems.AddFromDirectory(fullPath);
                }
                catch (NotImplementedException)
                {
                    // This is the case for F#'s project system, we can't add from directory so we fall back
                    // to this impl
                    return projectItems.AddFolder(folderName);
                }
            }

            return null;
        }

        private static bool IncludeExistingFolderToProject(Project project, string folderRelativePath)
        {
            IVsUIHierarchy projectHierarchy = (IVsUIHierarchy)project.ToVsHierarchy();

            uint itemId;
            int hr = projectHierarchy.ParseCanonicalName(folderRelativePath, out itemId);
            if (!ErrorHandler.Succeeded(hr))
            {
                return false;
            }

            // Execute command to include the existing folder into project. Must do this on UI thread.
            hr = ThreadHelper.Generic.Invoke(() =>
                    projectHierarchy.ExecCommand(
                        itemId,
                        ref VsMenus.guidStandardCommandSet2K,
                        (int)VSConstants.VSStd2KCmdID.INCLUDEINPROJECT,
                        0,
                        IntPtr.Zero,
                        IntPtr.Zero));

            return ErrorHandler.Succeeded(hr);
        }

        public static bool IsWebSite(this Project project)
        {
            return project.Kind != null && project.Kind.Equals(VsConstants.WebSiteProjectTypeGuid, StringComparison.OrdinalIgnoreCase);
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
            return project.Kind != null && _supportedProjectTypes.Contains(project.Kind);
        }

        public static bool IsExplicitlyUnsupported(this Project project)
        {
            return project.Kind == null || _unsupportedProjectTypes.Contains(project.Kind);
        }

        public static bool IsSolutionFolder(this Project project)
        {
            return project.Kind != null && project.Kind.Equals(VsConstants.VsProjectItemKindSolutionFolder, StringComparison.OrdinalIgnoreCase);
        }
                
        public static bool IsUnloaded(this Project project)
        {
            return VsConstants.UnloadedProjectTypeGuid.Equals(project.Kind, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetOutputPath(this Project project)
        {
            // For Websites the output path is the bin folder
            string outputPath = project.IsWebSite() ? BinFolder : project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
            return Path.Combine(project.GetDirectory(), outputPath);
        }

        public static IVsHierarchy ToVsHierarchy(this Project project)
        {
            IVsHierarchy hierarchy;

            // Get the vs solution
            IVsSolution solution = ServiceLocator.GetInstance<IVsSolution>();
            int hr = solution.GetProjectOfUniqueName(project.UniqueName, out hierarchy);

            if (hr != VsConstants.S_OK)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return hierarchy;
        }
        
        public static bool IsCompatible(this Project project, PackageReference packageReference)
        {
            if (packageReference == null)
            {
                return true;
            }
            var projectTypeGuids = project.GetProjectTypeGuids();

            if ((packageReference.Type.Contains("vc") && project.IsVcProject()) ||
                (packageReference.Type == "net" && project.IsNetProject()))
            {
                return true;
            }

            return false;
        }
        
        public static IEnumerable<string> GetProjectTypeGuids(this Project project)
        {
            // Get the vs hierarchy as an IVsAggregatableProject to get the project type guids

            var hierarchy = project.ToVsHierarchy();
            var aggregatableProject = hierarchy as IVsAggregatableProject;
            if (aggregatableProject != null)
            {
                string projectTypeGuids;
                int hr = aggregatableProject.GetAggregateProjectTypeGuids(out projectTypeGuids);

                if (hr != VsConstants.S_OK)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                return projectTypeGuids.Split(';');
            }
            else if (!String.IsNullOrEmpty(project.Kind))
            {
                return new[] { project.Kind };
            }
            else
            {
                return new string[0];
            }
        }
                
        /// <summary>
        /// Returns the unique name of the specified project including all solution folder names containing it.
        /// </summary>
        /// <remarks>
        /// This is different from the DTE Project.UniqueName property, which is the absolute path to the project file.
        /// </remarks>
        public static string GetCustomUniqueName(this Project project)
        {
            if (project.IsWebSite())
            {
                // website projects always have unique name
                return project.Name;
            }
            else
            {
                Stack<string> nameParts = new Stack<string>();

                Project cursor = project;
                nameParts.Push(cursor.GetName());

                // walk up till the solution root
                while (cursor.ParentProjectItem != null && cursor.ParentProjectItem.ContainingProject != null)
                {
                    cursor = cursor.ParentProjectItem.ContainingProject;
                    nameParts.Push(cursor.GetName());
                }

                return String.Join("\\", nameParts);
            }
        }

        public static bool IsParentProjectExplicitlyUnsupported(this Project project)
        {
            if (project.ParentProjectItem == null || project.ParentProjectItem.ContainingProject == null)
            {
                // this project is not a child of another project
                return false;
            }

            Project parentProject = project.ParentProjectItem.ContainingProject;
            return parentProject.IsExplicitlyUnsupported();
        }

        /// <summary>
        /// This method truncates Website projects into the VS-format, e.g. C:\..\WebSite1, but it uses Name instead of SafeName from Solution Manager.
        /// </summary>
        public static string GetDisplayName(this Project project)
        {
            return GetDisplayName(project, p => p.Name);
        }

        private static string GetDisplayName(this Project project, Func<Project, string> nameSelector)
        {
            string name = nameSelector(project);
            if (project.IsWebSite())
            {
                name = PathHelper.SmartTruncate(name, 40);
            }
            return name;
        }

        public static void ManageReferences(this Project project, string architecture, IEnumerable<Library> libraries)
        {
            string path = @"C:\ProgramData\ReferenceAssemblies\" + architecture + @"\";

            VSProject vsProject = (VSProject)project.Object;

            foreach (Library lib in libraries)
            {
                Reference reference = vsProject.References.Find(Path.GetFileNameWithoutExtension(lib.Name));

                if (reference == null && lib.IsSelected)
                    vsProject.References.Add(path + lib.Name);
                else if (reference != null && !lib.IsSelected)
                    reference.Remove();
            }
        }

        public static void ManageLinkerDependencies(this Project project, string architecture, IEnumerable<Project> projects, IEnumerable<Library> libraries)
        {
            string path = @"C:\ProgramData\lib\" + architecture + @"\";

            VCProject vcProject = (VCProject)project.Object;
            IVCCollection configs = vcProject.Configurations;

            foreach (VCConfiguration config in configs)
            {
                VCLinkerTool linker = config.Tools.Item("Linker Tool");

                ISet<string> paths = new HashSet<string>(linker.AdditionalLibraryDirectories.Split(';'));

                if (projects.Contains(project))
                {
                    paths.Add(path);
                }
                else
                {
                    paths.Remove(path);
                }

                linker.AdditionalLibraryDirectories = string.Join(";", paths);

                IEnumerable<Library> configLibraries = libraries.Where(lib => lib.ConfigurationName == config.ConfigurationName);
                
                IEnumerable<string> current = linker.AdditionalDependencies.Split(' ');

                IEnumerable<string> removed = configLibraries.Where(n => !n.IsSelected)
                                                             .Select(n => n.Name);
                
                IEnumerable<string> added = configLibraries.Where(n => n.IsSelected)
                                                           .Select(n => n.Name);

                IEnumerable<string> result = current.Except(removed)
                                                    .Union(added);

                linker.AdditionalDependencies = string.Join(" ", result);
            }
        }

        public static void ManageIncludeDirectories(this Project project, string packageNameAndVersion, IEnumerable<Project> projects)
        {
            string path = @"C:\ProgramData\include\" + packageNameAndVersion + @"\";

            VCProject vcProject = (VCProject)project.Object;
            IVCCollection configs = vcProject.Configurations;

            foreach (VCConfiguration config in configs)
            {
                VCCLCompilerTool compiler = config.Tools.Item("VCCLCompilerTool");

                ISet<string> paths = new HashSet<string>(compiler.AdditionalIncludeDirectories.Split(';'));

                if (projects.Contains(project))
                {
                    paths.Add(path);
                }
                else
                {
                    paths.Remove(path);
                }

                compiler.AdditionalIncludeDirectories = string.Join(";", paths);
            }
        }

    }
}