using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Amazon;
using Amazon.S3;
using LitS3;
using VersaVaultLibrary;

namespace VersaVaultUpdater
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
                if (File.Exists(Path.Combine(Application.StartupPath, "VersaVaultSyncTool.exe")))
                {
                    while (Process.GetProcessesByName("VersaVaultSyncTool").Length != 0)
                    {
                        Thread.Sleep(1000);
                        Application.DoEvents();
                    }
                    if (File.Exists(Path.Combine(Application.StartupPath, "VersaVaultSyncTool_old.exe")))
                        File.Delete(Path.Combine(Application.StartupPath, "VersaVaultSyncTool_old.exe"));
                    File.Move(Path.Combine(Application.StartupPath, "VersaVaultSyncTool.exe"),
                              Path.Combine(Application.StartupPath, "VersaVaultSyncTool_old.exe"));
                    var amazons3 = AWSClientFactory.CreateAmazonS3Client(Utilities.AwsAccessKey, Utilities.AwsSecretKey, new AmazonS3Config { CommunicationProtocol = Amazon.S3.Model.Protocol.HTTP });
                    var listVersionRequest = new Amazon.S3.Model.ListVersionsRequest { BucketName = "VersaVault", Prefix = "VersaVaultSyncTool.exe" };
                    foreach (var s3ObjectVersion in amazons3.ListVersions(listVersionRequest).Versions)
                    {
                        if (s3ObjectVersion.IsLatest)
                        {
                            if (s3ObjectVersion.VersionId != null && s3ObjectVersion.VersionId != "null")
                            {
                                if (Utilities.MyConfig.VersionId != s3ObjectVersion.VersionId)
                                {
                                    var service = new S3Service { AccessKeyID = Utilities.AwsAccessKey, SecretAccessKey = Utilities.AwsSecretKey };
                                    LblStatus.Text = "Started downloading update.";
                                    ShowForm();
                                    service.GetObjectProgress += ServiceGetObjectProgress;
                                    service.GetObject("VersaVault", s3ObjectVersion.Key, Path.Combine(Application.StartupPath, "VersaVaultSyncTool.exe"));
                                    LblStatus.Text = "Updating VersaVault";
                                    HideForm();
                                    if (File.Exists(Path.Combine(Application.StartupPath, "VersaVaultSyncTool.exe")))
                                    {
                                        Process.Start(Path.Combine(Application.StartupPath, "VersaVaultSyncTool.exe"), "set_version" + " " + s3ObjectVersion.VersionId);
                                        Application.Exit();
                                    }
                                }
                            }
                            else
                            {
                                // enable bucker versioning
                                var setBucketVersioning = new Amazon.S3.Model.SetBucketVersioningRequest { BucketName = "VersaVault", VersioningConfig = new Amazon.S3.Model.S3BucketVersioningConfig { Status = "Enabled" } };
                                amazons3.SetBucketVersioning(setBucketVersioning);
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                LblStatus.Text = "Unable to update VersaVault due to " + ex.Message;
                StartVersaVault();
            }
        }

        private void StartVersaVault()
        {
            HideForm();
            Process.Start(Path.Combine(Application.StartupPath, "VersaVaultSyncTool.exe"));
            Application.Exit();
        }

        private void ServiceGetObjectProgress(object sender, S3ProgressEventArgs e)
        {
            LblStatus.Text = "Downloading updates - " + Math.Round((Convert.ToDouble(e.BytesTransferred) / Convert.ToDouble(e.BytesTotal)) * 100, 0) + @"%";
            Application.DoEvents();
        }

        private void ShowForm()
        {
            Application.DoEvents();
            FadeIn();
            Application.DoEvents();
        }

        private void HideForm()
        {
            Application.DoEvents();
            Thread.Sleep(500);
            FadeOut();
            Application.DoEvents();
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