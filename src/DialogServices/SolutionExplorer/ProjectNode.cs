using System.Collections.Generic;
using EnvDTE;
using CoApp.VisualStudio.VsCore;

namespace CoApp.VisualStudio.Dialog
{
    public class ProjectNode : FolderNode
    {
        public ProjectNode(Project project, ICollection<ProjectNodeBase> children) :
            base(project, project.GetDisplayName(), children)
        {
        }

        public override IEnumerable<Project> GetSelectedProjects()
        {
            if (IsSelected != false && IsEnabled)
            {
                yield return _project;
            }
        }
    }
}