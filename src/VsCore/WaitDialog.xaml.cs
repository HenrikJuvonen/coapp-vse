using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.VisualStudio.PlatformUI;
using CoApp.Packaging.Common;

namespace CoApp.VisualStudio.VsCore
{
    /// <summary>
    /// Interaction logic for WaitDialog3.xaml
    /// </summary>
    public partial class WaitDialog : DialogWindow
    {
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        ObservableCollection<ProgressEvent> table;

        private bool IsOpen;

        public bool IsCanceled { get; set; }

        public WaitDialog()
        {
            InitializeComponent();
            
            table = new ObservableCollection<ProgressEvent>();

            DataContext = new WaitDialogViewModel(ref table);
        }

        public void OnProgressAvailable(object sender, ProgressEventArgs e)
        {
            if (e.Operation == "Error")
            {
                Hide();
                MessageHelper.ShowErrorMessage(e.Message, null);
                return;
            }
            else if (e.Operation == "Info")
            {
                Hide();
                MessageHelper.ShowInfoMessage(e.Message, null);
                return;
            }

            Update(e.Operation + " ...", e.Message, e.PercentComplete);
        }

        public void Show(string operation, Window owner = null)
        {
            if (!Dispatcher.CheckAccess())
            {
                // must use BeginInvoke() here to avoid blocking the worker thread
                Dispatcher.BeginInvoke(new Action<string, Window>(Show), operation, owner);
                return;
            }

            Clear();

            Operation.Text = operation;

            if (!IsOpen)
            {
                Owner = owner;
                IsOpen = true;
                ShowModal();
            }
        }

        public new void Hide()
        {
            if (!Dispatcher.CheckAccess())
            {
                // must use BeginInvoke() here to avoid blocking the worker thread
                Dispatcher.BeginInvoke(new Action(Hide));
                return;
            }

            IsOpen = false;
            base.Hide();
        }

        public void Update(string operation)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action<string>(Update), operation);
                return;
            }

            Operation.Text = operation;
        }

        private void Clear()
        {
            Operation.Text = null;
            CancelButton.IsEnabled = true;
            IsCanceled = false;
            table.Clear();
        }

        public bool? ShowQueryDialog(string message, bool showCancelButton)
        {
            if (!Dispatcher.CheckAccess())
            {
                return (bool?)Dispatcher.Invoke(new Func<string, bool, bool?>(ShowQueryDialog), message, showCancelButton);
            }

            var result = MessageBox.Show(
                            this,
                            message,
                            VsResources.DialogTitle,
                            showCancelButton ? MessageBoxButton.YesNoCancel : MessageBoxButton.YesNo,
                            MessageBoxImage.Question,
                            MessageBoxResult.None);

            if (result == MessageBoxResult.Cancel)
            {
                return null;
            }
            else
            {
                return (result == MessageBoxResult.Yes);
            }
        }
        
        private void Update(string operation, string message, int percentComplete)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action<string, string, int>(Update), operation, message, percentComplete);
                return;
            }

            if (percentComplete < 0)
            {
                percentComplete = 0;
            }
            else if (percentComplete >= 100)
            {
                percentComplete = 100;

                if (operation != "Downloading ...")
                {
                    operation = "Complete";
                }
            }
            
            var canonicalName = CanonicalName.Parse(message);
            message = string.Format("{0}{1}-{2}-{3}", canonicalName.Name, canonicalName.Flavor,
                                                      canonicalName.Version, canonicalName.Architecture);

            var tableItem = table.FirstOrDefault(n => n.Message == message);

            if (tableItem != null)
            {
                tableItem.Operation = operation;
                tableItem.PercentComplete = percentComplete;
            }
            else
            {
                var e = new ProgressEvent();
                e.Message = message;
                e.Operation = operation;
                e.PercentComplete = percentComplete;
                table.Add(e);
            }

            if (DataGrid.Items.Count > 0)
            {
                var border = VisualTreeHelper.GetChild(DataGrid, 0) as Decorator;
                if (border != null)
                {
                    var scroll = border.Child as ScrollViewer;
                    if (scroll != null) scroll.ScrollToEnd();
                }
            }
        }
        
        private void OnCanceled(object sender, RoutedEventArgs e)
        {
            CoAppWrapper.CancellationTokenSource.Cancel();
            CancelButton.IsEnabled = false;
            IsCanceled = true;
            Operation.Text = "Canceling ...";
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
    }

    internal class WaitDialogViewModel
    {
        public ICollectionView Table { get; private set; }

        public WaitDialogViewModel(ref ObservableCollection<ProgressEvent> table)
        {
            Table = CollectionViewSource.GetDefaultView(table);
        }
    }

    internal class ProgressEvent : INotifyPropertyChanged
    {
        private string _message;
        private string _operation;
        private int _percentComplete;

        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                NotifyPropertyChanged("Message");
            }
        }

        public string Operation
        {
            get { return _operation; }
            set
            {
                _operation = value;
                NotifyPropertyChanged("Operation");
            }
        }

        public int PercentComplete
        {
            get { return _percentComplete; }
            set
            {
                _percentComplete = value;
                NotifyPropertyChanged("PercentComplete");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
