using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using LitS3;
using Newtonsoft.Json;

namespace VersaVaultSyncTool
{
    public partial class VersaVault : Form
    {
        S3Service service;

        private string SyncStatus = string.Empty;

        private bool _syncIsPaused;

        private bool _isStartedMonitoring = false;

        private List<string> download_objects;

        public VersaVault()
        {
            InitializeComponent();
        }

        private void BtnAuthenticate_Click(object sender, EventArgs e)
        {
            getBucketId();
        }

        private void getBucketId()
        {
            this.Visible = false;
            this.Opacity = 0;
            Application.DoEvents();
            string url = string.Empty;
            if (Utilities.DevelopmentMode)
                url = "http://localhost:3000";
            else
                url = "http://versavault.com";
            url += "/get_amazon_bucket_id?username=" + TxtUsername.Text.Trim() + "&password=" + TxtPassword.Text.Trim();
            BtnAuthenticate.Enabled = false;
            BtnAuthenticate.Text = "Please Wait ...";
            startSyncToolStripMenuItem.Text = "Start sync";
            Utilities.MyConfig.BucketKey = string.Empty;
            Utilities.MyConfig.Username = TxtUsername.Text.Trim();
            Utilities.MyConfig.Password = TxtPassword.Text.Trim();
            Utilities.MyConfig.Save();
            _syncIsPaused = true;
            string result = GetResponse(url).ToString();
            if (result != null && result != string.Empty)
            {
                var res = JsonConvert.DeserializeObject<account>(result);
                if (res.bucket_id != null)
                {
                    Utilities.MyConfig.BucketKey = res.bucket_id;
                    Utilities.MyConfig.Save();
                    startSync();
                    return;
                }
                else
                {
                    LblError.Text = res.error;
                }
            }
            BtnAuthenticate.Enabled = true;
            BtnAuthenticate.Text = "Authenticate";
            PnlAuthentication.Visible = true;
            stopSync();
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

                service.AddObjectProgress += new EventHandler<S3ProgressEventArgs>(service_AddObjectProgress);
                service.GetObjectProgress += new EventHandler<S3ProgressEventArgs>(service_GetObjectProgress);

                download_objects = new List<string>();

                if (!Directory.Exists(Utilities.Path))
                    Directory.CreateDirectory(Utilities.Path);

                if (!Directory.Exists(Utilities.AppPath))
                    Directory.CreateDirectory(Utilities.AppPath);

                TxtUsername.Text = Utilities.MyConfig.Username;
                TxtPassword.Text = Utilities.MyConfig.Password;

                if (!string.IsNullOrEmpty(TxtUsername.Text) || !string.IsNullOrEmpty(TxtPassword.Text.Trim()))
                {
                    getBucketId();
                    if (string.IsNullOrEmpty(Utilities.MyConfig.BucketKey))
                        stopSync();
                    else
                        startSync();
                }
                else
                {
                    this.Visible = true;
                    this.Opacity = 1;
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        private void startSync()
        {
            _syncIsPaused = false;
            LblError.Text = "Authenticated successfully.";
            BtnAuthenticate.Enabled = true;
            BtnAuthenticate.Text = "Authenticate";
            StartActivityMonitoring(Utilities.Path);
            startSyncToolStripMenuItem.Text = "Pause Sync";
            this.Visible = false;
            this.Opacity = 0;
        }

        private void stopSync()
        {
            _syncIsPaused = true;
            startSyncToolStripMenuItem.Text = "Start Sync";
            this.Visible = true;
            this.Opacity = 1;
        }

        private void StartActivityMonitoring(string sPath)
        {
            if (!_isStartedMonitoring)
            {
                _watchFolder.Path = sPath;
                _watchFolder.IncludeSubdirectories = true;

                _watchFolder.NotifyFilter = NotifyFilters.DirectoryName;
                _watchFolder.NotifyFilter = _watchFolder.NotifyFilter | NotifyFilters.FileName;
                //_watchFolder.NotifyFilter = _watchFolder.NotifyFilter | NotifyFilters.Attributes;
                _watchFolder.NotifyFilter = _watchFolder.NotifyFilter | NotifyFilters.Size;
                //_watchFolder.NotifyFilter = _watchFolder.NotifyFilter | NotifyFilters.LastWrite;

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
                _isStartedMonitoring = true;
                var thread = new Thread(StartAmazonFilesSync);
                thread.Start();
            }
        }

        void _watchFolder_Error(object sender, ErrorEventArgs e)
        {
        }

        private void VersaVault_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Opacity = 0;
            if (string.IsNullOrEmpty(Utilities.MyConfig.BucketKey))
                startSyncToolStripMenuItem.Text = "Start sync";
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
            this.Opacity = 1;
            this.Visible = true;
            Application.DoEvents();
        }

        private void startSyncToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Utilities.MyConfig.BucketKey))
            {
                PnlAuthentication.Visible = true;
                this.Visible = true;
                this.Opacity = 1;
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

        private void showBalloon(string text, string title, ToolTipIcon icon)
        {
            VersaVaultNotifications.ShowBalloonTip(2000, title, text, icon);
        }

        private bool IsFileUsedbyAnotherProcess(string filename)
        {
            // Its not a way elegant way, need to find out the better  code
            try
            {
                using (FileStream filestream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    filestream.Close();
                    filestream.Dispose();
                }
            }
            catch (System.IO.IOException exp)
            {
                return true;
            }
            return false;
        }

        private void Uploadfiles(string folder_path)
        {
            foreach (string file_path in Directory.GetFiles(folder_path))
            {
                Addobject(file_path);
            }
            foreach (string path in Directory.GetDirectories(folder_path))
            {
                Createfolder(path);
                Uploadfiles(path);
            }
        }

        private void Modifyfolder(string new_path, string old_path)
        {
            Createfolder(new_path);
            foreach (string filename in Directory.GetFiles(new_path))
            {
                Moveobject(filename, old_path);
            }
            foreach (string directory_name in Directory.GetDirectories(new_path))
            {
                Modifyfolder(directory_name, old_path + "\\" + Path.GetFileName(directory_name));
                Removefolder(old_path + "\\" + Path.GetFileName(directory_name));
            }
            Removefolder(old_path);
        }

        private void Createfolder(string path)
        {
            try
            {
                string relativePath = path.Replace(Utilities.Path + "\\", "");
                var addobjectrequest = new AddObjectRequest(service, Utilities.MyConfig.BucketKey, relativePath + "/");
                addobjectrequest.ContentLength = 0;
                var resutl = addobjectrequest.GetResponse();
            }
            catch (Exception) { }
        }

        private void Removefolder(string path)
        {
            // If the user deleted a folder, it will be an exception
            string oldrelativePath = path.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
            try
            {
                var removeobject = new LitS3.DeleteObjectRequest(service, Utilities.MyConfig.BucketKey, oldrelativePath + "/");
                var result = removeobject.GetResponse();
            }
            catch (Exception) { }
        }

        private void Moveobject(string new_path, string old_path)
        {
            try
            {
                if (!IsFileUsedbyAnotherProcess(new_path))
                {
                    string newrelativePath = new_path.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                    string oldrelativePath = old_path.Replace(Utilities.Path + "\\", "").Replace("\\", "/");  // +"/" + Path.GetFileName(new_path);
                    if (service.ObjectExists(Utilities.MyConfig.BucketKey, oldrelativePath))
                    {
                        LitS3.CopyObjectRequest request = new CopyObjectRequest(service, Utilities.MyConfig.BucketKey, oldrelativePath, newrelativePath);
                        LitS3.CopyObjectResponse response = request.GetResponse();
                        if (response.Error == null)
                        {
                            service.DeleteObject(Utilities.MyConfig.BucketKey, oldrelativePath);
                        }
                    }
                    else
                    {
                        if (File.Exists(new_path))
                            Addobject(new_path);
                    }
                }
            }
            catch (Exception) { }
        }

        private void Addobject(string file_path)
        {
            try
            {
                var attribute = System.IO.File.GetAttributes(file_path);
                string relativePath = file_path.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                if (attribute != FileAttributes.Directory && attribute != FileAttributes.Temporary)
                {
                    if (File.Exists(file_path) && !IsFileUsedbyAnotherProcess(file_path))
                    {
                        service.AddObject(file_path, Utilities.MyConfig.BucketKey, relativePath);
                        update_application(relativePath);
                    }
                }
            }
            catch (Exception) { }
        }

        private void service_AddObjectProgress(object sender, S3ProgressEventArgs e)
        {
            modify_sync_status("Uploaded", e.BytesTransferred, e.BytesTotal, e.Key);
        }

        void service_GetObjectProgress(object sender, S3ProgressEventArgs e)
        {
            modify_sync_status("Downloaded", e.BytesTransferred, e.BytesTotal, e.Key);
        }

        private void modify_sync_status(string status, long BytesTransferred, long BytesTotal, string key)
        {
            if (BytesTransferred != BytesTotal)
                SyncStatus = status + " " + Math.Round(Convert.ToDouble(BytesTransferred / 1048576), 2) + "MB out of  " + Math.Round(Convert.ToDouble(BytesTotal / 1048576), 2) + "MB - " + key;
            else
                SyncStatus = status + " " + key;
            Application.DoEvents();
        }

        private void Removeobject(string file_path)
        {
            // Dont know it is folder or file, so try with the file first
            try
            {
                string relativePath = file_path.Replace(Utilities.Path + "\\", "");
                //if (service.ObjectExists(Utilities.MyConfig.BucketKey, relativePath))
                service.DeleteObject(Utilities.MyConfig.BucketKey, relativePath);
            }
            catch (Exception) { }
        }

        private void Deletefolder(string relative_path)
        {
            // Iteration through all the files in the folder
            try
            {
                foreach (var result_entry in service.ListAllObjects(Utilities.MyConfig.BucketKey, relative_path))
                {
                    ObjectEntry entry = null;
                    try
                    {
                        entry = (ObjectEntry)result_entry;
                    }
                    catch (Exception)
                    {
                        try
                        {
                            var prefix = (CommonPrefix)result_entry;
                            Deletefolder(prefix.Prefix);
                        }
                        catch (Exception) { }
                    }
                    if (entry != null && !string.IsNullOrEmpty(entry.Key))
                    {
                        try
                        {
                            var removeobject = new LitS3.DeleteObjectRequest(service, Utilities.MyConfig.BucketKey, entry.Key);
                            var result = removeobject.GetResponse();
                        }
                        catch (Exception)
                        {
                            try
                            {
                                var removeobject = new LitS3.DeleteObjectRequest(service, Utilities.MyConfig.BucketKey, entry.Name);
                                var result = removeobject.GetResponse();
                            }
                            catch (Exception) { }
                        }
                    }
                }
            }
            catch (Exception) { }

            // If the user deleted a folder, it will be an exception
            try
            {
                var removeobject = new LitS3.DeleteObjectRequest(service, Utilities.MyConfig.BucketKey, relative_path);
                var result = removeobject.GetResponse();
            }
            catch (Exception) { }
        }

        private void TxtPassword_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
                getBucketId();
        }

