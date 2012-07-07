using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CoApp.Packaging.Common;
using CoApp.Toolkit.Extensions;
using CoApp.Toolkit.Configuration;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace CoApp.VisualStudio.Dialog.Providers
{
    internal abstract class PackagesTreeNodeBase : IVsExtensionsTreeNode, IVsPageDataSource, IVsSortDataSource, IVsProgressPaneConsumer, INotifyPropertyChanged, IVsMessagePaneConsumer
    {
        // We cache the query until it changes (due to search)
        private IEnumerable<IPackage> _query;
        private int _totalCount;

#if VS10
        private IList<IVsExtension> _extensions;
#else
        private IList _extensions;
#endif

        private IList<IVsExtensionsTreeNode> _nodes;
        private int _totalPages = 1, _currentPage = 1;
        private bool _progressPaneActive;
        private bool _isExpanded;
        private bool _isSelected;
        private bool _loadingInProgress;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<EventArgs> PageDataChanged;

        protected PackagesTreeNodeBase(IVsExtensionsTreeNode parent, PackagesProviderBase provider)
        {
            Parent = parent;
            Provider = provider;
            
            try
            {
                PageSize = CoAppWrapper.Settings["#itemsOnPage"].IntValue;

                if (PageSize < 5)
                {
                    PageSize = 5;
                }
                else if (PageSize > 1000)
                {
                    PageSize = 1000;
                }
            }
            catch
            {
                PageSize = 100;
            }
        }

        private PackagesProviderBase Provider
        {
            get; set;
        }

        private IVsProgressPane ProgressPane
        {
            get;
            set;
        }

        private IVsMessagePane MessagePane
        {
            get;
            set;
        }

        /// <summary>
        /// Name of this node
        /// </summary>
        public abstract string Name
        {
            get;
        }

        public bool IsSearchResultsNode
        {
            get;
            set;
        }

        /// <summary>
        /// Select node (UI) property
        /// This property maps to TreeViewItem.IsSelected
        /// </summary>
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnNotifyPropertyChanged("IsSelected");
                }
            }
        }

        /// <summary>
        /// Expand node (UI) property
        /// This property maps to TreeViewItem.IsExpanded
        /// </summary>
        public bool IsExpanded
        {
            get
            {
                return _isExpanded;
            }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnNotifyPropertyChanged("IsExpanded");
                }
            }
        }

        public IEnumerable<IPackage> Query
        {
            get
            {
                return _query;
            }
        }

        /// <summary>
        /// List of templates at this node
        /// </summary>
#if VS10
        public IList<IVsExtension> Extensions
#else
        public IList Extensions
