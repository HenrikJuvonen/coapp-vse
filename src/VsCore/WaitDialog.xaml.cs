using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.VisualStudio.PlatformUI;

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

        private readonly Dispatcher _uiDispatcher;

        private bool IsOpen;

        public WaitDialog()
        {
            InitializeComponent();

            _uiDispatcher = Dispatcher.CurrentDispatcher;
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
            if (!_uiDispatcher.CheckAccess())
            {
                // must use BeginInvoke() here to avoid blocking the worker thread
                _uiDispatcher.BeginInvoke(new Action<string, Window>(Show), operation, owner);
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
            if (!_uiDispatcher.CheckAccess())
            {
                // must use BeginInvoke() here to avoid blocking the worker thread
                _uiDispatcher.BeginInvoke(new Action(Hide));
                return;
            }

            IsOpen = false;
            base.Hide();
        }

        private void Clear()
        {
            Operation.Text = null;
            Message.Text = null;
            Progress.Value = 0;
            Progress.IsIndeterminate = true;
        }

        private void Update(string operation, string message, int percentComplete)
        {
            if (!_uiDispatcher.CheckAccess())
            {
                _uiDispatcher.BeginInvoke(new Action<string, string, int>(Update), operation, message, percentComplete);
                return;
            }

            if (percentComplete < 0)
            {
                percentComplete = 0;
            }
            else if (percentComplete > 100)
            {
                percentComplete = 100;
            }

            Progress.IsIndeterminate = percentComplete == 0;

            Operation.Text = operation;
            Message.Text = message;
            Progress.Value = percentComplete;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
    }
}
