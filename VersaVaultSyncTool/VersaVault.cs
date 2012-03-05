using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using LitS3;
using Microsoft.Win32;
using Newtonsoft.Json;
using VersaVaultLibrary;
using CopyObjectRequest = LitS3.CopyObjectRequest;
using DeleteObjectRequest = LitS3.DeleteObjectRequest;
using ListObjectsRequest = Amazon.S3.Model.ListObjectsRequest;

namespace VersaVaultSyncTool
{
    public partial class VersaVault : Form
    {
        static void SystemEventsSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            Application.Exit();
        }

        static void SystemEventsSession(object sender, SessionEndingEventArgs sessionEndingEventArgs)
        {
            Application.Exit();
        }

        static void SystemEventsSessionEnded(object sender, SessionEndedEventArgs e)
        {
            Application.Exit();
        }

        readonly DropShadow _ds = new DropShadow();

        S3Service _service;

        private AmazonS3 _amazons3;

        private readonly Queue _syncStatus = new Queue();

        //private readonly Queue _applicationUpates = new Queue();

        private bool _isThreadStarted;

        private readonly Hashtable _fileQueue = new Hashtable();

        private bool _syncIsPaused;

        private bool _isStartedMonitoring;

        private List<string> _downloadObjects;

        public VersaVault()
        {
            notification = new Notification();
            notification.SetMessage("Started VersaVault");
            notification.Opacity = 0;
            notification.Show();
            InitializeComponent();
            Shown += VersaVaultShown;
            Resize += VersaVaultResize;
            LocationChanged += VersaVaultResize;
            SystemEvents.SessionSwitch += SystemEventsSessionSwitch;
            SystemEvents.SessionEnding += SystemEventsSession;
            SystemEvents.SessionEnded += SystemEventsSessionEnded;
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

        private bool GetBucketId()
        {
            HideForm();
            string url = Utilities.DevelopmentMode ? "http://localhost:3000" : "http://versavault.com";
            url += "/api/get_amazon_bucket_id?username=" + TxtUsername.Text.Trim() + "&password=" + TxtPassword.Text.Trim();
            startSyncToolStripMenuItem.Text = @"Start sync";
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
                    {
                        Utilities.MyConfig.MachineKey = Guid.NewGuid().ToString();
                        // MessageBox.Show(Utilities.MyConfig.MachineKey);
                    }
                    Utilities.MyConfig.Save();
                    StartSync();
                    return true;
                }
                LblError.Text = res.Error;
            }
            else
            {
                LblError.Text = @"Unable to connect to VersaVault." + Environment.NewLine + @"Check your internet connection and try again.";
            }
            StopSync();
            return false;
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

                _amazons3 = AWSClientFactory.CreateAmazonS3Client(Utilities.AwsAccessKey, Utilities.AwsSecretKey, new AmazonS3Config { CommunicationProtocol = Protocol.HTTP });

                _service.AddObjectProgress += ServiceAddObjectProgress;
                _service.GetObjectProgress += ServiceGetObjectProgress;

                _downloadObjects = new List<string>();

                if (!Directory.Exists(Utilities.Path))
                    Directory.CreateDirectory(Utilities.Path);

                if (!Directory.Exists(Utilities.AppPath))
                    Directory.CreateDirectory(Utilities.AppPath);

                if (!Directory.Exists(Utilities.SharedFolderPath))
                    Directory.CreateDirectory(Utilities.SharedFolderPath);

                TxtUsername.Text = Utilities.MyConfig.Username;
                TxtPassword.Text = Utilities.MyConfig.Password;

                if (!string.IsNullOrEmpty(TxtUsername.Text) || !string.IsNullOrEmpty(TxtPassword.Text.Trim()))
                {
                    if (!GetBucketId())
                    {
                        if (string.IsNullOrEmpty(Utilities.MyConfig.BucketKey))
                            StopSync();
                        else
                            StartSync();
                    }
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
            LblError.Text = @"Authenticated successfully.";
            _syncStatus.Enqueue("Started synchronization process");
            StartActivityMonitoring(Utilities.Path);
            startSyncToolStripMenuItem.Text = @"Pause Sync";
            Visible = false;
            Opacity = 0;
        }

        private static void EnableVersioning()
        {
            /*   try
               {
                   var setBucketVersioningRequest = new SetBucketVersioningRequest { BucketName = Utilities.MyConfig.BucketKey };
                   var versionConfig = new S3BucketVersioningConfig { Status = "Enabled" };
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
             */
        }

        private void StopSync()
        {
            _syncIsPaused = true;
            startSyncToolStripMenuItem.Text = @"Start Sync";
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
            if (e.CloseReason == CloseReason.None)
            {
                e.Cancel = true;
                Opacity = 0;
                if (string.IsNullOrEmpty(Utilities.MyConfig.BucketKey))
                    startSyncToolStripMenuItem.Text = @"Start sync";
                Application.DoEvents();
            }
            else
            {
                Application.Exit();
            }
        }

