using HarmonyLib;
using OCUnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity.GameClasses.Harmony
{
    public class CatchGameError : IDisposable
    {
        private Func<string, bool> OnError;

        public string GameError = null;

        public CatchGameError(Func<string, bool> onError = null)
        {
            OnError = onError;
            if (OnError == null) OnError = (msg) => true;
            GameLog.OnError += GameLog_OnError;
        }

        private bool GameLog_OnError(string msg)
        {
            GameError = msg ?? "";
            return OnError(msg);
        }

        public void Dispose()
        {
            GameLog.OnError -= GameLog_OnError;
        }
    }

    public static class GameLog
    {
        public static event Func<string, bool> OnError;

        internal static bool Error(string text)
        {
            var res = OnError == null ? true : OnError(text);

            if (res) Loger.Log("Error game log. " + text + Environment.NewLine
                + GetStackTrace()
                , Loger.LogLevel.GAMEERROR);

            return res;
        }

        private static string GetStackTrace()
        {
            var stackTrace = StackTraceUtility.ExtractStackTrace();
            var i = stackTrace.IndexOf("RimWorldOnlineCity.GameClasses.Harmony.Log_Error_Patch");
            if (i > 0)
            {
                i = stackTrace.IndexOf("\n", i);
                if (i > 0) stackTrace = stackTrace.Substring(i + 1);
            }
            return stackTrace;//.Trim();
        }
    }

    [HarmonyPatch(typeof(Log))]
    [HarmonyPatch("Error")]
    [HarmonyPatch(new Type[] { typeof(string) })]
    internal class Log_Error_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(string text)
        {
            return GameLog.Error(text);
        }
    }
}
