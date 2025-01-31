using System;
using System.Windows.Forms;

namespace BiometricsProject
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length > 0)
            {
                string command = args[0].ToLower();

                // New condition for launching attendance
                if (command == "attendance")
                {
                    Application.Run(new attendance());
                    return; // Stop further execution
                }

                // Keep the existing verify and enroll logic untouched
                string userId = args[0];

                if (userId.ToLower() == "verify")
                {
                    verify verifyForm = new verify();
                    Application.Run(verifyForm);
                }
                else
                {
                    enroll enrollForm = new enroll(userId);
                    enrollForm.OnTemplate += OnTemplate;
                    Application.Run(enrollForm);
                }
            }
            else
            {
                MessageBox.Show("No command provided. Use 'enroll <user_id>', 'verify', or 'attendance'.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Event handler for handling enrollment templates
        private static void OnTemplate(DPFP.Template template)
        {
            if (template != null)
            {
                MessageBox.Show("The fingerprint template is ready for fingerprint verification", "Fingerprint Enrollment");
            }
            else
            {
                MessageBox.Show("The fingerprint template is not valid. Repeat fingerprint scanning", "Fingerprint Enrollment");
            }
        }
    }
}
