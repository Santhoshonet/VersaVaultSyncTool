using System;
using System.Diagnostics;
using System.IO;

namespace Uninstaller
{
    class Program
    {
        private static void Main()
        {
            try
            {
                var syncToolProcess = GetProcess("VersaVaultSyncTool");
                if (syncToolProcess != null)
                    syncToolProcess.Kill();
            }
            catch (Exception)
            {
                return;
            }
        }

        private static Process GetProcess(string name)
        {
            var process = Process.GetProcessesByName(name);
            if (process.Length > 0)
                return process[0];
            return null;
        }

        private static void Removesetting()
        {
            var process = new Process();
            string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var stratInfo = new ProcessStartInfo
                                {
                                    WindowStyle = ProcessWindowStyle.Hidden,
                                    FileName = path + "\\VersaVaultSyncTool.exe ",
                                    Arguments = " remove_config"
                                };
            process.StartInfo = stratInfo;
            process.Start();
        }
    }
}