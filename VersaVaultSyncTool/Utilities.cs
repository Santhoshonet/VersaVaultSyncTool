using System;
using System.Configuration;
using Microsoft.Win32;

namespace VersaVaultSyncTool
{
    public class Utilities
    {
        public static string AwsAccessKey = "AKIAIW36YM46YELZCT3A";
        public static string AwsSecretKey = "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";
        public static bool DevelopmentMode = true;
        public static string Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VersaVault");
        public static string AppPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VersaVault");
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
                        key.SetValue("VersaVault", "\"" + applicationExecutablePath + "\"");
                    else
                    {
                        key.DeleteValue("VersaVault");
                    }
                    key.Close();
                }
            }
            catch (Exception)
            {
                return;
            }
        }
    }

    public sealed class Myconfiguration : ApplicationSettingsBase
    {
        [UserScopedSetting()]
        [DefaultSettingValueAttribute("")]
        public string BucketKey
        {
            get { return (string)this["BucketKey"]; }
            set { this["BucketKey"] = value; }
        }

        [UserScopedSetting()]
        [DefaultSettingValueAttribute("")]
        public string Username
        {
            get { return (string)this["Username"]; }
            set { this["Username"] = value; }
        }

        [UserScopedSetting()]
        [DefaultSettingValueAttribute("")]
        public string Password
        {
            get { return (string)this["Password"]; }
            set { this["Password"] = value; }
        }
    }

    public class account
    {
        public string error { get; set; }

        public string bucket_id { get; set; }
    }

    public class s3__object
    {
        public s3object s3_object { get; set; }
    }

    public class s3object
    {
        public int authentication_id { get; set; }

        public string content_length { get; set; }

        public DateTime created_at { get; set; }

        public string fileName { get; set; }

        public bool folder { get; set; }

        public int id { get; set; }

        public string key { get; set; }

        public DateTime lastModified { get; set; }

        public string parent { get; set; }

        public string parent_uid { get; set; }

        public bool rootFolder { get; set; }

        public string uid { get; set; }

        public DateTime updated_at { get; set; }

        public string url { get; set; }
    }
}