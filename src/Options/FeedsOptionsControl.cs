namespace CoApp.VisualStudio.Options
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using CoApp.VisualStudio.VsCore;

    /// <summary>
    /// Represents the Tools - Options - Package Manager dialog
    /// </summary>
    public partial class FeedsOptionsControl : UserControl
    {
        private bool _initialized;

        public FeedsOptionsControl()
        {
            InitializeComponent();
        }
        
        internal void InitializeOnActivated()
        {
            if (_initialized)
            {
                return;
            }

            UpdateFeeds();

            _initialized = true;
        }

        /// <summary>
        /// Persist the feeds, which was add/removed via the Options page, to the VS Settings store.
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
            // clear this flag so that we will set up the bindings again when the option page is activated next time
            _initialized = false;

            ClearFeed();
        }

        private void SetFeedsListBoxDataSource(BindingSource ds)
        {
            if (FeedsListBox.InvokeRequired)
            {
                FeedsListBox.Invoke((Action)(() => SetFeedsListBoxDataSource(ds)));
            }
            else
            {
                Enabled = true;
                FeedsListBox.DataSource = ds;
            }
        }

        private void SetFeedsListBoxMessage(string text)
        {
            if (FeedsListBox.InvokeRequired)
            {
                FeedsListBox.Invoke((Action)(() => SetFeedsListBoxMessage(text)));
            }
            else
            {
                Enabled = false;
                FeedsListBox.DataSource = null;
                FeedsListBox.Items.Clear();
                FeedsListBox.Items.Add(text);
            }
        }

        private void UpdateFeeds()
        {
            SetFeedsListBoxMessage("Loading feeds...");

            Task.Factory.StartNew(() =>
            {
                var feeds = CoAppWrapper.GetFeeds().Select(feed => feed.Location);

                SetFeedsListBoxDataSource(new BindingSource(feeds, null));
            });
        }

        private void OnRemoveButtonClick(object sender, EventArgs e)
        {
            if (FeedsListBox.SelectedItem == null)
            {
                return;
            }

            CoAppWrapper.RemoveFeed((string)FeedsListBox.SelectedItem);

            UpdateFeeds();
        }

        private void OnAddButtonClick(object sender, EventArgs e)
        {
            TryAddFeed();
            
            UpdateFeeds();
        }


        private void FeedsContextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem == CopyFeedsStripMenuItem && FeedsListBox.SelectedItem != null)
            {
                CopySelectedItem((string)FeedsListBox.SelectedItem);
            }
        }

        private static void CopySelectedItem(string selectedFeed)
        {
            Clipboard.Clear();
            Clipboard.SetText(selectedFeed);
        }

        private TryAddFeedResults TryAddFeed()
        {
            var feed = NewPackageFeed.Text.Trim();
            if (String.IsNullOrWhiteSpace(feed))
            {
                return TryAddFeedResults.NothingAdded;
            }

            // validate source
            if (String.IsNullOrWhiteSpace(feed))
            {
                MessageHelper.ShowWarningMessage(Resources.ShowWarning_SourceRequried, Resources.ShowWarning_Title);
                SelectAndFocus(NewPackageFeed);
                return TryAddFeedResults.InvalidFeed;
            }

            if (!(PathValidator.IsValidSource(feed)))
            {
                MessageHelper.ShowWarningMessage(Resources.ShowWarning_InvalidSource, Resources.ShowWarning_Title);
                SelectAndFocus(NewPackageFeed);
                return TryAddFeedResults.InvalidFeed;
            }

            CoAppWrapper.AddFeed(feed);

            // now clear the text boxes
            ClearFeed();

            return TryAddFeedResults.FeedAdded;
        }

        private static void SelectAndFocus(TextBox textBox)
        {
            textBox.Focus();
            textBox.SelectAll();
        }

        private void ClearFeed()
        {
            NewPackageFeed.Text = String.Empty;
        }

        private void FeedsListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            Graphics graphics = e.Graphics;
            e.DrawBackground();

            if (e.Index < 0 || e.Index >= FeedsListBox.Items.Count)
            {
                return;
            }

            string currentItem = (string)FeedsListBox.Items[e.Index];

            using (StringFormat drawFormat = new StringFormat())
            using (Brush foreBrush = new SolidBrush(e.ForeColor))
            using (Font italicFont = new Font(e.Font, FontStyle.Italic))
            {
                drawFormat.Alignment = StringAlignment.Near;
                drawFormat.Trimming = StringTrimming.EllipsisCharacter;
                drawFormat.LineAlignment = StringAlignment.Near;
                drawFormat.FormatFlags = StringFormatFlags.NoWrap;

                // the margin between the text and the edge of the list box
                const int edgeMargin = 8;

                GraphicsState oldState = graphics.Save();
                try
                {
                    // turn on high quality text rendering mode
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    var feedBounds = new Rectangle(e.Bounds.Left + edgeMargin, e.Bounds.Top, e.Bounds.Width - edgeMargin, 24);

                    graphics.DrawString((string)currentItem, italicFont, foreBrush, feedBounds, drawFormat);
                }
                finally
                {
                    graphics.Restore(oldState);
                }

                // If the ListBox has focus, draw a focus rectangle around the selected item.
                e.DrawFocusRectangle();
            }
        }

        private void FeedsListBox_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= FeedsListBox.Items.Count)
            {
                return;
            }

            string currentItem = (string)FeedsListBox.Items[e.Index];
            using (StringFormat drawFormat = new StringFormat())
            using (Font italicFont = new Font(Font, FontStyle.Italic))
            {
                drawFormat.Alignment = StringAlignment.Near;
                drawFormat.Trimming = StringTrimming.EllipsisCharacter;
                drawFormat.LineAlignment = StringAlignment.Near;
                drawFormat.FormatFlags = StringFormatFlags.NoWrap;

                SizeF feedLineHeight = e.Graphics.MeasureString((string)currentItem, italicFont, e.ItemWidth, drawFormat);

                e.ItemHeight = (int)Math.Ceiling(feedLineHeight.Height);
            }
        }

        private void FeedsListBox_MouseMove(object sender, MouseEventArgs e)
        {
            int index = FeedsListBox.IndexFromPoint(e.X, e.Y);

            if (index >= 0 && index < FeedsListBox.Items.Count && e.Y <= FeedsListBox.PreferredHeight)
            {
                string newToolTip = ((string)FeedsListBox.Items[index]);
                string currentToolTip = packageListToolTip.GetToolTip(FeedsListBox);
                if (currentToolTip != newToolTip)
                {
                    packageListToolTip.SetToolTip(FeedsListBox, newToolTip);
                }
            }
            else
            {
                packageListToolTip.SetToolTip(FeedsListBox, null);
                packageListToolTip.Hide(FeedsListBox);
            }
        }

        private static Rectangle NewBounds(Rectangle feedBounds, int xOffset, int yOffset)
        {
            return new Rectangle(feedBounds.Left + xOffset, feedBounds.Top + yOffset,
                feedBounds.Width - xOffset, feedBounds.Height - yOffset);
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
