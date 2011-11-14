using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms;
using LitS3;
using Newtonsoft.Json;

namespace VersaVaultSyncTool
{
    public partial class VersaVault : Form
    {
        S3Service service;

        private bool _syncIsPaused;

        public VersaVault()
        {
            InitializeComponent();
        }

        private void BtnAuthenticate_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            string url = string.Empty;
            if (Utilities.DevelopmentMode)
                url = "http://localhost:3000";
            else
                url = "http://versavault.com";
            url += "/get_amazon_bucket_id?username=" + TxtUsername.Text.Trim() + "&password=" + TxtPassword.Text.Trim();
            BtnAuthenticate.Enabled = false;
            BtnAuthenticate.Text = "Please Wait ...";
            string result = GetResponse(url).ToString();
            if (result != null && result != string.Empty)
            {
                var res = JsonConvert.DeserializeObject<account>(result);
                if (res.bucket_id != null)
                {
                    Utilities.MyConfig.BucketKey = res.bucket_id;
                    Utilities.MyConfig.Save();
                    this.WindowState = FormWindowState.Minimized;
                    this.Visible = true;
                    return;
                }
                else
                {
                    LblError.Text = res.error;
                }
            }
            this.WindowState = FormWindowState.Normal;
            Application.DoEvents();
            BtnAuthenticate.Enabled = true;
            BtnAuthenticate.Text = "Authenticate";
        }

        private string GetResponse(string strUrl)
        {
            string strReturn;
            HttpWebRequest objRequest;
            IAsyncResult ar;
            HttpWebResponse objResponse = null;
            StreamReader objs;
            try
            {
                objRequest = (HttpWebRequest)WebRequest.Create(strUrl);
                ar = objRequest.BeginGetResponse(GetScrapingResponse, objRequest);
                //// Wait for request to complete
                ar.AsyncWaitHandle.WaitOne(1000 * 60, true);
                if (objRequest.HaveResponse == false)
                {
                    return string.Empty;
                }
                objResponse = (HttpWebResponse)objRequest.EndGetResponse(ar);
                // ReSharper disable AssignNullToNotNullAttribute
                objs = new StreamReader(objResponse.GetResponseStream());
                // ReSharper restore AssignNullToNotNullAttribute
                strReturn = objs.ReadToEnd();
            }
            catch (Exception ex)
            {
                LblError.Text = ex.Message;
                return string.Empty;
            }
            finally
            {
                if (objResponse != null)
                    objResponse.Close();
            }
            return strReturn;
        }

        protected void GetScrapingResponse(IAsyncResult result)
        {
        }

