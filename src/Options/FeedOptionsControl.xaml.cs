using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using CoApp.VisualStudio.VsCore;

namespace CoApp.VisualStudio.Options
{
    /// <summary>
    /// Interaction logic for FeedsOptionsControl.xaml
    /// </summary>
    public partial class FeedOptionsControl : WpfControl
    {
        public FeedOptionsControl()
        {
            InitializeBase();
            InitializeComponent();
        }

        internal void InitializeOnActivated()
        {
            UpdateFeeds();
        }

        /// <summary>
        /// This gets called when users click OK button.
        /// </summary>
        internal bool ApplyChangedSettings()
        {
            // if user presses Enter after filling in Name/Source but doesn't click Add
            // the options will be closed without adding the source, try adding before closing
            // Only apply if nothing was added
            TryAddFeedResults result = TryAddFeed();
            if (result != TryAddFeedResults.NothingAdded)
            {
                UpdateFeeds();

                return false;
            }

            return true;
        }

        /// <summary>
        /// This gets called when users close the Options dialog
        /// </summary>
        internal void ClearSettings()
        {
            ClearFeedLocation();
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
            SetFeedsListBoxMessage("Loading feeds...");

            Task.Factory.StartNew(() =>
            {
                SetFeedsListBoxItemsSource(CoAppWrapper.GetFeedLocations());
            });
        }

        private void OnRemoveButtonClick(object sender, EventArgs e)
        {
            if (FeedsListBox.SelectedItem == null)
            {
                return;
            }

            string feed = (string)FeedsListBox.SelectedItem;
            
            Task.Factory.StartNew(() =>
            {
                SetFeedsListBoxMessage("Loading feeds...");
                CoAppWrapper.RemoveFeed(feed);
            }).ContinueWith(t => UpdateFeeds());
        }

        private void OnAddButtonClick(object sender, EventArgs e)
        {
            TryAddFeed();
        }

        private TryAddFeedResults TryAddFeed()
        {
            string feed = FeedLocation.Text.Trim();

            if (String.IsNullOrWhiteSpace(feed))
            {
                return TryAddFeedResults.NothingAdded;
            }

            // validate feed
            if (String.IsNullOrWhiteSpace(feed))
            {
                MessageHelper.ShowWarningMessage(CoApp.VisualStudio.Options.Resources.ShowWarning_SourceRequried, null);
                SelectAndFocus(FeedLocation);
                return TryAddFeedResults.InvalidFeed;
            }

            if (!IsValidFeed(feed))
            {
                MessageHelper.ShowWarningMessage(CoApp.VisualStudio.Options.Resources.ShowWarning_InvalidSource, null);
                SelectAndFocus(FeedLocation);
                return TryAddFeedResults.InvalidFeed;
            }

            Task.Factory.StartNew(() =>
            {
                SetFeedsListBoxMessage("Loading feeds...");
                CoAppWrapper.AddFeed(feed);
            }).ContinueWith(t => UpdateFeeds());
            
            ClearFeedLocation();

            return TryAddFeedResults.FeedAdded;
        }

        private static bool IsValidFeed(string feedLocation)
        {
            try
            {
                Uri result;
                if (Uri.TryCreate(feedLocation, UriKind.Absolute, out result))
                {
                    return System.IO.File.Exists(result.AbsolutePath) || result.IsWellFormedOriginalString();
                }
            }
            catch
            {
            }

            return false;
        }

        private static void SelectAndFocus(TextBox textBox)
        {
            textBox.Focus();
            textBox.SelectAll();
        }

        private void ClearFeedLocation()
        {
            FeedLocation.Text = String.Empty;
        }
    }

    internal enum TryAddFeedResults
    {
        NothingAdded = 0,
        FeedAdded = 1,
        InvalidFeed = 2,
        FeedAlreadyAdded = 3
    }
}
