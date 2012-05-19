using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using EnvDTE;

namespace CoGet.Dialog
{
    public class ConfigurationNode : FolderNode
    {
        public ConfigurationNode(Project project, string name, ICollection<ProjectNodeBase> children)
            : base(project, name, children)
        {
        }

    }
}