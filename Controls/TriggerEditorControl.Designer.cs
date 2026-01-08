namespace RARPEditor.Controls
{
    partial class TriggerEditorControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.mainGroupBox = new System.Windows.Forms.GroupBox();
            this.triggerGrid = new RARPEditor.Controls.DoubleBufferedDataGridView();
            this.toolbarPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.collapseChainsCheckBox = new System.Windows.Forms.CheckBox();
            this.chkAliasMode = new System.Windows.Forms.CheckBox();
            this.showDecimalCheckBox = new System.Windows.Forms.CheckBox();
            this.clearButton = new System.Windows.Forms.Button();
            this.copyButton = new System.Windows.Forms.Button();
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.moveUpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.moveDownToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.addConditionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.duplicateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.copyLogicToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mainGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.triggerGrid)).BeginInit();
            this.toolbarPanel.SuspendLayout();
            this.contextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainGroupBox
            // 
            this.mainGroupBox.Controls.Add(this.triggerGrid);
            this.mainGroupBox.Controls.Add(this.toolbarPanel);
            this.mainGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainGroupBox.Location = new System.Drawing.Point(0, 0);
            this.mainGroupBox.Name = "mainGroupBox";
            this.mainGroupBox.Padding = new System.Windows.Forms.Padding(8);
            this.mainGroupBox.Size = new System.Drawing.Size(700, 300);
            this.mainGroupBox.TabIndex = 0;
            this.mainGroupBox.TabStop = false;
            this.mainGroupBox.Text = "Logic";
            // 
            // triggerGrid
            // 
            this.triggerGrid.AllowUserToAddRows = false;
            this.triggerGrid.AllowUserToDeleteRows = false;
            this.triggerGrid.AllowUserToResizeRows = false;
            this.triggerGrid.BackgroundColor = System.Drawing.SystemColors.Window;
            this.triggerGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.triggerGrid.ContextMenuStrip = this.contextMenuStrip;
            this.triggerGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.triggerGrid.Location = new System.Drawing.Point(8, 63);
            this.triggerGrid.Name = "triggerGrid";
            this.triggerGrid.RowHeadersVisible = false;
            this.triggerGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.triggerGrid.Size = new System.Drawing.Size(684, 229);
            this.triggerGrid.TabIndex = 1;
            // 
            // toolbarPanel
            // 
            this.toolbarPanel.Controls.Add(this.showDecimalCheckBox);
            this.toolbarPanel.Controls.Add(this.chkAliasMode);
            this.toolbarPanel.Controls.Add(this.collapseChainsCheckBox);
            this.toolbarPanel.Controls.Add(this.clearButton);
            this.toolbarPanel.Controls.Add(this.copyButton);
            this.toolbarPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.toolbarPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.toolbarPanel.Location = new System.Drawing.Point(8, 24);
            this.toolbarPanel.Name = "toolbarPanel";
            this.toolbarPanel.Size = new System.Drawing.Size(684, 39);
            this.toolbarPanel.TabIndex = 2;
            // 
            // collapseChainsCheckBox
            // 
            this.collapseChainsCheckBox.Appearance = System.Windows.Forms.Appearance.Normal;
            this.collapseChainsCheckBox.AutoSize = true;
            this.collapseChainsCheckBox.Location = new System.Drawing.Point(363, 5);
            this.collapseChainsCheckBox.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            this.collapseChainsCheckBox.Name = "collapseChainsCheckBox";
            this.collapseChainsCheckBox.Size = new System.Drawing.Size(110, 19);
            this.collapseChainsCheckBox.TabIndex = 4;
            this.collapseChainsCheckBox.Text = "Collapse Chains";
            this.collapseChainsCheckBox.UseVisualStyleBackColor = true;
            // 
            // chkAliasMode
            // 
            this.chkAliasMode.Appearance = System.Windows.Forms.Appearance.Normal;
            this.chkAliasMode.AutoSize = true;
            this.chkAliasMode.Location = new System.Drawing.Point(479, 5);
            this.chkAliasMode.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            this.chkAliasMode.Name = "chkAliasMode";
            this.chkAliasMode.Size = new System.Drawing.Size(94, 19);
            this.chkAliasMode.TabIndex = 3;
            this.chkAliasMode.Text = "Show Aliases";
            this.chkAliasMode.UseVisualStyleBackColor = true;
            // 
            // showDecimalCheckBox
            // 
            this.showDecimalCheckBox.Appearance = System.Windows.Forms.Appearance.Normal;
            this.showDecimalCheckBox.AutoSize = true;
            this.showDecimalCheckBox.Location = new System.Drawing.Point(579, 5);
            this.showDecimalCheckBox.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            this.showDecimalCheckBox.Name = "showDecimalCheckBox";
            this.showDecimalCheckBox.Size = new System.Drawing.Size(102, 19);
            this.showDecimalCheckBox.TabIndex = 2;
            this.showDecimalCheckBox.Text = "Show Decimal";
            this.showDecimalCheckBox.UseVisualStyleBackColor = true;
            // 
            // clearButton
            // 
            this.clearButton.Location = new System.Drawing.Point(282, 3);
            this.clearButton.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new System.Drawing.Size(75, 25);
            this.clearButton.TabIndex = 1;
            this.clearButton.Text = "Clear";
            this.clearButton.UseVisualStyleBackColor = true;
            // 
            // copyButton
            // 
            this.copyButton.Location = new System.Drawing.Point(201, 3);
            this.copyButton.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.copyButton.Name = "copyButton";
            this.copyButton.Size = new System.Drawing.Size(75, 25);
            this.copyButton.TabIndex = 0;
            this.copyButton.Text = "Copy";
            this.copyButton.UseVisualStyleBackColor = true;
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.moveUpToolStripMenuItem,
            this.moveDownToolStripMenuItem,
            this.toolStripSeparator1,
            this.addConditionToolStripMenuItem,
            this.duplicateToolStripMenuItem,
            this.deleteToolStripMenuItem,
            this.toolStripSeparator2,
            this.copyLogicToolStripMenuItem,
            this.pasteToolStripMenuItem});
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(181, 192);
            // 
            // moveUpToolStripMenuItem
            // 
            this.moveUpToolStripMenuItem.Name = "moveUpToolStripMenuItem";
            this.moveUpToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Up)));
            this.moveUpToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.moveUpToolStripMenuItem.Text = "Move Up";
            // 
            // moveDownToolStripMenuItem
            // 
            this.moveDownToolStripMenuItem.Name = "moveDownToolStripMenuItem";
            this.moveDownToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Down)));
            this.moveDownToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.moveDownToolStripMenuItem.Text = "Move Down";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(177, 6);
            // 
            // addConditionToolStripMenuItem
            // 
            this.addConditionToolStripMenuItem.Name = "addConditionToolStripMenuItem";
            this.addConditionToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.addConditionToolStripMenuItem.Text = "Add Condition";
            // 
            // duplicateToolStripMenuItem
            // 
            this.duplicateToolStripMenuItem.Name = "duplicateToolStripMenuItem";
            this.duplicateToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
            this.duplicateToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.duplicateToolStripMenuItem.Text = "Duplicate";
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.deleteToolStripMenuItem.Text = "Delete";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(177, 6);
            // 
            // copyLogicToolStripMenuItem
            // 
            this.copyLogicToolStripMenuItem.Name = "copyLogicToolStripMenuItem";
            this.copyLogicToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.copyLogicToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.copyLogicToolStripMenuItem.Text = "Copy Logic";
            // 
            // pasteToolStripMenuItem
            // 
            this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
            this.pasteToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
            this.pasteToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.pasteToolStripMenuItem.Text = "Paste Logic";
            // 
            // TriggerEditorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainGroupBox);
            this.Name = "TriggerEditorControl";
            this.Size = new System.Drawing.Size(700, 300);
            this.mainGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.triggerGrid)).EndInit();
            this.toolbarPanel.ResumeLayout(false);
            this.toolbarPanel.PerformLayout();
            this.contextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.GroupBox mainGroupBox;
        private RARPEditor.Controls.DoubleBufferedDataGridView triggerGrid;
        private System.Windows.Forms.FlowLayoutPanel toolbarPanel;
        private System.Windows.Forms.CheckBox showDecimalCheckBox;
        private System.Windows.Forms.CheckBox chkAliasMode;
        private System.Windows.Forms.CheckBox collapseChainsCheckBox;
        private System.Windows.Forms.Button clearButton;
        private System.Windows.Forms.Button copyButton;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem moveUpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem moveDownToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem addConditionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem duplicateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem copyLogicToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
    }
}