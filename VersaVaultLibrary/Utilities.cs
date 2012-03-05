using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Cache;
using Microsoft.Win32;

namespace VersaVaultLibrary
{
    public class Utilities
    {
        public static string AwsAccessKey = "AKIAIW36YM46YELZCT3A";
        public static string AwsSecretKey = "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";
        public static bool DevelopmentMode = true;

        public static string AppRootBucketName
        {
            get
            {
                if (DevelopmentMode)
                    return "VersaVault_Demo";
                return "VersaVault";
            }
        }

        public static string Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), AppRootBucketName);

        public static string AppPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppRootBucketName);

        public static string ConfigPath = System.IO.Path.Combine(AppPath, "VersaVault.config");

        public static string SharedFolderPath = System.IO.Path.Combine(Path, "Shared");

        public static Myconfiguration MyConfig = new Myconfiguration();

        public static void Startup(bool add, string applicationExecutablePath)
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(
                           @"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    if (add)
                        key.SetValue(AppRootBucketName, "\"" + applicationExecutablePath + "\"");
                    else
                    {
                        key.DeleteValue(AppRootBucketName);
                    }
                    key.Close();
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        public static bool IsFileUsedbyAnotherProcess(string filename)
        {
            // Its not a way elegant way, need to find out the better  code
            try
            {
                if (File.Exists(filename))
                {
                    using (FileStream filestream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        filestream.Close();
                        filestream.Dispose();
                    }
                    return false;
                }
                return false;
            }
            catch (IOException)
            {
                return true;
            }
        }

        public static string GetResponse(string strUrl)
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
            catch (Exception)
            {
                return string.Empty;
            }
            finally
            {
                if (objResponse != null)
                    objResponse.Close();
            }
            return strReturn;
        }

        protected static void GetScrapingResponse(IAsyncResult result)
        {
        }
    }

    public sealed class Myconfiguration : ApplicationSettingsBase
    {
        [UserScopedSetting]
        [DefaultSettingValueAttribute("")]
        public string BucketKey
        {
            get { return (string)this["BucketKey"]; }
            set { this["BucketKey"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValueAttribute("")]
        public string MachineKey
        {
            get { return (string)this["MachineKey"]; }
            set { this["MachineKey"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValueAttribute("")]
        public string Username
        {
            get { return (string)this["Username"]; }
            set { this["Username"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValueAttribute("")]
        public string Password
        {
            get { return (string)this["Password"]; }
            set { this["Password"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValueAttribute("")]
        public string VersionId
        {
            get { return (string)this["VersionId"]; }
            set { this["VersionId"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValueAttribute("")]
        public DateTime LastUpdateDate
        {
            get { return (DateTime)this["LastUpdateDate"]; }
            set { this["LastUpdateDate"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValueAttribute("")]
        public string InstallerVersionId
        {
            get { return (string)this["InstallerVersionId"]; }
            set { this["InstallerVersionId"] = value; }
        }
    }

    public class Account
    {
        public string Error { get; set; }

        public string BucketId { get; set; }
    }
}