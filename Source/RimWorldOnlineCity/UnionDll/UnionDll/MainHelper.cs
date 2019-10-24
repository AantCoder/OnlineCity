using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Verse;

namespace OCUnion
{
    public static class MainHelper
    {
        public static bool DebugMode = true;

        public static string VersionInfo = "Версия 0.02.30a от 2019.10.22";

        /// <summary>
        /// Для автоматической проверки: версия клиента должна быть больше или равна версии сервера
        /// </summary>
        public static long VersionNum = 20030;

        public static string DefaultIP = DebugMode ? "localhost" : "194.87.95.90"; // rimworld.online
        
        private static CultureInfo CultureValue = null;
        public static CultureInfo Culture
        {
            get
            {
                if (CultureValue == null)
                {
                    try
                    {
                        if ((Prefs.LangFolderName ?? "").StartsWith("Russian"))
                            CultureValue = CultureInfo.GetCultureInfo("ru-RU");
                        else
                            CultureValue = CultureInfo.InvariantCulture;
                    }
                    catch
                    {
                        CultureValue = CultureInfo.InvariantCulture;
                    }
                }
                return CultureValue;
            }
        }
        
        public static string NeedTranslate(this string text)
        {
            return text;
        }
        public static string NeedTranslate(this string text, params object[] args)
        {
            return string.Format(text, args);
        }

        public static string ToGoodString(this DateTime that)
        {
            return that.ToString(Culture);
        }
        public static string ToGoodUtcString(this DateTime that)
        {
            var nowUtc = DateTime.Now - DateTime.UtcNow;
            return (that + nowUtc).ToString(Culture);
        }
        public static string ToGoodUtcString(this DateTime that, string format)
        {
            var nowUtc = DateTime.Now - DateTime.UtcNow;
            return (that + nowUtc).ToString(format, Culture);
        }
    }
}
