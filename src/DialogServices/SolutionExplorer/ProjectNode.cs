using System.Collections.Generic;
using EnvDTE;
using CoGet.VisualStudio;

namespace CoGet.Dialog
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