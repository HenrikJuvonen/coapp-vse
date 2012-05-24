using System;
using EnvDTE;

namespace CoApp.VisualStudio.VsCore
{
    public class ProjectEventArgs : EventArgs
    {
        public ProjectEventArgs(Project project)
        {
            Project = project;
        }

        public Project Project { get; private set; }
    }
}
