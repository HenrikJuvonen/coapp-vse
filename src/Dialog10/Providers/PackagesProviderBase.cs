using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using CoApp.Packaging.Common;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;

namespace CoApp.VisualStudio.Dialog.Providers
{
    internal abstract class PackagesProviderBase : VsExtensionsProvider
    {
        private readonly ResourceDictionary _resources;
        private readonly ProviderServices _providerServices;
        private object _mediumIconDataTemplate;
        private object _detailViewDataTemplate;

        private PackagesSearchNode _searchNode;
        private PackagesTreeNodeBase _lastSelectedNode;
        private IList<IVsSortDescriptor> _sortDescriptors;
                
        public PackagesProviderBase(ResourceDictionary resources,
                                    ProviderServices providerServices)
        {
            if (resources == null)
            {
                throw new ArgumentNullException("resources");
            }
            if (providerServices == null)
            {
                throw new ArgumentNullException("providerServices");
            }

            _providerServices = providerServices;
            _resources = resources;
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

        public override object MediumIconDataTemplate
        {
            get
            {
                if (_mediumIconDataTemplate == null)
                {
                    _mediumIconDataTemplate = _resources["PackageItemTemplate"];
                }
                return _mediumIconDataTemplate;
            }
        }

        public override object DetailViewDataTemplate
        {
            get
            {
                if (_detailViewDataTemplate == null)
                {
                    _detailViewDataTemplate = _resources["PackageDetailTemplate"];
                }
                return _detailViewDataTemplate;
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

            return (IVsExtensionsTreeNode)_searchNode;
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

        /// <summary>
        /// Adds All-node, aggregate-nodes and feed-nodes.
        /// </summary>
        /// <param name="installed">If true, displays only installed packages.</param>
        protected void FillRootNodes(bool installed)
        {
            string type = installed ? "installed" : null;

            RootNode.Nodes.Add((IVsExtensionsTreeNode)new SimpleTreeNode(RootNode, this, "All", null, type));

            // Keep it responsive...
            Task.Factory.StartNew(() =>
                {
                    IEnumerable<string> feeds = CoAppWrapper.GetFeedLocations();

                    IEnumerable<string> hosts = new HashSet<string>(
                        feeds.Select(f =>
                        {
                            Uri uri = new Uri(f);
                            return uri.Host;
                        }));

                    foreach (string host in hosts)
                    {
                        string aggregateName = string.IsNullOrEmpty(host) ? "Local" : host;

                        var treeNode = (IVsExtensionsTreeNode)new AggregateTreeNode(RootNode, this, aggregateName);

                        foreach (string f in feeds)
                        {
                            Uri uri = new Uri(f);

                            if (uri.Host == host)
                            {
                                string name = uri.GetComponents(UriComponents.Path, UriFormat.UriEscaped);

                                if (aggregateName == "Local")
                                {
                                    name = Path.GetFileNameWithoutExtension(name);
                                }

                                treeNode.Nodes.Add((IVsExtensionsTreeNode)new SimpleTreeNode(treeNode, this, name, f, type));
                            }
                        }

                        Application.Current.Dispatcher.Invoke(new Action(() => RootNode.Nodes.Add(treeNode)));
                    }
                });
        }

        protected virtual void FillRootNodes()
        {
        }

        public virtual bool RefreshOnNodeSelection
        {
            get
            {
                return true;
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
                new PackageSortDescriptor(String.Format(CultureInfo.CurrentCulture, "{0}: {1}", Resources.Dialog_SortOption_Name, Resources.Dialog_SortAscending), "CanonicalName", ListSortDirection.Ascending),
                new PackageSortDescriptor(String.Format(CultureInfo.CurrentCulture, "{0}: {1}", Resources.Dialog_SortOption_Name, Resources.Dialog_SortDescending), "CanonicalName", ListSortDirection.Descending)
            };
        }

        public override string ToString()
        {
            return Name;
        }

        public abstract IVsExtension CreateExtension(IPackage package);

        public virtual bool CanExecuteCore(PackageItem item)
        {
            return false;
        }

        public virtual bool CanExecuteManage(PackageItem item)
        {
            return false;
        }

        /// <summary>
        /// This method is called on background thread.
        /// </summary>
        /// <returns><c>true</c> if the method succeeded. <c>false</c> otherwise.</returns>
        protected virtual bool ExecuteCore(PackageItem item)
        {
            return true;
        }

        /// <summary>
        /// This method is called on background thread.
        /// </summary>
        /// <returns><c>true</c> if the method succeeded. <c>false</c> otherwise.</returns>
        protected virtual bool ExecuteManage(PackageItem item)
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

        public virtual void Execute(PackageItem item, string type)
        {
            if (OperationCoordinator.IsBusy)
            {
                return;
            }

            // disable all operations while this operation is in progress
            OperationCoordinator.IsBusy = true;

            CoAppWrapper.ProgressProvider.ProgressAvailable += _providerServices.WaitDialog.OnProgressAvailable;
            
            var worker = new BackgroundWorker();

            if (type == "core")
                worker.DoWork += OnRunWorkerDoWorkCore;
            else if (type == "manage")
                worker.DoWork += OnRunWorkerDoWorkManage;

            worker.RunWorkerCompleted += OnRunWorkerCompleted;
            worker.RunWorkerAsync(item);
        }

        protected void ShowWaitDialog()
        {
            _providerServices.WaitDialog.Show(ProgressWindowTitle, PackageManagerWindow.CurrentInstance);
        }

        private void OnRunWorkerDoWorkCore(object sender, DoWorkEventArgs e)
        {
            var item = (PackageItem)e.Argument;
            bool succeeded = ExecuteCore(item);
            e.Cancel = !succeeded;
            e.Result = item;
        }

        private void OnRunWorkerDoWorkManage(object sender, DoWorkEventArgs e)
        {
            var item = (PackageItem)e.Argument;
            bool succeeded = ExecuteManage(item);
            e.Cancel = !succeeded;
            e.Result = item;
        }

        private void OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            OperationCoordinator.IsBusy = false;

            if (e.Error == null)
            {
                if (!e.Cancelled)
                {
                    OnExecuteCompleted((PackageItem)e.Result);
                }
            }

            _providerServices.WaitDialog.Hide();

            Thread.Sleep(10);

            CoAppWrapper.ProgressProvider.ProgressAvailable -= _providerServices.WaitDialog.OnProgressAvailable;

            SelectedNode.Refresh(true);
        }
    }
}
