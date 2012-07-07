using System.Collections.Generic;
using System.Windows.Media;
using EnvDTE;
using CoApp.VisualStudio.VsCore;

namespace CoApp.VisualStudio.Dialog
{
    public class ProjectNode : FolderNode
    {
        private static ImageSource _expandedIcon, _collapsedIcon;

        public ProjectNode(Project project, ICollection<ViewModelNodeBase> children) :
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

        public override ImageSource Icon
        {
            get
            {
                if (IsExpanded)
                {
                    if (_expandedIcon == null)
                    {
                        _expandedIcon = ProjectUtilities.GetImage(Project, true);
                    }
                    return _expandedIcon;
                }

                if (_collapsedIcon == null)
                {
                    _collapsedIcon = ProjectUtilities.GetImage(Project);
                }

                return _collapsedIcon;
            }
        } 
    }
}