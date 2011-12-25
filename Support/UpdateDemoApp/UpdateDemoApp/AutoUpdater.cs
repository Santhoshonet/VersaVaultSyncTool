using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;

namespace UpdateDemoApp
{
    public partial class AutoUpdater : Component
    {
        public AutoUpdater()
        {
            InitializeComponent();
        }

        public AutoUpdater(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }

        [DefaultValue(false), Description("Set to True if you want to use http proxy."), Category("AutoUpdater Configuration")]
        public bool ProxyEnabled { get; set; }

        //Added 11/16/2004 For Proxy Clients, Thanks George for submitting these changes
        [DefaultValue(@"http://myproxy.com:8080/"), Description("The Proxy server URL.(For example:http://myproxy.com:port)"), Category("AutoUpdater Configuration")]
        public string ProxyURL { get; set; }

        [DefaultValue(@""), Description("The UserName to authenticate with."), Category("AutoUpdater Configuration")]
        public string LoginUserName { get; set; }

        [DefaultValue(@""), Description("The Password to authenticate with."), Category("AutoUpdater Configuration")]
        public string LoginUserPass { get; set; }

        [DefaultValue(@"http://localhost/UpdateConfig.xml"), Description("The URL Path to the configuration file."), Category("AutoUpdater Configuration")]
        public string ConfigURL { get; set; }

        [DefaultValue(true), Description("Set to True if you want the app to restart automatically, set to False if you want to use the DownloadForm to prompt the user, if AutoDownload is false and DownloadForm is null, the app will not download the latest version."), Category("AutoUpdater Configuration")]
        public bool AutoDownload { get; set; }

        public Form DownloadForm { get; set; }

        [DefaultValue(false), Description("Set to True if you want the app to restart automatically, set to False if you want to use the RestartForm to prompt the user, if AutoRestart is false and RestartForm is null, the app will not restart."), Category("AutoUpdater Configuration")]
        public bool AutoRestart { get; set; }

        public Form RestartForm { get; set; }

        [BrowsableAttribute(false)]
        public string LatestConfigChanges
        {
            get
            {
                string stRet = null;
                //Protect against NPE's
                if (_autoUpdateConfig != null)
                    stRet = _autoUpdateConfig.LatestChanges;
                return stRet;
            }
        }

        [BrowsableAttribute(false)]
        public Version CurrentAppVersion
        { get { return System.Reflection.Assembly.GetEntryAssembly().GetName().Version; } }

        [BrowsableAttribute(false)]
        public Version LatestConfigVersion
        {
            get
            {
                Version versionRet = null;
                //Protect against NPE's
                if (_autoUpdateConfig != null)
                    versionRet = new Version(_autoUpdateConfig.AvailableVersion);
                return versionRet;
            }
        }

        [BrowsableAttribute(false)]
        public bool NewVersionAvailable
        { get { return LatestConfigVersion > CurrentAppVersion; } }

        private AutoUpdateConfig _autoUpdateConfig;

        [BrowsableAttribute(false)]
        public AutoUpdateConfig AutoUpdateConfig
        { get { return _autoUpdateConfig; } }

        public delegate void ConfigFileDownloaded(bool bNewVersionAvailable);
        public event ConfigFileDownloaded OnConfigFileDownloaded;

        public delegate void AutoUpdateComplete();
        public event AutoUpdateComplete OnAutoUpdateComplete;

        public delegate void AutoUpdateError(string stMessage, Exception e);
        public event AutoUpdateError OnAutoUpdateError;

        /// <summary>
        /// TryUpdate: Invoke this method if you just want to load the config without autoupdating
        /// </summary>
        public void LoadConfig()
        {
            var backgroundLoadConfigThread = new Thread(LoadConfigThread) { IsBackground = true };
            backgroundLoadConfigThread.Start();
        }//TryUpdate()

        /// <summary>
        /// loadConfig: This method just loads the config file so the app can check the versions manually
        /// </summary>
        private void LoadConfigThread()
        {
            var config = new AutoUpdateConfig();
            config.OnLoadConfigError += config_OnLoadConfigError;

            //For using untrusted SSL Certificates
#pragma warning disable 612,618
            ServicePointManager.CertificatePolicy = new TrustAllCertificatePolicy();
#pragma warning restore 612,618

            //Do the load of the config file
            if (config.LoadConfig(ConfigURL, LoginUserName, LoginUserPass, ProxyURL, ProxyEnabled))
            {
                _autoUpdateConfig = config;
                if (OnConfigFileDownloaded != null)
                {
                    OnConfigFileDownloaded(NewVersionAvailable);
                }
            }
            //else
            //	MessageBox.Show("Problem loading config file, from: " + this.ConfigURL);
        }

        /// <summary>
        /// TryUpdate: Invoke this method when you are ready to run the update checking thread
        /// </summary>
        public void TryUpdate()
        {
            var backgroundThread = new Thread(UpdateThread) { IsBackground = true };
            backgroundThread.Start();
        }//TryUpdate()