        private void VersaVault_Load(object sender, EventArgs e)
        {
            try
            {
                service = new LitS3.S3Service();
                service.AccessKeyID = Utilities.AwsAccessKey;
                service.SecretAccessKey = Utilities.AwsSecretKey;
                service.UseSubdomains = true;

                if (!Directory.Exists(Utilities.Path))
                    Directory.CreateDirectory(Utilities.Path);

                if (!Directory.Exists(Utilities.AppPath))
                    Directory.CreateDirectory(Utilities.AppPath);

                if (string.IsNullOrEmpty(Utilities.MyConfig.BucketKey))
                {
                    _syncIsPaused = true;
                    startSyncToolStripMenuItem.Text = "Start Sync";
                    this.WindowState = FormWindowState.Normal;
                    this.Visible = true;
                }
                else
                {
                    _syncIsPaused = false;
                    StartActivityMonitoring(Utilities.Path);
                    startSyncToolStripMenuItem.Text = "Pause Sync";
                    this.WindowState = FormWindowState.Minimized;
                    this.Visible = false;
                    this.Opacity = 0;
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void StartActivityMonitoring(string sPath)
        {
            _watchFolder.Path = sPath;
            _watchFolder.IncludeSubdirectories = true;

            _watchFolder.NotifyFilter = NotifyFilters.DirectoryName;
            _watchFolder.NotifyFilter = _watchFolder.NotifyFilter | NotifyFilters.FileName;
            _watchFolder.NotifyFilter = _watchFolder.NotifyFilter | NotifyFilters.Attributes;
            _watchFolder.NotifyFilter = _watchFolder.NotifyFilter | NotifyFilters.Size;
            _watchFolder.NotifyFilter = _watchFolder.NotifyFilter | NotifyFilters.LastWrite;

            _watchFolder.Changed += EventRaised;
            _watchFolder.Created += EventRaised;
            _watchFolder.Deleted += EventRaised;
            _watchFolder.Renamed += EventRaised;
            _watchFolder.Error += new ErrorEventHandler(_watchFolder_Error);
            try
            {
                _watchFolder.EnableRaisingEvents = true;
            }
            catch (ArgumentException)
            {
            }
        }

        void _watchFolder_Error(object sender, ErrorEventArgs e)
        {
            showBalloon("Error", "", ToolTipIcon.Error);
        }

        private void VersaVault_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.WindowState = FormWindowState.Minimized;
            this.Opacity = 0;
            Application.DoEvents();
        }

        private void VersaVault_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utilities.Startup(startToolStripMenuItem.CheckState == CheckState.Checked, Application.ExecutablePath);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PnlAuthentication.Visible = true;
            this.WindowState = FormWindowState.Normal;
            this.Opacity = 1;
            Application.DoEvents();
        }

        private void startSyncToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Utilities.MyConfig.BucketKey))
            {
                PnlAuthentication.Visible = true;
            }
        }

        private void VersaVaultNotifications_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Utilities.Path);
        }

        private void VersaVaultNotifications_DoubleClick(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Utilities.Path);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                this.Close();
                this.Dispose();
                VersaVaultNotifications.Dispose();
            }
            catch (Exception)
            {
            }
        }

        private void EventRaised(object sender, FileSystemEventArgs e)
        {
            if (!_syncIsPaused)
            {
                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Changed:
                        {
                            var attribute = System.IO.File.GetAttributes(e.FullPath);
                            if (attribute != FileAttributes.Directory && attribute != FileAttributes.Temporary)
                            {
                                if (File.Exists(e.FullPath) && !IsFileUsedbyAnotherProcess(e.FullPath))
                                {
                                    showBalloon("Uploading file content......", "File Update.", ToolTipIcon.Info);
                                    string relativePath = e.FullPath.Replace(Utilities.Path + "\\", "");
                                    service.AddObject(e.FullPath, Utilities.MyConfig.BucketKey, relativePath);
                                }
                            }
                            break;
                        }
                    case WatcherChangeTypes.Created:
                        {
                            var attribute = System.IO.File.GetAttributes(e.FullPath);
                            string relativePath = e.FullPath.Replace(Utilities.Path + "\\", "");
                            if (attribute != FileAttributes.Directory && attribute != FileAttributes.Temporary)
                            {
                                if (File.Exists(e.FullPath) && !IsFileUsedbyAnotherProcess(e.FullPath))
                                {
                                    showBalloon("Uploading file content......", "New file.", ToolTipIcon.Info);
                                    service.AddObject(e.FullPath, Utilities.MyConfig.BucketKey, relativePath);
                                }
                            }
                            else
                            {
                                // To do
                                // Creating an Folder
                                //String extension = "_$folder$";
                                //service.AddObject(new MemoryStream(new byte[0]), Utilities.MyConfig.BucketKey, relativePath + extension);
                            }
                            break;
                        }
                    case WatcherChangeTypes.Deleted:
                        {
                            var attribute = System.IO.File.GetAttributes(e.FullPath);
                            if (attribute != FileAttributes.Temporary)
                            {
                                showBalloon("Deleting......", "File deleted.", ToolTipIcon.Info);
                                string relativePath = e.FullPath.Replace(Utilities.Path + "\\", "");
                                service.DeleteObject(Utilities.MyConfig.BucketKey, relativePath);
                            }
                            break;
                        }
                    case WatcherChangeTypes.Renamed:
                        {
                            var attribute = System.IO.File.GetAttributes(e.FullPath);
                            if (attribute != FileAttributes.Directory && attribute != FileAttributes.Temporary)
                            {
                                var renamedEvent = (RenamedEventArgs)e;
                                string newrelativePath = renamedEvent.FullPath.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                                string oldrelativePath = renamedEvent.OldFullPath.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                                if (service.ObjectExists(Utilities.MyConfig.BucketKey, oldrelativePath))
                                {
                                    showBalloon("Renaming file name...", "File name modified.", ToolTipIcon.Info);
                                    LitS3.CopyObjectRequest request = new CopyObjectRequest(service, Utilities.MyConfig.BucketKey, oldrelativePath, newrelativePath);
                                    LitS3.CopyObjectResponse response = request.GetResponse();
                                    if (response.Error == null)
                                    {
                                        service.DeleteObject(Utilities.MyConfig.BucketKey, oldrelativePath);
                                    }
                                }
                                else // if the previous file not found
                                {
                                    if (File.Exists(e.FullPath) && !IsFileUsedbyAnotherProcess(e.FullPath))
                                    {
                                        showBalloon("Uploading file content......", "New file.", ToolTipIcon.Info);
                                        service.AddObject(e.FullPath, Utilities.MyConfig.BucketKey, newrelativePath);
                                    }
                                }
                            }
                            else
                            {
                                // To do
                                // Folder rename
                                /*
                                 var renamedEvent = (RenamedEventArgs)e;
                                string newrelativePath = renamedEvent.FullPath.Replace(Utilities.Path + "\\", "");
                                string oldrelativePath = renamedEvent.OldFullPath.Replace(Utilities.Path + "\\", "");
                                //if (!service.ObjectExists(Utilities.MyConfig.BucketKey, relativePath))
                                //{
                                //}
                                LitS3.CopyObjectRequest request = new CopyObjectRequest(service, Utilities.MyConfig.BucketKey, oldrelativePath, newrelativePath);
                                LitS3.CopyObjectResponse response = request.GetResponse();
                                if (response.Error == null)
                                {
                                    service.DeleteObject(Utilities.MyConfig.BucketKey, oldrelativePath);
                                } */
                            }
                            break;
                        }
                }
            }
            else
            {
                // To do
                // Need to maintain queue
            }
        }

        private void showBalloon(string text, string title, ToolTipIcon icon)
        {
            VersaVaultNotifications.ShowBalloonTip(2000, title, text, icon);
        }

        private bool IsFileUsedbyAnotherProcess(string filename)
        {
            try
            {
                File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (System.IO.IOException exp)
            {
                return true;
            }
            return false;
        }
    }
}