using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;
using CoApp.Packaging.Common;

namespace CoApp.VisualStudio.Dialog.Providers
{
    internal class PackagesSearchNode : PackagesTreeNodeBase
    {
        private string _searchText;
        private readonly PackagesTreeNodeBase _baseNode;

        public PackagesSearchNode(PackagesProviderBase provider, IVsExtensionsTreeNode parent, PackagesTreeNodeBase baseNode, string searchText) :
            base(parent, provider)
        {

            if (baseNode == null)
            {
                throw new ArgumentNullException("baseNode");
            }

            _searchText = searchText;
            _baseNode = baseNode;

            // Mark this node as a SearchResults node to assist navigation in ExtensionsExplorer
            IsSearchResultsNode = true;
        }

        public PackagesTreeNodeBase BaseNode
        {
            get
            {
                return _baseNode;
            }
        }
        
        public override string Name
        {
            get
            {
                return Resources.Dialog_RootNodeSearch;
            }
        }

        public void SetSearchText(string newSearchText)
        {
            if (newSearchText == null)
            {
                throw new ArgumentNullException("newSearchText");
            }

            _searchText = newSearchText;

            if (IsSelected)
            {
                ResetQuery();
                LoadPage(1);
            }
        }

        public override IEnumerable<IPackage> GetPackages()
        {
            return from p in _baseNode.GetPackages()
                   where p.CanonicalName.PackageName.ToLower().Contains(_searchText.ToLower())
                   select p;
        }
    }
}