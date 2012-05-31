using System.Collections.Generic;
using System.Windows.Media;
using EnvDTE;
using CoApp.VisualStudio.VsCore;

namespace CoApp.VisualStudio.Dialog
{
    public class SolutionNode : FolderNode
    {
        public SolutionNode(Project project, string name, ICollection<ViewModelNodeBase> children) :
            base(project, name, children)
        {
        }

        public override ImageSource Icon
        {
            get
            {
                return ProjectUtilities.GetSolutionImage();
            }
        }
    }
}