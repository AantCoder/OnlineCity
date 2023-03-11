using Sidekick.Sidekick.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GuideTestGUI
{
    public class GuideUI : IDisposable
    {
        public Process WorkProcess { get; private set; }
        public bool ProcessIsConnected { get; private set; }
        public string EnvironmentSourceFolderName { get; set; }
        public string EnvironmentWorkFolderName { get; set; }
        public string EnvironmentBackupFolderName { get; set; }
        public bool NeedRecoveryWorkFolder { get; set; }
        public bool ModeCreateEnvironmentSource { get; set; }
        public string LogFileName { get; set; }
        public string ResultTestLogFileName { get; set; }
        public Encoding LogEncoding { get; set; } = Encoding.UTF8;
        public SKMouse Mouse { get; private set; }
        public SKKeybord Keybord { get; private set; }
        public SKProcessCanvas Graphics { get; private set; }
        public ILoaderImage LoaderImage { get; set; }
        public int Width => Mouse.ProcSize.Width;
        public int Height => Mouse.ProcSize.Height;

        public static bool DisableFinishProcess = false;


        private bool EnvironmentOn = false;
        private long LogLastPos = 0;

        public Process StartProcess(string fileExecName, bool forInput = true)
        {
            var process = Process.Start(new ProcessStartInfo(fileExecName)
            {
                WorkingDirectory = new FileInfo(fileExecName).DirectoryName
            });
            WorkProcess = process;
            ProcessIsConnected = false;

            if (!forInput) return process;
            var wait = DateTime.UtcNow;
            do
            {
                Thread.Sleep(100);
            }
            while (process.MainWindowHandle == IntPtr.Zero && (DateTime.UtcNow - wait).TotalMilliseconds < 10000);
            if (process.MainWindowHandle != IntPtr.Zero)
            {
                Keybord = new SKKeybord(process.MainWindowHandle);
                Mouse = new SKMouse(process.MainWindowHandle);
                Graphics = new SKProcessCanvas(process.MainWindowHandle, LoaderImage);
            }
            Thread.Sleep(500);
            return process;
        }
        
        public Process ConnectProcess(string windowTitle, bool forInput = true)
        {
            foreach (var process in Process.GetProcesses())
            {
                if (process.MainWindowTitle == windowTitle)
                {
                    WorkProcess = process;
                    ProcessIsConnected = true;
                }
            }
            if (WorkProcess == null) return WorkProcess;

            if (!forInput) return WorkProcess;
            var wait = DateTime.UtcNow;
            do
            {
                Thread.Sleep(100);
            }
            while (WorkProcess.MainWindowHandle == IntPtr.Zero && (DateTime.UtcNow - wait).TotalMilliseconds < 10000);
            if (WorkProcess.MainWindowHandle != IntPtr.Zero)
            {
                Keybord = new SKKeybord(WorkProcess.MainWindowHandle);
                Mouse = new SKMouse(WorkProcess.MainWindowHandle);
                Graphics = new SKProcessCanvas(WorkProcess.MainWindowHandle, LoaderImage);
            }
            Thread.Sleep(500);
            return WorkProcess;
        }

        public bool WaitProcessExit(int milliseconds = -1)
        {
            if (WorkProcess == null) return true;
            return WaitProcessExit(WorkProcess, milliseconds);
        }

        public bool WaitProcessExit(Process process, int milliseconds = -1)
        {
            return process.WaitForExit(milliseconds);
        }

        /// <summary>
        /// Выполняем body каждые 100 мс, пока она не вернет true или пока не выйдет время milliseconds
        /// </summary>
        /// <returns>true если функция body вернула true</returns>
        public bool Wait(int milliseconds, Func<bool> body)
        {
            var wait = DateTime.UtcNow.AddMilliseconds(milliseconds);
            do
            {
                if (body()) return true;
                Thread.Sleep(100);
            }
            while (wait > DateTime.UtcNow);
            return false;
        }

        private string LastSetLogFileFromFolderName;
        public void SetLogFileFromFolder(string folderName = null)
        {
            if (folderName == null) folderName = LastSetLogFileFromFolderName;
            else LastSetLogFileFromFolderName = folderName;

            var di = new DirectoryInfo(folderName);
            if (!di.Exists) return;

            var log = di.GetFiles()
                .Select(fi => new { fi, date = fi.LastWriteTimeUtc })
                .OrderByDescending(a => a.date)
                .Select(a => a.fi.FullName)
                .FirstOrDefault();

            if (File.Exists(log)) LogFileName = log;
            else LogFileName = null;
        }

        public string GetLogNewText()
        {
            if (string.IsNullOrEmpty(LogFileName) || !File.Exists(LogFileName)) return "";

            //wait current write
            long lengthLast, lengthNow;
            var wait = DateTime.UtcNow;
            lengthNow = new FileInfo(LogFileName).Length;
            do
            {
                lengthLast = lengthNow;
                Thread.Sleep(5);
                lengthNow = new FileInfo(LogFileName).Length;
            }
            while (lengthLast != lengthNow && (DateTime.UtcNow - wait).TotalMilliseconds < 500);

            //get new part file
            FileInfo log = new FileInfo(LogFileName);
            using (var fl = log.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var length = fl.Length;
                if (length <= LogLastPos) return "";
                fl.Seek(LogLastPos, SeekOrigin.Begin);
                var fd = new byte[length - LogLastPos];
                fl.Read(fd, 0, (int)(length - LogLastPos));
                LogLastPos = length;

                var logNewPart = LogEncoding.GetString(fd);
                if (!string.IsNullOrEmpty(ResultTestLogFileName))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(ResultTestLogFileName));
                    File.AppendAllText(ResultTestLogFileName, logNewPart);
                }
                return logNewPart;
            }
        }

        public void CopyDirectory(string fromDir, string toDir)
        {
            Directory.CreateDirectory(toDir);
            foreach (string source in Directory.GetFiles(fromDir))
            {
                string dest = toDir + "\\" + Path.GetFileName(source);
                File.Copy(source, dest, true);
            }
            foreach (string source in Directory.GetDirectories(fromDir))
            {
                CopyDirectory(source, toDir + "\\" + Path.GetFileName(source));
            }
        }

        public void StartEnvironment()
        {
            EnvironmentOn = true;
            if (string.IsNullOrEmpty(EnvironmentSourceFolderName)) return;
            if (string.IsNullOrEmpty(EnvironmentWorkFolderName)) return;
            if (ModeCreateEnvironmentSource) return;

            if (Directory.Exists(EnvironmentWorkFolderName))
            {
                if (NeedRecoveryWorkFolder && !string.IsNullOrEmpty(EnvironmentBackupFolderName))
                {
                    if (Directory.Exists(EnvironmentBackupFolderName))
                        Directory.Delete(EnvironmentBackupFolderName, true);
                    CopyDirectory(EnvironmentWorkFolderName, EnvironmentBackupFolderName);
                }

                if (Directory.Exists(EnvironmentWorkFolderName))
                    Directory.Delete(EnvironmentWorkFolderName, true);
            }
            CopyDirectory(EnvironmentSourceFolderName, EnvironmentWorkFolderName);
        }

        public void FinishEnvironment()
        {
            EnvironmentOn = false;

            if (Graphics != null) Graphics.Dispose();
            Graphics = null;
            Keybord = null;
            Mouse = null;

            if (DisableFinishProcess) return;

            try
            {
                if (!ProcessIsConnected && WorkProcess != null && !WorkProcess.HasExited)
                {
                    if (!WaitProcessExit(1000))  WorkProcess.Kill();
                }
            }
            catch { }
            if (string.IsNullOrEmpty(EnvironmentWorkFolderName)) return;
            Thread.Sleep(500);
            if (ModeCreateEnvironmentSource)
            {
                if (string.IsNullOrEmpty(EnvironmentSourceFolderName)) return;
                if (Directory.Exists(EnvironmentSourceFolderName))
                    Directory.Delete(EnvironmentSourceFolderName, true);
                CopyDirectory(EnvironmentWorkFolderName, EnvironmentSourceFolderName);
            }
            else
            {
                if (string.IsNullOrEmpty(EnvironmentWorkFolderName)) return;
                Directory.Delete(EnvironmentWorkFolderName, true);
                if (NeedRecoveryWorkFolder 
                    && !string.IsNullOrEmpty(EnvironmentBackupFolderName)
                    && Directory.Exists(EnvironmentBackupFolderName))
                {
                    CopyDirectory(EnvironmentBackupFolderName, EnvironmentWorkFolderName);
                    Directory.Delete(EnvironmentBackupFolderName, true);
                }
            }
        }

        /// <summary>
        /// Подготавливает строку с логами для сравнения. Если задан аргумент, то оставляет только строки с вхождением заданной подстроки.
        /// </summary>
        /// <returns></returns>
        public string PrepareCheckLog(string logText, string find = null)
        {
            var linesRaw = logText.Split('\n');
            var result = "";
            for (int i = 0; i < linesRaw.Length; i++)
            {
                var line = linesRaw[i];
                if (line.EndsWith("\r")) line = line.Remove(line.Length - 1);

                //берем текст после трёх | или берем как есть
                var splitPos = line.IndexOf('|');
                if (splitPos > 0 && splitPos + 1 < line.Length)
                {
                    splitPos = line.IndexOf('|', splitPos + 1);
                    if (splitPos > 0 && splitPos + 1 < line.Length)
                    {
                        splitPos = line.IndexOf('|', splitPos + 1);
                        if (splitPos > 0)
                        {
                            if (splitPos + 1 < line.Length)
                            {
                                line = line.Substring(splitPos + 1);
                                //если первый символ пробел, его тоже убираем
                                if (line.Length > 0 && line[0] == ' ') line = line.Remove(0, 1);
                            }
                            else line = "";
                        }
                    }
                }

                if (find != null && line.IndexOf(find, StringComparison.CurrentCultureIgnoreCase) < 0) continue;

                result += line + Environment.NewLine;
            }
            return result;
        }

        public void Dispose()
        {
            if (EnvironmentOn) FinishEnvironment();
        }

        public bool Test()
        {
            bool result = true;

            var process = Process.Start("notepad");
            Thread.Sleep(1000);
            var mouse = new SKMouse(process.MainWindowHandle);
            var keybord = new SKKeybord(process.MainWindowHandle);

            mouse.Move(200, 200);
            mouse.Click();
            keybord.Send("+test1~+hello +world+1~+test2", false);
            keybord.Send("+{HOME}{DEL}+new line");
            keybord.Send("^A+{UP}+{UP}+first line");

            keybord.Send("^A^C");
            Thread.Sleep(200);
            var checkText = SKKeybord.GetClipboardText();
            if (checkText != @"First line
Hello World!
New line") result = false;

            mouse.Move(150, 150);
            mouse.Click(true, null, true);
            keybord.Send("^C");
            Thread.Sleep(200);

            var checkText1 = SKKeybord.GetClipboardText();
            if (checkText1 != "line") result = false;

            mouse.Move(100, 100);
            mouse.Click(true, true);
            mouse.Move(0, 0);
            mouse.Click(true, false);
            keybord.Send("^C");
            Thread.Sleep(200);
            var checkText2 = SKKeybord.GetClipboardText();
            if (checkText != checkText2) result = false;

            keybord.Send("% {UP}~{RIGHT}~");            
            Thread.Sleep(100);

            if (!process.HasExited)
            {
                result = false;
                process.Kill();
            }

            return result;
        }
    }
}
