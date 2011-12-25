using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace UpdateDemoApp
{
    public class AutoUpdateStarter
    {
        private readonly string _executablePath;
        private readonly string _updatePath;

        public AutoUpdateStarter()
        {
            string stConfigFileName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (stConfigFileName != null)
                // ReSharper disable AssignNullToNotNullAttribute
                stConfigFileName = Path.Combine(stConfigFileName, Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location));
            // ReSharper restore AssignNullToNotNullAttribute
            stConfigFileName += @".config";

            AutoUpdateStarterConfig config = AutoUpdateStarterConfig.Load(stConfigFileName);
            _executablePath = config.ApplicationExePath;
            _updatePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "update" + Path.DirectorySeparatorChar;
            StartProcessAndWait();
        }

        public void StartProcessAndWait()
        {
            bool restart = true;
            string commandLineArgs = Environment.GetCommandLineArgs().Aggregate("", (current, arg) => current + ('"' + arg + '"' + " "));
            commandLineArgs = commandLineArgs.Trim();
            while (restart)
            {
                Process mainProcess;
                //Start the app
                try
                {
                    var p = new ProcessStartInfo(_executablePath)
                    {
                        // ReSharper disable AssignNullToNotNullAttribute
                        WorkingDirectory = Path.GetDirectoryName(_executablePath),
                        // ReSharper restore AssignNullToNotNullAttribute
                        Arguments = commandLineArgs
                    };
                    // ReSharper disable AssignNullToNotNullAttribute
                    // ReSharper restore AssignNullToNotNullAttribute
                    mainProcess = Process.Start(p);
                }
                catch (Exception)
                {
                    mainProcess = null;
                }
                if (mainProcess != null)
                {
                    try
                    {
                        mainProcess.WaitForExit();
                    }
                    catch (Exception)
                    {
                        return;
                    }

                    if (mainProcess.ExitCode != 2)
                        restart = false;
                    // ReSharper disable AssignNullToNotNullAttribute
                    if (Directory.Exists(Path.GetDirectoryName(_executablePath)) && Directory.Exists(Path.GetDirectoryName(_updatePath)) && !File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "update.zip")))
                    // ReSharper restore AssignNullToNotNullAttribute
                    {
                        //This is how we used to do it: Delete the old directory and then move(rename) the new one
                        //Directory.Delete(Path.GetDirectoryName(this.executablePath), true);
                        //Directory.Move(this.updatePath, Path.GetDirectoryName(this.executablePath));

                        //Now we just move the new files in the update directory and then delete it
                        MoveDirectoryFiles(_updatePath, Path.GetDirectoryName(_executablePath));
                        Directory.Delete(_updatePath, true);
                    }
                }
            }//while(restart)
        }//StartProcessAndWait()

        private static void MoveDirectoryFiles(string stSourcePath, string stDestPath)
        {
            if (!stSourcePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                stSourcePath = stSourcePath + Path.DirectorySeparatorChar;
            if (!stDestPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                stDestPath = stDestPath + Path.DirectorySeparatorChar;

            foreach (string stFile in Directory.GetFiles(stSourcePath))
            {
                string stFileName = Path.GetFileName(stFile);
                try
                {
                    if (File.Exists(stDestPath + stFileName))
                        File.Delete(stDestPath + stFileName);
                    File.Move(stSourcePath + stFileName, stDestPath + stFileName);
                }
                catch (Exception)
                {
                    continue;
                }
            }
            foreach (string stDir in Directory.GetDirectories(stSourcePath))
            {
                string stDirName = Path.GetFileName(stDir);
                try
                {
                    if (!Directory.Exists(stDestPath + stDirName))
                        Directory.CreateDirectory(stDestPath + stDirName);
                    MoveDirectoryFiles(stSourcePath + stDirName, stDestPath + stDirName);
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }
    }
}