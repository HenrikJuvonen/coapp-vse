using System;
using System.Collections.Generic;
using EnvDTE;

namespace CoApp.VisualStudio.VsCore
{
    /// <summary>
    /// Solution manager caches projects, handles events and is used for adding packages to projects (or removing them).
    /// </summary>
    public interface ISolutionManager
    {
        event EventHandler SolutionOpened;
        event EventHandler SolutionClosing;
        event EventHandler SolutionClosed;
        event EventHandler<ProjectEventArgs> ProjectAdded;

        string SolutionDirectory { get; }

        string DefaultProjectName { get; set; }
        Project DefaultProject { get; }

        Project GetProject(string projectSafeName);

        IEnumerable<Project> GetProjects();

        /// <summary>
        /// Get the safe name of the specified project which guarantees not to conflict with other projects.
        /// </summary>
        /// <remarks>
        /// It tries to return simple name if possible. Otherwise it returns the unique name.
        /// </remarks>
        string GetProjectSafeName(Project project);

        bool IsSolutionOpen { get; }

        void ManagePackage(PackageReference packageReference, IEnumerable<Project> projects, IEnumerable<Library> libraries);
    }
}