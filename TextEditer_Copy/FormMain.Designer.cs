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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
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
            this.tsbtnMonitoring = new System.Windows.Forms.ToolStripButton();
            this.tcTabControl = new System.Windows.Forms.TabControl();
            this.defaultTabPage = new System.Windows.Forms.TabPage();
            this.fileSystemWatcher = new System.IO.FileSystemWatcher();
            this.menuStripMain.SuspendLayout();
            this.toolStripMain.SuspendLayout();
            this.tcTabControl.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher)).BeginInit();
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
            this.tsMenuItemNewFile.Size = new System.Drawing.Size(245, 30);
            this.tsMenuItemNewFile.Text = "New File";
            // 
            // tsMenuItemOpen
            // 
            this.tsMenuItemOpen.Name = "tsMenuItemOpen";
            this.tsMenuItemOpen.Size = new System.Drawing.Size(245, 30);
            this.tsMenuItemOpen.Text = "Open";
            // 
            // tsMenuItemSave
            // 
            this.tsMenuItemSave.Name = "tsMenuItemSave";
            this.tsMenuItemSave.Size = new System.Drawing.Size(245, 30);
            this.tsMenuItemSave.Text = "Save";
            // 
            // tsMenuItemSaveAs
            // 
            this.tsMenuItemSaveAs.Name = "tsMenuItemSaveAs";
            this.tsMenuItemSaveAs.Size = new System.Drawing.Size(245, 30);
            this.tsMenuItemSaveAs.Text = "SaveAs";
            // 
            // tsMenuItemSaveAll
            // 
            this.tsMenuItemSaveAll.Name = "tsMenuItemSaveAll";
            this.tsMenuItemSaveAll.Size = new System.Drawing.Size(245, 30);
            this.tsMenuItemSaveAll.Text = "SaveAll";
            // 
            // tsMenuItemFileNameChange
            // 
            this.tsMenuItemFileNameChange.Name = "tsMenuItemFileNameChange";
            this.tsMenuItemFileNameChange.Size = new System.Drawing.Size(245, 30);
            this.tsMenuItemFileNameChange.Text = "File Name Change";
            // 
            // tsMenuItemClose
            // 
            this.tsMenuItemClose.Name = "tsMenuItemClose";
            this.tsMenuItemClose.Size = new System.Drawing.Size(245, 30);
            this.tsMenuItemClose.Text = "Close";
            // 
            // tsMenuItemCloseAll
            // 
            this.tsMenuItemCloseAll.Name = "tsMenuItemCloseAll";
            this.tsMenuItemCloseAll.Size = new System.Drawing.Size(245, 30);
            this.tsMenuItemCloseAll.Text = "CloseAll";
            // 
            // toolStripMain
            // 
            this.toolStripMain.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStripMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbtnMonitoring});
            this.toolStripMain.Location = new System.Drawing.Point(0, 33);
            this.toolStripMain.Name = "toolStripMain";
            this.toolStripMain.Size = new System.Drawing.Size(1860, 31);
            this.toolStripMain.TabIndex = 1;
            this.toolStripMain.Text = "toolStrip1";
            // 
            // tsbtnMonitoring
            // 
            this.tsbtnMonitoring.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbtnMonitoring.Image = ((System.Drawing.Image)(resources.GetObject("tsbtnMonitoring.Image")));
            this.tsbtnMonitoring.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbtnMonitoring.Name = "tsbtnMonitoring";
            this.tsbtnMonitoring.Size = new System.Drawing.Size(28, 28);
            this.tsbtnMonitoring.Text = "tsBtnMonitoring";
            // 
            // tcTabControl
            // 
            this.tcTabControl.Controls.Add(this.defaultTabPage);
            this.tcTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcTabControl.Location = new System.Drawing.Point(0, 64);
            this.tcTabControl.Name = "tcTabControl";
            this.tcTabControl.SelectedIndex = 0;
            this.tcTabControl.Size = new System.Drawing.Size(1860, 807);
            this.tcTabControl.TabIndex = 2;
            this.tcTabControl.Selected += new System.Windows.Forms.TabControlEventHandler(this.tcTabControl_Selected);
            // 
            // defaultTabPage
            // 
            this.defaultTabPage.Location = new System.Drawing.Point(4, 28);
            this.defaultTabPage.Name = "defaultTabPage";
            this.defaultTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.defaultTabPage.Size = new System.Drawing.Size(1852, 775);
            this.defaultTabPage.TabIndex = 0;
            this.defaultTabPage.Text = "New Tab1";
            this.defaultTabPage.UseVisualStyleBackColor = true;
            // 
            // fileSystemWatcher
            // 
            this.fileSystemWatcher.EnableRaisingEvents = true;
            this.fileSystemWatcher.SynchronizingObject = this;
            this.fileSystemWatcher.Changed += new System.IO.FileSystemEventHandler(this.fileSystemWatcher_Changed);
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
            this.Text = "New Tab";
            this.menuStripMain.ResumeLayout(false);
            this.menuStripMain.PerformLayout();
            this.toolStripMain.ResumeLayout(false);
            this.toolStripMain.PerformLayout();
            this.tcTabControl.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher)).EndInit();
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
        private System.Windows.Forms.ToolStripButton tsbtnMonitoring;
        private System.IO.FileSystemWatcher fileSystemWatcher;
    }
}

