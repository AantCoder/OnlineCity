using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Diagnostics;

namespace Sidekick.Sidekick
{
    public class MMFRW : IDisposable
    {
        private MemoryMappedFile MMFControl;
        private Process ProcessWorker;
        private DateTime WorkerStartTime;
        public Action<string> LogSend = (msg) => { };
        public Double Timeout = 0;

        public MMFRW()
        {
            MMFControl = MemoryMappedFile.CreateOrOpen("SidekickMMFRW_Control", 1);
            using (MemoryMappedViewAccessor writer = MMFControl.CreateViewAccessor(0, 1))
            {
                byte controlByte = 0;
                writer.Write<byte>(0, ref controlByte);
            }

            StopWorker();
            StartWorker();
        }

        public void Dispose()
        {
            try
            {
                ProcessWorker.Kill();
            }
            catch { }
        }

        private void StopWorker()
        {
            LogSend("RW-StopWorker");
            var ps = Process.GetProcesses();
            foreach (var p in ps)
            {
                if (p.ProcessName == "SidekickRW") p.Kill();
            }
        }

        private void StartWorker()
        {
            LogSend("RW-StartWorker");
            WorkerStartTime = DateTime.UtcNow;
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "SidekickRW.exe";
            startInfo.Arguments = "";
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            ProcessWorker = new Process();
            ProcessWorker.StartInfo = startInfo;
            ProcessWorker.EnableRaisingEvents = true;
            try
            {
                ProcessWorker.Start();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void RestartWorker()
        {
            try
            {
                ProcessWorker.Kill();
            }
            catch { }
            StartWorker();
        }

        public string ReadTest(byte[] source, bool numbersMode, bool small)
        {
            if (ProcessWorker != null && (DateTime.UtcNow - WorkerStartTime).TotalMinutes > 20)
            {
                LogSend("RW-RestartWorker on time");
                RestartWorker();
            }
            /*
            //тут проверка состояния не нужна, т.к. не поддерживается многопоточность
            byte readControlByte;
            do
            {
                using (MemoryMappedViewAccessor reader = MMFControl.CreateViewAccessor(0, 1, MemoryMappedFileAccess.Read))
                {
                    readControlByte = reader.ReadByte(0);
                }
            } while (readControlByte != 0);
            */

            var MMFData = MemoryMappedFile.CreateOrOpen("SidekickMMFRW_Data", 1);
            using (MemoryMappedViewAccessor writer = MMFData.CreateViewAccessor(0, source.Length + 4))
            {
                writer.Write(0, source.Length);
                writer.WriteArray<byte>(4, source, 0, source.Length);
            }

            byte controlByte = (byte)(1 + (numbersMode ? 2 : 0) + (small ? 4 : 0));
            using (MemoryMappedViewAccessor writer = MMFControl.CreateViewAccessor(0, 1))
            {
                writer.Write<byte>(0, ref controlByte);
            }

            //ожидаем ответ
            byte readControlByte;
            var begin = DateTime.UtcNow;
            do
            {
                Thread.Sleep(0);
                using (MemoryMappedViewAccessor reader = MMFControl.CreateViewAccessor(0, 1, MemoryMappedFileAccess.Read))
                {
                    readControlByte = reader.ReadByte(0);
                }
            } while (readControlByte != 255 && (Timeout <= 0 || (DateTime.UtcNow - begin).TotalSeconds < Timeout));

            if (Timeout > 0 && (DateTime.UtcNow - begin).TotalSeconds >= Timeout)
            {
                LogSend($"RW-RestartWorker {Timeout} sec");
                RestartWorker();
                return "unknown";
            }

            MMFData.Dispose();

            var MMFResult = MemoryMappedFile.CreateOrOpen("SidekickMMFRW_Result", 1);
            int countChar;
            using (MemoryMappedViewAccessor reader = MMFResult.CreateViewAccessor(0, 4, MemoryMappedFileAccess.Read))
            {
                countChar = reader.ReadInt32(0);
            }
            string result;
            using (MemoryMappedViewAccessor reader = MMFResult.CreateViewAccessor(4, countChar * 2, MemoryMappedFileAccess.Read))
            {
                //Массив символов сообщения
                var message = new char[countChar];
                reader.ReadArray<char>(0, message, 0, countChar);
                result = new string(message);
            }
            MMFResult.Dispose();

            return result;
        }
    }
}
