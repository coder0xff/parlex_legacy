namespace IntegratedDevelopmentEnvironment {
    partial class GrammarEditor {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GrammarEditor));
            this.treeView = new System.Windows.Forms.TreeView();
            this.toolStripButtonSequence = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonRepetition = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonChoice = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonOptional = new System.Windows.Forms.ToolStripButton();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonLeaf = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonTester = new System.Windows.Forms.ToolStripButton();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportCSharpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.generateCParserToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportCSharpSaveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.productionContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.testToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.productionContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // treeView
            // 
            this.treeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeView.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.treeView.LabelEdit = true;
            this.treeView.Location = new System.Drawing.Point(0, 52);
            this.treeView.Name = "treeView";
            this.treeView.Size = new System.Drawing.Size(548, 271);
            this.treeView.TabIndex = 0;
            this.treeView.BeforeLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.treeView_BeforeLabelEdit);
            this.treeView.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.treeView_AfterLabelEdit);
            this.treeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView_AfterSelect);
            this.treeView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.treeView_KeyDown);
            this.treeView.MouseUp += new System.Windows.Forms.MouseEventHandler(this.treeView_MouseUp);
            // 
            // toolStripButtonSequence
            // 
            this.toolStripButtonSequence.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonSequence.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonSequence.Image")));
            this.toolStripButtonSequence.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSequence.Name = "toolStripButtonSequence";
            this.toolStripButtonSequence.Size = new System.Drawing.Size(89, 22);
            this.toolStripButtonSequence.Text = "New Sequence";
            this.toolStripButtonSequence.Click += new System.EventHandler(this.toolStripButtonSequence_Click);
            // 
            // toolStripButtonRepetition
            // 
            this.toolStripButtonRepetition.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonRepetition.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonRepetition.Image")));
            this.toolStripButtonRepetition.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonRepetition.Name = "toolStripButtonRepetition";
            this.toolStripButtonRepetition.Size = new System.Drawing.Size(92, 22);
            this.toolStripButtonRepetition.Text = "New Repetition";
            this.toolStripButtonRepetition.Click += new System.EventHandler(this.toolStripButtonRepetition_Click);
            // 
            // toolStripButtonChoice
            // 
            this.toolStripButtonChoice.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonChoice.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonChoice.Image")));
            this.toolStripButtonChoice.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonChoice.Name = "toolStripButtonChoice";
            this.toolStripButtonChoice.Size = new System.Drawing.Size(75, 22);
            this.toolStripButtonChoice.Text = "New Choice";
            this.toolStripButtonChoice.Click += new System.EventHandler(this.toolStripButtonChoice_Click);
            // 
            // toolStripButtonOptional
            // 
            this.toolStripButtonOptional.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonOptional.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonOptional.Image")));
            this.toolStripButtonOptional.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonOptional.Name = "toolStripButtonOptional";
            this.toolStripButtonOptional.Size = new System.Drawing.Size(84, 22);
            this.toolStripButtonOptional.Text = "New Optional";
            this.toolStripButtonOptional.Click += new System.EventHandler(this.toolStripButtonOptional_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonSequence,
            this.toolStripButtonChoice,
            this.toolStripButtonRepetition,
            this.toolStripButtonOptional,
            this.toolStripSeparator1,
            this.toolStripButtonLeaf,
            this.toolStripSeparator2,
            this.toolStripButtonTester});
            this.toolStrip1.Location = new System.Drawing.Point(0, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(548, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonLeaf
            // 
            this.toolStripButtonLeaf.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonLeaf.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonLeaf.Image")));
            this.toolStripButtonLeaf.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonLeaf.Name = "toolStripButtonLeaf";
            this.toolStripButtonLeaf.Size = new System.Drawing.Size(60, 22);
            this.toolStripButtonLeaf.Text = "New Leaf";
            this.toolStripButtonLeaf.Click += new System.EventHandler(this.toolStripButtonLeaf_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonTester
            // 
            this.toolStripButtonTester.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonTester.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonTester.Image")));
            this.toolStripButtonTester.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonTester.Name = "toolStripButtonTester";
            this.toolStripButtonTester.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonTester.Text = "toolStripButton1";
            this.toolStripButtonTester.Click += new System.EventHandler(this.toolStripButtonTester_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(548, 24);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.exportCSharpToolStripMenuItem,
            this.generateCParserToolStripMenuItem});
            this.fileToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.MatchOnly;
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // exportCSharpToolStripMenuItem
            // 
            this.exportCSharpToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
            this.exportCSharpToolStripMenuItem.MergeIndex = 6;
            this.exportCSharpToolStripMenuItem.Name = "exportCSharpToolStripMenuItem";
            this.exportCSharpToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.exportCSharpToolStripMenuItem.Text = "Export C#";
            this.exportCSharpToolStripMenuItem.Click += new System.EventHandler(this.exportCSharpToolStripMenuItem_Click);
            // 
            // generateCParserToolStripMenuItem
            // 
            this.generateCParserToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
            this.generateCParserToolStripMenuItem.MergeIndex = 7;
            this.generateCParserToolStripMenuItem.Name = "generateCParserToolStripMenuItem";
            this.generateCParserToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.generateCParserToolStripMenuItem.Text = "Generate C# Parser";
            this.generateCParserToolStripMenuItem.Click += new System.EventHandler(this.generateCParserToolStripMenuItem_Click);
            // 
            // exportCSharpSaveFileDialog
            // 
            this.exportCSharpSaveFileDialog.Filter = "C# files|*.cs|All files|*.*";
            // 
            // productionContextMenu
            // 
            this.productionContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.testToolStripMenuItem});
            this.productionContextMenu.Name = "productionContextMenu";
            this.productionContextMenu.Size = new System.Drawing.Size(97, 26);
            // 
            // testToolStripMenuItem
            // 
            this.testToolStripMenuItem.Name = "testToolStripMenuItem";
            this.testToolStripMenuItem.Size = new System.Drawing.Size(96, 22);
            this.testToolStripMenuItem.Text = "Test";
            this.testToolStripMenuItem.Click += new System.EventHandler(this.testToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.MergeAction = System.Windows.Forms.MergeAction.Insert;
            this.toolStripMenuItem1.MergeIndex = 5;
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(171, 6);
            // 
            // GrammarEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(548, 323);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.treeView);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "GrammarEditor";
            this.Text = "GrammarEditor";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.productionContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView treeView;
        private System.Windows.Forms.ToolStripButton toolStripButtonSequence;
        private System.Windows.Forms.ToolStripButton toolStripButtonRepetition;
        private System.Windows.Forms.ToolStripButton toolStripButtonChoice;
        private System.Windows.Forms.ToolStripButton toolStripButtonOptional;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton toolStripButtonLeaf;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton toolStripButtonTester;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportCSharpToolStripMenuItem;
        private System.Windows.Forms.SaveFileDialog exportCSharpSaveFileDialog;
        private System.Windows.Forms.ContextMenuStrip productionContextMenu;
        private System.Windows.Forms.ToolStripMenuItem testToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem generateCParserToolStripMenuItem;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;

    }
}