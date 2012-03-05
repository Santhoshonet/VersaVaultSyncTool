using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using LitS3;
using VersaVaultLibrary;

namespace Testconsole
{
    class Program
    {
        static void Main()
        {
            var service = new S3Service { AccessKeyID = Utilities.AwsAccessKey, SecretAccessKey = Utilities.AwsSecretKey };
            service.GetObject(Utilities.AppRootBucketName, "FacebookSampleMVC2Appv3.zip", @"C:\Users\Santhosh\Desktop\Facebook.zip");
            UnZipFile(@"C:\Users\Santhosh\Desktop\Facebook.zip");
        }

        public static bool UnZipFile(string inputPathOfZipFile)
        {
            bool ret = true;
            try
            {
                if (File.Exists(inputPathOfZipFile))
                {
                    string baseDirectory = Path.GetDirectoryName(inputPathOfZipFile);

                    using (var zipStream = new ZipInputStream(File.OpenRead(inputPathOfZipFile)))
                    {
                        ZipEntry theEntry;
                        while ((theEntry = zipStream.GetNextEntry()) != null)
                        {
                            if (theEntry.IsFile)
                            {
                                if (theEntry.Name != "")
                                {
                                    string strNewFile = @"" + baseDirectory + @"\" + theEntry.Name;
                                    if (File.Exists(strNewFile))
                                    {
                                        continue;
                                    }
                                    using (FileStream streamWriter = File.Create(strNewFile))
                                    {
                                        var data = new byte[2048];
                                        while (true)
                                        {
                                            int size = zipStream.Read(data, 0, data.Length);
                                            if (size > 0)
                                                streamWriter.Write(data, 0, size);
                                            else
                                                break;
                                        }
                                        streamWriter.Close();
                                    }
                                }
                            }
                            else if (theEntry.IsDirectory)
                            {
                                string strNewDirectory = @"" + baseDirectory + @"\" + theEntry.Name;
                                if (!Directory.Exists(strNewDirectory))
                                {
                                    Directory.CreateDirectory(strNewDirectory);
                                }
                            }
                        }
                        zipStream.Close();
                    }
                }
            }
            catch (Exception)
            {
                ret = false;
            }
            return ret;
        }
    }
}