using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace CoApp.VisualStudio.Dialog.Providers
{
    /// <summary>
    /// Base implementation of IVsExtensionsTreeNode
    /// </summary>
    public class RootPackagesTreeNode : IVsExtensionsTreeNode
    {
        private readonly IList<IVsExtensionsTreeNode> _nodes = new ObservableCollection<IVsExtensionsTreeNode>();

        #region IVsExtensionsTreeNode Members

#if VS10
        private readonly IList<IVsExtension> _extensions = new ObservableCollection<IVsExtension>();

        public IList<IVsExtension> Extensions
        {
            get { return _extensions; }
        }
#else
        private readonly IList _extensions = new ObservableCollection<IVsExtension>();

        public IList Extensions
        {
            get { return _extensions; }
        }
#endif

        public bool IsSearchResultsNode
        {
            get { return false; }
        }

        public bool IsExpanded
        {
            get;
            set;
        }

        public bool IsSelected
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public IList<IVsExtensionsTreeNode> Nodes
        {
            get { return _nodes; }
        }

        public IVsExtensionsTreeNode Parent
        {
            get;
            set;
        }

        #endregion

        public RootPackagesTreeNode(IVsExtensionsTreeNode parent, string name)
        {
            Parent = parent;
            Name = name;
        }
    }
}
