using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using LitS3;
using Newtonsoft.Json;
using CopyObjectRequest = LitS3.CopyObjectRequest;
using DeleteObjectRequest = LitS3.DeleteObjectRequest;

namespace VersaVaultSyncTool
{
    public partial class VersaVault : Form
    {
        readonly DropShadow _ds = new DropShadow();

        S3Service _service;

        private AmazonS3 _amazons3;

        private readonly Queue _syncStatus = new Queue();

        //private readonly Queue _applicationUpates = new Queue();

        private List<string> _processingFiles = new List<string>();

        private readonly List<string> _syncStatusDictionary = new List<string>();

        private bool _isThreadStarted;

        private readonly Hashtable _fileQueue = new Hashtable();

        private bool _syncIsPaused;

        private bool _isStartedMonitoring;

        private List<string> _downloadObjects;

        public VersaVault()
        {
            InitializeComponent();
            Shown += VersaVaultShown;
            Resize += VersaVaultResize;
            LocationChanged += VersaVaultResize;
        }

        private void VersaVaultShown(object sender, EventArgs e)
        {
            Rectangle rc = Bounds;
            rc.Inflate(10, 10);
            _ds.Bounds = rc;
            BringToFront();
        }

        private void VersaVaultResize(object sender, EventArgs e)
        {
            //_ds.Visible = (WindowState == FormWindowState.Normal);
            _ds.Visible = Visible;
            if (_ds.Visible)
            {
                Rectangle rc = Bounds;
                rc.Inflate(10, 10);
                _ds.Bounds = rc;
            }
            BringToFront();
        }

        private void BtnAuthenticateClick(object sender, EventArgs e)
        {
            GetBucketId();
        }

