using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace OCUnion
{
    public static class Loger
    {
        public enum LogLevel
        {
            //Description
            [Description("EE")]
            ERROR,
            [Description("WW")]
            WARNING,
            [Description("--")]
            INFO,
            [Description("DB")]
            DEBUG,
            [Description("EH")]
            EXCHANGE,
            [Description("RG")]
            REGISTER,
            [Description("LG")]
            LOGIN,
            [Description("GE")]
            GAMEERROR,
        }

        private static string _PathLog;
        private static Dictionary<int, DateTime> LastMsg = new Dictionary<int, DateTime>();
        private static Object ObjLock = new Object();
        public static bool IsServer;
        public static bool Enable = false;

        public static string PathLog
        {
            get { return _PathLog; }
            set
            {
                _PathLog = Path.GetDirectoryName(value + Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            }
        }

        public static string Bytes(byte[] bs)
        {
            return BitConverter.ToString(bs).Replace('-', ' ');
        }

        private static string LogErr = null;
        private static int LogErrThr = 0;

        private static DateTime lastTime;
        private static int lastMsg;
        private static int spam;

        private static void LogWrite(string msg, bool withCatch, int threadId = 0, string suffix = null, DateTime time = default)
        {
            //spam test
            var h = msg.GetHashCode();
            if (h == lastMsg && (DateTime.UtcNow - lastTime).TotalMilliseconds < 2000)
            {
                spam++;
                return;
            }
            var lastLastTime = lastTime;
            lastTime = DateTime.UtcNow;
            lastMsg = h;
            if (spam > 0)
            {
                var lastspam = spam;
                spam = 0;
                LogWrite("Was removed as spam " + lastspam, withCatch, time: lastLastTime);
                lastMsg = h;
            }

            var thn = threadId != 0 ? threadId : Thread.CurrentThread.ManagedThreadId;
            var dn = time == default ? DateTime.UtcNow : time;
            var dd = !LastMsg.ContainsKey(thn) ? 0 : (long)(dn - LastMsg[thn]).TotalMilliseconds;
            LastMsg[thn] = dn;
            if (dd >= 1000000) dd = 0;
            //var logMsg = "U: " + dn.ToString("T") + " |" + dd.ToString().PadLeft(6) + " |" + thn.ToString().PadLeft(4) + " | " + msg;
            var logMsg = dn.ToString("HH:mm:ss.ffff") + " |" + dd.ToString().PadLeft(6) + " |" + thn.ToString().PadLeft(4) + " | " + msg;
            var fileName = $"Log_{DateTime.Now.ToString("yyyy-MM-dd")}_{MainHelper.LockCode}{(suffix == null ? "" : "_" + suffix)}.txt";

            if (!MainHelper.InGame && withCatch && suffix == null) Console.WriteLine(logMsg);
            lock (ObjLock)
            {
                try
                {
                    //if (LogMessage != null) LogMessage(logMsg);
                    File.AppendAllText(PathLog + fileName, logMsg + Environment.NewLine, Encoding.UTF8);
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
        
        
        /// <summary>
        /// Вывод в консоль и в файл.
        /// </summary>
        /// <param name="msg">Строка для вывода в лог</param>
        /// <param name="logType">
        /// Тип вывода
        /// ERROR,
        /// WARNING,
        /// INFO,
        /// DEBUG1,
        /// DEBUG2,
        /// LOGIN,
        /// REGISTER. По умолчанию стоит INFO
        /// </param>
        public static void Log(string msg, LogLevel logType = LogLevel.INFO, string suffix = null)
        {
            if (!Enable) return;
            if (MainHelper.OffAllLog) return;
            msg = $"[{GetEnumDescription(logType)}] {msg}";
            if (LogErr != null)
            {
                LogWrite(LogErr, false, LogErrThr, suffix);
                LogErr = null;
            }
            LogWrite(msg, true, default, suffix);
        }
        
        public static string GetEnumDescription(Enum enumValue)
        {
            MemberInfo[] memInfo = enumValue.GetType().GetMember(enumValue.ToString());
            DescriptionAttribute description =
                CustomAttributeExtensions.GetCustomAttribute<DescriptionAttribute>(memInfo[0]);
            return description.Description;
        }
        
        private static string _TransLogData;
        public static void TransLog(string msg)
        {
            lock (ObjLock)
            {
                _TransLogData += Environment.NewLine + msg;
            }
        }
        public static string GetTransLog()
        {
            lock (ObjLock)
            {
                var log = _TransLogData;
                _TransLogData = null;
                return log;
            }
        }
    }
}

