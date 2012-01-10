using System;
using System.Configuration;
using Microsoft.Win32;

namespace VersaVaultLibrary
{
    public class Utilities
    {
        public static string AwsAccessKey = "AKIAIW36YM46YELZCT3A";
        public static string AwsSecretKey = "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";
        public static bool DevelopmentMode = false;
        public static string Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VersaVault");
        public static string AppPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VersaVault");
        public static string ConfigPath = System.IO.Path.Combine(AppPath, "VersaVault.config");
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
    }

    public class Account
    {
        public string Error { get; set; }

        public string BucketId { get; set; }
    }
}