        private void GetBucketId()
        {
            HideForm();
            string url = Utilities.DevelopmentMode ? "http://localhost:3000" : "http://versavault.com";
            url += "/api/get_amazon_bucket_id?username=" + TxtUsername.Text.Trim() + "&password=" + TxtPassword.Text.Trim();
            startSyncToolStripMenuItem.Text = "Start sync";
            Utilities.MyConfig.BucketKey = string.Empty;
            Utilities.MyConfig.Username = TxtUsername.Text.Trim();
            Utilities.MyConfig.Password = TxtPassword.Text.Trim();
            Utilities.MyConfig.Save();
            _syncIsPaused = true;
            string result = GetResponse(url);
            if (!string.IsNullOrEmpty(result))
            {
                var res = JsonConvert.DeserializeObject<Account>(result);
                if (res.BucketId != null)
                {
                    Utilities.MyConfig.BucketKey = res.BucketId;
                    if (string.IsNullOrEmpty(Utilities.MyConfig.MachineKey))
                        Utilities.MyConfig.MachineKey = Guid.NewGuid().ToString();
                    Utilities.MyConfig.Save();
                    StartSync();
                    return;
                }
                LblError.Text = res.Error;
            }
            else
            {
                LblError.Text = "Unable to connect to VersaVault." + Environment.NewLine + "Check your internet connection and try again.";
            }
            StopSync();
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
                var policy = new HttpRequestCachePolicy(HttpRequestCacheLevel.BypassCache);
                objRequest.CachePolicy = policy;
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

        private void VersaVaultLoad(object sender, EventArgs e)
        {
            try
            {
                CheckForIllegalCrossThreadCalls = false;
                _service = new S3Service
                               {
                                   AccessKeyID = Utilities.AwsAccessKey,
                                   SecretAccessKey = Utilities.AwsSecretKey,
                                   UseSubdomains = true
                               };

                _amazons3 = AWSClientFactory.CreateAmazonS3Client(Utilities.AwsAccessKey, Utilities.AwsSecretKey, new AmazonS3Config() { CommunicationProtocol = Protocol.HTTP });

                _service.AddObjectProgress += ServiceAddObjectProgress;
                _service.GetObjectProgress += ServiceGetObjectProgress;

                _downloadObjects = new List<string>();

                if (!Directory.Exists(Utilities.Path))
                    Directory.CreateDirectory(Utilities.Path);

                if (!Directory.Exists(Utilities.AppPath))
                    Directory.CreateDirectory(Utilities.AppPath);

                TxtUsername.Text = Utilities.MyConfig.Username;
                TxtPassword.Text = Utilities.MyConfig.Password;

                if (!string.IsNullOrEmpty(TxtUsername.Text) || !string.IsNullOrEmpty(TxtPassword.Text.Trim()))
                {
                    GetBucketId();
                    if (string.IsNullOrEmpty(Utilities.MyConfig.BucketKey))
                        StopSync();
                    else
                        StartSync();
                }
                else
                    ShowForm();
            }
            catch (Exception)
            {
                return;
            }
        }

        private void StartSync()
        {
            EnableVersioning();
            _syncIsPaused = false;
            LblError.Text = "Authenticated successfully.";
            ShowBalloon("Started versavault synchronization process");
            StartActivityMonitoring(Utilities.Path);
            startSyncToolStripMenuItem.Text = "Pause Sync";
            Visible = false;
            Opacity = 0;
        }

        private void EnableVersioning()
        {
            try
            {
                var setBucketVersioningRequest = new SetBucketVersioningRequest { BucketName = Utilities.MyConfig.BucketKey };
                var versionConfig = new S3BucketVersioningConfig() { Status = "Enabled" };
                setBucketVersioningRequest.VersioningConfig = versionConfig;
                var response = _amazons3.SetBucketVersioning(setBucketVersioningRequest);
                if (string.IsNullOrEmpty(response.AmazonId2))
                {
                }
            }
            catch (Exception)
            {
                // ToDo
                // need to handle log here
            }
        }

        private void StopSync()
        {
            _syncIsPaused = true;
            startSyncToolStripMenuItem.Text = "Start Sync";
            ShowForm();
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
                _watchFolder.Error += WatchFolderError;
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

        private static void WatchFolderError(object sender, ErrorEventArgs e)
        {
        }

        private void VersaVaultFormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Opacity = 0;
            if (string.IsNullOrEmpty(Utilities.MyConfig.BucketKey))
                startSyncToolStripMenuItem.Text = "Start sync";
            Application.DoEvents();
        }

        private void VersaVaultFormClosed(object sender, FormClosedEventArgs e)
        {
        }

        private void StartToolStripMenuItemClick(object sender, EventArgs e)
        {
            Utilities.Startup(startToolStripMenuItem.CheckState == CheckState.Checked, Application.ExecutablePath);
        }

        private void SettingsToolStripMenuItemClick(object sender, EventArgs e)
        {
            ShowForm();
        }

        private void StartSyncToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Utilities.MyConfig.BucketKey))
                ShowForm();
        }

        private static void VersaVaultNotificationsClick(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Utilities.Path);
        }

        private void VersaVaultNotificationsDoubleClick(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Utilities.Path);
        }

        private void ExitToolStripMenuItemClick(object sender, EventArgs e)
        {
            try
            {
                Close();
                Dispose();
                VersaVaultNotifications.Dispose();
            }
            catch (Exception)
            {
                return;
            }
        }

        private void ShowBalloon(string text)
        {
            _syncStatus.Enqueue(text);
        }

        private static bool IsFileUsedbyAnotherProcess(string filename)
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
            catch (IOException)
            {
                return true;
            }
            return false;
        }

        private void Uploadfiles(string folderPath)
        {
            Createfolder(folderPath);
            foreach (string filePath in Directory.GetFiles(folderPath))
                Addobject(filePath);
            foreach (string path in Directory.GetDirectories(folderPath))
                Uploadfiles(path);
        }

        private void Modifyfolder(string newPath, string oldPath)
        {
            Createfolder(newPath);
            foreach (string filename in Directory.GetFiles(newPath))
            {
                Moveobject(filename, oldPath);
            }
            foreach (string directoryName in Directory.GetDirectories(newPath))
            {
                Modifyfolder(directoryName, oldPath + "\\" + new DirectoryInfo(directoryName).Name);
                Removefolder(oldPath + "\\" + new DirectoryInfo(directoryName).Name);
            }
            Removefolder(oldPath);
        }

        private void Createfolder(string path)
        {
            try
            {
                string relativePath = path.Replace(Utilities.Path + "\\", "");
                var addobjectrequest = new AddObjectRequest(_service, Utilities.MyConfig.BucketKey, relativePath + "/") { ContentLength = 0 };
                addobjectrequest.GetResponse();
                ShowBalloon("Creating folder - " + relativePath);
                //_applicationUpates.Enqueue(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new DirectoryInfo(path).LastWriteTime, Status = UpdateStatus.Update });
                ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new DirectoryInfo(path).LastWriteTime, Status = UpdateStatus.Update });
            }
            catch (Exception)
            {
                return;
            }
        }

        private void Removefolder(string path)
        {
            try
            {
                // If the user deleted a folder, it will be an exception
                string oldrelativePath = path.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                var removeobject = new DeleteObjectRequest(_service, Utilities.MyConfig.BucketKey, oldrelativePath + "/");
                removeobject.GetResponse();
                //_applicationUpates.Enqueue(new AppUpdateInfo { Key = oldrelativePath.Replace("\\", "/"), LastModifiedTime = DateTime.Now, Status = UpdateStatus.Delete });
                ProcessApplicationUpdates(new AppUpdateInfo { Key = oldrelativePath.Replace("\\", "/"), LastModifiedTime = DateTime.Now, Status = UpdateStatus.Delete });
            }
            catch (Exception) { }
        }

        private void Moveobject(string newPath, string oldPath)
        {
            try
            {
                if (!IsFileUsedbyAnotherProcess(newPath))
                {
                    string newrelativePath = newPath.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                    string oldrelativePath = oldPath.Replace(Utilities.Path + "\\", "").Replace("\\", "/");  // +"/" + Path.GetFileName(new_path);
                    if (_service.ObjectExists(Utilities.MyConfig.BucketKey, oldrelativePath))
                    {
                        var request = new CopyObjectRequest(_service, Utilities.MyConfig.BucketKey, oldrelativePath, newrelativePath);
                        var response = request.GetResponse();
                        if (response.Error == null)
                        {
                            _service.DeleteObject(Utilities.MyConfig.BucketKey, oldrelativePath);
                        }
                        ProcessApplicationUpdates(new AppUpdateInfo { Key = oldrelativePath.Replace("\\", "/"), LastModifiedTime = DateTime.Now, Status = UpdateStatus.Delete });
                        //_applicationUpates.Enqueue(new AppUpdateInfo { Key = oldrelativePath.Replace("\\", "/"), LastModifiedTime = DateTime.Now, Status = UpdateStatus.Delete });
                        ProcessApplicationUpdates(new AppUpdateInfo { Key = newrelativePath.Replace("\\", "/"), LastModifiedTime = DateTime.Now, Status = UpdateStatus.Update });
                        //_applicationUpates.Enqueue(new AppUpdateInfo { Key = newrelativePath.Replace("\\", "/"), LastModifiedTime = DateTime.Now, Status = UpdateStatus.Update });
                    }
                    else
                    {
                        if (File.Exists(newPath))
                            Addobject(newPath);
                    }
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        private void Addobject(string filePath)
        {
            try
            {
                var attribute = File.GetAttributes(filePath);
                string relativePath = filePath.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                if (attribute != FileAttributes.Directory && attribute != (FileAttributes.Archive | FileAttributes.Hidden))
                {
                    if (File.Exists(filePath) && !IsFileUsedbyAnotherProcess(filePath))
                    {
                        var appUpdateInfo = new AppUpdateInfo
                                                {
                                                    Key = relativePath.Replace("\\", "/"),
                                                    LastModifiedTime = new FileInfo(filePath).LastWriteTime,
                                                    Status = UpdateStatus.Update
                                                };
                        //if (!_applicationUpates.Contains(appUpdateInfo))
                        //{
                        //_applicationUpates.Enqueue(appUpdateInfo);
                        _processingFiles.Add(filePath);
                        _service.AddObject(filePath, Utilities.MyConfig.BucketKey, relativePath);
                        SetAcltoObject(relativePath);
                        ProcessApplicationUpdates(appUpdateInfo);
                        _processingFiles.Remove(filePath);
                        //}
                        //else
                        // {
                        //}
                    }
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        private void SetAcltoObject(string key)
        {
            try
            {
                var versionsRequest = new ListVersionsRequest
                                          {
                                              BucketName = Utilities.MyConfig.BucketKey,
                                              Prefix = key
                                          };
                var result = _amazons3.ListVersions(versionsRequest);
                foreach (S3ObjectVersion s3ObjectVersion in result.Versions)
                {
                    if (!s3ObjectVersion.IsDeleteMarker)
                    {
                        try
                        {
                            // Get ACL.
                            var getRequest = new GetACLRequest { BucketName = Utilities.MyConfig.BucketKey, Key = key, VersionId = s3ObjectVersion.VersionId };
                            GetACLResponse getResponse = _amazons3.GetACL(getRequest);
                            if (getResponse.AccessControlList.Grants.Count < 2)
                            {
                                S3AccessControlList acl = getResponse.AccessControlList;
                                getResponse.Dispose();
                                //acl.Grants.Clear();
                                //var grantee0 = new S3Grantee();
                                //grantee0.WithCanonicalUser(acl.Owner.Id, acl.Owner.DisplayName);
                                //acl.AddGrant(grantee0, S3Permission.FULL_CONTROL);
                                var grantee1 = new S3Grantee();
                                grantee1.WithURI("http://acs.amazonaws.com/groups/global/AllUsers");
                                acl.AddGrant(grantee1, S3Permission.READ);
                                var request = new SetACLRequest
                                                  {
                                                      BucketName = Utilities.MyConfig.BucketKey,
                                                      ACL = acl,
                                                      Key = key,
                                                      VersionId = s3ObjectVersion.VersionId
                                                  };
                                SetACLResponse response = _amazons3.SetACL(request);
                                response.Dispose();
                            }
                        }
                        catch (Exception)
                        {
                            // Todo
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void ServiceAddObjectProgress(object sender, S3ProgressEventArgs e)
        {
            ModifySyncStatus("Uploaded", e.BytesTransferred, e.BytesTotal, e.Key);
        }

        private void ServiceGetObjectProgress(object sender, S3ProgressEventArgs e)
        {
            ModifySyncStatus("Downloaded", e.BytesTransferred, e.BytesTotal, e.Key);
        }

        private void Removeobject(string filePath)
        {
            // Dont know it is folder or file, so try with the file first
            try
            {
                string relativePath = filePath.Replace(Utilities.Path + "\\", "");
                if (_service.ObjectExists(Utilities.MyConfig.BucketKey, relativePath))
                {
                    _service.DeleteObject(Utilities.MyConfig.BucketKey, relativePath);
                    ProcessApplicationUpdates(new AppUpdateInfo
                    {
                        Key = relativePath.Replace("\\", "/"),
                        LastModifiedTime = DateTime.Now,
                        Status = UpdateStatus.Delete
                    });
                    //_applicationUpates.Enqueue(new AppUpdateInfo{Key = relativePath.Replace("\\", "/"),LastModifiedTime = DateTime.Now,Status = UpdateStatus.Delete});
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        private void Deletefolder(string relativePath)
        {
            // Iteration through all the files in the folder
            try
            {
                foreach (var resultEntry in _service.ListAllObjects(Utilities.MyConfig.BucketKey, relativePath))
                {
                    ObjectEntry entry = null;
                    try
                    {
                        entry = (ObjectEntry)resultEntry;
                    }
                    catch (Exception)
                    {
                        try
                        {
                            var prefix = (CommonPrefix)resultEntry;
                            Deletefolder(prefix.Prefix);
                        }
                        catch (Exception) { }
                    }
                    if (entry != null && !string.IsNullOrEmpty(entry.Key))
                    {
                        try
                        {
                            var removeobject = new DeleteObjectRequest(_service, Utilities.MyConfig.BucketKey, entry.Key);
                            removeobject.GetResponse();
                        }
                        catch (Exception)
                        {
                            try
                            {
                                var removeobject = new DeleteObjectRequest(_service, Utilities.MyConfig.BucketKey, entry.Name);
                                removeobject.GetResponse();
                            }
                            catch (Exception) { }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Todo
                // need to hanlde something when error raises
            }
            finally
            {
                // deleting entire folder
                ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = DateTime.Now, Status = UpdateStatus.Delete });
                //_applicationUpates.Enqueue(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = DateTime.Now, Status = UpdateStatus.Delete });
            }

            // If the user deleted a folder, it will be an exception
            try
            {
                var attribute = File.GetAttributes(Path.Combine(Utilities.Path, relativePath));
                var removeobject = new DeleteObjectRequest(_service, Utilities.MyConfig.BucketKey, relativePath);
                removeobject.GetResponse();
            }
            catch (Exception)
            {
                return;
            }
        }

        private void TxtPasswordKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
                GetBucketId();
        }

        private void EventRaised(object sender, FileSystemEventArgs e)
        {
            if (!_syncIsPaused)
            {
                _syncIsPaused = true;
                DoObjectSync(e.ChangeType, e.FullPath, e.Name, string.Empty, string.Empty, e);
                _syncIsPaused = false;
            }
            else
            {
                // Need to maintain queue
                if (!_fileQueue.ContainsKey(e.FullPath))
                    _fileQueue.Add(e.FullPath, new FileQueue { Type = e.ChangeType, Name = e.Name });
                else
                    _fileQueue[e.FullPath] = new FileQueue { Type = e.ChangeType, Name = e.Name };
            }
        }

        private void StartAmazonFilesSync()
        {
            try
            {
                string url = Utilities.DevelopmentMode ? "http://localhost:3000" : "http://versavault.com";
                url += "/api/root_files?bucket_key=" + Utilities.MyConfig.BucketKey + "&machine_key=" + Utilities.MyConfig.MachineKey;
                string result = GetResponse(url);
                if (!string.IsNullOrEmpty(result))
                {
                    var res = JsonConvert.DeserializeObject<s3_object>(result);
                    if (res != null)
                    {
                        foreach (var s3Obj in res.S3Object)
                        {
                            if (s3Obj != null)
                            {
                                string fullPath = Path.Combine(Utilities.Path, s3Obj.Key.Replace("/", "\\"));
                                if (s3Obj.Folder)
                                {
                                    if (s3Obj.Status)
                                    {
                                        if (!Directory.Exists(fullPath))
                                        {
                                            DownloadFolder(s3Obj);
                                        }
                                        else
                                        {
                                            try
                                            {
                                                TimeSpan timeSpan = new DirectoryInfo(fullPath).LastWriteTime.Subtract(s3Obj.LastModified);
                                                if (Math.Floor(timeSpan.TotalSeconds) != 0)
                                                {
                                                    string relativePath = fullPath.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                                                    ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new DirectoryInfo(fullPath).LastWriteTime, Status = UpdateStatus.Update });
                                                    //_applicationUpates.Enqueue(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new DirectoryInfo(fullPath).LastWriteTime, Status = UpdateStatus.Update });
                                                    DownloadFolder(s3Obj);
                                                }
                                            }
                                            catch (Exception)
                                            {
                                                // Todo
                                                // need to fix this when an error occured
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // Need to delete the folder
                                        try
                                        {
                                            // need to check if the directory is updated in your machine
                                            if (Directory.Exists(fullPath))
                                            {
                                                TimeSpan timeSpan = new DirectoryInfo(fullPath).LastWriteTime.Subtract(s3Obj.LastModified);
                                                if (Math.Floor(timeSpan.TotalSeconds) < 1)
                                                    Directory.Delete(fullPath, true);
                                            }
                                        }
                                        catch (Exception)
                                        {
                                            // Todo
                                            // how to handle when it is used by some other process
                                        }
                                    }
                                }
                                else
                                {
                                    if (s3Obj.Status)
                                    {
                                        if (!File.Exists(fullPath))
                                        {
                                            try
                                            {
                                                _downloadObjects.Add(fullPath);
                                                _service.GetObject(Utilities.MyConfig.BucketKey, s3Obj.Key, fullPath);
                                                string relativePath = fullPath.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                                                ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPath).LastWriteTime, Status = UpdateStatus.Update });
                                                //_applicationUpates.Enqueue(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPath).LastWriteTime, Status = UpdateStatus.Update });
                                            }
                                            catch (Exception)
                                            {
                                                // Todo
                                                // need to handle if an error occured when downloading file
                                            }
                                        }
                                        else
                                        {
                                            try
                                            {
                                                if (!IsFileUsedbyAnotherProcess(fullPath))
                                                {
                                                    TimeSpan timeSpan = new FileInfo(fullPath).LastWriteTime.Subtract(s3Obj.LastModified);
                                                    if (Math.Floor(timeSpan.TotalSeconds) != 0)
                                                    {
                                                        _downloadObjects.Add(fullPath);
                                                        _service.GetObject(Utilities.MyConfig.BucketKey, s3Obj.Key, fullPath);
                                                        string relativePath = fullPath.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                                                        ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPath).LastWriteTime, Status = UpdateStatus.Update });
                                                        //_applicationUpates.Enqueue(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPath).LastWriteTime, Status = UpdateStatus.Update });
                                                    }
                                                }
                                            }
                                            catch (Exception)
                                            {
                                                // Todo
                                                // need to fix this when an error occured
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // need to delete the file here
                                        try
                                        {
                                            if (File.Exists(fullPath))
                                            {
                                                TimeSpan timeSpan = new FileInfo(fullPath).LastWriteTime.Subtract(s3Obj.LastModified);
                                                if (Math.Floor(timeSpan.TotalSeconds) < 1)
                                                    File.Delete(fullPath);
                                                else
                                                    Addobject(fullPath);
                                            }
                                        }
                                        catch (Exception)
                                        {
                                            // Todo
                                            // need to fix if the file is used
                                        }
                                    }
                                }
                            }
                        }
                        UploadMissedFiles(res.S3Object, Utilities.Path);
                        _downloadObjects.Clear();
                    }
                    else
                    {
                        var thread = new Thread(StartAmazonFilesSync);
                        thread.Start();
                    }
                }
                else
                {
                    var thread = new Thread(StartAmazonFilesSync);
                    thread.Start();
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        private void UploadMissedFiles(IEnumerable<S3Object> s3Objects, string path)
        {
            // Finally Here we checking whether any files found in this folder to upload
            // files first
            foreach (string file in Directory.GetFiles(path))
            {
                bool isFilefound = false;
                foreach (S3Object s3Object in s3Objects)
                {
                    string filename = file.Replace(Utilities.Path + "\\", "");
                    if (s3Object != null && !s3Object.Folder && s3Object.Key.Replace("/", "\\").ToLower().Trim() == filename.Trim().ToLower())
                    {
                        isFilefound = true;
                        break;
                    }
                }
                if (!isFilefound && new FileInfo(file).Length != 0)
                    Addobject(file);
            }

            // folders
            foreach (string directory in Directory.GetDirectories(path))
            {
                bool isFolderfound = false;
                S3Object directoryObject = null;
                foreach (S3Object s3Object in s3Objects)
                {
                    string key = directory.Replace(Utilities.Path + "\\", "");
                    if (s3Object != null && s3Object.Folder && s3Object.Key.Replace("/", "\\").ToLower().Trim() == key.Trim().ToLower())
                    {
                        directoryObject = s3Object;
                        isFolderfound = true;
                        break;
                    }
                }
                if (!isFolderfound)
                    Uploadfiles(directory);
                else
                {
                    string url = Utilities.DevelopmentMode ? "http://localhost:3000" : "http://versavault.com";
                    url += "/api/child_files?bucket_key=" + Utilities.MyConfig.BucketKey + "&parent_uid=" + directoryObject.Uid + "&machine_key=" + Utilities.MyConfig.MachineKey;
                    string result = GetResponse(url);
                    if (!string.IsNullOrEmpty(result))
                    {
                        var res = JsonConvert.DeserializeObject<s3_object>(result);
                        UploadMissedFiles(res.S3Object, directory);
                    }
                }
            }
        }

        private void DownloadFolder(S3Object s3Obj)
        {
            string fullPath = Path.Combine(Utilities.Path, s3Obj.Key.Replace("/", "\\"));
            try
            {
                // need to import complete directory
                _downloadObjects.Add(fullPath);
                if (!Directory.Exists(fullPath))
                    Directory.CreateDirectory(fullPath);
                // get the folder structure from application database
                string url = Utilities.DevelopmentMode ? "http://localhost:3000" : "http://versavault.com";
                url += "/api/child_files?bucket_key=" + Utilities.MyConfig.BucketKey + "&parent_uid=" + s3Obj.Uid + "&machine_key=" + Utilities.MyConfig.MachineKey;
                string result = GetResponse(url);
                if (!string.IsNullOrEmpty(result))
                {
                    var res = JsonConvert.DeserializeObject<s3_object>(result);
                    foreach (var s3ObjSub in res.S3Object)
                    {
                        if (s3ObjSub != null)
                        {
                            string fullPathSub = Path.Combine(Utilities.Path, s3ObjSub.Key.Replace("/", "\\"));
                            if (s3ObjSub.Folder)
                            {
                                if (!Directory.Exists(fullPathSub))
                                {
                                    DownloadFolder(s3ObjSub);
                                }
                                else
                                {
                                    try
                                    {
                                        TimeSpan timeSpan = new DirectoryInfo(fullPathSub).LastWriteTime.Subtract(s3ObjSub.LastModified);
                                        if (Math.Floor(timeSpan.TotalSeconds) != 0)
                                        {
                                            _downloadObjects.Add(fullPathSub);
                                            DownloadFolder(s3ObjSub);
                                            string relativePath = fullPathSub.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                                            ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPathSub).LastWriteTime, Status = UpdateStatus.Update });
                                            //_applicationUpates.Enqueue(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPathSub).LastWriteTime, Status = UpdateStatus.Update });
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        // Todo
                                        // need to fix this when an error occured
                                    }
                                }
                            }
                            else
                            {
                                if (!File.Exists(fullPathSub))
                                {
                                    _downloadObjects.Add(fullPathSub);
                                    _service.GetObject(Utilities.MyConfig.BucketKey, s3ObjSub.Key, fullPathSub);
                                    string relativePath = fullPathSub.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                                    ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPathSub).LastWriteTime, Status = UpdateStatus.Update });
                                    //_applicationUpates.Enqueue(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPathSub).LastWriteTime, Status = UpdateStatus.Update });
                                }
                                else
                                {
                                    try
                                    {
                                        if (!IsFileUsedbyAnotherProcess(fullPathSub))
                                        {
                                            TimeSpan timeSpan = new FileInfo(fullPathSub).LastWriteTime.Subtract(s3ObjSub.LastModified);
                                            if (Math.Floor(timeSpan.TotalSeconds) != 0)
                                            {
                                                _downloadObjects.Add(fullPathSub);
                                                _service.GetObject(Utilities.MyConfig.BucketKey, s3ObjSub.Key, fullPathSub);
                                                string relativePath = fullPathSub.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                                                ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPathSub).LastWriteTime, Status = UpdateStatus.Update });
                                                //_applicationUpates.Enqueue(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPathSub).LastWriteTime, Status = UpdateStatus.Update });
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        // Todo
                                        // need to fix this when an error occured
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Todo
                // need to figure it out why it is happening
            }
            finally
            {
                ProcessApplicationUpdates(new AppUpdateInfo { Key = s3Obj.Key, LastModifiedTime = new DirectoryInfo(fullPath).LastWriteTime, Status = UpdateStatus.Update });
                //_applicationUpates.Enqueue(new AppUpdateInfo { Key = s3Obj.Key, LastModifiedTime = new DirectoryInfo(fullPath).LastWriteTime, Status = UpdateStatus.Update });
            }
        }

        private void ModifySyncStatus(string status, long bytesTransferred, long bytesTotal, string key)
        {
            if (bytesTransferred != 0)
            {
                string value = string.Empty;
                if (bytesTransferred != bytesTotal)
                {
                    if (bytesTotal >= 1048576)
                    {
                        var completed = Math.Round(Convert.ToDouble(bytesTransferred / 1048576), 2);
                        var total = Math.Round(Convert.ToDouble(bytesTotal / 1048576), 2);
                        var percentage = Math.Round((completed / total) * 100, 0);
                        if (percentage != 0 && percentage % 3 == 0 && !_syncStatusDictionary.Contains(key + "_" + percentage))
                        {
                            _syncStatusDictionary.Add(key + "_" + percentage);
                            value = status + " " + completed + "MB out of  " + total + "MB - " + key;
                        }
                    }
                    else if (bytesTotal >= 1024)
                    {
                        var completed = Math.Round(Convert.ToDouble(bytesTransferred / 1024), 2);
                        var total = Math.Round(Convert.ToDouble(bytesTotal / 1024), 2);
                        var percentage = Math.Round((completed / total) * 100, 0);
                        if (percentage != 0 && percentage % 10 == 0 && !_syncStatusDictionary.Contains(key + "_" + percentage))
                        {
                            _syncStatusDictionary.Add(key + "_" + percentage);
                            value = status + " " + completed + "KB out of  " + total + "KB - " + key;
                        }
                    }
                    else
                    {
                        var percentage = Math.Round(Convert.ToDouble(bytesTransferred / bytesTotal) * 100, 0);
                        if (percentage % 25 == 0 && !_syncStatusDictionary.Contains(key + "_" + percentage))
                        {
                            _syncStatusDictionary.Add(key + "_" + percentage);
                            value = status + " " + bytesTransferred + "Bytes out of  " + bytesTotal + "Bytes - " + key;
                        }
                    }
                }
                else
                {
                    value = status + " " + key;
                    // clear the SysStatusDictionary
                    while (true)
                    {
                        bool iskeyremoved = false;
                        foreach (string keyString in _syncStatusDictionary)
                        {
                            if (keyString.IndexOf(key) == 0)
                            {
                                _syncStatusDictionary.Remove(keyString);
                                iskeyremoved = true;
                                break;
                            }
                        }
                        if (iskeyremoved == false)
                            break;
                    }
                }
                if (!string.IsNullOrEmpty(value) && !_syncStatus.Contains(value))
                    _syncStatus.Enqueue(value);
            }
            Application.DoEvents();
        }

        private void TimerStatusUpdateTick(object sender, EventArgs e)
        {
            if (_syncStatus.Count != 0)
                VersaVaultNotifications.ShowBalloonTip(10, string.Empty, _syncStatus.Dequeue().ToString(), ToolTipIcon.Info);

            if (_fileQueue.Count > 0 && !_isThreadStarted)
            {
                _isThreadStarted = true;
                var thread = new Thread(ProcessFileQueueThead);
                thread.Start();
                Application.DoEvents();
            }

            //if (_applicationUpates.Count > 0)
            //{
            //    var thread = new Thread(ProcessApplicationUpdates);
            //    thread.Start();
            // }

            if (_syncStatus.Count == 0 && _fileQueue.Count == 0) //&& _applicationUpates.Count == 0)
                timer_status_update.Interval = 1000;
            else
                timer_status_update.Interval = 500;
        }

        private void ProcessFileQueueThead()
        {
            if (_fileQueue.Count > 0)
            {
                while (true)
                {
                    foreach (var obj in _fileQueue.Keys)
                    {
                        var fileQ = (FileQueue)_fileQueue[obj];
                        _fileQueue.Remove(obj);
                        if (File.Exists(obj.ToString()))
                        {
                            if (new FileInfo(obj.ToString()).Length != 0)
                                DoObjectSync(fileQ.Type, obj.ToString(), fileQ.Name, fileQ.OldFullpath, fileQ.OldName, null);
                            else
                                _fileQueue.Add(obj.ToString(), fileQ);
                        }
                        break;
                    }
                    if (_fileQueue.Count == 0)
                        break;
                    Application.DoEvents();
                }
            }
            _isThreadStarted = false;
        }

        private void ProcessApplicationUpdates(AppUpdateInfo appUpdateInfo)
        {
            try
            {
                //if (_applicationUpates.Count > 0)
                //{
                //var appUpdateInfo = (AppUpdateInfo)_applicationUpates.Dequeue();
                string url = Utilities.DevelopmentMode ? "http://localhost:3000" : "http://versavault.com";
                switch (appUpdateInfo.Status)
                {
                    case UpdateStatus.Update:
                        {
                            url += "/api/update_files?bucket_key=" + Utilities.MyConfig.BucketKey + "&key=" +
                                   appUpdateInfo.Key + "&last_modified=" +
                                   appUpdateInfo.LastModifiedTime.ToUniversalTime().ToString(
                                       "yyyy-MM-dd HH:mm:ss tt") + "&machine_key=" +
                                   Utilities.MyConfig.MachineKey;
                            GetResponse(url);

                            break;
                        }
                    case UpdateStatus.Delete:
                        {
                            url += "/api/delete_files?bucket_key=" + Utilities.MyConfig.BucketKey + "&key=" +
                                   appUpdateInfo.Key + "&machine_key=" + Utilities.MyConfig.MachineKey;
                            GetResponse(url);
                            break;
                        }
                }
                //}
            }
            catch (Exception)
            {
                // Todo
                // Need to fix some how if any error happens here
                return;
            }
        }

        private void DoObjectSync(WatcherChangeTypes type, string fullPath, string name, string oldfullpath, string oldname, FileSystemEventArgs e)
        {
            switch (type)
            {
                case WatcherChangeTypes.Changed: // some how we have to avoid unncessary uploads
                    {
                        try
                        {
                            if (!_downloadObjects.Contains(fullPath))
                            {
                                var attribute = File.GetAttributes(fullPath);
                                if (attribute != (FileAttributes.Archive | FileAttributes.Hidden))
                                {
                                    if (attribute != FileAttributes.Directory && !_processingFiles.Contains(fullPath))
                                    {
                                        if (!IsFileUsedbyAnotherProcess(fullPath) && new FileInfo(fullPath).Length != 0)
                                        {
                                            // get the file last modified from database
                                            string relativePath = fullPath.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                                            string url = Utilities.DevelopmentMode
                                                             ? "http://localhost:3000"
                                                             : "http://versavault.com";
                                            url += "/api/get_object_status?bucket_key=" +
                                                   Utilities.MyConfig.BucketKey + "&key=" + relativePath +
                                                   "&machine_key=" + Utilities.MyConfig.MachineKey;
                                            string result = GetResponse(url);
                                            if (!string.IsNullOrEmpty(result))
                                            {
                                                var res = JsonConvert.DeserializeObject<s3_object>(result);
                                                if (res.S3Object.Length > 0)
                                                {
                                                    foreach (var s3ObjSub in res.S3Object)
                                                    {
                                                        if (s3ObjSub != null)
                                                        {
                                                            string fullPathSub = Path.Combine(Utilities.Path, s3ObjSub.Key.Replace("/", "\\"));
                                                            if (!s3ObjSub.Folder)
                                                            {
                                                                try
                                                                {
                                                                    TimeSpan timeSpan = new DirectoryInfo(fullPathSub).LastWriteTime.Subtract(s3ObjSub.LastModified);
                                                                    if (Math.Floor(timeSpan.TotalSeconds) != 0)
                                                                    {
                                                                        //relativePath = fullPathSub.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                                                                        //_applicationUpates.Enqueue(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPathSub).LastWriteTime, Status = UpdateStatus.Update });
                                                                        ShowBalloon("Uploading file content......");
                                                                        Addobject(fullPath);
                                                                    }
                                                                }
                                                                catch (Exception)
                                                                {
                                                                    // Todo
                                                                    // need to fix this when an error occured
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    try
                                                    {
                                                        if (!IsFileUsedbyAnotherProcess(fullPath) && new FileInfo(fullPath).Length != 0)
                                                        {
                                                            //relativePath = fullPath.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                                                            //_applicationUpates.Enqueue(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPath).LastWriteTime, Status = UpdateStatus.Update });
                                                            ShowBalloon("Uploading file content......");
                                                            Addobject(fullPath);
                                                        }
                                                        else
                                                        {
                                                            // Need to maintain queue
                                                            if (!_fileQueue.ContainsKey(fullPath))
                                                                _fileQueue.Add(fullPath, new FileQueue { Type = type, Name = name });
                                                            else
                                                                _fileQueue[fullPath] = new FileQueue { Type = type, Name = name };
                                                            return;
                                                        }
                                                    }
                                                    catch (Exception)
                                                    {
                                                        // Todo
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // Need to maintain queue
                                            if (!_fileQueue.ContainsKey(fullPath))
                                                _fileQueue.Add(fullPath, new FileQueue { Type = type, Name = name });
                                            else
                                                _fileQueue[fullPath] = new FileQueue { Type = type, Name = name };
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            return;
                        }
                        break;
                    }
                case WatcherChangeTypes.Created:
                    {
                        try
                        {
                            if (!_downloadObjects.Contains(fullPath))
                            {
                                var attribute = File.GetAttributes(fullPath);
                                if (attribute != (FileAttributes.Archive | FileAttributes.Hidden))
                                {
                                    if (attribute != FileAttributes.Directory && !_processingFiles.Contains(fullPath))
                                    {
                                        if (!IsFileUsedbyAnotherProcess(fullPath) && new FileInfo(fullPath).Length != 0)
                                        {
                                            // get the file last modified from database
                                            string relativePath = fullPath.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                                            string url = Utilities.DevelopmentMode ? "http://localhost:3000" : "http://versavault.com";
                                            url += "/api/get_object_status?bucket_key=" + Utilities.MyConfig.BucketKey + "&key=" + relativePath + "&machine_key=" + Utilities.MyConfig.MachineKey;
                                            string result = GetResponse(url);
                                            if (!string.IsNullOrEmpty(result))
                                            {
                                                var res = JsonConvert.DeserializeObject<s3_object>(result);
                                                if (res.S3Object.Length > 0)
                                                {
                                                    foreach (var s3ObjSub in res.S3Object)
                                                    {
                                                        if (s3ObjSub != null)
                                                        {
                                                            string fullPathSub = Path.Combine(Utilities.Path, s3ObjSub.Key.Replace("/", "\\"));
                                                            if (!s3ObjSub.Folder)
                                                            {
                                                                try
                                                                {
                                                                    TimeSpan timeSpan = new DirectoryInfo(fullPathSub).LastWriteTime.Subtract(s3ObjSub.LastModified);
                                                                    if (Math.Floor(timeSpan.TotalSeconds) != 0)
                                                                    {
                                                                        ShowBalloon("Uploading file content......");
                                                                        Addobject(fullPath);
                                                                    }
                                                                }
                                                                catch (Exception)
                                                                {
                                                                    // Todo
                                                                    // need to fix this when an error occured
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    ShowBalloon("Uploading file content......");
                                                    Addobject(fullPath);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // Need to maintain queue
                                            if (!_fileQueue.ContainsKey(fullPath))
                                                _fileQueue.Add(fullPath, new FileQueue { Type = type, Name = name });
                                            else
                                                _fileQueue[fullPath] = new FileQueue { Type = type, Name = name };
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        // Creating an Folder
                                        Createfolder(fullPath);
                                        // If it is restored from the recylebin / pasted from another location then upload folder structure completely
                                        Uploadfiles(fullPath);
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            return;
                        }
                        break;
                    }
                case WatcherChangeTypes.Deleted:
                    {
                        string relativePath = fullPath.Replace(Utilities.Path + "\\", "");
                        ShowBalloon("Deleting ......");
                        Removeobject(fullPath);
                        Deletefolder(relativePath + "/");
                        break;
                    }
                case WatcherChangeTypes.Renamed:
                    {
                        try
                        {
                            var attribute = File.GetAttributes(fullPath);
                            if (attribute != (FileAttributes.Archive | FileAttributes.Hidden))
                            {
                                if (attribute != FileAttributes.Directory)
                                {
                                    if (!IsFileUsedbyAnotherProcess(fullPath))
                                    {
                                        if (new FileInfo(fullPath).Length != 0)
                                        {
                                            ShowBalloon("Renaming a file...");
                                            if (e != null)
                                            {
                                                var renamedEvent = (RenamedEventArgs)e;
                                                Moveobject(renamedEvent.FullPath, renamedEvent.OldFullPath);
                                            }
                                            else
                                                Moveobject(fullPath, oldfullpath);
                                        }
                                    }
                                    else
                                    {
                                        // Need to maintain queue
                                        if (e != null)
                                        {
                                            var renamedEvent = (RenamedEventArgs)e;
                                            if (!_fileQueue.ContainsKey(fullPath))
                                                _fileQueue.Add(fullPath, new FileQueue { Type = type, Name = name, OldFullpath = renamedEvent.OldFullPath, OldName = renamedEvent.OldName });
                                            else
                                                _fileQueue[fullPath] = new FileQueue { Type = type, Name = name, OldFullpath = renamedEvent.OldFullPath, OldName = renamedEvent.OldName };
                                        }
                                        else
                                        {
                                            if (!_fileQueue.ContainsKey(fullPath))
                                                _fileQueue.Add(fullPath, new FileQueue { Type = type, Name = name, OldFullpath = oldfullpath, OldName = oldname });
                                            else
                                                _fileQueue[fullPath] = new FileQueue { Type = type, Name = name, OldFullpath = oldfullpath, OldName = oldname };
                                        }
                                        return;
                                    }
                                }
                                else
                                {
                                    var renamedEvent = (RenamedEventArgs)e;
                                    ShowBalloon("Renaming a folder...");
                                    Modifyfolder(fullPath, renamedEvent.OldFullPath);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            return;
                        }
                        break;
                    }
            }
            return;
        }

        private void closeWindow_Click(object sender, EventArgs e)
        {
            HideForm();
        }

        private void ShowForm()
        {
            _ds.Show();
            Application.DoEvents();
            Visible = true;
            Application.DoEvents();
            Opacity = 1;
            Application.DoEvents();
            Show();
            Application.DoEvents();
            WindowState = FormWindowState.Normal;
            Application.DoEvents();
        }

        private void HideForm()
        {
            Visible = false;
            Application.DoEvents();
            Opacity = 0;
            Application.DoEvents();
            Hide();
            Application.DoEvents();
            WindowState = FormWindowState.Minimized;
            Application.DoEvents();
            _ds.Hide();
            Application.DoEvents();
        }

        private void closeWindow_MouseLeave(object sender, EventArgs e)
        {
            closeWindow.Image = Properties.Resources.closeDefault;
        }

        private void closeWindow_MouseEnter(object sender, EventArgs e)
        {
            closeWindow.Image = Properties.Resources.closeHover;
        }

        //const and dll functions for moving form
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd,
            int Msg, int wParam, int lParam);

        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        private void connectBtn_MouseEnter(object sender, EventArgs e)
        {
            connectBtn.Image = Properties.Resources.connectButton_Hover;
        }

        private void connectBtn_MouseLeave(object sender, EventArgs e)
        {
            connectBtn.Image = Properties.Resources.connectButton_Normal;
        }

        private void connectBtn_MouseDown(object sender, MouseEventArgs e)
        {
            connectBtn.Image = Properties.Resources.connectButton_Activel;
        }

        private void connectBtn_MouseUp(object sender, MouseEventArgs e)
        {
            connectBtn.Image = Properties.Resources.connectButton_Hover;
        }
    }

    [Serializable]
    class FileQueue
    {
        public WatcherChangeTypes Type { get; set; }

        public string Name { get; set; }

        public string OldFullpath { get; set; }

        public string OldName { get; set; }
    }

    [Serializable]
    class AppUpdateInfo
    {
        public string Key { get; set; }

        public DateTime LastModifiedTime { get; set; }

        public UpdateStatus Status { get; set; }
    }

    enum UpdateStatus
    {
        Update,
        Delete
    }

    public class DropShadow : Form
    {
        public DropShadow()
        {
            Opacity = 0.2;
            BackColor = Color.Gray;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
        }

        public override sealed Color BackColor
        {
            get { return base.BackColor; }
            set { base.BackColor = value; }
        }

        private const int WsExTransparent = 0x20;
        private const int WsExNoactivate = 0x8000000;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle = cp.ExStyle | WsExTransparent | WsExNoactivate;
                return cp;
            }
        }
    }
}