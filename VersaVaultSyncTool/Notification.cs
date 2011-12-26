using System;
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
    }
}