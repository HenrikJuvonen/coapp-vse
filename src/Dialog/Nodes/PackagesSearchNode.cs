using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;
using CoApp.Toolkit.Engine.Client;
using System.Threading;

namespace CoApp.VsExtension.Dialog.Providers
{
    internal class PackagesSearchNode : PackagesTreeNodeBase
    {
        private string _searchText;
        private readonly PackagesTreeNodeBase _baseNode;

        public PackagesSearchNode(PackagesProviderBase provider, IVsExtensionsTreeNode parent, PackagesTreeNodeBase baseNode, string searchText) :
            base(parent, provider, baseNode.CollapseVersions)
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

        public override IQueryable<Package> GetPackages()
        {
            if (!(_baseNode is UpdatesTreeNode))
            {
                var simpleNode = _baseNode as SimpleTreeNode;
                if (simpleNode != null)
                {
                    return simpleNode.GetPackages().Find(_searchText);
                }
            }

            return _baseNode.GetPackages().Find(_searchText);
        }

        public override IQueryable<Package> GetDetailedPackages(IQueryable<Package> packages)
        {
            return GetPackages();
        }

        public override void SetCancellationTokenSourceForRepository(CancellationTokenSource cts)
        {
            
        }
    }
}