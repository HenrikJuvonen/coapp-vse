using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using CoApp.Packaging.Common;
using CoApp.Packaging.Client;
using CoApp.Toolkit.Extensions;
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

        private List<ProgressEventArgs> _progress;
        
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

            _progress = new List<ProgressEventArgs>();
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

        private void Log(MessageLevel level, string message, params object[] args)
        {
            string formattedMessage = String.Format(CultureInfo.CurrentCulture, message, args);

            // for the dialog we ignore debug messages
            if (_providerServices.ProgressWindow.IsOpen && level != MessageLevel.Debug)
            {
                _providerServices.ProgressWindow.AddMessage(level, formattedMessage);
            }
        }

        protected virtual PackagesTreeNodeBase CreateTreeNodeForPackages(string name, string location, string type)
        {
            return new SimpleTreeNode(RootNode, this, name, location, type);
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

        /// <summary>
        /// Adds All-node, aggregate-nodes and feed-nodes.
        /// </summary>
        /// <param name="installed">If true, displays only installed packages.</param>
        protected void FillRootNodes(bool installed)
        {
            string type = installed ? "installed" : null;

            RootNode.Nodes.Add(CreateTreeNodeForPackages("All", null, type));

            IEnumerable<Feed> feeds = CoAppWrapper.GetFeeds();

            IEnumerable<string> hosts = new HashSet<string>(
                feeds.Select(f =>
                {
                    Uri uri = new Uri(f.Location);
                    return uri.Host;
                }));

            foreach (string host in hosts)
            {
                string aggregateName = string.IsNullOrEmpty(host) ? "Local" : host;

                AggregateTreeNode treeNode = new AggregateTreeNode(RootNode, this, aggregateName);

                foreach (Feed f in feeds)
                {
                    Uri uri = new Uri(f.Location);

                    if (uri.Host == host)
                    {
                        string name = uri.GetComponents(UriComponents.Path, UriFormat.UriEscaped);

                        if (aggregateName == "Local")
                        {
                            name = Path.GetFileNameWithoutExtension(name);
                        }

                        treeNode.Nodes.Add(new SimpleTreeNode(treeNode, this, name, f.Location, type));
                    }
                }

                RootNode.Nodes.Add(treeNode);
            }
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

            CoAppWrapper.ProgressProvider.ProgressAvailable += OnProgressAvailable;

            ClearProgressMessages();

            var worker = new BackgroundWorker();

            if (type == "core")
                worker.DoWork += OnRunWorkerDoWorkCore;
            else if (type == "manage")
                worker.DoWork += OnRunWorkerDoWorkManage;

            worker.RunWorkerCompleted += OnRunWorkerCompleted;
            worker.RunWorkerAsync(item);
        }
        
        private void ClearProgressMessages()
        {
            _progress.Clear();

            _providerServices.ProgressWindow.ClearMessages();
        }

        protected void ShowProgressWindow()
        {
            _providerServices.ProgressWindow.Show(ProgressWindowTitle, PackageManagerWindow.CurrentInstance);
        }

        protected void HideProgressWindow()
        {
            _providerServices.ProgressWindow.Hide();
        }

        protected void CloseProgressWindow()
        {
            _providerServices.ProgressWindow.Close();
        }

        private int TotalProgress(string operation)
        {
            int total = 0;
            int count = 0;

            foreach (var n in _progress.Where(n => n.Operation.Contains(operation)))
            {
                total += n.PercentComplete;
                count++;
            }

            return count == 0 ? 0 : total / count;
        }

        private void OnProgressAvailable(object sender, ProgressEventArgs e)
        {
            var progressItem = _progress.FirstOrDefault(n => n.Operation == e.Operation);

            if (progressItem == null)
            {
                _progress.Add(e);
                Log(MessageLevel.Info, e.Operation);
            }
            else
            {
                _progress.Remove(progressItem);
                _progress.Add(e);
            }

            string operation = e.Operation.Split(' ')[0];

            _providerServices.ProgressWindow.ShowProgress(operation + "...", TotalProgress(operation));

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

            CoAppWrapper.ProgressProvider.ProgressAvailable -= OnProgressAvailable;

            if (e.Error == null)
            {
                if (e.Cancelled)
                {
                    CloseProgressWindow();
                }
                else
                {
                    OnExecuteCompleted((PackageItem)e.Result);
                    _providerServices.ProgressWindow.SetCompleted(successful: true);
                }
            }
            else
            {
                // show error message in the progress window in case of error
                Log(MessageLevel.Error, e.Error.Unwrap().Message);
                _providerServices.ProgressWindow.SetCompleted(successful: false);
            }

            SelectedNode.Refresh(true);
        }
    }
}
