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

            // ALWAYS show main menu now
            Application.Run(new main());
        }

            
    }
}