#endif
        {
            get
            {
                if (_extensions == null)
                {
                    EnsureExtensionCollection();
                    LoadPage(1);
                }

                return _extensions;
            }
        }

        /// <summary>
        /// Children at this node
        /// </summary>
        public IList<IVsExtensionsTreeNode> Nodes
        {
            get
            {
                if (_nodes == null)
                {
                    _nodes = new ObservableCollection<IVsExtensionsTreeNode>();
                }
                return _nodes;
            }
        }
        /// <summary>
        /// Parent of this node
        /// </summary>
        public IVsExtensionsTreeNode Parent
        {
            get;
            private set;
        }

        public int TotalPages
        {
            get
            {
                return _totalPages;
            }
            private set
            {
                _totalPages = value;
                NotifyPropertyChanged();
            }
        }

        public int CurrentPage
        {
            get
            {
                return _currentPage;
            }
            internal set
            {
                _currentPage = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Raised when the current node completes loading packages.
        /// </summary>
        internal event EventHandler PackageLoadCompleted = delegate { };

        private int PageSize
        {
            get;
            set;
        }

        /// <summary>
        /// Refresh the list of packages belong to this node
        /// </summary>
        public void Refresh(bool resetQueryBeforeRefresh = false)
        {
            if (resetQueryBeforeRefresh)
            {
                ResetQuery();
            }
            LoadPage(CurrentPage);
        }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Get all packages belonging to this node.
        /// </summary>
        public abstract IEnumerable<IPackage> GetPackages();
                
        /// <summary>
        /// Helper function to raise property changed events
        /// </summary>
        private void NotifyPropertyChanged()
        {
            if (PageDataChanged != null)
            {
                PageDataChanged(this, EventArgs.Empty);
            }
        }
        
        /// <summary>
        /// Loads the packages in the specified page.
        /// </summary>
        public void LoadPage(int pageNumber)
        {
            if (_loadingInProgress)
                return;

            EnsureExtensionCollection();

            Extensions.Clear();

            ShowProgressPane();

            // avoid more than one loading occurring at the same time
            _loadingInProgress = true;

            TaskScheduler uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            Task.Factory.StartNew(() => ExecuteAsync(pageNumber)).ContinueWith(QueryExecutionCompleted, uiScheduler);
        }

        private void EnsureExtensionCollection()
        {
            if (_extensions == null)
            {
                _extensions = new ObservableCollection<IVsExtension>();
            }
        }
        
        /// <summary>
        /// This method executes on background thread.
        /// </summary>
        private LoadPageResult ExecuteAsync(int pageNumber)
        {
            if (_query == null)
            {
                CoAppWrapper.SetNewCancellationTokenSource();

                _query = GetPackages();
            }

            var orderedQuery = ApplyOrdering(_query);

            var filteredQuery = ApplyFiltering(orderedQuery);

            _totalCount = filteredQuery.Count();
            
            var packages = filteredQuery.Skip((pageNumber - 1) * PageSize).Take(PageSize);

            return new LoadPageResult(packages, pageNumber, _totalCount);
        }

        protected virtual IEnumerable<IPackage> ApplyFiltering(IEnumerable<IPackage> query)
        {            
            return CoAppWrapper.FilterPackages(query, VsCore.VsVersionHelper.VsMajorVersion);
        }

        private IEnumerable<IPackage> ApplyOrdering(IEnumerable<IPackage> query)
        {
            return Provider.CurrentSort.Direction == ListSortDirection.Descending ? query.OrderByDescending(n => n.CanonicalName) :
                                                                                    query.OrderBy(n => n.CanonicalName);
        }

        public IList<IVsSortDescriptor> GetSortDescriptors()
        {
            // Get the sort descriptor from the provider
            return Provider.SortDescriptors;
        }

        protected internal void ResetQuery()
        {
            _query = null;
        }

        public bool SortSelectionChanged(IVsSortDescriptor selectedDescriptor)
        {
            Provider.CurrentSort = selectedDescriptor as PackageSortDescriptor;

            if (Provider.CurrentSort != null)
            {
                // Reload the first page since the sort order changed
                LoadPage(1);
                return true;
            }

            return false;
        }

        private void QueryExecutionCompleted(Task<LoadPageResult> task)
        {
            // If a task throws, the exception must be handled or the Exception
            // property must be accessed or the exception will tear down the process when finalized
            Exception exception = task.Exception;
            
            _loadingInProgress = false;

            // Only process the result if this node is still selected.
            if (IsSelected)
            {
                if (task.IsCanceled)
                {
                    HideProgressPane();
                    
                }
                else if (task.IsFaulted)
                {
                    // show error message in the Message pane
                    ShowMessagePane(exception.Unwrap().Message);
                }
                else
                {
                    var result = task.Result;

                    UpdateNewPackages(result.Packages);
                    
                    int totalPages = (result.TotalCount + PageSize - 1) / PageSize;
                    TotalPages = Math.Max(1, totalPages);
                    CurrentPage = Math.Max(1, result.PageNumber);

                    HideProgressPane();
                }
            }

            PackageLoadCompleted(this, EventArgs.Empty);
        }

        private void UpdateNewPackages(IEnumerable<IPackage> packages)
        {
            _extensions.Clear();

            foreach (var package in packages)
            {
                _extensions.Add(Provider.CreateExtension(package));
            }

            if (_extensions.Count > 0)
            {
                // select the first package by default
                ((IVsExtension)_extensions[0]).IsSelected = true;
            }
        }

        public void RefreshSelectedPackage()
        {
            for (int i = 0; i < _extensions.Count; i++)
            {
                var item = (PackageItem)_extensions[i];

                if (item != null && item.IsSelected)
                {
                    ShowProgressPane();

                    _extensions[i] = Provider.CreateExtension(CoAppWrapper.GetPackage(item.PackageIdentity.CanonicalName));

                    item = (PackageItem)_extensions[i];
                    item.IsSelected = true;

                    HideProgressPane();
                    
                    return;
                }
            }
        }

        private void OnNotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void SetProgressPane(IVsProgressPane progressPane)
        {
            ProgressPane = progressPane;
        }

        public void SetMessagePane(IVsMessagePane messagePane)
        {
            MessagePane = messagePane;
        }

        private bool ShowProgressPane()
        {
            if (ProgressPane != null)
            {
                _progressPaneActive = true;
                return ProgressPane.Show(null, false);
            }
            return false;
        }

        private void HideProgressPane()
        {
            if (_progressPaneActive && ProgressPane != null)
            {
                ProgressPane.Close();
                _progressPaneActive = false;
            }
        }

        private bool ShowMessagePane(string message)
        {
            if (MessagePane != null)
            {
                MessagePane.SetMessageThreadSafe(message);
                return MessagePane.Show();
            }
            return false;
        }

        /// <summary>
        /// Called when this node is opened.
        /// </summary>
        internal void OnOpened()
        {
            if (!Provider.SuppressNextRefresh)
            {
                _loadingInProgress = false;
                Provider.SelectedNode = this;
                if (!IsSearchResultsNode)
                {
                    Refresh();
                }
            }
        }
    }
}
