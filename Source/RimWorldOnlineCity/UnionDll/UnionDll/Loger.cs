using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace OCUnion
{
    public static class Loger
    {

        private static string _PathLog;
        private static DateTime LastMsg;
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

        public static void Log(string msg)
        {
            var dn = DateTime.Now;
            lock (ObjLock)
            {
                try
                {
                    var dd = (long)(dn - LastMsg).TotalMilliseconds;
                    LastMsg = dn;
                    if (dd >= 1000000) dd = 0;
                    var logMsg = dn.ToString(Culture) + " | " + dd.ToString().PadLeft(6) + " | " + msg + Environment.NewLine;
                    var date = DateTime.Now.ToString("yyyy-MM-dd");
                    File.AppendAllText(PathLog + @"Log " + date + ".txt", logMsg, Encoding.UTF8);
                }
                catch
                { }
            }
        }
    }
}
