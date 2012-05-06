using System.Collections.Generic;
using EnvDTE;
using CoGet.VisualStudio;

namespace CoGet.Dialog
{
    public class ProjectNode : ProjectNodeBase
    {
        private readonly Project _project;

        public Project Project
        {
            get
            {
                return _project;
            }
        }

        public ProjectNode(Project project) :
            base(project.GetDisplayName())
        {
            _project = project;
        }

        public override IEnumerable<Project> GetSelectedProjects()
        {
            if (IsSelected == true && IsEnabled)
            {
                yield return _project;
            }
        }
    }
}