namespace TextEditer
{
    partial class ucTabTextBox
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

        #region 구성 요소 디자이너에서 생성한 코드

        /// <summary> 
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.textEditer = new TextEditer.VirtualTextEditer();
            this.SuspendLayout();
            // 
            // textEditer
            // 
            this.textEditer.BackColor = System.Drawing.Color.White;
            this.textEditer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textEditer.ForeColor = System.Drawing.Color.Black;
            this.textEditer.Location = new System.Drawing.Point(0, 0);
            this.textEditer.Margin = new System.Windows.Forms.Padding(2);
            this.textEditer.Name = "textEditer";
            this.textEditer.Size = new System.Drawing.Size(1326, 660);
            this.textEditer.TabIndex = 0;
            // 
            // ucTabTextBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.textEditer);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "ucTabTextBox";
            this.Size = new System.Drawing.Size(1326, 660);
            this.ResumeLayout(false);

        }

        #endregion

        private VirtualTextEditer textEditer;
    }
}
