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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.enrol_btn = new System.Windows.Forms.Button();
            this.verify_btn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // enrol_btn
            // 
            this.enrol_btn.Location = new System.Drawing.Point(302, 48);
            this.enrol_btn.Name = "enrol_btn";
            this.enrol_btn.Size = new System.Drawing.Size(172, 79);
            this.enrol_btn.TabIndex = 0;
            this.enrol_btn.Text = "Enroll";
            this.enrol_btn.UseVisualStyleBackColor = true;
            this.enrol_btn.Click += new System.EventHandler(this.enrol_btn_Click);
            // 
            // verify_btn
            // 
            this.verify_btn.Location = new System.Drawing.Point(302, 238);
            this.verify_btn.Name = "verify_btn";
            this.verify_btn.Size = new System.Drawing.Size(172, 79);
            this.verify_btn.TabIndex = 1;
            this.verify_btn.Text = "Verify";
            this.verify_btn.UseVisualStyleBackColor = true;
            this.verify_btn.Click += new System.EventHandler(this.verify_btn_Click);
            // 
            // main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.verify_btn);
            this.Controls.Add(this.enrol_btn);
            this.Name = "main";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button enrol_btn;
        private System.Windows.Forms.Button verify_btn;
    }
}

