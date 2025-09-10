namespace IDS.Amace.GUI
{
    partial class ScrewPanel
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
            this.lstScrews = new System.Windows.Forms.ListBox();
            this.lblInfo = new System.Windows.Forms.Label();
            this.lblTitle = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lstScrews
            // 
            this.lstScrews.FormattingEnabled = true;
            this.lstScrews.Location = new System.Drawing.Point(16, 60);
            this.lstScrews.Name = "lstScrews";
            this.lstScrews.Size = new System.Drawing.Size(197, 225);
            this.lstScrews.TabIndex = 0;
            this.lstScrews.SelectedIndexChanged += new System.EventHandler(this.OnSelectedScrewsChanged);
            // 
            // lblInfo
            // 
            this.lblInfo.AutoSize = true;
            this.lblInfo.Location = new System.Drawing.Point(13, 304);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(95, 13);
            this.lblInfo.TabIndex = 1;
            this.lblInfo.Text = "No screw selected";
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.Location = new System.Drawing.Point(16, 21);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(102, 20);
            this.lblTitle.TabIndex = 2;
            this.lblTitle.Text = "IDS - Screws";
            // 
            // ScrewPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.lstScrews);
            this.Name = "ScrewPanel";
            this.Size = new System.Drawing.Size(230, 583);
            this.Resize += new System.EventHandler(this.ScrewPanel_Resize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox lstScrews;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.Label lblTitle;
    }
}
