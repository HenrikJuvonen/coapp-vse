using System;
using System.Collections.Generic;
using System.Globalization;
using EnvDTE;
using ProjectThunk = System.Func<EnvDTE.Project, CoApp.VsExtension.VisualStudio.IFileSystemProvider, CoApp.VsExtension.IProjectSystem>;

namespace CoApp.VsExtension.VisualStudio
{
    public static class VsProjectSystemFactory
    {
        private static Dictionary<string, ProjectThunk> _factories = new Dictionary<string, ProjectThunk>(StringComparer.OrdinalIgnoreCase) {

        };


        public static IProjectSystem CreateProjectSystem(Project project, IFileSystemProvider fileSystemProvider)
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }

            if (String.IsNullOrEmpty(project.FullName))
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                    VsResources.DTE_ProjectUnsupported, project.GetName()));
            }

            // Try to get a factory for the project type guid            
            foreach (var guid in project.GetProjectTypeGuids())
            {
                ProjectThunk factory;
                if (_factories.TryGetValue(guid, out factory))
                {
                    return factory(project, fileSystemProvider);
                }
            }

            // Fall back to the default if we have no special project types
            return new VsProjectSystem(project, fileSystemProvider);
        }
    }
}