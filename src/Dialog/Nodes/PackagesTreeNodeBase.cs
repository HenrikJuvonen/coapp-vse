using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ExtensionsExplorer;
//using CoApp.VsExtension.VisualStudio;
using CoApp.Toolkit.Engine.Client;

namespace CoApp.VsExtension.Dialog.Providers
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    internal abstract class PackagesTreeNodeBase : IVsExtensionsTreeNode, IVsPageDataSource, IVsSortDataSource, IVsProgressPaneConsumer, INotifyPropertyChanged, IVsMessagePaneConsumer
    {

        // The number of extensions to show per page.
        private const int DefaultItemsPerPage = 10;

        // We cache the query until it changes (due to sort order or search)
        private IEnumerable<Package> _query;
        private int _totalCount;

        private IList<IVsExtension> _extensions;

        private IList<IVsExtensionsTreeNode> _nodes;
        private int _totalPages = 1, _currentPage = 1;
        private bool _progressPaneActive;
        private bool _isExpanded;
        private bool _isSelected;
        private bool _loadingInProgress;
        private readonly bool _collapseVersions;

        private CancellationTokenSource _currentCancellationSource;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<EventArgs> PageDataChanged;

        protected PackagesTreeNodeBase(IVsExtensionsTreeNode parent, PackagesProviderBase provider, bool collapseVersions = true)
        {
            Debug.Assert(provider != null);

            _collapseVersions = collapseVersions;
            Parent = parent;
            Provider = provider;
            PageSize = DefaultItemsPerPage;
        }

        public bool CollapseVersions
        {
            get
            {
                return _collapseVersions;
            }
        }

        protected PackagesProviderBase Provider
        {
            get;
            private set;
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

        /// <summary>
        /// List of templates at this node
        /// </summary>
        public IList<IVsExtension> Extensions
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
            internal set
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

        internal int PageSize
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
        /// <returns></returns>
        public abstract IQueryable<Package> GetPackages();

        public abstract IQueryable<Package> GetDetailedPackages(IQueryable<Package> packages);
        
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

        public abstract void SetCancellationTokenSourceForRepository(CancellationTokenSource cts);

        /// <summary>
        /// Loads the packages in the specified page.
        /// </summary>
        /// <param name="pageNumber"></param>
        public void LoadPage(int pageNumber)
        {
            if (pageNumber < 1)
            {
                throw new ArgumentOutOfRangeException(
                    "pageNumber",
                    String.Format(CultureInfo.CurrentCulture, CommonResources.Argument_Must_Be_GreaterThanOrEqualTo, 1));
            }

            Debug.WriteLine("Dialog loading page: " + pageNumber);
            if (_loadingInProgress)
            {
                return;
            }

            EnsureExtensionCollection();

            // Bug #1930: this will clear the content of details pane
            Extensions.Clear();

            ShowProgressPane();

            // avoid more than one loading occurring at the same time
            _loadingInProgress = true;

            _currentCancellationSource = new CancellationTokenSource();

            SetCancellationTokenSourceForRepository(_currentCancellationSource);

            TaskScheduler uiScheduler;
            try
            {
                uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            }
            catch (InvalidOperationException)
            {
                // FromCurrentSynchronizationContext() fails when running from unit test
                uiScheduler = TaskScheduler.Default;
            }

            Task.Factory.StartNew(
                (state) => ExecuteAsync(pageNumber, _currentCancellationSource.Token),
                _currentCancellationSource,
                _currentCancellationSource.Token).ContinueWith(QueryExecutionCompleted, uiScheduler);
        }

        private void EnsureExtensionCollection()
        {
            if (_extensions == null)
            {
                _extensions = new ObservableCollection<IVsExtension>();
            }
        }

        /// <summary>
        /// Called when user clicks on the Cancel button in the progress pane.
        /// </summary>
        private void CancelCurrentExtensionQuery()
        {
            Debug.WriteLine("Cancelling pending extensions query.");

            if (_currentCancellationSource != null)
            {
                _currentCancellationSource.Cancel();
                _loadingInProgress = false;
            }
        }

        /// <summary>
        /// This method executes on background thread.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We want to show error message inside the dialog, rather than blowing up VS.")]
        private LoadPageResult ExecuteAsync(int pageNumber, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (_query == null)
            {
                IQueryable<Package> query = GetPackages();

                token.ThrowIfCancellationRequested();

                // Execute the total count query
                _totalCount = query.Count();

                // make sure we don't query a page that is greater than the maximum page number.
                int maximumPages = (_totalCount + PageSize - 1)/PageSize;
                pageNumber = Math.Min(pageNumber, maximumPages);

                token.ThrowIfCancellationRequested();

                // Apply the ordering then sort by id
                IQueryable<Package> orderedQuery = ApplyOrdering(query).ThenBy(p => p.CanonicalName);

                // Buffer 3 pages
                _query = orderedQuery.AsBufferedEnumerable(PageSize * 3);

                if (CollapseVersions)
                {
                    // If we are connecting to an older gallery implementation, we need to use the Published field. 
                    // For newer gallery, the package is never unpublished, it is only unlisted.
                    //_query = _query.Where(PackageExtensions.IsListed).AsCollapsed();
                }
            }

            IQueryable<Package> pkx = _query.Skip((pageNumber - 1) * PageSize)
                                             .Take(PageSize).AsQueryable();

            pkx = GetDetailedPackages(pkx);

            IList<Package> packages = pkx.ToList();

            // get detailed packages here
            
            if (packages.Count < PageSize)
            {
                _totalCount = (pageNumber - 1) * PageSize + packages.Count;
            }

            token.ThrowIfCancellationRequested();

            return new LoadPageResult(packages, pageNumber, _totalCount);
        }

        private IOrderedQueryable<Package> ApplyOrdering(IQueryable<Package> query)
        {
            // If the default sort is null then fall back to download count
            if (Provider.CurrentSort == null)
            {
                return query.OrderByDescending(p => p.CanonicalName);
            }

            // Order by the current descriptor
            return query.SortBy<Package>(Provider.CurrentSort.SortProperties, Provider.CurrentSort.Direction, typeof(Package));
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
                // If we changed the sort order then invalidate the cache.
                ResetQuery();

                // Reload the first page since the sort order changed
                LoadPage(1);
                return true;
            }

            return false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We don't want it to crash VS.")]
        private void QueryExecutionCompleted(Task<LoadPageResult> task)
        {
            // If a task throws, the exception must be handled or the Exception
            // property must be accessed or the exception will tear down the process when finalized
            Exception exception = task.Exception;

            if (task.IsFaulted)
            {
                
            }

            var cancellationSource = (CancellationTokenSource)task.AsyncState;
            if (cancellationSource != _currentCancellationSource)
            {
                return;
            }

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
                    ShowMessagePane(ExceptionUtility.Unwrap(exception).Message);
                }
                else
                {
                    LoadPageResult result = task.Result;

                    UpdateNewPackages(result.Packages.ToList());
                    
                    int totalPages = (result.TotalCount + PageSize - 1) / PageSize;
                    TotalPages = Math.Max(1, totalPages);
                    CurrentPage = Math.Max(1, result.PageNumber);

                    HideProgressPane();
                }
            }

            PackageLoadCompleted(this, EventArgs.Empty);
        }

        private void UpdateNewPackages(IList<Package> packages)
        {
            int newPackagesIndex = 0;
            int oldPackagesIndex = 0;
            while (oldPackagesIndex < _extensions.Count)
            {
                if (newPackagesIndex >= packages.Count)
                {
                    _extensions.RemoveAt(oldPackagesIndex);
                }
                else
                {
                    PackageItem currentOldItem = (PackageItem)_extensions[oldPackagesIndex];
                    if (packages[newPackagesIndex].CanonicalName == currentOldItem.PackageIdentity.CanonicalName)
                    {
                        newPackagesIndex++;
                        oldPackagesIndex++;
                    }
                    else
                    {
                        _extensions.RemoveAt(oldPackagesIndex);
                    }
                }
            }

            while (newPackagesIndex < packages.Count)
            {
                _extensions.Add(Provider.CreateExtension(packages[newPackagesIndex++]));
            }
           
            if (_extensions.Count > 0)
            {
                // select the first package by default
                ((IVsExtension)_extensions[0]).IsSelected = true;
            }
        }

        protected void OnNotifyPropertyChanged(string propertyName)
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

        protected bool ShowProgressPane()
        {
            if (ProgressPane != null)
            {
                _progressPaneActive = true;
                return ProgressPane.Show(new CancelProgressCallback(CancelCurrentExtensionQuery), true);
            }
            else
            {
                return false;
            }
        }

        protected void HideProgressPane()
        {
            if (_progressPaneActive && ProgressPane != null)
            {
                ProgressPane.Close();
                _progressPaneActive = false;
            }
        }

        protected bool ShowMessagePane(string message)
        {
            if (MessagePane != null)
            {
                MessagePane.SetMessageThreadSafe(message);
                return MessagePane.Show();
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Called when this node is opened.
        /// </summary>
        internal void OnOpened()
        {
            if (!Provider.SuppressNextRefresh)
            {
                Provider.SelectedNode = this;
                if (Provider.RefreshOnNodeSelection && !this.IsSearchResultsNode)
                {
                    Refresh();
                }
            }
        }
    }
}
