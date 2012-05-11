using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using CoGet.VisualStudio;
using CoGet.VisualStudio.Resources;
using CoApp.Toolkit.Engine.Client;

namespace CoGet.Options
{
    /// <summary>
    /// Represents the Tools - Options - Package Manager dialog
    /// </summary>
    /// <remarks>
    /// The code in this class assumes that while the dialog is open, noone is modifying the VSPackageSourceProvider directly.
    /// Otherwise, we have a problem with synchronization with the package source provider.
    /// </remarks>
    public partial class PackageSourcesOptionsControl : UserControl
    {
        private readonly IServiceProvider _serviceProvider;
        private bool _initialized;

        private BindingSource _allPackageSources;

        public PackageSourcesOptionsControl(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider;
        }

        private void UpdateFeeds()
        {
            _allPackageSources = new BindingSource(Proxy.ListFeeds().Select(feed => feed.Location), null);
            PackageSourcesListBox.DataSource = _allPackageSources;
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
        /// Persist the package sources, which was add/removed via the Options page, to the VS Settings store.
        /// This gets called when users click OK button.
        /// </summary>
        internal bool ApplyChangedSettings()
        {
            // if user presses Enter after filling in Name/Source but doesn't click Add
            // the options will be closed without adding the source, try adding before closing
            // Only apply if nothing was added
            TryAddSourceResults result = TryAddSource();
            if (result != TryAddSourceResults.NothingAdded)
            {
                _allPackageSources = new BindingSource(Proxy.ListFeeds().Select(feed => feed.Location), null);
                PackageSourcesListBox.DataSource = _allPackageSources;

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

            ClearNameSource();
        }

        private void OnRemoveButtonClick(object sender, EventArgs e)
        {
            if (PackageSourcesListBox.SelectedItem == null)
            {
                return;
            }

            Proxy.RemoveFeed((string)PackageSourcesListBox.SelectedItem);

            UpdateFeeds();
        }

        private void OnAddButtonClick(object sender, EventArgs e)
        {
            TryAddSource();

            _allPackageSources = new BindingSource(Proxy.ListFeeds().Select(feed => feed.Location), null);
            PackageSourcesListBox.DataSource = _allPackageSources;

            UpdateFeeds();
        }


        private void PackageSourcesContextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem == CopyPackageSourceStripMenuItem && PackageSourcesListBox.SelectedItem != null)
            {
                CopySelectedItem((string)PackageSourcesListBox.SelectedItem);
            }
        }

        private static void CopySelectedItem(string selectedPackageSource)
        {
            Clipboard.Clear();
            Clipboard.SetText(selectedPackageSource);
        }

        private TryAddSourceResults TryAddSource()
        {
            var source = NewPackageSource.Text.Trim();
            if (String.IsNullOrWhiteSpace(source))
            {
                return TryAddSourceResults.NothingAdded;
            }

            // validate source
            if (String.IsNullOrWhiteSpace(source))
            {
                MessageHelper.ShowWarningMessage(Resources.ShowWarning_SourceRequried, Resources.ShowWarning_Title);
                SelectAndFocus(NewPackageSource);
                return TryAddSourceResults.InvalidSource;
            }

            if (!(PathValidator.IsValidLocalPath(source) || PathValidator.IsValidUncPath(source) || PathValidator.IsValidUrl(source)))
            {
                MessageHelper.ShowWarningMessage(Resources.ShowWarning_InvalidSource, Resources.ShowWarning_Title);
                SelectAndFocus(NewPackageSource);
                return TryAddSourceResults.InvalidSource;
            }

            Proxy.AddFeed(source);

            // now clear the text boxes
            ClearNameSource();

            return TryAddSourceResults.SourceAdded;
        }

        private static void SelectAndFocus(TextBox textBox)
        {
            textBox.Focus();
            textBox.SelectAll();
        }

