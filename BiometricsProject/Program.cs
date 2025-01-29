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
                // If a user ID is passed as an argument
                string userId = args[0];
                //string userId = (args.Length > 0) ? args[0] : "";

                // Check if the command is "verify"
                if (userId.ToLower() == "verify")
                {
                    verify verifyForm = new verify();
                    Application.Run(verifyForm);
                }
                else
                {
                    // Run the enrollment form with the provided user ID
                    enroll enrollForm = new enroll(userId);
                    enrollForm.OnTemplate += OnTemplate;
                    Application.Run(enrollForm);
                }
            }
            else
            {
                // Default behavior if no arguments are passed
                MessageBox.Show("No user ID provided. Please provide a user ID.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
