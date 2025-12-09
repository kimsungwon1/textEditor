namespace TextEditer
{
    partial class FormMain
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStripMain = new System.Windows.Forms.MenuStrip();
            this.tsmItemFile = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuItemNewFile = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuItemOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuItemSave = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuItemSaveAs = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuItemSaveAll = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuItemFileNameChange = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuItemClose = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuItemCloseAll = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMain = new System.Windows.Forms.ToolStrip();
            this.tcTabControl = new System.Windows.Forms.TabControl();
            this.defaultTabPage = new System.Windows.Forms.TabPage();
            this.menuStripMain.SuspendLayout();
            this.tcTabControl.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStripMain
            // 
            this.menuStripMain.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.menuStripMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmItemFile});
            this.menuStripMain.Location = new System.Drawing.Point(0, 0);
            this.menuStripMain.Name = "menuStripMain";
            this.menuStripMain.Size = new System.Drawing.Size(1860, 33);
            this.menuStripMain.TabIndex = 0;
            this.menuStripMain.Text = "menuStrip1";
            // 
            // tsmItemFile
            // 
            this.tsmItemFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsMenuItemNewFile,
            this.tsMenuItemOpen,
            this.tsMenuItemSave,
            this.tsMenuItemSaveAs,
            this.tsMenuItemSaveAll,
            this.tsMenuItemFileNameChange,
            this.tsMenuItemClose,
            this.tsMenuItemCloseAll});
            this.tsmItemFile.Name = "tsmItemFile";
            this.tsmItemFile.Size = new System.Drawing.Size(51, 29);
            this.tsmItemFile.Text = "File";
            // 
            // tsMenuItemNewFile
            // 
            this.tsMenuItemNewFile.Name = "tsMenuItemNewFile";
            this.tsMenuItemNewFile.Size = new System.Drawing.Size(252, 30);
            this.tsMenuItemNewFile.Text = "New File";
            this.tsMenuItemNewFile.Click += new System.EventHandler(this.tsMenuItemNewFile_Click);
            // 
            // tsMenuItemOpen
            // 
            this.tsMenuItemOpen.Name = "tsMenuItemOpen";
            this.tsMenuItemOpen.Size = new System.Drawing.Size(252, 30);
            this.tsMenuItemOpen.Text = "Open";
            this.tsMenuItemOpen.Click += new System.EventHandler(this.tsMenuItemOpen_Click);
            // 
            // tsMenuItemSave
            // 
            this.tsMenuItemSave.Name = "tsMenuItemSave";
            this.tsMenuItemSave.Size = new System.Drawing.Size(252, 30);
            this.tsMenuItemSave.Text = "Save";
            this.tsMenuItemSave.Click += new System.EventHandler(this.tsMenuItemSave_Click);
            // 
            // tsMenuItemSaveAs
            // 
            this.tsMenuItemSaveAs.Name = "tsMenuItemSaveAs";
            this.tsMenuItemSaveAs.Size = new System.Drawing.Size(252, 30);
            this.tsMenuItemSaveAs.Text = "SaveAs";
            this.tsMenuItemSaveAs.Click += new System.EventHandler(this.tsMenuItemSaveAs_Click);
            // 
            // tsMenuItemSaveAll
            // 
            this.tsMenuItemSaveAll.Name = "tsMenuItemSaveAll";
            this.tsMenuItemSaveAll.Size = new System.Drawing.Size(252, 30);
            this.tsMenuItemSaveAll.Text = "SaveAll";
            this.tsMenuItemSaveAll.Click += new System.EventHandler(this.tsMenuItemSaveAll_Click);
            // 
            // tsMenuItemFileNameChange
            // 
            this.tsMenuItemFileNameChange.Name = "tsMenuItemFileNameChange";
            this.tsMenuItemFileNameChange.Size = new System.Drawing.Size(252, 30);
            this.tsMenuItemFileNameChange.Text = "File Name Change";
            this.tsMenuItemFileNameChange.Click += new System.EventHandler(this.tsMenuItemFileNameChange_Click);
            // 
            // tsMenuItemClose
            // 
            this.tsMenuItemClose.Name = "tsMenuItemClose";
            this.tsMenuItemClose.Size = new System.Drawing.Size(252, 30);
            this.tsMenuItemClose.Text = "Close";
            this.tsMenuItemClose.Click += new System.EventHandler(this.tsMenuItemClose_Click);
            // 
            // tsMenuItemCloseAll
            // 
            this.tsMenuItemCloseAll.Name = "tsMenuItemCloseAll";
            this.tsMenuItemCloseAll.Size = new System.Drawing.Size(252, 30);
            this.tsMenuItemCloseAll.Text = "CloseAll";
            this.tsMenuItemCloseAll.Click += new System.EventHandler(this.tsMenuItemCloseAll_Click);
            // 
            // toolStripMain
            // 
            this.toolStripMain.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStripMain.Location = new System.Drawing.Point(0, 33);
            this.toolStripMain.Name = "toolStripMain";
            this.toolStripMain.Size = new System.Drawing.Size(1860, 25);
            this.toolStripMain.TabIndex = 1;
            this.toolStripMain.Text = "toolStrip1";
            // 
            // tcTabControl
            // 
            this.tcTabControl.Controls.Add(this.defaultTabPage);
            this.tcTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcTabControl.Location = new System.Drawing.Point(0, 58);
            this.tcTabControl.Name = "tcTabControl";
            this.tcTabControl.SelectedIndex = 0;
            this.tcTabControl.Size = new System.Drawing.Size(1860, 813);
            this.tcTabControl.TabIndex = 2;
            // 
            // defaultTabPage
            // 
            this.defaultTabPage.Location = new System.Drawing.Point(4, 28);
            this.defaultTabPage.Name = "defaultTabPage";
            this.defaultTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.defaultTabPage.Size = new System.Drawing.Size(1852, 781);
            this.defaultTabPage.TabIndex = 0;
            this.defaultTabPage.Text = "New Tab";
            this.defaultTabPage.UseVisualStyleBackColor = true;
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1860, 871);
            this.Controls.Add(this.tcTabControl);
            this.Controls.Add(this.toolStripMain);
            this.Controls.Add(this.menuStripMain);
            this.MainMenuStrip = this.menuStripMain;
            this.Name = "FormMain";
            this.Text = "Form1";
            this.menuStripMain.ResumeLayout(false);
            this.menuStripMain.PerformLayout();
            this.tcTabControl.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStripMain;
        private System.Windows.Forms.ToolStripMenuItem tsmItemFile;
        private System.Windows.Forms.ToolStripMenuItem tsMenuItemNewFile;
        private System.Windows.Forms.ToolStripMenuItem tsMenuItemOpen;
        private System.Windows.Forms.ToolStripMenuItem tsMenuItemSave;
        private System.Windows.Forms.ToolStripMenuItem tsMenuItemSaveAs;
        private System.Windows.Forms.ToolStripMenuItem tsMenuItemSaveAll;
        private System.Windows.Forms.ToolStripMenuItem tsMenuItemFileNameChange;
        private System.Windows.Forms.ToolStripMenuItem tsMenuItemClose;
        private System.Windows.Forms.ToolStripMenuItem tsMenuItemCloseAll;
        private System.Windows.Forms.ToolStrip toolStripMain;
        private System.Windows.Forms.TabControl tcTabControl;
        private System.Windows.Forms.TabPage defaultTabPage;
    }
}

