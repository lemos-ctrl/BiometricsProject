﻿namespace BiometricsProject
{
    partial class capture
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
            this.components = new System.ComponentModel.Container();
            this.fImage = new System.Windows.Forms.PictureBox();
            this.StatusLabel = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.start_scan = new System.Windows.Forms.Button();
            this.Prompt = new System.Windows.Forms.TextBox();
            this.fname = new System.Windows.Forms.TextBox();
            this.StatusText = new System.Windows.Forms.TextBox();
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.fImage)).BeginInit();
            this.SuspendLayout();
            // 
            // fImage
            // 
            this.fImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.fImage.Enabled = false;
            this.fImage.Location = new System.Drawing.Point(12, 12);
            this.fImage.Name = "fImage";
            this.fImage.Size = new System.Drawing.Size(301, 321);
            this.fImage.TabIndex = 0;
            this.fImage.TabStop = false;
            // 
            // StatusLabel
            // 
            this.StatusLabel.AutoSize = true;
            this.StatusLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.StatusLabel.Location = new System.Drawing.Point(19, 342);
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(58, 15);
            this.StatusLabel.TabIndex = 3;
            this.StatusLabel.Text = "[STATUS]";
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // start_scan
            // 
            this.start_scan.Location = new System.Drawing.Point(689, 402);
            this.start_scan.Name = "start_scan";
            this.start_scan.Size = new System.Drawing.Size(99, 36);
            this.start_scan.TabIndex = 5;
            this.start_scan.Text = "Start Scan";
            this.start_scan.UseVisualStyleBackColor = true;
            this.start_scan.Click += new System.EventHandler(this.start_scan_Click);
            // 
            // Prompt
            // 
            this.Prompt.Location = new System.Drawing.Point(330, 12);
            this.Prompt.Name = "Prompt";
            this.Prompt.Size = new System.Drawing.Size(458, 20);
            this.Prompt.TabIndex = 1;
            // 
            // fname
            // 
            this.fname.Location = new System.Drawing.Point(12, 370);
            this.fname.Name = "fname";
            this.fname.Size = new System.Drawing.Size(301, 20);
            this.fname.TabIndex = 4;
            this.fname.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.fname.TextChanged += new System.EventHandler(this.fname_TextChanged);
            // 
            // StatusText
            // 
            this.StatusText.Location = new System.Drawing.Point(330, 38);
            this.StatusText.Multiline = true;
            this.StatusText.Name = "StatusText";
            this.StatusText.Size = new System.Drawing.Size(458, 295);
            this.StatusText.TabIndex = 2;
            // 
            // capture
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(324, 405);
            this.Controls.Add(this.start_scan);
            this.Controls.Add(this.fname);
            this.Controls.Add(this.StatusLabel);
            this.Controls.Add(this.StatusText);
            this.Controls.Add(this.Prompt);
            this.Controls.Add(this.fImage);
            this.Name = "capture";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "capture";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.capture_FormClosing);
            this.Load += new System.EventHandler(this.capture_Load);
            ((System.ComponentModel.ISupportInitialize)(this.fImage)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox fImage;
        private System.Windows.Forms.Label StatusLabel;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button start_scan;
        private System.Windows.Forms.TextBox Prompt;
        private System.Windows.Forms.TextBox fname;
        private System.Windows.Forms.TextBox StatusText;
    }
}