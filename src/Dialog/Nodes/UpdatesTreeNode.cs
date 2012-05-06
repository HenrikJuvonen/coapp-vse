using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.VisualStudio.ExtensionsExplorer;
using CoApp.Toolkit.Engine.Client;

namespace CoGet.Dialog.Providers
{
    /// <summary>
    /// This class represents a tree node under the Updates tab
    /// </summary>
    internal class UpdatesTreeNode : SimpleTreeNode
    {
        public UpdatesTreeNode(
            PackagesProviderBase provider,
            string category,
            IVsExtensionsTreeNode parent,
            IPackageRepository respository) :
            base(provider, category, parent, respository)
        {
        }
    }
}