        /// <summary>
        /// updateThread: This is the Thread that runs for checking updates against the config file
        /// </summary>
        private void UpdateThread()
        {
            const string stUpdateName = "update";
            if (_autoUpdateConfig == null)//if we haven't already downloaded the config file, do so now
                LoadConfigThread();
            if (_autoUpdateConfig != null)//make sure we were able to download it
            {
                //Check the file for an update
                if (LatestConfigVersion > CurrentAppVersion)
                {
                    //Download file if the user requests or AutoDownload is True
                    if (AutoDownload || (DownloadForm != null && DownloadForm.ShowDialog() == DialogResult.Yes))
                    {
                        //MessageBox.Show("New Version Available, New Version: " + vConfig.ToString() + "\r\nDownloading File from: " + config.AppFileURL);
                        // ReSharper disable AssignNullToNotNullAttribute
                        var diDest = new DirectoryInfo(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location));
                        // ReSharper restore AssignNullToNotNullAttribute
                        if (diDest.Parent != null)
                        {
                            string stPath = diDest.Parent.FullName + Path.DirectorySeparatorChar + stUpdateName + ".zip";
                            //There is a new version available
                            if (DownloadFile(_autoUpdateConfig.AppFileUrl, stPath))
                            {
                                //MessageBox.Show("Downloaded New File");
                                string stDest = diDest.Parent.FullName + Path.DirectorySeparatorChar + stUpdateName + Path.DirectorySeparatorChar;
                                //Extract Zip File
                                Unzip(stPath, stDest);
                                //Delete Zip File
                                File.Delete(stPath);
                                if (OnAutoUpdateComplete != null)
                                {
                                    OnAutoUpdateComplete();
                                }
                                //Restart App if Necessary
                                //If true, the app will restart automatically, if false the app will use the RestartForm to prompt the user, if RestartForm is null, it doesn't restart
                                if (AutoRestart || (RestartForm != null && RestartForm.ShowDialog() == DialogResult.Yes))
                                    Restart();
                                //else don't restart
                            }
                        }
                        //else
                        //	MessageBox.Show("Didn't Download File");
                    }
                }
                //else
                //	MessageBox.Show("No New Version Available, Web Version: " + vConfig.ToString() + ", Current Version: " +  vCurrent.ToString());
            }
        }//updateThread()

        /// <summary>
        /// downloadFile: Download a file from the specified url and copy it to the specified path
        /// </summary>
        private bool DownloadFile(string url, string path)
        {
            try
            {
                //create web request/response

                var request = (HttpWebRequest)WebRequest.Create(url);
                //Request.Headers.Add("Translate: f"); //Commented out 11/16/2004 Matt Palmerlee, this Header is more for DAV and causes a known security issue
                request.Credentials = !string.IsNullOrEmpty(LoginUserName) ? new NetworkCredential(LoginUserName, LoginUserPass) : CredentialCache.DefaultCredentials;

                //Added 11/16/2004 For Proxy Clients, Thanks George for submitting these changes
                if (ProxyEnabled)
                    request.Proxy = new WebProxy(ProxyURL);

                var response = (HttpWebResponse)request.GetResponse();

                Stream respStream = response.GetResponseStream();

                //Do the Download
                var buffer = new byte[4096];

                FileStream fs = File.Open(path, FileMode.Create, FileAccess.Write);

                if (respStream != null)
                {
                    int length = respStream.Read(buffer, 0, 4096);
                    while (length > 0)
                    {
                        fs.Write(buffer, 0, length);
                        length = respStream.Read(buffer, 0, 4096);
                    }
                }
                fs.Close();
            }
            catch (Exception e)
            {
                string stMessage = "Problem downloading and copying file from: " + url + " to: " + path;
                //MessageBox.Show(stMessage);
                if (File.Exists(path))
                    File.Delete(path);
                SendAutoUpdateError(stMessage, e);
                return false;
            }
            return true;
        }//downloadFile(string url, string path)

        /// <summary>
        /// unzip: Open the zip file specified by stZipPath, into the stDestPath Directory
        /// </summary>
        private static void Unzip(string stZipPath, string stDestPath)
        {
            var s = new ZipInputStream(File.OpenRead(stZipPath));

            ZipEntry theEntry;
            while ((theEntry = s.GetNextEntry()) != null)
            {
                string fileName = stDestPath + Path.GetDirectoryName(theEntry.Name) + Path.DirectorySeparatorChar + Path.GetFileName(theEntry.Name);

                //create directory for file (if necessary)
                // ReSharper disable AssignNullToNotNullAttribute
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                // ReSharper restore AssignNullToNotNullAttribute

                if (!theEntry.IsDirectory)
                {
                    FileStream streamWriter = File.Create(fileName);

                    var data = new byte[2048];
                    try
                    {
                        while (true)
                        {
                            int size = s.Read(data, 0, data.Length);
                            if (size > 0)
                            {
                                streamWriter.Write(data, 0, size);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    catch
                    {
                        streamWriter.Close();
                    }
                    finally
                    {
                        streamWriter.Close();
                    }
                }
            }
            s.Close();
        }//unzip(string stZipPath, string stDestPath)

        /// <summary>
        /// restart: Restart the app, the AppStarter will be responsible for actually restarting the main application.
        /// </summary>
        private static void Restart()
        {
            Environment.ExitCode = 2; //the surrounding AppStarter must look for this to restart the app.
            Application.Exit();
        }//restart()

        private void config_OnLoadConfigError(string stMessage, Exception e)
        {
            SendAutoUpdateError(stMessage, e);
        }

        private void SendAutoUpdateError(string stMessage, Exception e)
        {
            if (OnAutoUpdateError != null)
                OnAutoUpdateError(stMessage, e);
        }
    }//class AutoUpdater

    public class TrustAllCertificatePolicy : ICertificatePolicy
    {
        public bool CheckValidationResult(ServicePoint sp,
            System.Security.Cryptography.X509Certificates.X509Certificate cert, WebRequest req, int problem)
        {
            return true;
        }
    }//class TrustAllCertificatePolicy
}