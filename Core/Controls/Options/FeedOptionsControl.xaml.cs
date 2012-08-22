using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
    
namespace CoApp.VSE.Core.Controls.Options
{
    public partial class FeedOptionsControl
    {
        public FeedOptionsControl()
        {
            InitializeComponent();

            UpdateFeeds();

            FeedLocation.DataContext = this;

            RemoveButtonShield.UpdateShield(RemoveButton.IsEnabled);
            AddButtonShield.UpdateShield(AddButton.IsEnabled);
        }

        private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender == RemoveButton && RemoveButtonShield != null)
                RemoveButtonShield.UpdateShield(RemoveButton.IsEnabled);

            if (sender == AddButton && AddButtonShield != null)
                AddButtonShield.UpdateShield(AddButton.IsEnabled);
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (sender == FeedsListBox)
                RemoveButton.IsEnabled = FeedsListBox.SelectedIndex >= 0;

        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (IsKeyboardFocusWithin && (e.Key == Key.Enter || e.Key == Key.Return))
            {
                ApplyChangedSettings();
            }
        }

        private void OnTextChanged(object sender, EventArgs e)
        {
            AddButton.IsEnabled = !string.IsNullOrEmpty(FeedLocation.Text) && !string.IsNullOrWhiteSpace(FeedLocation.Text) && IsValidFeed(FeedLocation.Text);
        }

        public static bool IsValidFeed(string feedLocation)
        {
            Uri result;
            if (Uri.TryCreate(feedLocation, UriKind.Absolute, out result))
            {
                return File.Exists(result.AbsolutePath) || result.IsWellFormedOriginalString();
            }

            return false;
        }

        private void ApplyChangedSettings()
        {
            if (AddButton.IsEnabled && TryAddFeed())
                UpdateFeeds();
        }
        
        private void SetFeedsListBoxItemsSource(IEnumerable<string> source)
        {
            if (!FeedsListBox.Dispatcher.CheckAccess())
            {
                FeedsListBox.Dispatcher.BeginInvoke(new Action<IEnumerable<string>>(SetFeedsListBoxItemsSource), source);
            }
            else
            {
                IsEnabled = true;
                
                if (FeedsListBox.ItemsSource == null)
                    FeedsListBox.Items.Clear();

                FeedsListBox.ItemsSource = source;
            }
        }

        private void SetFeedsListBoxMessage(string text)
        {
            if (!FeedsListBox.Dispatcher.CheckAccess())
            {
                FeedsListBox.Dispatcher.BeginInvoke(new Action<string>(SetFeedsListBoxMessage), text);
            }
            else
            {
                IsEnabled = false;
                FeedsListBox.ItemsSource = null;
                FeedsListBox.Items.Clear();
                FeedsListBox.Items.Add(text);
            }
        }
        
        private void UpdateFeeds()
        {
            Task.Factory.StartNew(() => SetFeedsListBoxItemsSource(Module.PackageManager.GetFeedLocations()));
        }

        private void OnRemoveButtonClick(object sender, EventArgs e)
        {
            if (FeedsListBox.SelectedItem == null)
                return;

            var feed = (string)FeedsListBox.SelectedItem;

            SetFeedsListBoxMessage("Loading feeds...");

            Task.Factory.StartNew(() =>
            {
                Module.PackageManager.RemoveFeed(feed);
            }).ContinueWith(t => UpdateFeeds());
        }

        private void OnAddButtonClick(object sender, EventArgs e)
        {
            TryAddFeed();
        }

        private bool TryAddFeed()
        {
            var feed = FeedLocation.Text.Trim();
            FeedLocation.Clear();

            SetFeedsListBoxMessage("Loading feeds...");

            Task.Factory.StartNew(() =>
            {
                Module.PackageManager.AddFeed(feed);
            }).ContinueWith(t => UpdateFeeds());

            return true;
        }
    }
}
