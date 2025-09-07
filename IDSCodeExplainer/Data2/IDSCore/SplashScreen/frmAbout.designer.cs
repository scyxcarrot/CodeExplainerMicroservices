namespace IDS.Core.SplashScreen
{
    partial class frmAbout
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmAbout));
            this.lblVersion = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.lblManufactured = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lblCopyrightAndLNumber = new System.Windows.Forms.Label();
            this.lblDisclaimer = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblVersion
            // 
            this.lblVersion.BackColor = System.Drawing.Color.Transparent;
            this.lblVersion.Font = new System.Drawing.Font("Century Gothic", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblVersion.ForeColor = System.Drawing.Color.Black;
            this.lblVersion.Location = new System.Drawing.Point(197, 173);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(160, 24);
            this.lblVersion.TabIndex = 2;
            this.lblVersion.Text = "0.0";
            this.lblVersion.Click += new System.EventHandler(this.CloseForm);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Black;
            this.label1.Location = new System.Drawing.Point(76, 238);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(263, 30);
            this.label1.TabIndex = 3;
            this.label1.Text = "Materialise NV\r\nTechnologielaan 15, BE 3001 Leuven, Belgium";
            // 
            // lblManufactured
            // 
            this.lblManufactured.AutoSize = true;
            this.lblManufactured.BackColor = System.Drawing.Color.Transparent;
            this.lblManufactured.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblManufactured.ForeColor = System.Drawing.Color.Black;
            this.lblManufactured.Location = new System.Drawing.Point(76, 220);
            this.lblManufactured.Name = "label2";
            this.lblManufactured.Size = new System.Drawing.Size(0, 15);
            this.lblManufactured.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.Black;
            this.label3.Location = new System.Drawing.Point(40, 268);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(320, 30);
            this.label3.TabIndex = 3;
            this.label3.Text = "                       This software is intended for Materialise \r\ninternal use o" +
    "nly and is not intended to be used externally";
            // 
            // lblCopyrightAndLNumber
            // 
            this.lblCopyrightAndLNumber.AutoSize = true;
            this.lblCopyrightAndLNumber.BackColor = System.Drawing.Color.Transparent;
            this.lblCopyrightAndLNumber.Font = new System.Drawing.Font("Arial", 8F);
            this.lblCopyrightAndLNumber.ForeColor = System.Drawing.Color.Black;
            this.lblCopyrightAndLNumber.Location = new System.Drawing.Point(461, 336);
            this.lblCopyrightAndLNumber.Name = "label4";
            this.lblCopyrightAndLNumber.Size = new System.Drawing.Size(119, 28);
            this.lblCopyrightAndLNumber.TabIndex = 3;
            this.lblCopyrightAndLNumber.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblDisclaimer
            // 
            this.lblDisclaimer.AutoSize = true;
            this.lblDisclaimer.BackColor = System.Drawing.Color.Transparent;
            this.lblDisclaimer.Font = new System.Drawing.Font("Arial Narrow", 7F);
            this.lblDisclaimer.ForeColor = System.Drawing.Color.Black;
            this.lblDisclaimer.Location = new System.Drawing.Point(40, 304);
            this.lblDisclaimer.Name = "label5";
            this.lblDisclaimer.Size = new System.Drawing.Size(402, 56);
            this.lblDisclaimer.TabIndex = 3;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.BackColor = System.Drawing.Color.Transparent;
            this.label6.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.Color.Black;
            this.label6.Location = new System.Drawing.Point(40, 268);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(71, 15);
            this.label6.TabIndex = 4;
            this.label6.Text = "Disclaimer:";
            // 
            // frmAbout
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("IDSSplashScreenBG")));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(607, 374);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.lblManufactured);
            this.Controls.Add(this.lblCopyrightAndLNumber);
            this.Controls.Add(this.lblDisclaimer);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblVersion);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmAbout";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "frmAbout";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmAbout_FormClosing);
            this.Click += new System.EventHandler(this.CloseForm);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblManufactured;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lblCopyrightAndLNumber;
        private System.Windows.Forms.Label lblDisclaimer;
        private System.Windows.Forms.Label label6;
    }
}