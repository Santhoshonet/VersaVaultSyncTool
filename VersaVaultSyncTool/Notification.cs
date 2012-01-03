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
            try
            {
                int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
                int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;
                Left = screenWidth - Width - 5;
                Top = screenHeight - Height;
                //ShowForm(10);
            }
            catch (Exception)
            {
                return;
            }
        }

        public void SetMessage(string message)
        {
            LblStatus.Text = message;
        }

        public void ShowForm(int speed)
        {
            Application.DoEvents();
            FadeIn(speed);
            Application.DoEvents();
        }

        public void HideForm(int speed)
        {
            Application.DoEvents();
            Thread.Sleep(500);
            FadeOut(speed);
            Application.DoEvents();
            Opacity = 0;
        }

        private void FadeOut(int speed)
        {
            try
            {
                int loopctr;
                for (loopctr = 100; loopctr >= 5; loopctr -= speed)
                {
                    if (Opacity > loopctr / 95.0)
                        Opacity = loopctr / 95.0;
                    Refresh();
                    Thread.Sleep(100);
                    Application.DoEvents();
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        private void FadeIn(int speed)
        {
            try
            {
                int loopctr;
                for (loopctr = 10; loopctr <= 105; loopctr += speed)
                {
                    if (Opacity < loopctr / 95.0)
                        Opacity = loopctr / 95.0;
                    Refresh();
                    Thread.Sleep(100);
                    Application.DoEvents();
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        private void Notification_FormClosing(object sender, FormClosingEventArgs e)
        {
            //e.Cancel = true;
        }
    }
}