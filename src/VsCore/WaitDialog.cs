using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.VisualStudio.Shell.Interop;

namespace CoApp.VisualStudio.VsCore
{
    public sealed class WaitDialog
    {
        private readonly Dispatcher _uiDispatcher;

        private readonly IVsThreadedWaitDialogFactory _waitDialogFactory;
        private readonly IVsThreadedWaitDialog2 _waitDialog;

        private Window _owner;

        private string _progressTitle;
        private string _progressMessage;
        private string _progressOperation;
        private int _progressPercentage;
        private bool _isOpen;

        public WaitDialog()
        {
            _uiDispatcher = Dispatcher.CurrentDispatcher;

            _waitDialogFactory = ServiceLocator.GetGlobalService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>();
            _waitDialogFactory.CreateInstance(out _waitDialog);

            _isOpen = false;
        }

        public void OnProgressAvailable(object sender, ProgressEventArgs e)
        {
            if (e.Operation == "Error")
            {
                Close();
                MessageHelper.ShowErrorMessage(e.Message, null);
                return;
            }
            else if (e.Operation == "Info")
            {
                Close();
                MessageHelper.ShowInfoMessage(e.Message, null);
                return;
            }

            Update(e.Operation + " ...", e.Message, e.PercentComplete);
        }

        /// <summary>
        /// Show the progress window with the specified title.
        /// </summary>
        /// <param name="title">The window title</param>
        /// <remarks>
        /// This method can be called from worker thread.
        /// </remarks>
        public void Show(string title, Window owner = null, bool indeterminate = false)
        {
            if (!_uiDispatcher.CheckAccess())
            {
                // must use BeginInvoke() here to avoid blocking the worker thread
                _uiDispatcher.BeginInvoke(new Action<string, Window, bool>(Show), title, owner, indeterminate);
                return;
            }

            Clear();

            Close();

            _progressTitle = title;

            _owner = owner;

            if (_owner != null)
            {
                _owner.IsEnabled = false;
                _owner.Focusable = false;
            }

            if (indeterminate)
            {
                _waitDialog.StartWaitDialog(
                    VsResources.DialogTitle,
                    _progressTitle,
                    String.Empty,
                    null,
                    String.Empty,
                    0,
                    false,
                    true);
            }
            else
            {
                _waitDialog.StartWaitDialogWithPercentageProgress(
                    VsResources.DialogTitle,
                    _progressTitle,
                    String.Empty,
                    null,
                    String.Empty,
                    false,
                    0,
                    100,
                    1);
            }

            _isOpen = true;
        }

        public bool Close()
        {
            if (_isOpen)
            {
                int canceled;
                _waitDialog.EndWaitDialog(out canceled);

                if (_owner != null)
                {
                    _owner.IsEnabled = true;
                    _owner.Focusable = true;
                }

                _isOpen = false;
                return true;
            }
            else
            {
                return false;
            }
        }
        
        public void Clear()
        {
            if (!_uiDispatcher.CheckAccess())
            {
                _uiDispatcher.Invoke(new Action(Clear));
                return;
            }

            _progressMessage = null;
            _progressOperation = null;
            _progressPercentage = 0;
        }

        public void Update(string operation, string message, int percentComplete)
        {
            if (!_uiDispatcher.CheckAccess())
            {
                _uiDispatcher.BeginInvoke(new Action<string, string, int>(Update), operation, message, percentComplete);
                return;
            }

            if (_isOpen)
            {
                if (percentComplete < 0)
                {
                    percentComplete = 0;
                }
                else if (percentComplete > 100)
                {
                    percentComplete = 100;
                }

                bool canceled;
                
                _progressOperation = operation;
                _progressMessage = message;
                _progressPercentage = percentComplete;

                _waitDialog.UpdateProgress(_progressOperation, _progressMessage, _progressMessage, _progressPercentage, 100, true, out canceled);
            }
        }
    }
}