        private void EventRaised(object sender, FileSystemEventArgs e)
        {
            if (!_syncIsPaused)
            {
                _syncIsPaused = true;
                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Changed: // some how we have to avoid unncessary uploads
                        {
                            try
                            {
                                if (!download_objects.Contains(e.FullPath))
                                {
                                    var attribute = System.IO.File.GetAttributes(e.FullPath);
                                    if (attribute != FileAttributes.Directory && attribute != FileAttributes.Temporary)
                                    {
                                        showBalloon("Uploading file content......", "File Update.", ToolTipIcon.Info);
                                        Addobject(e.FullPath);
                                    }
                                }
                            }
                            catch (Exception) { }
                            break;
                        }
                    case WatcherChangeTypes.Created:
                        {
                            try
                            {
                                if (!download_objects.Contains(e.FullPath))
                                {
                                    var attribute = System.IO.File.GetAttributes(e.FullPath);
                                    if (attribute != FileAttributes.Directory && attribute != FileAttributes.Temporary)
                                    {
                                        showBalloon("Uploading file content......", "New file.", ToolTipIcon.Info);
                                        Addobject(e.FullPath);
                                    }
                                    else
                                    {
                                        // Creating an Folder
                                        showBalloon("Creating a folder......", "New folder.", ToolTipIcon.Info);
                                        Createfolder(e.FullPath);
                                        // If it is restored from the recylebin / pasted from another location then upload folder structure completely
                                        Uploadfiles(e.FullPath);
                                    }
                                }
                            }
                            catch (Exception) { }
                            break;
                        }
                    case WatcherChangeTypes.Deleted:
                        {
                            string relativePath = e.FullPath.Replace(Utilities.Path + "\\", "");
                            try
                            {
                                var attribute = System.IO.File.GetAttributes(e.FullPath);
                                if (attribute != FileAttributes.Directory && attribute != FileAttributes.Temporary)
                                {
                                    showBalloon("Deleting a file......", "File delete.", ToolTipIcon.Info);
                                    service.DeleteObject(Utilities.MyConfig.BucketKey, relativePath);
                                }
                            }
                            catch (Exception)
                            {
                                showBalloon("Deleting file/folder......", "Delete.", ToolTipIcon.Info);
                                Removeobject(e.FullPath);
                                Deletefolder(relativePath + "/");
                            }
                            break;
                        }
                    case WatcherChangeTypes.Renamed:
                        {
                            try
                            {
                                var attribute = System.IO.File.GetAttributes(e.FullPath);
                                var renamedEvent = (RenamedEventArgs)e;
                                if (attribute != FileAttributes.Directory && attribute != FileAttributes.Temporary)
                                {
                                    showBalloon("Renaming file name...", "File name modified.", ToolTipIcon.Info);
                                    Moveobject(renamedEvent.FullPath, renamedEvent.OldFullPath);
                                }
                                else
                                {
                                    showBalloon("Renaming folder name...", "Folder name modified.", ToolTipIcon.Info);
                                    Modifyfolder(renamedEvent.FullPath, renamedEvent.OldFullPath);
                                }
                            }
                            catch (Exception) { }
                            break;
                        }
                }
                _syncIsPaused = false;
            }
            else
            {
                // To do
                // Need to maintain queue
            }
        }

        private void StartAmazonFilesSync()
        {
            try
            {
                string url = string.Empty;
                if (Utilities.DevelopmentMode)
                    url = "http://localhost:3000";
                else
                    url = "http://versavault.com";
                url += "/api/root_files?bucket_key=" + Utilities.MyConfig.BucketKey;
                string result = GetResponse(url).ToString();
                if (result != null && result != string.Empty)
                {
                    var res = JsonConvert.DeserializeObject<s3__object[]>(result);
                    if (res != null)
                    {
                        foreach (var root_object in res)
                        {
                            var s3_obj = root_object.s3_object;
                            if (s3_obj != null)
                            {
                                string full_path = Path.Combine(Utilities.Path, s3_obj.fileName);
                                if (s3_obj.folder)
                                {
                                    if (!Directory.Exists(full_path))
                                    {
                                        download_folder(s3_obj);
                                    }
                                    else
                                    {
                                        // need to check the modified time here
                                    }
                                }
                                else
                                {
                                    if (!File.Exists(full_path))
                                    {
                                        download_objects.Add(full_path);
                                        service.GetObject(Utilities.MyConfig.BucketKey, s3_obj.key, full_path);
                                    }
                                    else
                                    {
                                        // need to check the modified time here
                                    }
                                }
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
            }
        }

        private void download_folder(s3object s3_obj)
        {
            string full_path = Path.Combine(Utilities.Path, s3_obj.key.Replace("/", "\\"));
            // need to import complete directory
            string url;
            download_objects.Add(full_path);
            Directory.CreateDirectory(full_path);
            // get the folder structure from application database
            if (Utilities.DevelopmentMode)
                url = "http://localhost:3000";
            else
                url = "http://versavault.com";
            url += "/api/child_files?bucket_key=" + Utilities.MyConfig.BucketKey + "&parent_uid=" + s3_obj.uid;
            string result = GetResponse(url).ToString();
            if (result != null && result != string.Empty)
            {
                var res = JsonConvert.DeserializeObject<s3__object[]>(result);
                foreach (var root_object in res)
                {
                    var s3_obj_sub = root_object.s3_object;
                    if (s3_obj_sub != null)
                    {
                        string full_path_sub = Path.Combine(Utilities.Path, s3_obj_sub.key.Replace("/", "\\"));
                        if (s3_obj_sub.folder)
                        {
                            if (!Directory.Exists(full_path_sub))
                            {
                                download_folder(s3_obj_sub);
                            }
                            else
                            {
                                // need to check the modified time here
                            }
                        }
                        else
                        {
                            if (!File.Exists(full_path_sub))
                            {
                                download_objects.Add(full_path_sub);
                                service.GetObject(Utilities.MyConfig.BucketKey, s3_obj_sub.key, full_path_sub);
                            }
                            else
                            {
                                // need to check the modified time here
                            }
                        }
                    }
                }
            }
        }

        private void update_application(string key)
        {
            try
            {
                string url = string.Empty;
                if (Utilities.DevelopmentMode)
                    url = "http://localhost:3000";
                else
                    url = "http://versavault.com";
                url += "/api/update_files?bucket_key=" + Utilities.MyConfig.BucketKey + "&key=" + key;
                string result = GetResponse(url).ToString();
                if (string.IsNullOrEmpty(result))
                {
                }
            }
            catch (Exception) { }
        }

        private void statusToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void timer_status_update_Tick(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(SyncStatus))
                VersaVaultNotifications.ShowBalloonTip(100, string.Empty, SyncStatus, ToolTipIcon.Info);
        }
    }
}