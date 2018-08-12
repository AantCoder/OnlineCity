using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCUnion
{
    public static class ExceptionUtil
    {
        public static string ExceptionLog(Exception e, string local)
        {
            var msg = local + " " + e.Message
                + (e.InnerException == null ? "" : " -> " + e.InnerException.Message)
                + Environment.NewLine
                + " (" + (e.InnerException != null ? e.InnerException : e).GetType().Name + ") "
                + (e.InnerException != null && e.InnerException.StackTrace != null ? e.InnerException.StackTrace : e.StackTrace);
            Loger.Log(msg);
            return msg;
        }
    }
}
