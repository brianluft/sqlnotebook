﻿namespace SqlNotebook.Import {
    partial class ImportColumnsControl {
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImportColumnsControl));
            this._grid = new System.Windows.Forms.DataGridView();
            this._importColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this._sourceNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._targetNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._conversionColumn = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this._toolStrip = new System.Windows.Forms.ToolStrip();
            this._detectTypesButton = new System.Windows.Forms.ToolStripButton();
            this._toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this._setSelectedTypesMenu = new System.Windows.Forms.ToolStripDropDownButton();
            this._setTypeTextMenu = new System.Windows.Forms.ToolStripMenuItem();
            this._setTypeIntegerMenu = new System.Windows.Forms.ToolStripMenuItem();
            this._setTypeRealMenu = new System.Windows.Forms.ToolStripMenuItem();
            this._setTypeDateMenu = new System.Windows.Forms.ToolStripMenuItem();
            this._setTypeDateTimeMenu = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
            this._toolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // _grid
            // 
            this._grid.AllowUserToAddRows = false;
            this._grid.AllowUserToDeleteRows = false;
            this._grid.AllowUserToResizeRows = false;
            this._grid.BackgroundColor = System.Drawing.SystemColors.Window;
            this._grid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._grid.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this._grid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this._grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._importColumn,
            this._sourceNameColumn,
            this._targetNameColumn,
            this._conversionColumn});
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this._grid.DefaultCellStyle = dataGridViewCellStyle3;
            this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grid.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(229)))), ((int)(((byte)(229)))), ((int)(((byte)(229)))));
            this._grid.Location = new System.Drawing.Point(0, 34);
            this._grid.Name = "_grid";
            this._grid.RowHeadersVisible = false;
            this._grid.RowHeadersWidth = 62;
            this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this._grid.ShowCellToolTips = false;
            this._grid.ShowEditingIcon = false;
            this._grid.ShowRowErrors = false;
            this._grid.Size = new System.Drawing.Size(540, 226);
            this._grid.TabIndex = 1;
            // 
            // _importColumn
            // 
            this._importColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this._importColumn.DataPropertyName = "import";
            this._importColumn.HeaderText = "Import?";
            this._importColumn.MinimumWidth = 8;
            this._importColumn.Name = "_importColumn";
            this._importColumn.Width = 80;
            // 
            // _sourceNameColumn
            // 
            this._sourceNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this._sourceNameColumn.DataPropertyName = "source_name";
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.WhiteSmoke;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.WindowText;
            this._sourceNameColumn.DefaultCellStyle = dataGridViewCellStyle2;
            this._sourceNameColumn.HeaderText = "Original name";
            this._sourceNameColumn.MinimumWidth = 8;
            this._sourceNameColumn.Name = "_sourceNameColumn";
            this._sourceNameColumn.ReadOnly = true;
            this._sourceNameColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // _targetNameColumn
            // 
            this._targetNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this._targetNameColumn.DataPropertyName = "target_name";
            this._targetNameColumn.HeaderText = "Imported name";
            this._targetNameColumn.MinimumWidth = 8;
            this._targetNameColumn.Name = "_targetNameColumn";
            this._targetNameColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // _conversionColumn
            // 
            this._conversionColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this._conversionColumn.DataPropertyName = "conversion";
            this._conversionColumn.FillWeight = 130F;
            this._conversionColumn.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this._conversionColumn.HeaderText = "Conversion";
            this._conversionColumn.Items.AddRange(new object[] {
            "TEXT",
            "INTEGER",
            "REAL",
            "DATE",
            "DATETIME"});
            this._conversionColumn.MinimumWidth = 8;
            this._conversionColumn.Name = "_conversionColumn";
            this._conversionColumn.Width = 130;
            // 
            // _toolStrip
            // 
            this._toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this._toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._detectTypesButton,
            this._toolStripSeparator,
            this._setSelectedTypesMenu});
            this._toolStrip.Location = new System.Drawing.Point(0, 0);
            this._toolStrip.Name = "_toolStrip";
            this._toolStrip.Size = new System.Drawing.Size(540, 34);
            this._toolStrip.TabIndex = 3;
            this._toolStrip.Text = "toolStrip1";
            // 
            // _detectTypesButton
            // 
            this._detectTypesButton.Image = ((System.Drawing.Image)(resources.GetObject("_detectTypesButton.Image")));
            this._detectTypesButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._detectTypesButton.Name = "_detectTypesButton";
            this._detectTypesButton.Size = new System.Drawing.Size(161, 29);
            this._detectTypesButton.Text = "Detect all types";
            this._detectTypesButton.ToolTipText = "Automatically set the type conversion for all columns.";
            this._detectTypesButton.Click += new System.EventHandler(this.DetectTypesButton_Click);
            // 
            // _toolStripSeparator
            // 
            this._toolStripSeparator.Name = "_toolStripSeparator";
            this._toolStripSeparator.Size = new System.Drawing.Size(6, 34);
            // 
            // _setSelectedTypesMenu
            // 
            this._setSelectedTypesMenu.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this._setSelectedTypesMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._setTypeTextMenu,
            this._setTypeIntegerMenu,
            this._setTypeRealMenu,
            this._setTypeDateMenu,
            this._setTypeDateTimeMenu});
            this._setSelectedTypesMenu.Image = ((System.Drawing.Image)(resources.GetObject("_setSelectedTypesMenu.Image")));
            this._setSelectedTypesMenu.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._setSelectedTypesMenu.Name = "_setSelectedTypesMenu";
            this._setSelectedTypesMenu.Size = new System.Drawing.Size(172, 29);
            this._setSelectedTypesMenu.Text = "Set selected t&ypes";
            this._setSelectedTypesMenu.ToolTipText = "Change the type conversion for all selected columns.";
            // 
            // _setTypeTextMenu
            // 
            this._setTypeTextMenu.Name = "_setTypeTextMenu";
            this._setTypeTextMenu.Size = new System.Drawing.Size(270, 34);
            this._setTypeTextMenu.Text = "&TEXT";
            this._setTypeTextMenu.Click += new System.EventHandler(this.SetTypeMenu_Click);
            // 
            // _setTypeIntegerMenu
            // 
            this._setTypeIntegerMenu.Name = "_setTypeIntegerMenu";
            this._setTypeIntegerMenu.Size = new System.Drawing.Size(270, 34);
            this._setTypeIntegerMenu.Text = "&INTEGER";
            this._setTypeIntegerMenu.Click += new System.EventHandler(this.SetTypeMenu_Click);
            // 
            // _setTypeRealMenu
            // 
            this._setTypeRealMenu.Name = "_setTypeRealMenu";
            this._setTypeRealMenu.Size = new System.Drawing.Size(270, 34);
            this._setTypeRealMenu.Text = "&REAL";
            this._setTypeRealMenu.Click += new System.EventHandler(this.SetTypeMenu_Click);
            // 
            // _setTypeDateMenu
            // 
            this._setTypeDateMenu.Name = "_setTypeDateMenu";
            this._setTypeDateMenu.Size = new System.Drawing.Size(270, 34);
            this._setTypeDateMenu.Text = "&DATE";
            this._setTypeDateMenu.Click += new System.EventHandler(this.SetTypeMenu_Click);
            // 
            // _setTypeDateTimeMenu
            // 
            this._setTypeDateTimeMenu.Name = "_setTypeDateTimeMenu";
            this._setTypeDateTimeMenu.Size = new System.Drawing.Size(270, 34);
            this._setTypeDateTimeMenu.Text = "D&ATETIME";
            this._setTypeDateTimeMenu.Click += new System.EventHandler(this.SetTypeMenu_Click);
            // 
            // ImportColumnsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this._grid);
            this.Controls.Add(this._toolStrip);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.Name = "ImportColumnsControl";
            this.Size = new System.Drawing.Size(540, 260);
            ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
            this._toolStrip.ResumeLayout(false);
            this._toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView _grid;
        private System.Windows.Forms.DataGridViewCheckBoxColumn _importColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn _sourceNameColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn _targetNameColumn;
        private System.Windows.Forms.DataGridViewComboBoxColumn _conversionColumn;
        private System.Windows.Forms.ToolStrip _toolStrip;
        private System.Windows.Forms.ToolStripButton _detectTypesButton;
        private System.Windows.Forms.ToolStripSeparator _toolStripSeparator;
        private System.Windows.Forms.ToolStripDropDownButton _setSelectedTypesMenu;
        private System.Windows.Forms.ToolStripMenuItem _setTypeTextMenu;
        private System.Windows.Forms.ToolStripMenuItem _setTypeIntegerMenu;
        private System.Windows.Forms.ToolStripMenuItem _setTypeRealMenu;
        private System.Windows.Forms.ToolStripMenuItem _setTypeDateMenu;
        private System.Windows.Forms.ToolStripMenuItem _setTypeDateTimeMenu;
    }
}