        private static void VersaVaultFormClosed(object sender, FormClosedEventArgs e)
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
            //Process.Start("explorer.exe", Utilities.Path);
            //ShowForm();
        }

        private static void VersaVaultNotificationsDoubleClick(object sender, EventArgs e)
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
                if (relativePath.ToLower().IndexOf("shared") < 0)
                {
                    var addobjectrequest = new AddObjectRequest(_service, Utilities.MyConfig.BucketKey, relativePath + "/") { ContentLength = 0 };
                    addobjectrequest.GetResponse();
                    _syncStatus.Enqueue("Creating folder - " + relativePath);
                    //_applicationUpates.Enqueue(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new DirectoryInfo(path).LastWriteTime, Status = UpdateStatus.Update });
                    ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new DirectoryInfo(path).LastWriteTime, Status = UpdateStatus.Update }, false, string.Empty);
                }
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
                if (oldrelativePath.ToLower().IndexOf("shared") == 0)
                {
                }
                else
                    ProcessApplicationUpdates(new AppUpdateInfo { Key = oldrelativePath.Replace("\\", "/"), LastModifiedTime = DateTime.Now, Status = UpdateStatus.Delete }, false, string.Empty);
            }
            catch (Exception)
            {
                return;
            }
        }

        private void Moveobject(string newPath, string oldPath)
        {
            try
            {
                string newrelativePath = newPath.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                string oldrelativePath = oldPath.Replace(Utilities.Path + "\\", "").Replace("\\", "/");  // +"/" + Path.GetFileName(new_path);
                if (newrelativePath.ToLower().IndexOf("shared") != 0 && oldrelativePath.ToLower().IndexOf("shared") != 0)
                {
                    if (!Utilities.IsFileUsedbyAnotherProcess(newPath))
                    {
                        if (_service.ObjectExists(Utilities.MyConfig.BucketKey, oldrelativePath))
                        {
                            var request = new CopyObjectRequest(_service, Utilities.MyConfig.BucketKey, oldrelativePath, newrelativePath);
                            var response = request.GetResponse();
                            if (response.Error == null)
                            {
                                _service.DeleteObject(Utilities.MyConfig.BucketKey, oldrelativePath);
                            }
                            SetAcltoObject(newrelativePath);
                            ProcessApplicationUpdates(new AppUpdateInfo { Key = oldrelativePath.Replace("\\", "/"), LastModifiedTime = DateTime.Now, Status = UpdateStatus.Delete }, false, string.Empty);
                            //_applicationUpates.Enqueue(new AppUpdateInfo { Key = oldrelativePath.Replace("\\", "/"), LastModifiedTime = DateTime.Now, Status = UpdateStatus.Delete });
                            ProcessApplicationUpdates(new AppUpdateInfo { Key = newrelativePath.Replace("\\", "/"), LastModifiedTime = DateTime.Now, Status = UpdateStatus.Update }, false, string.Empty);
                            //_applicationUpates.Enqueue(new AppUpdateInfo { Key = newrelativePath.Replace("\\", "/"), LastModifiedTime = DateTime.Now, Status = UpdateStatus.Update });
                        }
                        else
                        {
                            if (File.Exists(newPath))
                                Addobject(newPath);
                        }
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
                    if (File.Exists(filePath) && !Utilities.IsFileUsedbyAnotherProcess(filePath))
                    {
                        var appUpdateInfo = new AppUpdateInfo
                                                {
                                                    Key = relativePath.Replace("\\", "/"),
                                                    LastModifiedTime = new FileInfo(filePath).LastWriteTime,
                                                    Status = UpdateStatus.Update
                                                };
                        _processingFiles.Add(filePath);
                        try
                        {
                            var uploadResponses = new List<UploadPartResponse>();
                            byte[] bytes;
                            long contentLength;
                            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                contentLength = fileStream.Length;
                                bytes = new byte[contentLength];
                                fileStream.Read(bytes, 0, Convert.ToInt32(contentLength));
                            }
                            var lastUploadedPartdetail = GetLastUploadedPartResponse(relativePath, Utilities.MyConfig.BucketKey, uploadResponses);
                            int alreadyUploadedParts = lastUploadedPartdetail.LastPartNumber;
                            string uploadId;
                            if (string.IsNullOrEmpty(lastUploadedPartdetail.UploadId))
                            {
                                InitiateMultipartUploadRequest initiateRequest = new InitiateMultipartUploadRequest().WithBucketName(Utilities.MyConfig.BucketKey).WithKey(relativePath);
                                InitiateMultipartUploadResponse initResponse = _amazons3.InitiateMultipartUpload(initiateRequest);
                                uploadId = initResponse.UploadId;
                            }
                            else
                                uploadId = lastUploadedPartdetail.UploadId;
                            try
                            {
                                long partSize = 5 * (long)Math.Pow(2, 20); // 5 MB
                                long filePosition = partSize * alreadyUploadedParts;
                                for (int i = alreadyUploadedParts + 1; filePosition < contentLength; i++)
                                {
                                    // Before upload the next set of upload part, need to check the last modified because user might modify it in the mean time
                                    if (File.Exists(filePath) && appUpdateInfo.LastModifiedTime == new FileInfo(filePath).LastWriteTime)
                                    {
                                        byte[] bytesToStream;
                                        if (filePosition + partSize < contentLength)
                                        {
                                            bytesToStream = new byte[partSize];
                                            Array.Copy(bytes, filePosition, bytesToStream, 0, partSize);
                                        }
                                        else
                                        {
                                            bytesToStream = new byte[contentLength - filePosition];
                                            Array.Copy(bytes, filePosition, bytesToStream, 0,
                                                       contentLength - filePosition);
                                        }
                                        Stream stream = new MemoryStream(bytesToStream);
                                        UploadPartRequest uploadRequest = new UploadPartRequest()
                                            .WithBucketName(Utilities.MyConfig.BucketKey)
                                            .WithKey(relativePath)
                                            .WithUploadId(uploadId)
                                            .WithPartNumber(i)
                                            .WithPartSize(partSize)
                                            .WithFilePosition(filePosition)
                                            .WithTimeout(1000000000);
                                        uploadRequest.WithInputStream(stream);
                                        // Upload part and add response to our list.
                                        var response = _amazons3.UploadPart(uploadRequest);
                                        WriteResponseToFile(relativePath, Utilities.MyConfig.BucketKey, uploadId, appUpdateInfo.LastModifiedTime, response);
                                        uploadResponses.Add(response);
                                        filePosition += partSize;
                                        ModifySyncStatus("Uploaded",
                                                         contentLength <= filePosition ? contentLength : filePosition,
                                                         contentLength, relativePath);
                                    }
                                    else
                                    {
                                        // need to abort the upload process
                                        _processingFiles.Remove(filePath);
                                        RemoveConfig(relativePath, lastUploadedPartdetail.BucketKey);
                                        _amazons3.AbortMultipartUpload(new AbortMultipartUploadRequest()
                                                                  .WithBucketName(Utilities.MyConfig.BucketKey)
                                                                  .WithKey(relativePath)
                                                                  .WithUploadId(uploadId));
                                        return;
                                    }
                                }
                                CompleteMultipartUploadRequest completeRequest = new CompleteMultipartUploadRequest()
                                        .WithBucketName(Utilities.MyConfig.BucketKey)
                                        .WithKey(relativePath)
                                        .WithUploadId(uploadId)
                                        .WithPartETags(uploadResponses);
                                CompleteMultipartUploadResponse completeUploadResponse = _amazons3.CompleteMultipartUpload(completeRequest);
                                RemoveConfig(relativePath, completeUploadResponse.BucketName);
                                //_service.AddObject(filePath, Utilities.MyConfig.BucketKey, relativePath);
                                SetAcltoObject(relativePath);
                                if (relativePath.ToLower().IndexOf("shared") == 0)
                                {
                                    var folders = relativePath.Split('\\');
                                    if (folders.Length > 1)
                                        ProcessApplicationUpdates(appUpdateInfo, true, folders[1]);
                                    else if (folders.Length > 0)
                                        ProcessApplicationUpdates(appUpdateInfo, true, folders[0]);
                                }
                                else
                                    ProcessApplicationUpdates(appUpdateInfo, false, string.Empty);
                                _processingFiles.Remove(filePath);
                                UploadContentForSearch(filePath, relativePath);
                            }
                            catch (Exception)
                            {
                                _processingFiles.Remove(filePath);
                                _amazons3.AbortMultipartUpload(new AbortMultipartUploadRequest()
                                                                  .WithBucketName(Utilities.MyConfig.BucketKey)
                                                                  .WithKey(relativePath)
                                                                  .WithUploadId(uploadId));
                                RemoveConfig(relativePath, Utilities.MyConfig.BucketKey);
                                if (!_fileQueue.ContainsKey(filePath))
                                    _fileQueue.Add(filePath, new FileQueue { Type = WatcherChangeTypes.Created, Name = Path.GetFileName(filePath) });
                                else
                                    _fileQueue.Add(filePath, new FileQueue { Type = WatcherChangeTypes.Created, Name = Path.GetFileName(filePath) });
                            }
                        }
                        catch (Exception)
                        {
                            return;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        private void UploadContentForSearch(string filePath, string relativePath)
        {
            try
            {
                if (File.Exists(filePath) && !Utilities.IsFileUsedbyAnotherProcess(filePath))
                {
                    switch (Path.GetExtension(filePath))
                    {
                        case ".doc":
                        case ".docx":
                            {
                                string url = Utilities.DevelopmentMode
                                                 ? "http://localhost:3000"
                                                 : "http://versavault.com";
                                if (relativePath.ToLower().IndexOf("shared") == 0)
                                {
                                    var folders = relativePath.Split('\\');
                                    relativePath = folders.Length > 1
                                                       ? relativePath.Substring(relativePath.IndexOf(folders[0] + "\\" + folders[1])) : relativePath.Substring(relativePath.IndexOf(folders[0]));
                                    url += "/api/get_object_id?shared=" + true + "&username" + (folders.Length > 1 ? folders[1] : folders[0]) + "&relativePath" + relativePath;
                                }
                                else
                                    url += "/api/get_object_id?bucket_key=" + Utilities.MyConfig.BucketKey + "&key=" + relativePath;
                                string result = GetResponse(url);
                                if (!string.IsNullOrEmpty(result))
                                {
                                    var startInfo =
                                        new ProcessStartInfo(
                                            Path.Combine(Application.StartupPath, "VersaVaultSearchUtility.exe"),
                                            filePath.Replace(" ", "%20") + " " + result)
                                            {
                                                UseShellExecute = true,
                                                WindowStyle = ProcessWindowStyle.Hidden,
                                            };
                                    Process.Start(startInfo);
                                }
                                break;
                            }
                    }
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        private void DeleteObjectVersions(string key)
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
                    try
                    {
                        var deleteObjectRequest = new Amazon.S3.Model.DeleteObjectRequest
                                                      {
                                                          BucketName = Utilities.MyConfig.BucketKey,
                                                          Key = key,
                                                          VersionId = s3ObjectVersion.VersionId
                                                      };
                        _amazons3.DeleteObject(deleteObjectRequest);
                    }
                    catch (Exception)
                    {
                        // ToDo
                        return;
                    }
                }
            }
            catch (Exception)
            {
                // ToDo
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
                            return;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return;
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
                if (relativePath.ToLower().IndexOf("shared") != 0)
                {
                    var response =
                        _amazons3.DeleteObject(new Amazon.S3.Model.DeleteObjectRequest { BucketName = Utilities.MyConfig.BucketKey, Key = relativePath });
                    if (!string.IsNullOrEmpty(response.RequestId))
                    {
                        ProcessApplicationUpdates(new AppUpdateInfo
                                                      {
                                                          Key = relativePath.Replace("\\", "/"),
                                                          LastModifiedTime = DateTime.Now,
                                                          Status = UpdateStatus.Delete
                                                      }, false, string.Empty);
                        RemoveConfig(relativePath, Utilities.MyConfig.BucketKey);
                    }
                    /*if (_service.ObjectExists(Utilities.MyConfig.BucketKey, relativePath))
                {
                    _service.DeleteObject(Utilities.MyConfig.BucketKey, relativePath);
                    ProcessApplicationUpdates(new AppUpdateInfo
                    {
                        Key = relativePath.Replace("\\", "/"),
                        LastModifiedTime = DateTime.Now,
                        Status = UpdateStatus.Delete
                    });
                    //_applicationUpates.Enqueue(new AppUpdateInfo{Key = relativePath.Replace("\\", "/"),LastModifiedTime = DateTime.Now,Status = UpdateStatus.Delete});
                }*/
                    Application.DoEvents();
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        private void Deletefolder(string relativePath)
        {
            if (relativePath.ToLower().IndexOf("shared") != 0)
            {
                // Iteration through all the files in the folder
                try
                {
                    foreach (
                        var s3Object in
                            _amazons3.ListObjects(new ListObjectsRequest { BucketName = Utilities.MyConfig.BucketKey, Prefix = relativePath })
                                .S3Objects)
                    {
                        try
                        {
                            _amazons3.DeleteObject(new Amazon.S3.Model.DeleteObjectRequest { BucketName = Utilities.MyConfig.BucketKey, Key = s3Object.Key });
                            DeleteObjectVersions(s3Object.Key);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                    foreach (var resultEntry in _service.ListAllObjects(Utilities.MyConfig.BucketKey, relativePath))
                    {
                        ObjectEntry entry;
                        try
                        {
                            entry = (ObjectEntry)resultEntry;
                            try
                            {
                                var removeobject = new DeleteObjectRequest(_service, Utilities.MyConfig.BucketKey,
                                                                           entry.Key);
                                removeobject.GetResponse();
                                DeleteObjectVersions(entry.Key);
                            }
                            catch (Exception)
                            {
                                try
                                {
                                    var removeobject = new DeleteObjectRequest(_service, Utilities.MyConfig.BucketKey,
                                                                               entry.Name);
                                    removeobject.GetResponse();
                                    DeleteObjectVersions(entry.Name);
                                }
                                catch (Exception)
                                {
                                    continue;
                                }
                            }
                        }
                        catch (Exception)
                        {
                            try
                            {
                                var prefix = (CommonPrefix)resultEntry;
                                Deletefolder(prefix.Prefix);
                            }
                            catch (Exception)
                            {
                                continue;
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
                    ProcessApplicationUpdates(new AppUpdateInfo
                                                  {
                                                      Key = relativePath.Replace("\\", "/"),
                                                      LastModifiedTime = DateTime.Now,
                                                      Status = UpdateStatus.Delete
                                                  }, false, string.Empty);
                    //_applicationUpates.Enqueue(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = DateTime.Now, Status = UpdateStatus.Delete });
                }
                // If the user deleted a folder, it will be an exception
                try
                {
                    //var attribute = File.GetAttributes(Path.Combine(Utilities.Path, relativePath));
                    var removeobject = new DeleteObjectRequest(_service, Utilities.MyConfig.BucketKey, relativePath);
                    removeobject.GetResponse();
                }
                catch (Exception)
                {
                    return;
                }
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
            ResyncToolStripMenuItem.Enabled = false;
            ResyncToolStripMenuItem.Text = @"Sync in progress";
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
                                string fullPath = Path.Combine(s3Obj.Shared ? Utilities.SharedFolderPath + "\\" + s3Obj.Username : Utilities.Path, s3Obj.Key.Replace("/", "\\"));
                                if (s3Obj.Shared)
                                {
                                    if (!Directory.Exists(Utilities.SharedFolderPath + "\\" + s3Obj.Username))
                                        Directory.CreateDirectory(Utilities.SharedFolderPath + "\\" + s3Obj.Username);
                                }
                                if (s3Obj.Folder)
                                {
                                    if (s3Obj.Status)
                                    {
                                        if (!Directory.Exists(fullPath))
                                        {
                                            DownloadFolder(s3Obj, fullPath);
                                        }
                                        else
                                        {
                                            try
                                            {
                                                var timeSpan = new DirectoryInfo(fullPath).LastWriteTime.ToUniversalTime().Subtract(s3Obj.LastModified);
                                                var seconds = Math.Floor(timeSpan.TotalSeconds);
                                                if (seconds != 0)
                                                {
                                                    if (seconds < 0)
                                                    {
                                                        string relativePath = fullPath.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                                                        //_applicationUpates.Enqueue(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new DirectoryInfo(fullPath).LastWriteTime, Status = UpdateStatus.Update });
                                                        DownloadFolder(s3Obj, fullPath);
                                                        if (relativePath.ToLower().IndexOf("shared") == 0)
                                                        {
                                                            var folders = relativePath.Split('\\');
                                                            ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new DirectoryInfo(fullPath).LastWriteTime, Status = UpdateStatus.Add }, true, folders.Length > 1 ? folders[1] : folders[0]);
                                                        }
                                                        else
                                                            ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new DirectoryInfo(fullPath).LastWriteTime, Status = UpdateStatus.Add }, false, string.Empty);
                                                    }
                                                    //else
                                                    //  Uploadfiles(fullPath); // need to figure it out how to do this case, we cannot upload the entire folder
                                                }
                                            }
                                            catch (Exception)
                                            {
                                                // Todo
                                                // need to fix this when an error occured
                                                continue;
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
                                                TimeSpan timeSpan = new DirectoryInfo(fullPath).LastWriteTime.ToUniversalTime().Subtract(s3Obj.LastModified);
                                                if (Math.Floor(timeSpan.TotalSeconds) < 1)
                                                    Directory.Delete(fullPath, true);
                                            }
                                        }
                                        catch (Exception)
                                        {
                                            // Todo
                                            // how to handle when it is used by some other process
                                            continue;
                                        }
                                    }
                                }
                                else
                                {
                                    if (s3Obj.Status)
                                    {
                                        if (!File.Exists(fullPath)) // file is not available here , so download
                                        {
                                            try
                                            {
                                                _downloadObjects.Add(fullPath);
                                                _service.GetObject(Utilities.MyConfig.BucketKey, s3Obj.Key, fullPath);
                                                string relativePath = fullPath.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                                                if (relativePath.ToLower().IndexOf("shared") == 0)
                                                {
                                                    var folders = relativePath.Split('\\');
                                                    ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new DirectoryInfo(fullPath).LastWriteTime, Status = UpdateStatus.Add }, true, folders.Length > 1 ? folders[1] : folders[0]);
                                                }
                                                else
                                                    ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPath).LastWriteTime, Status = UpdateStatus.Add }, false, string.Empty);
                                                //_applicationUpates.Enqueue(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPath).LastWriteTime, Status = UpdateStatus.Update });
                                            }
                                            catch (Exception)
                                            {
                                                // Todo
                                                // need to handle if an error occured when downloading file
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            try
                                            {
                                                if (!Utilities.IsFileUsedbyAnotherProcess(fullPath))
                                                {
                                                    TimeSpan timeSpan = new FileInfo(fullPath).LastWriteTime.ToUniversalTime().Subtract(s3Obj.LastModified);
                                                    var seconds = Math.Floor(timeSpan.TotalSeconds);
                                                    if (seconds != 0)
                                                    {
                                                        if (seconds < 0)
                                                        {
                                                            // download the latest from server
                                                            _downloadObjects.Add(fullPath);
                                                            _service.GetObject(Utilities.MyConfig.BucketKey, s3Obj.Key, fullPath);
                                                            string relativePath = fullPath.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                                                            if (relativePath.ToLower().IndexOf("shared") == 0)
                                                            {
                                                                var folders = relativePath.Split('\\');
                                                                ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new DirectoryInfo(fullPath).LastWriteTime, Status = UpdateStatus.Add }, true, folders.Length > 1 ? folders[1] : folders[0]);
                                                            }
                                                            else
                                                                ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPath).LastWriteTime, Status = UpdateStatus.Add }, false, string.Empty);
                                                            //_applicationUpates.Enqueue(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPath).LastWriteTime, Status = UpdateStatus.Update });
                                                        }
                                                        else // Upload the latest to server
                                                            Addobject(fullPath);
                                                    }
                                                }
                                            }
                                            catch (Exception)
                                            {
                                                // Todo
                                                // need to fix this when an error occured
                                                continue;
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
                                                TimeSpan timeSpan = new FileInfo(fullPath).LastWriteTime.ToUniversalTime().Subtract(s3Obj.LastModified);
                                                if (Math.Floor(timeSpan.TotalSeconds) < 1)
                                                    File.Delete(fullPath);
                                                else // upload the latest to server
                                                    Addobject(fullPath);
                                            }
                                        }
                                        catch (Exception)
                                        {
                                            // Todo
                                            // need to fix if the file is used
                                            continue;
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
                ResyncToolStripMenuItem.Enabled = true;
                ResyncToolStripMenuItem.Text = @"Resync VersaVault";
                return;
            }
            finally
            {
                ResyncToolStripMenuItem.Enabled = true;
                ResyncToolStripMenuItem.Text = @"Resync VersaVault";
            }
        }

        private void UploadMissedFiles(IEnumerable<S3Object> s3Objects, string path)
        {
            // Finally Here we checking whether any files found in this folder to upload
            // files first
            foreach (string file in Directory.GetFiles(path))
            {
                string file1 = file;
                bool isFilefound = (from s3Object in s3Objects
                                    let filename = file1.Replace(Utilities.Path + "\\", "")
                                    where s3Object != null && !s3Object.Folder && s3Object.Key.Replace("/", "\\").ToLower().Trim() == filename.Trim().ToLower()
                                    select s3Object).Any();
                if (!isFilefound && new FileInfo(file).Length != 0)
                    Addobject(file);
            }

            // folders
            foreach (string directory in Directory.GetDirectories(path))
            {
                string relativePath = directory.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                if (!string.IsNullOrEmpty(relativePath) && relativePath.ToLower().IndexOf("shared") != 0)
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
                        url += "/api/child_files?bucket_key=" + Utilities.MyConfig.BucketKey + "&parent_uid=" + directoryObject.Uid + "&machine_key=" + Utilities.MyConfig.MachineKey + "&shared=" + false;
                        string result = GetResponse(url);
                        if (!string.IsNullOrEmpty(result))
                        {
                            var res = JsonConvert.DeserializeObject<s3_object>(result);
                            UploadMissedFiles(res.S3Object, directory);
                        }
                    }
                }
                else
                {
                    // If it shared
                    bool isFolderfound = false;
                    S3Object directoryObject = null;
                    foreach (S3Object s3Object in s3Objects)
                    {
                        if (s3Object != null && s3Object.Shared && s3Object.Folder)
                        {
                            string key = directory.Replace(Utilities.SharedFolderPath + "\\" + s3Object.Username, "");
                            if (s3Object.Key.Replace("/", "\\").ToLower().Trim() == key.Trim().ToLower())
                            {
                                directoryObject = s3Object;
                                isFolderfound = true;
                                break;
                            }
                        }
                    }
                    if (!isFolderfound)
                        Uploadfiles(directory);
                    else
                    {
                        string url = Utilities.DevelopmentMode ? "http://localhost:3000" : "http://versavault.com";
                        url += "/api/child_files?bucket_key=" + Utilities.MyConfig.BucketKey + "&parent_uid=" + directoryObject.Uid + "&machine_key=" + Utilities.MyConfig.MachineKey + "&shared=" + true;
                        string result = GetResponse(url);
                        if (!string.IsNullOrEmpty(result))
                        {
                            var res = JsonConvert.DeserializeObject<s3_object>(result);
                            UploadMissedFiles(res.S3Object, directory);
                        }
                    }
                }
            }
        }

        private void DownloadFolder(S3Object s3Obj, string folderPath)
        {
            string fullPath = Path.Combine(folderPath, s3Obj.Key.Replace("/", "\\"));
            try
            {
                // need to import complete directory
                _downloadObjects.Add(fullPath);
                if (!Directory.Exists(fullPath))
                    Directory.CreateDirectory(fullPath);
                // get the folder structure from application database
                string url = Utilities.DevelopmentMode ? "http://localhost:3000" : "http://versavault.com";
                url += "/api/child_files?bucket_key=" + Utilities.MyConfig.BucketKey + "&parent_uid=" + s3Obj.Uid + "&machine_key=" + Utilities.MyConfig.MachineKey + "&shared=" + s3Obj.Shared;
                string result = GetResponse(url);
                if (!string.IsNullOrEmpty(result))
                {
                    var res = JsonConvert.DeserializeObject<s3_object>(result);
                    foreach (var s3ObjSub in res.S3Object)
                    {
                        if (s3ObjSub != null)
                        {
                            string fullPathSub = Path.Combine(folderPath, s3ObjSub.Key.Replace("/", "\\"));
                            if (s3ObjSub.Folder)
                            {
                                if (!Directory.Exists(fullPathSub))
                                {
                                    DownloadFolder(s3ObjSub, fullPathSub);
                                }
                                else
                                {
                                    try
                                    {
                                        TimeSpan timeSpan = new DirectoryInfo(fullPathSub).LastWriteTime.ToUniversalTime().Subtract(s3ObjSub.LastModified);
                                        var seconds = Math.Floor(timeSpan.TotalSeconds);
                                        if (seconds != 0)
                                        {
                                            if (seconds < 0)
                                            {
                                                _downloadObjects.Add(fullPathSub);
                                                DownloadFolder(s3ObjSub, fullPathSub);
                                                string relativePath = fullPathSub.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                                                if (s3ObjSub.Shared)
                                                {
                                                    var folders = relativePath.Split('\\');
                                                    ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPathSub).LastWriteTime, Status = UpdateStatus.Add }, s3ObjSub.Shared, folders.Length > 1 ? folders[1] : folders[0]);
                                                }
                                                else
                                                    ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPathSub).LastWriteTime, Status = UpdateStatus.Add }, false, string.Empty);
                                                //_applicationUpates.Enqueue(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPathSub).LastWriteTime, Status = UpdateStatus.Update });
                                            }
                                            //else
                                            //   Uploadfiles(fullPathSub); // need to figure it out how to do this beacause we cannot upload the entire folder here
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        // Todo
                                        // need to fix this when an error occured
                                        continue;
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
                                    if (s3ObjSub.Shared)
                                    {
                                        var folders = relativePath.Split('\\');
                                        ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPathSub).LastWriteTime, Status = UpdateStatus.Add }, s3ObjSub.Shared, folders.Length > 1 ? folders[1] : folders[0]);
                                    }
                                    else
                                        ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPathSub).LastWriteTime, Status = UpdateStatus.Add }, false, string.Empty);
                                    //_applicationUpates.Enqueue(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPathSub).LastWriteTime, Status = UpdateStatus.Update });
                                }
                                else
                                {
                                    try
                                    {
                                        if (!Utilities.IsFileUsedbyAnotherProcess(fullPathSub))
                                        {
                                            TimeSpan timeSpan = new FileInfo(fullPathSub).LastWriteTime.ToUniversalTime().Subtract(s3ObjSub.LastModified);
                                            var seconds = Math.Floor(timeSpan.TotalSeconds);
                                            if (seconds != 0)
                                            {
                                                if (seconds < 0)
                                                {
                                                    _downloadObjects.Add(fullPathSub);
                                                    _service.GetObject(Utilities.MyConfig.BucketKey, s3ObjSub.Key, fullPathSub);
                                                    string relativePath = fullPathSub.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                                                    if (s3ObjSub.Shared)
                                                    {
                                                        var folders = relativePath.Split('\\');
                                                        ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPathSub).LastWriteTime, Status = UpdateStatus.Add }, s3ObjSub.Shared, folders.Length > 1 ? folders[1] : folders[0]);
                                                    }
                                                    else
                                                        ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPathSub).LastWriteTime, Status = UpdateStatus.Add }, false, string.Empty);
                                                    //_applicationUpates.Enqueue(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPathSub).LastWriteTime, Status = UpdateStatus.Update });
                                                }
                                                else
                                                    Addobject(fullPathSub);
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        // Todo
                                        // need to fix this when an error occured
                                        continue;
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
                string relativePath = fullPath.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                if (relativePath.ToLower().IndexOf("shared") == 0)
                {
                    var folders = relativePath.Split('\\');
                    ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPath).LastWriteTime, Status = UpdateStatus.Add }, true, folders.Length > 1 ? folders[1] : folders[0]);
                }
                else
                    ProcessApplicationUpdates(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPath).LastWriteTime, Status = UpdateStatus.Add }, false, string.Empty);
                //_applicationUpates.Enqueue(new AppUpdateInfo { Key = s3Obj.Key, LastModifiedTime = new DirectoryInfo(fullPath).LastWriteTime, Status = UpdateStatus.Update });
            }
        }

        private void ModifySyncStatus(string status, long bytesTransferred, long bytesTotal, string key)
        {
            if (key.Length > 10)
                _syncStatus.Enqueue(status + " - " + key.Substring(0, 10) + "... " + Math.Round((Convert.ToDouble(bytesTransferred) / Convert.ToDouble(bytesTotal)) * 100, 0) + @"%");
            else
                _syncStatus.Enqueue(status + " - " + key + "... " + Math.Round((Convert.ToDouble(bytesTransferred) / Convert.ToDouble(bytesTotal)) * 100, 0) + @"%");
            Application.DoEvents();
            return;
            //if (bytesTransferred != 0)
            //{
            //    string value = string.Empty;
            //    if (bytesTransferred != bytesTotal)
            //    {
            //        if (bytesTotal >= 1048576)
            //        {
            //            var completed = Math.Round(Convert.ToDouble(bytesTransferred / 1048576), 4);
            //            var total = Math.Round(Convert.ToDouble(bytesTotal / 1048576), 4);
            //            var percentage = Math.Round((completed / total) * 100, 2);
            //            if (percentage != 0 && percentage % 20 == 0 && !_syncStatusDictionary.Contains(key + "_" + percentage))
            //            {
            //                _syncStatusDictionary.Add(key + "_" + percentage);
            //                value = status + " " + completed + "MB out of  " + total + "MB - " + key;
            //            }
            //        }
            //        /*else if (bytesTotal >= 1024)
            //        {
            //            var completed = Math.Round(Convert.ToDouble(bytesTransferred / 1024), 2);
            //            var total = Math.Round(Convert.ToDouble(bytesTotal / 1024), 2);
            //            var percentage = Math.Round((completed / total) * 100, 0);
            //            if (percentage != 0 && percentage % 50 == 0 && !_syncStatusDictionary.Contains(key + "_" + percentage))
            //            {
            //                _syncStatusDictionary.Add(key + "_" + percentage);
            //                value = status + " " + completed + "KB out of  " + total + "KB - " + key;
            //            }
            //        }
            //        else
            //        {
            //            var percentage = Math.Round(Convert.ToDouble(bytesTransferred / bytesTotal) * 100, 0);
            //            if (percentage % 99 == 0 && !_syncStatusDictionary.Contains(key + "_" + percentage))
            //            {
            //                _syncStatusDictionary.Add(key + "_" + percentage);
            //                value = status + " " + bytesTransferred + "Bytes out of  " + bytesTotal + "Bytes - " + key;
            //            }
            //        }*/
            //    }
            //    else
            //    {
            //        value = status + " " + key;
            //        // clear the SysStatusDictionary
            //        while (true)
            //        {
            //            bool iskeyremoved = false;
            //            foreach (string keyString in _syncStatusDictionary)
            //            {
            //                if (keyString.IndexOf(key) == 0)
            //                {
            //                    _syncStatusDictionary.Remove(keyString);
            //                    iskeyremoved = true;
            //                    break;
            //                }
            //            }
            //            if (iskeyremoved == false)
            //                break;
            //        }
            //    }
            //    if (!string.IsNullOrEmpty(value) && !_syncStatus.Contains(value))
            //        _syncStatus.Enqueue(value);
            //}
            //Application.DoEvents();
        }

        private void TimerStatusUpdateTick(object sender, EventArgs e)
        {
            if (_syncStatus.Count != 0)
            {
                if (hideNotificationsToolStripMenuItem.Checked)
                {
                    notification.SetMessage(_syncStatus.Dequeue().ToString());
                    var thread = new Thread(Hidenotification);
                    thread.Start();
                }
                //VersaVaultNotifications.ShowBalloonTip(10, string.Empty, _syncStatus.Dequeue().ToString(),ToolTipIcon.Info);
            }
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
                timer_status_update.Interval = 50;
        }

        private void Hidenotification()
        {
            notification.ShowForm(10);
            if (_syncStatus.Count == 0)
                notification.HideForm(10);
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
                        if (fileQ.Type != WatcherChangeTypes.Deleted)
                        {
                            if (File.Exists(obj.ToString()))
                            {
                                if (new FileInfo(obj.ToString()).Length != 0)
                                    DoObjectSync(fileQ.Type, obj.ToString(), fileQ.Name, fileQ.OldFullpath, fileQ.OldName, null);
                                else
                                    _fileQueue.Add(obj.ToString(), fileQ);
                            }
                        }
                        else
                            DoObjectSync(fileQ.Type, obj.ToString(), fileQ.Name, fileQ.OldFullpath, fileQ.OldName, null);

                        break;
                    }
                    if (_fileQueue.Count == 0)
                        break;
                    Application.DoEvents();
                }
            }
            _isThreadStarted = false;
        }

        private void ProcessApplicationUpdates(AppUpdateInfo appUpdateInfo, bool IsShared, string Username)
        {
            try
            {
                //if (_applicationUpates.Count > 0)
                //{
                //var appUpdateInfo = (AppUpdateInfo)_applicationUpates.Dequeue();
                string url = Utilities.DevelopmentMode ? "http://localhost:3000" : "http://versavault.com";
                if (!IsShared)
                {
                    switch (appUpdateInfo.Status)
                    {
                        case UpdateStatus.Add:
                            {
                                url += "/api/add_files?bucket_key=" + Utilities.MyConfig.BucketKey + "&key=" +
                                       appUpdateInfo.Key + "&last_modified=" +
                                       appUpdateInfo.LastModifiedTime.ToUniversalTime().ToString(
                                           "yyyy-MM-dd HH:mm:ss tt") + "&machine_key=" +
                                       Utilities.MyConfig.MachineKey;
                                break;
                            }
                        case UpdateStatus.Update:
                            {
                                url += "/api/update_files?bucket_key=" + Utilities.MyConfig.BucketKey + "&key=" +
                                       appUpdateInfo.Key + "&last_modified=" +
                                       appUpdateInfo.LastModifiedTime.ToUniversalTime().ToString(
                                           "yyyy-MM-dd HH:mm:ss tt") + "&machine_key=" +
                                       Utilities.MyConfig.MachineKey;
                                break;
                            }
                        case UpdateStatus.Delete:
                            {
                                url += "/api/delete_files?bucket_key=" + Utilities.MyConfig.BucketKey + "&key=" +
                                       appUpdateInfo.Key + "&machine_key=" + Utilities.MyConfig.MachineKey;
                                break;
                            }
                    }
                }
                else
                {
                    switch (appUpdateInfo.Status)
                    {
                        case UpdateStatus.Add:
                            {
                                url += "/api/add_files?shared=" + IsShared + "&key=" + appUpdateInfo.Key + "&last_modified=" + appUpdateInfo.LastModifiedTime.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss tt") + "&username=" + Username;
                                break;
                            }
                        case UpdateStatus.Update:
                            {
                                url += "/api/update_files?shared=" + IsShared + "&key=" + appUpdateInfo.Key + "&last_modified=" + appUpdateInfo.LastModifiedTime.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss tt") + "&username=" + Username;
                                break;
                            }
                        case UpdateStatus.Delete:
                            {
                                url += "/api/delete_files?shared=" + IsShared + "&key=" + appUpdateInfo.Key + "&last_modified=" + appUpdateInfo.LastModifiedTime.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss tt") + "&username=" + Username;
                                break;
                            }
                    }
                }
                GetResponse(url);
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
                                        if (!Utilities.IsFileUsedbyAnotherProcess(fullPath) && new FileInfo(fullPath).Length != 0)
                                        {
                                            // get the file last modified from database
                                            string relativePath = fullPath.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                                            string url = Utilities.DevelopmentMode
                                                             ? "http://localhost:3000"
                                                             : "http://versavault.com";
                                            if (relativePath.IndexOf("shared", StringComparison.OrdinalIgnoreCase) == 0)
                                                url += "/api/get_object_status?bucket_key=" + Utilities.MyConfig.BucketKey + "&key=" + relativePath + "&machine_key=" + Utilities.MyConfig.MachineKey + "&shared=" + true;
                                            else
                                                url += "/api/get_object_status?bucket_key=" + Utilities.MyConfig.BucketKey + "&key=" + relativePath + "&machine_key=" + Utilities.MyConfig.MachineKey + "&shared=" + false;
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
                                                                    TimeSpan timeSpan = new DirectoryInfo(fullPathSub).LastWriteTime.ToUniversalTime().Subtract(s3ObjSub.LastModified);
                                                                    if (Math.Floor(timeSpan.TotalSeconds) != 0)
                                                                    {
                                                                        //relativePath = fullPathSub.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                                                                        //_applicationUpates.Enqueue(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPathSub).LastWriteTime, Status = UpdateStatus.Update });
                                                                        _syncStatus.Enqueue("Uploading file content......");
                                                                        Addobject(fullPath);
                                                                    }
                                                                }
                                                                catch (Exception)
                                                                {
                                                                    // Todo
                                                                    // need to fix this when an error occured
                                                                    continue;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    try
                                                    {
                                                        if (!Utilities.IsFileUsedbyAnotherProcess(fullPath) && new FileInfo(fullPath).Length != 0)
                                                        {
                                                            //relativePath = fullPath.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                                                            //_applicationUpates.Enqueue(new AppUpdateInfo { Key = relativePath.Replace("\\", "/"), LastModifiedTime = new FileInfo(fullPath).LastWriteTime, Status = UpdateStatus.Update });
                                                            _syncStatus.Enqueue("Uploading file content......");
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
                                                        return;
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
                                    if (attribute != FileAttributes.Directory)
                                    {
                                        if (!_processingFiles.Contains(fullPath))
                                        {
                                            if (!Utilities.IsFileUsedbyAnotherProcess(fullPath) && new FileInfo(fullPath).Length != 0)
                                            {
                                                // get the file last modified from database
                                                string relativePath = fullPath.Replace(Utilities.Path + "\\", "").Replace("\\", "/");
                                                string url = Utilities.DevelopmentMode ? "http://localhost:3000" : "http://versavault.com";
                                                if (relativePath.IndexOf("shared", StringComparison.OrdinalIgnoreCase) == 0)
                                                    url += "/api/get_object_status?bucket_key=" + Utilities.MyConfig.BucketKey + "&key=" + relativePath + "&machine_key=" + Utilities.MyConfig.MachineKey + "&shared=" + true;
                                                else
                                                    url += "/api/get_object_status?bucket_key=" + Utilities.MyConfig.BucketKey + "&key=" + relativePath + "&machine_key=" + Utilities.MyConfig.MachineKey + "&shared=" + false;
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
                                                                        TimeSpan timeSpan = new DirectoryInfo(fullPathSub).LastWriteTime.ToUniversalTime().Subtract(s3ObjSub.LastModified);
                                                                        if (Math.Floor(timeSpan.TotalSeconds) != 0)
                                                                        {
                                                                            _syncStatus.Enqueue("Uploading file content......");
                                                                            Addobject(fullPath);
                                                                        }
                                                                    }
                                                                    catch (Exception)
                                                                    {
                                                                        // Todo
                                                                        // need to fix this when an error occured
                                                                        continue;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        _syncStatus.Enqueue("Uploading file content......");
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
                        _syncStatus.Enqueue("Deleting -" + relativePath);
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
                                    if (!Utilities.IsFileUsedbyAnotherProcess(fullPath))
                                    {
                                        if (new FileInfo(fullPath).Length != 0)
                                        {
                                            _syncStatus.Enqueue("Renaming a file...");
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
                                    _syncStatus.Enqueue("Renaming a folder...");
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

        private void CloseWindowClick(object sender, EventArgs e)
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
            TopMost = true;
            Application.DoEvents();
            Focus();
            Application.DoEvents();
            BringToFront();
            Application.DoEvents();
            TopMost = false;
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

        private void CloseWindowMouseLeave(object sender, EventArgs e)
        {
            closeWindow.Image = Properties.Resources.closeDefault;
        }

        private void CloseWindowMouseEnter(object sender, EventArgs e)
        {
            closeWindow.Image = Properties.Resources.closeHover;
        }

        //const and dll functions for moving form
        public const int WmNclbuttondown = 0xA1; public const int HtCaption = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        private void PictureBox1MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(Handle, WmNclbuttondown, HtCaption, 0);
        }

        private void ConnectBtnMouseEnter(object sender, EventArgs e)
        {
            connectBtn.Image = Properties.Resources.connectButton_Hover;
        }

        private void ConnectBtnMouseLeave(object sender, EventArgs e)
        {
            connectBtn.Image = Properties.Resources.connectButton_Normal;
        }

        private void ConnectBtnMouseDown(object sender, MouseEventArgs e)
        {
            connectBtn.Image = Properties.Resources.connectButton_Activel;
        }

        private void ConnectBtnMouseUp(object sender, MouseEventArgs e)
        {
            connectBtn.Image = Properties.Resources.connectButton_Hover;
        }

        private static void CheckForUpdatesToolStripMenuItemClick(object sender, EventArgs e)
        {
            Program.CheckVersion(true);
        }

        private void ResyncToolStripMenuItemClick(object sender, EventArgs e)
        {
            StartResync();
        }

        private void StartResync()
        {
            _syncStatus.Enqueue("Started synchronization process");
            startSyncToolStripMenuItem.Text = @"Pause Sync";
            Visible = false;
            Opacity = 0;
            var thread = new Thread(StartAmazonFilesSync);
            thread.Start();
        }

        private void HideNotificationsToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (hideNotificationsToolStripMenuItem.Checked)
                notification.ShowForm(20);
            else
                notification.HideForm(20);
        }

        private static void WaitUntilFileBusy(string filePath)
        {
            while (Utilities.IsFileUsedbyAnotherProcess(filePath))
            {
                Application.DoEvents();
                Thread.Sleep(3000);
                Application.DoEvents();
            }
        }

        // Config file methods
        private static void RemoveConfig(string relativePath, string bucketkey)
        {
            try
            {
                if (!File.Exists(Utilities.ConfigPath))
                    File.Create(Utilities.ConfigPath);
                WaitUntilFileBusy(Utilities.ConfigPath);
                using (var stream = new StreamReader(Utilities.ConfigPath))
                {
                    string fileContent = stream.ReadToEnd();
                    var objs = JsonConvert.DeserializeObject<List<ObjectInfo>>(fileContent);
                    if (objs != null && objs.Count > 0)
                    {
                        foreach (ObjectInfo objectInfo in objs)
                        {
                            if (objectInfo.Bucketkey.ToLower() == bucketkey.ToLower() &&
                                objectInfo.RelativePath.ToLower() == relativePath.ToLower())
                            {
                                objs.Remove(objectInfo);
                                break;
                            }
                        }
                    }
                    stream.Close();
                    stream.Dispose();
                    WaitUntilFileBusy(Utilities.ConfigPath);
                    File.Delete(Utilities.ConfigPath);
                    using (var streamwriter = new StreamWriter(Utilities.ConfigPath, true))
                    {
                        streamwriter.Write(JsonConvert.SerializeObject(objs));
                    }
                }
            }
            catch (Exception)
            {
                //ToDo Need to fix
                return;
            }
        }

        private static LastUploadedPartDetail GetLastUploadedPartResponse(string relativePath, string bucketkey, List<UploadPartResponse> uploadPartResponses)
        {
            if (File.Exists(Utilities.ConfigPath))
            {
                WaitUntilFileBusy(Utilities.ConfigPath);
                using (var stream = new StreamReader(Utilities.ConfigPath))
                {
                    string fileContent = stream.ReadToEnd();
                    var objs = JsonConvert.DeserializeObject<List<ObjectInfo>>(fileContent);
                    foreach (ObjectInfo objectInfo in objs)
                    {
                        if (objectInfo.Bucketkey.ToLower() == bucketkey.ToLower() &&
                               objectInfo.RelativePath.ToLower() == relativePath.ToLower())
                        {
                            if (objectInfo.UploadPartResponses.Count > 0)
                            {
                                uploadPartResponses.AddRange(objectInfo.UploadPartResponses);
                                return new LastUploadedPartDetail
                                           {
                                               LastPartNumber = objectInfo.UploadPartResponses[objectInfo.UploadPartResponses.Count - 1].PartNumber,
                                               UploadId = objectInfo.UploadId,
                                               LastModified = objectInfo.LastModified,
                                               BucketKey = objectInfo.Bucketkey
                                           };
                            }
                        }
                    }
                }
            }
            return new LastUploadedPartDetail { LastPartNumber = 0, UploadId = string.Empty };
        }

        private static void WriteResponseToFile(string relativePath, string bucketkey, string uploadId, DateTime lastModified, UploadPartResponse uploadPartResponse)
        {
            try
            {
                if (!File.Exists(Utilities.ConfigPath))
                    File.Create(Utilities.ConfigPath);
                WaitUntilFileBusy(Utilities.ConfigPath);
                using (var stream = new StreamReader(Utilities.ConfigPath))
                {
                    string fileContent = stream.ReadToEnd();
                    var objs = JsonConvert.DeserializeObject<List<ObjectInfo>>(fileContent);
                    bool isObjectFound = false;
                    if (objs != null && objs.Count > 0)
                    {
                        foreach (ObjectInfo objectInfo in objs)
                        {
                            if (objectInfo.Bucketkey.ToLower() == bucketkey.ToLower() &&
                                objectInfo.RelativePath.ToLower() == relativePath.ToLower() &&
                                objectInfo.UploadId.ToLower() == uploadId.ToLower())
                            {
                                objectInfo.UploadPartResponses.Add(uploadPartResponse);
                                isObjectFound = true;
                                break;
                            }
                        }
                    }
                    if (!isObjectFound)
                    {
                        if (objs == null)
                            objs = new List<ObjectInfo>();
                        var objInfo = new ObjectInfo
                                          {
                                              Bucketkey = bucketkey,
                                              RelativePath = relativePath,
                                              UploadId = uploadId,
                                              LastModified = lastModified
                                          };
                        objInfo.UploadPartResponses.Add(uploadPartResponse);
                        objs.Add(objInfo);
                    }
                    stream.Close();
                    stream.Dispose();
                    WaitUntilFileBusy(Utilities.ConfigPath);
                    File.Delete(Utilities.ConfigPath);
                    using (var streamwriter = new StreamWriter(Utilities.ConfigPath, true))
                    {
                        streamwriter.Write(JsonConvert.SerializeObject(objs));
                    }
                }
            }
            catch (Exception)
            {
                //ToDo Need to fix
                return;
            }
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

    internal struct LastUploadedPartDetail
    {
        public string UploadId;
        public int LastPartNumber;
        public DateTime LastModified;
        public string BucketKey;
    }

    enum UpdateStatus
    {
        Add,
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