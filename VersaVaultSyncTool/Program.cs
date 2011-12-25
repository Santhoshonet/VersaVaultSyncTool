using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace VersaVaultSyncTool
{
    static class Program
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [STAThread]
        static void Main(string[] args)
        {
            SystemEvents.SessionSwitch += SystemEventsSessionSwitch;
            SystemEvents.SessionEnding += SystemEventsSession;
            SystemEvents.SessionEnded += SystemEventsSessionEnded;
            if (args.Length == 0)
            {
                bool isthisNewApp;
                using (var mutex = new Mutex(true, "VersaVault", out isthisNewApp))
                {
                    if (isthisNewApp)
                    {
                        MessageBox.Show("Updated");
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        var notification = new Notification();
                        notification.ShowDialog();
                        //Application.Run(new VersaVault());
                    }
                    else
                    {
                        var current = Process.GetCurrentProcess();
                        foreach (var process in Process.GetProcessesByName(current.ProcessName))
                        {
                            if (process.Id == current.Id)
                            {
                                SetForegroundWindow(process.MainWindowHandle);
                                break;
                            }
                        }
                    }
                }
            }
            else if (args[0].ToLower().Trim() == "remove_config")
            {
                try
                {
                    Utilities.MyConfig.Username = string.Empty;
                    Utilities.MyConfig.Password = string.Empty;
                    Utilities.MyConfig.BucketKey = string.Empty;
                    Utilities.MyConfig.Save();
                }
                catch (Exception)
                {
                    return;
                }
            }
        }

        static void SystemEventsSessionEnded(object sender, SessionEndedEventArgs e)
        {
            Application.Exit();
        }

        static void SystemEventsSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            Application.Exit();
        }

        static void SystemEventsSession(object sender, SessionEndingEventArgs sessionEndingEventArgs)
        {
            Application.Exit();
        }
    }
}