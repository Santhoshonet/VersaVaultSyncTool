using System;
using System.Windows.Forms;

namespace UpdateDemoApp
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var result = new AutoUpdateStarter();
            result.StartProcessAndWait();
        }
    }
}