using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Office.Interop.Word;
using VersaVaultLibrary;

namespace VersaVaultSearchUtility
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                //if (args.Length < 2)
                //    args = new[] { @"C:\Users\Santhosh\Desktop\test1.docx", "999" };
                // return;
                string file = args[0].Replace("%20", " ");
                int id = Convert.ToInt32(args[1]);
                if (File.Exists(file) && !Utilities.IsFileUsedbyAnotherProcess(file))
                {
                    switch (Path.GetExtension(file))
                    {
                        case ".doc":
                        case ".docx":
                            {
                                var word = new ApplicationClass();
                                object miss = Missing.Value;
                                object path = file;
                                object readOnly = false;
                                var docs = word.Documents.Open(ref path, ref miss, ref readOnly, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss);
                                try
                                {
                                    docs.ActiveWindow.Selection.WholeStory();
                                    docs.ActiveWindow.Selection.Copy();
                                    if (Clipboard.ContainsText())
                                    {
                                        var data = Clipboard.GetText(TextDataFormat.Text);
                                        string url = Utilities.DevelopmentMode ? "http://localhost:3000" : "http://versavault.com";
                                        url += "/api/update_content?id=" + id + "&content=" + data;
                                        Utilities.GetResponse(url);
                                    }
                                }
                                catch (Exception)
                                {
                                    docs.Close(ref miss, ref miss, ref miss);
                                }
                                finally
                                {
                                    if (docs != null)
                                        docs.Close(ref miss, ref miss, ref miss);
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
    }
}