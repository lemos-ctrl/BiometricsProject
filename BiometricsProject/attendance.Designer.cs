using System;

namespace BiometricsProject
{
    partial class attendance
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.PictureBox picFingerprint;
        private System.Windows.Forms.Label lblScanMessage;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblTime;
        private System.Windows.Forms.Timer countdownTimer;
        private System.Windows.Forms.Button btnExit;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(attendance));
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblScanMessage = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblTime = new System.Windows.Forms.Label();
            this.countdownTimer = new System.Windows.Forms.Timer(this.components);
            this.btnExit = new System.Windows.Forms.Button();
            this.picFingerprint = new System.Windows.Forms.PictureBox();
            this.lblCountdown = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.picFingerprint)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.ForeColor = System.Drawing.Color.Black;
            this.lblTitle.Location = new System.Drawing.Point(411, 23);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(221, 32);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Attendance Portal";
            // 
            // lblScanMessage
            // 
            this.lblScanMessage.AutoSize = true;
            this.lblScanMessage.BackColor = System.Drawing.Color.Transparent;
            this.lblScanMessage.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblScanMessage.ForeColor = System.Drawing.Color.Black;
            this.lblScanMessage.Location = new System.Drawing.Point(357, 232);
            this.lblScanMessage.Name = "lblScanMessage";
            this.lblScanMessage.Size = new System.Drawing.Size(358, 21);
            this.lblScanMessage.TabIndex = 2;
            this.lblScanMessage.Text = "Please scan your fingerprint to Clock-in/Clock-out.";
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStatus.BackColor = System.Drawing.Color.Transparent;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatus.ForeColor = System.Drawing.Color.Green;
            this.lblStatus.Location = new System.Drawing.Point(379, 262);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(290, 65);
            this.lblStatus.TabIndex = 5;
            this.lblStatus.Text = "Status: Ready";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblTime
            // 
            this.lblTime.AutoSize = true;
            this.lblTime.BackColor = System.Drawing.Color.Transparent;
            this.lblTime.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTime.ForeColor = System.Drawing.Color.Black;
            this.lblTime.Location = new System.Drawing.Point(458, 342);
            this.lblTime.Name = "lblTime";
            this.lblTime.Size = new System.Drawing.Size(138, 21);
            this.lblTime.TabIndex = 6;
            this.lblTime.Text = "Time: 02:41:20 PM";
            // 
            // countdownTimer
            // 
            this.countdownTimer.Enabled = true;
            this.countdownTimer.Interval = 1000;
            this.countdownTimer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // btnExit
            // 
            this.btnExit.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExit.Location = new System.Drawing.Point(480, 372);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(100, 30);
            this.btnExit.TabIndex = 7;
            this.btnExit.Text = "Exit";
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // picFingerprint
            // 
            this.picFingerprint.BackColor = System.Drawing.Color.Transparent;
            this.picFingerprint.Image = global::BiometricsProject.Properties.Resources.fingerprint_icon;
            this.picFingerprint.Location = new System.Drawing.Point(450, 70);
            this.picFingerprint.Name = "picFingerprint";
            this.picFingerprint.Size = new System.Drawing.Size(150, 150);
            this.picFingerprint.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picFingerprint.TabIndex = 1;
            this.picFingerprint.TabStop = false;
            // 
            // lblCountdown
            // 
            this.lblCountdown.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCountdown.AutoSize = true;
            this.lblCountdown.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblCountdown.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.lblCountdown.Location = new System.Drawing.Point(338, 284);
            this.lblCountdown.Name = "lblCountdown";
            this.lblCountdown.Size = new System.Drawing.Size(392, 21);
            this.lblCountdown.TabIndex = 8;
            this.lblCountdown.Text = "Please wait for: 10 seconds before scanning again.";
            this.lblCountdown.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.lblCountdown.Visible = false;
            // 
            // attendance
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(1060, 483);
            this.Controls.Add(this.lblCountdown);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.picFingerprint);
            this.Controls.Add(this.lblScanMessage);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblTime);
            this.Controls.Add(this.btnExit);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "attendance";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AMS Attendance Portal";
            this.TopMost = true;
            this.Resize += new System.EventHandler(this.attendance_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.picFingerprint)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private void timer_Tick(object sender, EventArgs e)
        {
            lblTime.Text = "Time: " + DateTime.Now.ToString("hh:mm:ss tt");
        }


        // Handle form resizing for dynamic control positioning
        private void attendance_Resize(object sender, EventArgs e)
        {
            // Center the controls horizontally
            lblTitle.Left = (this.ClientSize.Width - lblTitle.Width) / 2;
            lblScanMessage.Left = (this.ClientSize.Width - lblScanMessage.Width) / 2;

            lblStatus.Left = (this.ClientSize.Width - lblStatus.Width) / 2;
            lblTime.Left = (this.ClientSize.Width - lblTime.Width) / 2;
            btnExit.Left = (this.ClientSize.Width - btnExit.Width) / 2;
            picFingerprint.Left = (this.ClientSize.Width - picFingerprint.Width) / 2;

            // Center the controls vertically
            int totalHeight = lblTitle.Height + picFingerprint.Height + lblScanMessage.Height + lblStatus.Height + lblTime.Height + btnExit.Height + 50; // Adjust for spacing
            int startY = (this.ClientSize.Height - totalHeight) / 2;

            lblTitle.Top = startY;
            picFingerprint.Top = lblTitle.Bottom + 10;  // Adjust spacing as needed
            lblScanMessage.Top = picFingerprint.Bottom + 10;

            lblTime.Top = lblStatus.Bottom + 10;
            btnExit.Top = lblTime.Bottom + 10;
        }

        private System.Windows.Forms.Label lblCountdown;
    }
}
