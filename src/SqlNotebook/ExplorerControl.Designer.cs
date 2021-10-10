﻿namespace SqlNotebook {
    partial class ExplorerControl {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.ColumnHeader _nameColumn;
            System.Windows.Forms.ColumnHeader columnHeader1;
            System.Windows.Forms.ListViewGroup listViewGroup1 = new System.Windows.Forms.ListViewGroup("Notes", System.Windows.Forms.HorizontalAlignment.Center);
            System.Windows.Forms.ListViewGroup listViewGroup2 = new System.Windows.Forms.ListViewGroup("Consoles", System.Windows.Forms.HorizontalAlignment.Center);
            System.Windows.Forms.ListViewGroup listViewGroup3 = new System.Windows.Forms.ListViewGroup("Scripts", System.Windows.Forms.HorizontalAlignment.Center);
            System.Windows.Forms.ListViewGroup listViewGroup4 = new System.Windows.Forms.ListViewGroup("Tables", System.Windows.Forms.HorizontalAlignment.Center);
            System.Windows.Forms.ListViewGroup listViewGroup5 = new System.Windows.Forms.ListViewGroup("Views", System.Windows.Forms.HorizontalAlignment.Center);
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExplorerControl));
            System.Windows.Forms.ListViewGroup listViewGroup6 = new System.Windows.Forms.ListViewGroup("Group", System.Windows.Forms.HorizontalAlignment.Center);
            this._list = new System.Windows.Forms.ListView();
            this._contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this._openMnu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this._deleteMnu = new System.Windows.Forms.ToolStripMenuItem();
            this._renameMnu = new System.Windows.Forms.ToolStripMenuItem();
            this._imageList = new System.Windows.Forms.ImageList(this.components);
            this._splitContainer = new System.Windows.Forms.SplitContainer();
            this._detailsLst = new System.Windows.Forms.ListView();
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            _nameColumn = new System.Windows.Forms.ColumnHeader();
            columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this._contextMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._splitContainer)).BeginInit();
            this._splitContainer.Panel1.SuspendLayout();
            this._splitContainer.Panel2.SuspendLayout();
            this._splitContainer.SuspendLayout();
            this.toolStripContainer1.ContentPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // _nameColumn
            // 
            _nameColumn.Text = "";
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "Name";
            columnHeader1.Width = 157;
            // 
            // _list
            // 
            this._list.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._list.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            _nameColumn});
            this._list.ContextMenuStrip = this._contextMenuStrip;
            this._list.Dock = System.Windows.Forms.DockStyle.Fill;
            listViewGroup1.Header = "Notes";
            listViewGroup1.HeaderAlignment = System.Windows.Forms.HorizontalAlignment.Center;
            listViewGroup1.Name = "Note";
            listViewGroup2.Header = "Consoles";
            listViewGroup2.HeaderAlignment = System.Windows.Forms.HorizontalAlignment.Center;
            listViewGroup2.Name = "Console";
            listViewGroup3.Header = "Scripts";
            listViewGroup3.HeaderAlignment = System.Windows.Forms.HorizontalAlignment.Center;
            listViewGroup3.Name = "Script";
            listViewGroup4.Header = "Tables";
            listViewGroup4.HeaderAlignment = System.Windows.Forms.HorizontalAlignment.Center;
            listViewGroup4.Name = "Table";
            listViewGroup5.Header = "Views";
            listViewGroup5.HeaderAlignment = System.Windows.Forms.HorizontalAlignment.Center;
            listViewGroup5.Name = "View";
            this._list.Groups.AddRange(new System.Windows.Forms.ListViewGroup[] {
            listViewGroup1,
            listViewGroup2,
            listViewGroup3,
            listViewGroup4,
            listViewGroup5});
            this._list.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._list.HideSelection = false;
            this._list.LabelEdit = true;
            this._list.LabelWrap = false;
            this._list.Location = new System.Drawing.Point(0, 0);
            this._list.MultiSelect = false;
            this._list.Name = "_list";
            this._list.Size = new System.Drawing.Size(340, 287);
            this._list.SmallImageList = this._imageList;
            this._list.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this._list.TabIndex = 0;
            this._list.UseCompatibleStateImageBehavior = false;
            this._list.View = System.Windows.Forms.View.Details;
            this._list.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.List_AfterLabelEdit);
            this._list.ItemActivate += new System.EventHandler(this.List_ItemActivate);
            this._list.SelectedIndexChanged += new System.EventHandler(this.List_SelectedIndexChanged);
            this._list.KeyDown += new System.Windows.Forms.KeyEventHandler(this.List_KeyDown);
            // 
            // _contextMenuStrip
            // 
            this._contextMenuStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this._contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._openMnu,
            this.toolStripSeparator1,
            this._deleteMnu,
            this._renameMnu});
            this._contextMenuStrip.Name = "_contextMenuStrip";
            this._contextMenuStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this._contextMenuStrip.Size = new System.Drawing.Size(148, 106);
            this._contextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.ContextMenuStrip_Opening);
            // 
            // _openMnu
            // 
            this._openMnu.Name = "_openMnu";
            this._openMnu.Size = new System.Drawing.Size(147, 32);
            this._openMnu.Text = "&Open";
            this._openMnu.Click += new System.EventHandler(this.List_ItemActivate);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(144, 6);
            // 
            // _deleteMnu
            // 
            this._deleteMnu.Name = "_deleteMnu";
            this._deleteMnu.Size = new System.Drawing.Size(147, 32);
            this._deleteMnu.Text = "&Delete";
            this._deleteMnu.Click += new System.EventHandler(this.DeleteMnu_Click);
            // 
            // _renameMnu
            // 
            this._renameMnu.Name = "_renameMnu";
            this._renameMnu.Size = new System.Drawing.Size(147, 32);
            this._renameMnu.Text = "&Rename";
            this._renameMnu.Click += new System.EventHandler(this.RenameMnu_Click);
            // 
            // _imageList
            // 
            this._imageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this._imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("_imageList.ImageStream")));
            this._imageList.TransparentColor = System.Drawing.Color.Transparent;
            this._imageList.Images.SetKeyName(0, "script32.png");
            this._imageList.Images.SetKeyName(1, "table32.png");
            this._imageList.Images.SetKeyName(2, "filter32.png");
            this._imageList.Images.SetKeyName(3, "bullet_black32.png");
            this._imageList.Images.SetKeyName(4, "bullet_key32.png");
            this._imageList.Images.SetKeyName(5, "table_link32.png");
            // 
            // _splitContainer
            // 
            this._splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this._splitContainer.Location = new System.Drawing.Point(0, 0);
            this._splitContainer.Name = "_splitContainer";
            this._splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // _splitContainer.Panel1
            // 
            this._splitContainer.Panel1.BackColor = System.Drawing.SystemColors.Window;
            this._splitContainer.Panel1.Controls.Add(this._list);
            // 
            // _splitContainer.Panel2
            // 
            this._splitContainer.Panel2.Controls.Add(this._detailsLst);
            this._splitContainer.Size = new System.Drawing.Size(340, 583);
            this._splitContainer.SplitterDistance = 287;
            this._splitContainer.TabIndex = 1;
            // 
            // _detailsLst
            // 
            this._detailsLst.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._detailsLst.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            columnHeader1});
            this._detailsLst.Dock = System.Windows.Forms.DockStyle.Fill;
            listViewGroup6.Header = "Group";
            listViewGroup6.HeaderAlignment = System.Windows.Forms.HorizontalAlignment.Center;
            listViewGroup6.Name = "Group";
            this._detailsLst.Groups.AddRange(new System.Windows.Forms.ListViewGroup[] {
            listViewGroup6});
            this._detailsLst.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._detailsLst.HideSelection = false;
            this._detailsLst.Location = new System.Drawing.Point(0, 0);
            this._detailsLst.MultiSelect = false;
            this._detailsLst.Name = "_detailsLst";
            this._detailsLst.Size = new System.Drawing.Size(340, 292);
            this._detailsLst.SmallImageList = this._imageList;
            this._detailsLst.TabIndex = 0;
            this._detailsLst.UseCompatibleStateImageBehavior = false;
            this._detailsLst.View = System.Windows.Forms.View.Details;
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.Controls.Add(this._splitContainer);
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(340, 583);
            this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.Size = new System.Drawing.Size(340, 608);
            this.toolStripContainer1.TabIndex = 2;
            this.toolStripContainer1.Text = "toolStripContainer1";
            // 
            // toolStripContainer1.TopToolStripPanel
            // 
            this.toolStripContainer1.TopToolStripPanel.BackColor = System.Drawing.SystemColors.Window;
            // 
            // ExplorerControl
            // 
            this.Controls.Add(this.toolStripContainer1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.Name = "ExplorerControl";
            this.Size = new System.Drawing.Size(340, 608);
            this._contextMenuStrip.ResumeLayout(false);
            this._splitContainer.Panel1.ResumeLayout(false);
            this._splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._splitContainer)).EndInit();
            this._splitContainer.ResumeLayout(false);
            this.toolStripContainer1.ContentPanel.ResumeLayout(false);
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.SplitContainer _splitContainer;
        private System.Windows.Forms.ListView _detailsLst;
        private System.Windows.Forms.ListView _list;
        private System.Windows.Forms.ImageList _imageList;
        private System.Windows.Forms.ContextMenuStrip _contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem _deleteMnu;
        private System.Windows.Forms.ToolStripMenuItem _renameMnu;
        private System.Windows.Forms.ToolStripContainer toolStripContainer1;
        private System.Windows.Forms.ToolStripMenuItem _openMnu;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
    }
}