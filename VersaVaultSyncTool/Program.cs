using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Win32;
using VersaVaultLibrary;

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
                StartSyncApp();
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
            else if (args.Length > 1 && args[0].ToLower().Trim() == "set_version")
            {
                Utilities.MyConfig.VersionId = args[1];
                Utilities.MyConfig.LastUpdateDate = DateTime.Now;
                Utilities.MyConfig.Save();
                StartSyncApp();
            }
        }

        static void StartSyncApp()
        {
            bool isthisNewApp;
            using (var mutex = new Mutex(true, "VersaVault", out isthisNewApp))
            {
                if (isthisNewApp)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    if (CheckVersion())
                        Application.Run(new VersaVault());
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

        static bool CheckVersion()
        {
            // first check the last updated date
            var needtoUpdate = false;
            if (Utilities.MyConfig.LastUpdateDate == DateTime.MinValue)
                needtoUpdate = true;
            else
            {
                TimeSpan timeSpan = DateTime.Now.Subtract(Utilities.MyConfig.LastUpdateDate);
                if (timeSpan.Days >= 10)
                    needtoUpdate = true;
            }
            if (needtoUpdate)
            {
                var amazons3 = AWSClientFactory.CreateAmazonS3Client(Utilities.AwsAccessKey, Utilities.AwsSecretKey,
                                                                     new AmazonS3Config { CommunicationProtocol = Protocol.HTTP });
                var listVersionRequest = new ListVersionsRequest() { BucketName = "VersaVault", Prefix = "VersaVaultSyncTool.exe" };
                foreach (var s3ObjectVersion in amazons3.ListVersions(listVersionRequest).Versions)
                {
                    if (s3ObjectVersion.IsLatest)
                    {
                        if (s3ObjectVersion.VersionId != null && s3ObjectVersion.VersionId != "null")
                        {
                            if (!string.IsNullOrEmpty(Utilities.MyConfig.VersionId) && Utilities.MyConfig.VersionId != s3ObjectVersion.VersionId)
                            {
                                var startInfo =
                                    new ProcessStartInfo(
                                        Path.Combine(Application.StartupPath, "VersaVaultUpdater.exe"),
                                        s3ObjectVersion.VersionId + " " + "update") { Verb = "runas" };
                                Process.Start(startInfo);
                                Application.Exit();
                                return false;
                            }
                            Utilities.MyConfig.VersionId = s3ObjectVersion.VersionId;
                            Utilities.MyConfig.Save();
                            return true;
                        }
                        else
                        {
                            // enable bucker versioning
                            var setBucketVersioning = new SetBucketVersioningRequest
                                                          {
                                                              BucketName = "VersaVault",
                                                              VersioningConfig =
                                                                  new S3BucketVersioningConfig() { Status = "Enabled" }
                                                          };
                            amazons3.SetBucketVersioning(setBucketVersioning);
                        }
                        break;
                    }
                }
                return true;
            }
            return true;
        }
    }
}