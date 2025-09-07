namespace IDS.Core.GUI
{
    partial class frmWaitbar
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblProgress = new System.Windows.Forms.Label();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblLoader = new System.Windows.Forms.Label();
            this.lblClose = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblProgress
            // 
            this.lblProgress.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblProgress.AutoSize = true;
            this.lblProgress.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblProgress.ForeColor = System.Drawing.Color.White;
            this.lblProgress.Location = new System.Drawing.Point(373, 84);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(83, 31);
            this.lblProgress.TabIndex = 1;
            this.lblProgress.Text = "100%";
            this.lblProgress.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblProgress.UseWaitCursor = true;
            this.lblProgress.MouseHover += new System.EventHandler(this.frmWaitbar_MouseHover);
            // 
            // lblTitle
            // 
            this.lblTitle.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(12, 29);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(66, 31);
            this.lblTitle.TabIndex = 2;
            this.lblTitle.Text = "Title";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblTitle.UseWaitCursor = true;
            this.lblTitle.MouseHover += new System.EventHandler(this.frmWaitbar_MouseHover);
            // 
            // lblLoader
            // 
            this.lblLoader.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblLoader.AutoSize = true;
            this.lblLoader.Font = new System.Drawing.Font("Courier New", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLoader.ForeColor = System.Drawing.Color.White;
            this.lblLoader.Location = new System.Drawing.Point(12, 84);
            this.lblLoader.Name = "lblLoader";
            this.lblLoader.Size = new System.Drawing.Size(77, 30);
            this.lblLoader.TabIndex = 3;
            this.lblLoader.Text = "|==>";
            this.lblLoader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblLoader.UseWaitCursor = true;
            this.lblLoader.MouseHover += new System.EventHandler(this.frmWaitbar_MouseHover);
            // 
            // lblClose
            // 
            this.lblClose.AutoSize = true;
            this.lblClose.BackColor = System.Drawing.Color.Red;
            this.lblClose.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.lblClose.Font = new System.Drawing.Font("Microsoft Sans Serif", 22F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblClose.ForeColor = System.Drawing.Color.White;
            this.lblClose.Location = new System.Drawing.Point(419, 9);
            this.lblClose.Name = "lblClose";
            this.lblClose.Size = new System.Drawing.Size(37, 36);
            this.lblClose.TabIndex = 4;
            this.lblClose.Text = "X";
            this.lblClose.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblClose.UseWaitCursor = true;
            this.lblClose.Visible = false;
            this.lblClose.Click += new System.EventHandler(this.label1_Click);
            this.lblClose.MouseHover += new System.EventHandler(this.lblClose_MouseHover);
            // 
            // frmWaitbar
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(96)))), ((int)(((byte)(146)))));
            this.ClientSize = new System.Drawing.Size(468, 143);
            this.Controls.Add(this.lblClose);
            this.Controls.Add(this.lblLoader);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblProgress);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmWaitbar";
            this.Text = "Waitbar";
            this.UseWaitCursor = true;
            this.Load += new System.EventHandler(this.frmWaitbar_Load);
            this.MouseHover += new System.EventHandler(this.frmWaitbar_MouseHover);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblProgress;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblLoader;
        private System.Windows.Forms.Label lblClose;
    }
}