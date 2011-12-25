using System;
using System.IO;
using System.Net;
using System.Xml;

namespace UpdateDemoApp
{
    public class AutoUpdateConfig
    {
        public string AvailableVersion { get; set; }

        public string AppFileUrl { get; set; }

        public string LatestChanges { get; set; }

        public string ChangeLogUrl { get; set; }

        public delegate void LoadConfigError(string stMessage, Exception e);

        public event LoadConfigError OnLoadConfigError;

        /// <summary>
        /// LoadConfig: Invoke this method when you are ready to populate this object
        /// </summary>
        public bool LoadConfig(string url, string user, string pass, string proxyUrl, bool proxyEnabled)
        {
            try
            {
                //Load the xml config file
                var xmlDoc = new XmlDocument();
                //Retrieve the File

                var request = (HttpWebRequest)WebRequest.Create(url);
                //Request.Headers.Add("Translate: f"); //Commented out 11/16/2004 Matt Palmerlee, this Header is more for DAV and causes a known security issue
                request.Credentials = !string.IsNullOrEmpty(user) ? new NetworkCredential(user, pass) : CredentialCache.DefaultCredentials;

                //Added 11/16/2004 For Proxy Clients, Thanks George for submitting these changes
                if (proxyEnabled)
                    request.Proxy = new WebProxy(proxyUrl, true);

                var response = (HttpWebResponse)request.GetResponse();

                Stream respStream = response.GetResponseStream();

                //Load the XML from the stream
                if (respStream != null) xmlDoc.Load(respStream);

                //Parse out the AvailableVersion
                XmlNode availableVersionNode = xmlDoc.SelectSingleNode(@"//AvailableVersion");
                if (availableVersionNode != null) AvailableVersion = availableVersionNode.InnerText;

                //Parse out the AppFileURL
                XmlNode appFileUrlNode = xmlDoc.SelectSingleNode(@"//AppFileURL");
                if (appFileUrlNode != null) AppFileUrl = appFileUrlNode.InnerText;

                //Parse out the LatestChanges
                XmlNode latestChangesNode = xmlDoc.SelectSingleNode(@"//LatestChanges");
                LatestChanges = latestChangesNode != null ? latestChangesNode.InnerText : "";

                //Parse out the ChangLogURL
                XmlNode changeLogURLNode = xmlDoc.SelectSingleNode(@"//ChangeLogURL");
                ChangeLogUrl = changeLogURLNode != null ? changeLogURLNode.InnerText : "";
            }
            catch (Exception e)
            {
                string stMessage = "Failed to read the config file at: " + url + "\r\nMake sure that the config file is present and has a valid format.";
                //MessageBox.Show(stMessage);
                if (OnLoadConfigError != null)
                    OnLoadConfigError(stMessage, e);

                return false;
            }
            return true;
        }//LoadConfig(string url, string user, string pass)
    }
}