        private void ClearNameSource()
        {
            NewPackageSource.Text = String.Empty;
        }

        private void PackageSourcesListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            Graphics graphics = e.Graphics;
            e.DrawBackground();

            if (e.Index < 0 || e.Index >= PackageSourcesListBox.Items.Count)
            {
                return;
            }

            string currentItem = (string)PackageSourcesListBox.Items[e.Index];

            using (StringFormat drawFormat = new StringFormat())
            using (Brush foreBrush = new SolidBrush(e.ForeColor))
            using (Font italicFont = new Font(e.Font, FontStyle.Italic))
            {
                drawFormat.Alignment = StringAlignment.Near;
                drawFormat.Trimming = StringTrimming.EllipsisCharacter;
                drawFormat.LineAlignment = StringAlignment.Near;
                drawFormat.FormatFlags = StringFormatFlags.NoWrap;

                // the margin between the checkbox and the edge of the list box
                const int edgeMargin = 8;
                // the margin between the checkbox and the text
                const int textMargin = 4;

                GraphicsState oldState = graphics.Save();
                try
                {
                    // turn on high quality text rendering mode
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    // draw each package source as
                    // 
                    // [checkbox] Name
                    //            Source (italics)

                    int textWidth = e.Bounds.Width - edgeMargin - textMargin;

                    var sourceBounds = new Rectangle(
                        e.Bounds.Left + edgeMargin + textMargin,
                        e.Bounds.Top,
                        textWidth,
                        (int)24);
                    graphics.DrawString((string)currentItem, italicFont, foreBrush, sourceBounds, drawFormat);
                }
                finally
                {
                    graphics.Restore(oldState);
                }

                // If the ListBox has focus, draw a focus rectangle around the selected item.
                e.DrawFocusRectangle();
            }
        }

        private void PackageSourcesListBox_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= PackageSourcesListBox.Items.Count)
            {
                return;
            }

            string currentItem = (string)PackageSourcesListBox.Items[e.Index];
            using (StringFormat drawFormat = new StringFormat())
            using (Font italicFont = new Font(Font, FontStyle.Italic))
            {
                drawFormat.Alignment = StringAlignment.Near;
                drawFormat.Trimming = StringTrimming.EllipsisCharacter;
                drawFormat.LineAlignment = StringAlignment.Near;
                drawFormat.FormatFlags = StringFormatFlags.NoWrap;

                SizeF sourceLineHeight = e.Graphics.MeasureString((string)currentItem, italicFont, e.ItemWidth, drawFormat);

                e.ItemHeight = (int)Math.Ceiling(sourceLineHeight.Height);
            }
        }

        private void PackageSourcesListBox_MouseMove(object sender, MouseEventArgs e)
        {
            int index = PackageSourcesListBox.IndexFromPoint(e.X, e.Y);

            if (index >= 0 && index < PackageSourcesListBox.Items.Count && e.Y <= PackageSourcesListBox.PreferredHeight)
            {
                string newToolTip = ((string)PackageSourcesListBox.Items[index]);
                string currentToolTip = packageListToolTip.GetToolTip(PackageSourcesListBox);
                if (currentToolTip != newToolTip)
                {
                    packageListToolTip.SetToolTip(PackageSourcesListBox, newToolTip);
                }
            }
            else
            {
                packageListToolTip.SetToolTip(PackageSourcesListBox, null);
                packageListToolTip.Hide(PackageSourcesListBox);
            }
        }

        private static Rectangle NewBounds(Rectangle sourceBounds, int xOffset, int yOffset)
        {
            return new Rectangle(sourceBounds.Left + xOffset, sourceBounds.Top + yOffset,
                sourceBounds.Width - xOffset, sourceBounds.Height - yOffset);
        }
    }

    internal enum TryAddSourceResults
    {
        NothingAdded = 0,
        SourceAdded = 1,
        InvalidSource = 2,
        SourceAlreadyAdded = 3
    }
}
