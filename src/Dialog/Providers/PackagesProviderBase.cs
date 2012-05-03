﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.ComponentModel;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using CoApp.Toolkit.Engine.Client;

namespace CoApp.VsExtension.Dialog.Providers
{
    internal abstract class PackagesProviderBase : VsExtensionsProvider
    {
        private PackagesSearchNode _searchNode;
        private PackagesTreeNodeBase _lastSelectedNode;
        private IList<IVsSortDescriptor> _sortDescriptors;
        
        public PackagesProviderBase()
        {
        }

        public override IVsExtensionsTreeNode ExtensionsTree
        { 
            get
            {
                if (RootNode == null)
                {
                    RootNode = new RootPackagesTreeNode(null, String.Empty);
                    CreateExtensionsTree();
                }

                return RootNode;
            }
        }

        protected void SelectNode(PackagesTreeNodeBase node)
        {
            node.IsSelected = true;
            SelectedNode = node;
        }

        private void RemoveSearchNode()
        {
            if (_searchNode != null)
            {

                // When remove the search node, the dialog will automatically select the first node (All node)
                // Since we are going to restore the previously selected node anyway, we don't want the first node
                // to refresh. Hence suppress it here.
                SuppressNextRefresh = true;

                try
                {
                    // dispose any search results
                    RootNode.Nodes.Remove(_searchNode);
                }
                finally
                {
                    _searchNode = null;
                    SuppressNextRefresh = false;
                }

                if (_lastSelectedNode != null)
                {
                    // after search, we want to reset the original node to page 1 (Work Item #461) 
                    _lastSelectedNode.CurrentPage = 1;
                    SelectNode(_lastSelectedNode);
                }
            }
        }

        private void AddSearchNode()
        {
            if (_searchNode != null && !RootNode.Nodes.Contains(_searchNode))
            {
                // remember the currently selected node so that when search term is cleared, we can restore it.
                _lastSelectedNode = SelectedNode;

                RootNode.Nodes.Add(_searchNode);
                SelectNode(_searchNode);
            }
        }
        
        public override IVsExtensionsTreeNode Search(string searchText)
        {
            if (OperationCoordinator.IsBusy)
            {
                return null;
            }

            if (!String.IsNullOrWhiteSpace(searchText) && SelectedNode != null)
            {
                searchText = searchText.Trim();
                if (_searchNode != null)
                {
                    _searchNode.Extensions.Clear();
                    _searchNode.SetSearchText(searchText);
                }
                else
                {
                    _searchNode = new PackagesSearchNode(this, RootNode, SelectedNode, searchText);
                    AddSearchNode();
                }
            }
            else
            {
                RemoveSearchNode();
            }

            return _searchNode;
        }

        private void CreateExtensionsTree()
        {
            // The user may have done a search before we finished getting the category list; temporarily remove it
            RemoveSearchNode();

            // give subclass a chance to populate the child nodes under Root node
            FillRootNodes();

            // Re-add the search node and select it if the user was doing a search
            AddSearchNode();
        }

        protected virtual void FillRootNodes()
        {
        }

        public virtual bool RefreshOnNodeSelection
        {
            get
            {
                return false;
            }
        }

        public PackagesTreeNodeBase SelectedNode
        {
            get;
            set;
        }

        public bool SuppressNextRefresh { get; private set; }

        /// <summary>
        /// Gets the root node of the tree
        /// </summary>
        protected IVsExtensionsTreeNode RootNode
        {
            get;
            set;
        }

        public PackageSortDescriptor CurrentSort
        {
            get;
            set;
        }

        public IList<IVsSortDescriptor> SortDescriptors
        {
            get
            {
                if (_sortDescriptors == null)
                {
                    _sortDescriptors = CreateSortDescriptors();
                }
                return _sortDescriptors;
            }
        }

        protected virtual IList<IVsSortDescriptor> CreateSortDescriptors()
        {
            return new List<IVsSortDescriptor> {
                new PackageSortDescriptor(String.Format(CultureInfo.CurrentCulture, "{0}: {1}", Resources.Dialog_SortOption_Name, Resources.Dialog_SortAscending), new[] { "Name" }, ListSortDirection.Ascending),
                new PackageSortDescriptor(String.Format(CultureInfo.CurrentCulture, "{0}: {1}", Resources.Dialog_SortOption_Name, Resources.Dialog_SortDescending), new[] { "Name" }, ListSortDirection.Descending)
            };
        }

        public override string ToString()
        {
            return Name;
        }

        public abstract IVsExtension CreateExtension(Package package);

        public abstract bool CanExecute(PackageItem item);

        protected virtual string GetProgressMessage(Package package)
        {
            return package.ToString();
        }

        /// <summary>
        /// This method is called on background thread.
        /// </summary>
        /// <returns><c>true</c> if the method succeeded. <c>false</c> otherwise.</returns>
        protected virtual bool ExecuteCore(PackageItem item)
        {
            return true;
        }

        protected virtual void OnExecuteCompleted(PackageItem item)
        {
            // After every operation, just update the status of all packages in the current node.
            // Strictly speaking, this is not required; only affected packages need to be updated.
            // But doing so would require us to keep a Dictionary<IPackage, PackageItem> which is not worth it.
            if (SelectedNode != null)
            {
                foreach (PackageItem node in SelectedNode.Extensions)
                {
                    node.UpdateEnabledStatus();
                }
            }
        }

        public virtual string NoItemsMessage
        {
            get
            {
                return String.Empty;
            }
        }

        public virtual string ProgressWindowTitle
        {
            get
            {
                return String.Empty;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Globalization",
            "CA1303:Do not pass literals as localized parameters",
            MessageId = "NuGet.Dialog.Providers.PackagesProviderBase.WriteLineToOutputWindow(System.String)",
            Justification = "No need to localize the --- strings"),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public virtual void Execute(PackageItem item)
        {
            if (OperationCoordinator.IsBusy)
            {
                return;
            }

            // disable all operations while this install is in progress
            OperationCoordinator.IsBusy = true;
            
            var worker = new BackgroundWorker();
            worker.DoWork += OnRunWorkerDoWork;
            worker.RunWorkerCompleted += OnRunWorkerCompleted;
            worker.RunWorkerAsync(item);

            // write an introductory sentence before every operation starts to make the console easier to read
            string progressMessage = GetProgressMessage(item.PackageIdentity);
        }


        private void OnRunWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            var item = (PackageItem)e.Argument;
            bool succeeded = ExecuteCore(item);
            e.Cancel = !succeeded;
            e.Result = item;
        }

        private void OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            OperationCoordinator.IsBusy = false;
            
            if (e.Error == null)
            {
                if (e.Cancelled)
                {
                    
                }
                else
                {
                    OnExecuteCompleted((PackageItem)e.Result);
                    
                }
            }
            else
            {
                // show error message in the progress window in case of error
            }
        }
    }
}
