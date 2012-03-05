using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using LitS3;
using Microsoft.Win32;
using VersaVaultLibrary;

namespace VersaVaultSyncTool
{
    public static class Program
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        private static Notification _notification;

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
                    if (!Utilities.DevelopmentMode && CheckVersion(true))
                        Application.Run(new VersaVault());
                    else if (Utilities.DevelopmentMode)
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

        public static bool CheckVersion(bool forceCheck)
        {
            // first check the last updated date
            _notification = new Notification();
            _notification.SetMessage("Checking for updates");
            try
            {
                if (forceCheck)
                {
                    _notification.Show();
                    _notification.ShowForm(10);
                }
                if (forceCheck)
                {
                    var amazons3 = AWSClientFactory.CreateAmazonS3Client(Utilities.AwsAccessKey, Utilities.AwsSecretKey, new AmazonS3Config { CommunicationProtocol = Protocol.HTTP });
                    var listVersionRequest = new ListVersionsRequest { BucketName = Utilities.AppRootBucketName, Prefix = "VersaVaultSyncTool_32Bit.exe" };
                    foreach (var s3ObjectVersion in amazons3.ListVersions(listVersionRequest).Versions)
                    {
                        if (s3ObjectVersion.IsLatest)
                        {
                            if (!string.IsNullOrEmpty(Utilities.MyConfig.InstallerVersionId) && Utilities.MyConfig.InstallerVersionId != s3ObjectVersion.VersionId)
                            {
                                while (Process.GetProcessesByName("VersaVaultSyncTool_32Bit").Length != 0)
                                {
                                    Thread.Sleep(1000);
                                    Application.DoEvents();
                                }
                                if (File.Exists(Path.Combine(Path.GetTempPath(), "VersaVaultSyncTool_32Bit_old.exe")))
                                    File.Delete(Path.Combine(Path.GetTempPath(), "VersaVaultSyncTool_32Bit_old.exe"));
                                if (File.Exists(Path.Combine(Path.GetTempPath(), "VersaVaultSyncTool_32Bit.exe")))
                                {
                                    File.Move(Path.Combine(Path.GetTempPath(), "VersaVaultSyncTool_32Bit.exe"),
                                              Path.Combine(Path.GetTempPath(), "VersaVaultSyncTool_32Bit_old.exe"));
                                }
                                var service = new S3Service { AccessKeyID = Utilities.AwsAccessKey, SecretAccessKey = Utilities.AwsSecretKey };
                                _notification.SetMessage("Started downloading update.");
                                service.GetObjectProgress += ServiceGetObjectProgress;
                                service.GetObject(Utilities.AppRootBucketName, s3ObjectVersion.Key, Path.Combine(Path.GetTempPath(), "VersaVaultSyncTool_32Bit.exe"));
                                _notification.SetMessage("Updating VersaVault");
                                var startInfo = new ProcessStartInfo(Path.Combine(Path.GetTempPath(), "VersaVaultSyncTool_32Bit.exe")) { Verb = "runas" };
                                Process.Start(startInfo);
                                Application.Exit();
                                return false;
                            }
                            Utilities.MyConfig.InstallerVersionId = s3ObjectVersion.VersionId;
                            Utilities.MyConfig.Save();
                            break;
                        }
                    }
                    listVersionRequest = new ListVersionsRequest { BucketName = Utilities.AppRootBucketName, Prefix = "VersaVaultSyncTool.exe" };
                    foreach (var s3ObjectVersion in amazons3.ListVersions(listVersionRequest).Versions)
                    {
                        if (s3ObjectVersion.IsLatest)
                        {
                            if (s3ObjectVersion.VersionId != null && s3ObjectVersion.VersionId != "null")
                            {
                                if (!string.IsNullOrEmpty(Utilities.MyConfig.VersionId) && Utilities.MyConfig.VersionId != s3ObjectVersion.VersionId)
                                {
                                    _notification.Dispose();
                                    var startInfo = new ProcessStartInfo(Path.Combine(Application.StartupPath, "VersaVaultUpdater.exe"), s3ObjectVersion.VersionId + " " + "update") { Verb = "runas" };
                                    Process.Start(startInfo);
                                    Application.Exit();
                                    return false;
                                }
                                Utilities.MyConfig.VersionId = s3ObjectVersion.VersionId;
                                Utilities.MyConfig.Save();
                                return true;
                            }
                            // enable bucker versioning
                            var setBucketVersioning = new SetBucketVersioningRequest { BucketName = Utilities.AppRootBucketName, VersioningConfig = new S3BucketVersioningConfig { Status = "Enabled" } };
                            amazons3.SetBucketVersioning(setBucketVersioning);
                            break;
                        }
                    }
                    return true;
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                try
                {
                    _notification.Controls["LblStatus"].Text = @"VersaVault is upto date.";
                    _notification.HideForm(10);
                    _notification.Close();
                    _notification.Dispose();
                }
                catch (Exception)
                {
                }
            }
            return true;
        }

        private static void ServiceGetObjectProgress(object sender, S3ProgressEventArgs e)
        {
            _notification.SetMessage("Downloading updates - " + Math.Round((Convert.ToDouble(e.BytesTransferred) / Convert.ToDouble(e.BytesTotal)) * 100, 0) + @"%");
            Application.DoEvents();
        }
    }
}