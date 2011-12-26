using System;
using System.Windows.Forms;

namespace VersaVaultUpdater
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length > 1 && !string.IsNullOrEmpty(args[0]) && !string.IsNullOrEmpty(args[1]))
                Application.Run(new Notification());
        }
    }
}