using System.IO;
using System.Xml;

namespace UpdateDemoApp
{
    public class AutoUpdateStarterConfig
    {
        public string ApplicationFolderName { get; set; }

        public string ApplicationExeName { get; set; }

        private string _configFilePath;

        //calculated/readonly property
        public string ApplicationExePath
        {
            get
            {
                if (_configFilePath != null)
                    // ReSharper disable AssignNullToNotNullAttribute
                    return (string.Format(@"{0}\{1}", Path.Combine(Path.GetDirectoryName(_configFilePath), ApplicationFolderName), ApplicationExeName));
                // ReSharper restore AssignNullToNotNullAttribute
                return null;
            }
            set { _configFilePath = value; }
        }

        //calculated/readonly property
        public string ApplicationPath
        {
            get
            {
                // ReSharper disable AssignNullToNotNullAttribute
                return (string.Format(@"{0}\", Path.Combine(Path.GetDirectoryName(_configFilePath), ApplicationFolderName)));
                // ReSharper restore AssignNullToNotNullAttribute
            }
            set { _configFilePath = value; }
        }

        /// <summary>
        /// Load: Returns a AutoUpdateStarterConfig object based on the xml file specified by filepath parameter
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static AutoUpdateStarterConfig Load(string filePath)
        {
            var config = new AutoUpdateStarterConfig { _configFilePath = filePath };

            try
            {
                //Load the xml config file
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath);

                //Parse out the XML Nodes
                XmlNode pathNode = xmlDoc.SelectSingleNode(@"//ApplicationFolderName");
                if (pathNode != null) config.ApplicationFolderName = pathNode.InnerText;

                XmlNode exeNode = xmlDoc.SelectSingleNode(@"//ApplicationExeName");
                if (exeNode != null) config.ApplicationExeName = exeNode.InnerText;

                return config;
            }
            catch
            {
                return null;
            }
        }//Load(string filePath)
    }
}