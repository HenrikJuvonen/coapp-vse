using System.Windows.Forms;
namespace CoApp.VisualStudio.Options
{
    partial class FeedsOptionsControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FeedsOptionsControl));
            this.HeaderLabel = new System.Windows.Forms.Label();
            this.PackageSourcesContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.CopyFeedsStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeButton = new System.Windows.Forms.Button();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.packageListToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.addButton = new System.Windows.Forms.Button();
            this.NewPackageFeed = new System.Windows.Forms.TextBox();
            this.NewPackageSourceLabel = new System.Windows.Forms.Label();
            this.FeedsListBox = new System.Windows.Forms.ListBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.imageList2 = new System.Windows.Forms.ImageList(this.components);
            this.PackageSourcesContextMenu.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // HeaderLabel
            // 
            resources.ApplyResources(this.HeaderLabel, "HeaderLabel");
            this.tableLayoutPanel1.SetColumnSpan(this.HeaderLabel, 2);
            this.HeaderLabel.Name = "HeaderLabel";
            // 
            // PackageSourcesContextMenu
            // 
            this.PackageSourcesContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CopyFeedsStripMenuItem});
            this.PackageSourcesContextMenu.Name = "contextMenuStrip1";
            resources.ApplyResources(this.PackageSourcesContextMenu, "PackageSourcesContextMenu");
            this.PackageSourcesContextMenu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.FeedsContextMenu_ItemClicked);
            // 
            // CopyPackageSourceStripMenuItem
            // 
            this.CopyFeedsStripMenuItem.Name = "CopyPackageSourceStripMenuItem";
            resources.ApplyResources(this.CopyFeedsStripMenuItem, "CopyPackageSourceStripMenuItem");
            // 
            // removeButton
            // 
            resources.ApplyResources(this.removeButton, "removeButton");
            this.removeButton.ImageList = this.imageList1;
            this.removeButton.Name = "removeButton";
            this.removeButton.UseVisualStyleBackColor = true;
            this.removeButton.Click += new System.EventHandler(this.OnRemoveButtonClick);
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "uparrow.png");
            this.imageList1.Images.SetKeyName(1, "downarrow.png");
            this.imageList1.Images.SetKeyName(2, "Delete.png");
            // 
            // addButton
            // 
            resources.ApplyResources(this.addButton, "addButton");
            this.addButton.Name = "addButton";
            this.addButton.UseVisualStyleBackColor = true;
            this.addButton.Click += new System.EventHandler(this.OnAddButtonClick);
            // 
            // NewPackageSource
            // 
            resources.ApplyResources(this.NewPackageFeed, "NewPackageSource");
            this.NewPackageFeed.Name = "NewPackageSource";
            // 
            // NewPackageSourceLabel
            // 
            resources.ApplyResources(this.NewPackageSourceLabel, "NewPackageSourceLabel");
            this.NewPackageSourceLabel.Name = "NewPackageSourceLabel";
            // 
            // PackageSourcesListBox
            // 
            resources.ApplyResources(this.FeedsListBox, "PackageSourcesListBox");
            this.FeedsListBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tableLayoutPanel1.SetColumnSpan(this.FeedsListBox, 4);
            this.FeedsListBox.ContextMenuStrip = this.PackageSourcesContextMenu;
            this.FeedsListBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.FeedsListBox.FormattingEnabled = true;
            this.FeedsListBox.Name = "PackageSourcesListBox";
            this.FeedsListBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.FeedsListBox_DrawItem);
            this.FeedsListBox.MeasureItem += new System.Windows.Forms.MeasureItemEventHandler(this.FeedsListBox_MeasureItem);
            this.FeedsListBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.FeedsListBox_MouseMove);
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.HeaderLabel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.FeedsListBox, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.NewPackageSourceLabel, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.NewPackageFeed, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.addButton, 3, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel1.SetColumnSpan(this.tableLayoutPanel2, 2);
            this.tableLayoutPanel2.Controls.Add(this.removeButton, 0, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // imageList2
            // 
            this.imageList2.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList2.ImageStream")));
            this.imageList2.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList2.Images.SetKeyName(0, "uparrow.png");
            this.imageList2.Images.SetKeyName(1, "downarrow.png");
            this.imageList2.Images.SetKeyName(2, "delete.png");
            // 
            // PackageSourcesOptionsControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "PackageSourcesOptionsControl";
            this.PackageSourcesContextMenu.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label HeaderLabel;
        private System.Windows.Forms.Button removeButton;
        private ContextMenuStrip PackageSourcesContextMenu;
        private ToolStripMenuItem CopyFeedsStripMenuItem;
        private ToolTip packageListToolTip;
        private Button addButton;
        private TextBox NewPackageFeed;
        private Label NewPackageSourceLabel;
        private TableLayoutPanel tableLayoutPanel1;
        private ListBox FeedsListBox;
        private TableLayoutPanel tableLayoutPanel2;
        private ImageList imageList1;
        private ImageList imageList2;
    }
}
