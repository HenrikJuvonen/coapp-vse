using System.Collections.Generic;
using EnvDTE;

namespace CoApp.VisualStudio.Dialog
{
    public class ConfigurationNode : FolderNode
    {
        public ConfigurationNode(Project project, string name, ICollection<ProjectNodeBase> children)
            : base(project, name, children)
        {
        }

    }
}