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
    }

    public class account
    {
        public string error { get; set; }

        public string bucket_id { get; set; }
    }
}