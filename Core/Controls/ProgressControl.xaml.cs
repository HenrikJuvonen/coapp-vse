using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using CoApp.Packaging.Client;

namespace CoApp.VSE.Core.Controls
{
    public partial class ProgressControl
    {
        public ObservableCollection<ProgressEventArgs> Packages;

        public ProgressControl()
        {
            InitializeComponent();

            Module.PackageManager.ProgressAvailable += OnProgressAvailable;
            Module.PackageManager.Message += OnMessage;
            Module.PackageManager.Warning += OnWarning;
            Module.PackageManager.Error += OnError;
            Module.PackageManager.Elevated += OnElevated;
        }

        public void OnElevated()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (Module.IsApplying)
                {
                    Module.PackageManager.SetNewCancellationTokenSource();

                    var trimPlan = Module.PackageManager.TrimPlan;

                    var worker = new BackgroundWorker();
                    worker.DoWork += (sender, args) => Module.PackageManager.RemovePackages(Module.PackageManager.RemovePlan);
                    worker.DoWork += (sender, args) => Module.PackageManager.InstallPackages(Module.PackageManager.InstallPlan);

                    if (Module.PackageManager.Settings["#autoTrim"].BoolValue)
                        worker.DoWork += (sender, args) => Module.PackageManager.RemovePackages(trimPlan);

                    worker.DoWork += (sender, args) => Module.FinishPackageRestore();
                    worker.RunWorkerCompleted += (sender, args) =>
                    {
                        var unrecoverable = Module.GetUnrecoverablePackages();

                        if (unrecoverable.Any())
                        {
                            WriteToLog("Unable to recover following packages:\n" + string.Join("\n", unrecoverable), Brushes.DarkGoldenrod);
                        }

                        var text = string.Format("Completed {0}/{1} operations.", Packages.Count(n => n.Progress == 100), Packages.Count());

                        WriteToLog(text, Brushes.Blue);

                        if (Module.MainWindow.WindowState == WindowState.Minimized || Module.MainWindow.IsVisible == false)
                            Module.ShowBalloonTip(text);

                        End();
                    };
                    worker.RunWorkerAsync();
                }
            }));
        }

        public void Initialize()
        {
            Module.IsApplying = true;
            Module.HideBalloonTip();

            EndButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Visible;
            CancelButton.IsEnabled = true;

            OverallProgress.Value = 0;

            var packages = Module.PackageManager.RemovePlan.Union(Module.PackageManager.InstallPlan);

            if (Module.PackageManager.Settings["#autoTrim"].BoolValue)
                packages = packages.Union(Module.PackageManager.TrimPlan);
            
            var plan =
                from package in packages
                orderby package.CanonicalName
                select new ProgressEventArgs(package.CanonicalName, null, 0);

            Packages = new ObservableCollection<ProgressEventArgs>(plan);

            ProgressDataGrid.ItemsSource = Packages;
            
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, args) => Module.PackageManager.Elevate();
            worker.RunWorkerAsync();
        }

        private void ExecuteCancel(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!CancelButton.IsEnabled)
                    return;

                CancelButton.IsEnabled = false;

                if (sender != null)
                    WriteToLog("Cancellation requested. Waiting for current operation to complete...", Brushes.DarkGoldenrod);

                Module.PackageManager.Cancel();
            }));
        }

        public void ExecuteOk(object sender = null, EventArgs e = null)
        {
            Module.ShowMainControl();
            Module.HideBalloonTip();

            Module.PackageManager.Reset();
            Module.ReloadMainControl();
        }

        private void OnMessage(object sender, LogEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => WriteToLog(e.Message, Brushes.Black)));
        }

        private void OnWarning(object sender, LogEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => WriteToLog(e.Message, Brushes.DarkGoldenrod)));
        }

        private void OnError(object sender, LogEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                WriteToLog(e.Message, Brushes.Red);

                if (IsVisible)
                {
                    if (e.Message == "Not elevated.")
                    {
                        End();
                        return;
                    }
                    ExecuteCancel(null, null);
                }
            }));
        }

        private void OnProgressAvailable(object sender, ProgressEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (Packages == null)
                    return;

                var item = Packages.FirstOrDefault(n => n.Name == e.Name && n.Flavor == e.Flavor && n.Version == e.Version && n.Architecture == e.Architecture);

                if (item != null)
                {
                    //WriteToLog(string.Format("{0};{1};{2}", e.CanonicalName, e.Status, e.Progress), Brushes.Blue);
                    item.Status = e.Status;
                    item.Progress = e.Progress;
                }
                
                OverallProgress.Value = Packages.Average(n => n.Progress);
            }));
        }

        private void End()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Module.IsApplying = false;

                CancelButton.Visibility = Visibility.Collapsed;
                EndButton.Visibility = Visibility.Visible;

                if (Module.PackageManager.Settings["#autoEnd"].BoolValue && Packages.Count() != 0 && Packages.Count(n => n.Progress == 100) / Packages.Count() == 1)
                    ExecuteOk();
            }));
        }

        private void WriteToLog(string message, Brush brush)
        {
            Log.Document.Blocks.Add(new Paragraph(new Run(message) { Foreground = brush }));
            Log.ScrollToEnd();
        }
    }
}
