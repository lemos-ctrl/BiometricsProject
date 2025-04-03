using System;
using System.Drawing;
using System.Windows.Forms;

namespace BiometricsProject
{
    public partial class ConfirmationForm : Form
    {
        public bool Confirmed { get; private set; }
        private string userName;

        public ConfirmationForm(string userName)
        {
            InitializeComponent();
            this.userName = userName;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
        }

        private void InitializeComponent()
        {
            this.lblMessage = new Label();
            this.btnYes = new Button();
            this.btnNo = new Button();
            this.pictureBox1 = new PictureBox();

            // lblMessage
            this.lblMessage.AutoSize = true;
            this.lblMessage.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.lblMessage.Location = new Point(80, 20);
            this.lblMessage.MaximumSize = new Size(250, 0);
            this.lblMessage.Text = $"{userName} You are currently checked in.\nDo you want to check out now?";

            // btnYes
            this.btnYes.Location = new Point(80, 70);
            this.btnYes.Size = new Size(80, 30);
            this.btnYes.Text = "Yes";
            this.btnYes.Click += (sender, e) => {
                this.Confirmed = true;
                this.DialogResult = DialogResult.Yes;
                this.Close();
            };

            // btnNo
            this.btnNo.Location = new Point(170, 70);
            this.btnNo.Size = new Size(80, 30);
            this.btnNo.Text = "No";
            this.btnNo.Click += (sender, e) => {
                this.Confirmed = false;
                this.DialogResult = DialogResult.No;
                this.Close();
            };

            // pictureBox1
            this.pictureBox1.Image = SystemIcons.Question.ToBitmap();
            this.pictureBox1.Location = new Point(20, 20);
            this.pictureBox1.Size = new Size(40, 40);
            this.pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;

            // ConfirmationForm
            this.ClientSize = new Size(330, 120);
            this.Controls.Add(this.lblMessage);
            this.Controls.Add(this.btnYes);
            this.Controls.Add(this.btnNo);
            this.Controls.Add(this.pictureBox1);
            this.Text = "Confirm Check Out";
        }

        private Label lblMessage;
        private Button btnYes;
        private Button btnNo;
        private PictureBox pictureBox1;
    }
}