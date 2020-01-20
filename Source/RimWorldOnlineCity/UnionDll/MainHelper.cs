using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace OCUnion
{
    public static class MainHelper
    {
        /// <summary>
        /// Активирует разрабатваемые блоки и дополнительное логирования
        /// </summary>
        public static bool DebugMode = false;
        /// <summary>
        /// Отключить все логи в файл и консоль. Сервер всегда меняет значение на false
        /// </summary>
        public static bool OffAllLog = false;

        //public static string VersionInfo = $"Version {Assembly.GetExecutingAssembly().FullName}";
        public static string VersionInfo = "Version 0.03.33a.0001 from 2020.01.20";

        /// <summary>
        /// Для автоматической проверки: версия клиента должна быть больше или равна версии сервера
        /// </summary>
        //public static readonly long  VersionNum = Assembly.GetExecutingAssembly().GetName().Version.Revision;
        public static long VersionNum = 30033;

        public static string DefaultIP = DebugMode ? "localhost" : "194.87.95.90:19020"; // rimworld.online

        private static CultureInfo CultureValue = null;
        public static string CultureFromGame = null;
        public static CultureInfo Culture
        {
            get
            {
                if (CultureValue == null)
                {
                    try
                    {
                        if (CultureFromGame.StartsWith("Russian"))
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
