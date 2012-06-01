using System.Collections.Generic;
using System.Windows.Media;
using EnvDTE;

namespace CoApp.VisualStudio.Dialog
{
    public class ConfigurationNode : FolderNode
    {
        private static ImageSource _expandedIcon, _collapsedIcon;

        public ConfigurationNode(Project project, string name, ICollection<ViewModelNodeBase> children)
            : base(project, name, children)
        {
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
                else
                {
                    if (_collapsedIcon == null)
                    {
                        _collapsedIcon = ProjectUtilities.GetImage(Project);
                    }
                    return _collapsedIcon;
                }
            }
        }
    }
}