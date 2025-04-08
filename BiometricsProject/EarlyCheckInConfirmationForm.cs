using System;
using System.Drawing;
using System.Windows.Forms;

namespace BiometricsProject
{
    public class EarlyCheckInConfirmationForm : Form
    {
        public EarlyCheckInConfirmationForm(string userName, TimeSpan earlyBy)
        {
            this.Text = "Early Clock-In Confirmation";
            this.Size = new Size(450, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lblMessage = new Label();
            int totalMinutes = (int)earlyBy.TotalMinutes;
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;

            string timeString;
            if (hours > 0 && minutes > 0)
            {
                timeString = $"{hours} hour{(hours > 1 ? "s" : "")} {minutes} minute{(minutes > 1 ? "s" : "")}";
            }
            else if (hours > 0)
            {
                timeString = $"{hours} hour{(hours > 1 ? "s" : "")}";
            }
            else
            {
                timeString = $"{minutes} minute{(minutes > 1 ? "s" : "")}";
            }

            lblMessage.Text = $"{userName}, you are clocking in {timeString} early. Are you sure you want to proceed?";
            lblMessage.AutoSize = false;
            lblMessage.Size = new Size(400, 50);
            lblMessage.Location = new Point(20, 20);
            lblMessage.TextAlign = ContentAlignment.MiddleCenter;

            Button btnYes = new Button();
            btnYes.Text = "Yes, Clock In Early";
            btnYes.DialogResult = DialogResult.Yes;
            btnYes.Size = new Size(150, 40);
            btnYes.Location = new Point(70, 80);

            Button btnNo = new Button();
            btnNo.Text = "No, Cancel";
            btnNo.DialogResult = DialogResult.No;
            btnNo.Size = new Size(150, 40);
            btnNo.Location = new Point(230, 80);

            this.Controls.Add(lblMessage);
            this.Controls.Add(btnYes);
            this.Controls.Add(btnNo);
        }
    }
}