using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace OCUnion
{
    public static class Loger
    {
        //public static event Action<string> LogMessage;

        private static string _PathLog;
        private static Dictionary<int, DateTime> LastMsg = new Dictionary<int, DateTime>();
        public static CultureInfo Culture = CultureInfo.GetCultureInfo("ru-RU");
        private static Object ObjLock = new Object();
        public static bool IsServer;

        public static string PathLog
        {
            get { return _PathLog; }
            set { _PathLog = Path.GetDirectoryName(value + @"\") + @"\"; }
        }

        public static string Bytes(byte[] bs)
        {
            return BitConverter.ToString(bs).Replace('-', ' ');
        }

        private static string LogErr = null;
        private static int LogErrThr = 0;

        private static void LogWrite(string msg, bool withCatch, int threadId = 0)
        {
            var thn = threadId != 0 ? threadId : Thread.CurrentThread.ManagedThreadId;
            var dn = DateTime.Now;
            var dd = !LastMsg.ContainsKey(thn) ? 0 : (long)(dn - LastMsg[thn]).TotalMilliseconds;
            LastMsg[thn] = dn;
            if (dd >= 1000000) dd = 0;
            var logMsg = dn.ToString(Culture) + " |" + dd.ToString().PadLeft(6) + " |" + thn.ToString().PadLeft(4) + " | " + msg;
            var date = DateTime.Now.ToString("yyyy-MM-dd");

            if (withCatch) Console.WriteLine(logMsg);
            lock (ObjLock)
            {
                try
                {
                    //if (LogMessage != null) LogMessage(logMsg);
                    File.AppendAllText(PathLog + @"Log " + date + ".txt", logMsg + Environment.NewLine, Encoding.UTF8);
                }
                catch (Exception exp)
                {
                    if (withCatch)
                    {
                        LogErr = "Log exception: " + exp.Message + Environment.NewLine + logMsg;
                        LogErrThr = thn;
                    }
                }
            }
        }

        public static void Log(string msg)
        {
            if (MainHelper.OffAllLog) return;

            if (LogErr != null)
            {
                LogWrite(LogErr, false, LogErrThr);
                LogErr = null;
            }
            LogWrite(msg, true);
        }
    }
}

