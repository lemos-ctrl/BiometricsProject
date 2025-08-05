namespace BiometricsProject
{
    partial class main
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



        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Button attendance_btn;
        private System.Windows.Forms.Button enrol_btn;
        private System.Windows.Forms.Button verify_btn;
        private System.Windows.Forms.Button exit_btn;

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(main));
            this.titleLabel = new System.Windows.Forms.Label();
            this.attendance_btn = new System.Windows.Forms.Button();
            this.enrol_btn = new System.Windows.Forms.Button();
            this.verify_btn = new System.Windows.Forms.Button();
            this.exit_btn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // titleLabel
            // 
            this.titleLabel.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.titleLabel.ForeColor = System.Drawing.Color.Maroon;
            this.titleLabel.Location = new System.Drawing.Point(0, 20);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(800, 50);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "🔒 Biometric Attendance System";
            this.titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // attendance_btn
            // 
            this.attendance_btn.Font = new System.Drawing.Font("Segoe UI", 14F);
            this.attendance_btn.Location = new System.Drawing.Point(300, 180);
            this.attendance_btn.Name = "attendance_btn";
            this.attendance_btn.Size = new System.Drawing.Size(200, 60);
            this.attendance_btn.TabIndex = 2;
            this.attendance_btn.Text = "📋 Attendance";
            this.attendance_btn.UseVisualStyleBackColor = true;
            this.attendance_btn.Click += new System.EventHandler(this.attendance_btn_Click);
            // 
            // enrol_btn
            // 
            this.enrol_btn.Font = new System.Drawing.Font("Segoe UI", 14F);
            this.enrol_btn.Location = new System.Drawing.Point(300, 100);
            this.enrol_btn.Name = "enrol_btn";
            this.enrol_btn.Size = new System.Drawing.Size(200, 60);
            this.enrol_btn.TabIndex = 4;
            this.enrol_btn.Text = "📝 Enroll";
            this.enrol_btn.UseVisualStyleBackColor = true;
            this.enrol_btn.Click += new System.EventHandler(this.enrol_btn_Click);
            // 
            // verify_btn
            // 
            this.verify_btn.Font = new System.Drawing.Font("Segoe UI", 14F);
            this.verify_btn.Location = new System.Drawing.Point(300, 260);
            this.verify_btn.Name = "verify_btn";
            this.verify_btn.Size = new System.Drawing.Size(200, 60);
            this.verify_btn.TabIndex = 3;
            this.verify_btn.Text = "🧐 Verify";
            this.verify_btn.UseVisualStyleBackColor = true;
            this.verify_btn.Click += new System.EventHandler(this.verify_btn_Click);
            // 
            // exit_btn
            // 
            this.exit_btn.Font = new System.Drawing.Font("Segoe UI", 14F);
            this.exit_btn.Location = new System.Drawing.Point(300, 340);
            this.exit_btn.Name = "exit_btn";
            this.exit_btn.Size = new System.Drawing.Size(200, 60);
            this.exit_btn.TabIndex = 5;
            this.exit_btn.Text = "🚪 Exit";
            this.exit_btn.UseVisualStyleBackColor = true;
            this.exit_btn.Click += new System.EventHandler(this.exit_btn_Click);
            // 
            // main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(800, 437);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.attendance_btn);
            this.Controls.Add(this.verify_btn);
            this.Controls.Add(this.enrol_btn);
            this.Controls.Add(this.exit_btn);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SWU-SHS AMS Biometric System";
            this.ResumeLayout(false);

        }

    }
}

