using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace VersaVaultSyncTool
{
    public partial class Notification : Form
    {
        public Notification()
        {
            InitializeComponent();
        }

        private void NotificationLoad(object sender, EventArgs e)
        {
            int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
            int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;
            Left = screenWidth - Width - 5;
            Top = screenHeight - Height;
            FadeIn();
            //automaticUpdater1.ForceCheckForUpdate(true);
            var thread = new Thread(StartSilent);
            thread.Start();
            // Compute the updater.exe path relative to the application main module path
            updaterModulePath = Path.Combine(Application.StartupPath, "updater.exe");
        }

        private static void StartSilent()
        {
            Thread.Sleep(10000);
            Process process = Process.Start(updaterModulePath, "/silentall");
            if (process != null) process.Close();
        }

        private static String updaterModulePath;

        private void automaticUpdater1_ClosingAborted(object sender, EventArgs e)
        {
            HideForm();
        }

        private void HideForm()
        {
            Application.DoEvents();
            Thread.Sleep(1500);
            FadeOut();
        }

        private void FadeOut()
        {
            int loopctr;
            for (loopctr = 100; loopctr >= 5; loopctr -= 10)
            {
                Opacity = loopctr / 95.0;
                Refresh();
                Thread.Sleep(100);
            }
            Close();
            Dispose();
        }

        private void FadeIn()
        {
            int loopctr;
            for (loopctr = 10; loopctr <= 105; loopctr += 10)
            {
                Opacity = loopctr / 95.0;
                Refresh();
                Thread.Sleep(100);
            }
        }

        private void automaticUpdater1_ReadyToBeInstalled(object sender, EventArgs e)
        {
            try
            {
                Application.Exit();
                Process.Start(Application.StartupPath + "\\wyUpdate.exe");
            }
            catch (Exception)
            {
            }
        }

        private void automaticUpdater1_UpdateAvailable(object sender, EventArgs e)
        {
            HideForm();
        }

        private void automaticUpdater1_Cancelled(object sender, EventArgs e)
        {
            HideForm();
        }
    }
}