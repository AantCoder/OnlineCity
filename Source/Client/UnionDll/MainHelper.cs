using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

        public static bool InGame = false;

        private static Assembly AssemblyAssembly = Assembly.GetAssembly(typeof(MainHelper));
        private static Version AssemblyVersion = AssemblyAssembly.GetName().Version;
        private static DateTime? AssemblyDate = string.IsNullOrEmpty(AssemblyAssembly.Location) ? (DateTime?)null 
            : new FileInfo(AssemblyAssembly.Location).LastWriteTime;

        public static string VersionInfo = $"Version {(AssemblyVersion.ToString() + "$$").Replace(".0$$", "").Replace("$$", "")}a" +
            (AssemblyDate == null ? "" : " from " + AssemblyDate.Value.ToString("yyyy.MM.dd"));
        //public static string VersionInfo = "Version 0.04.70a from 2021.10.31";

        public static string Key = "";

        /// <summary>
        /// Для автоматической проверки: версия клиента должна быть больше или равна версии сервера
        /// </summary>
        public static long VersionNum = AssemblyVersion.Major * 10000 * 10000
          + AssemblyVersion.Minor * 10000
          + AssemblyVersion.Build;
        //public static long VersionNum = 40070;

        public static string DefaultIP = DebugMode ? "localhost" : " ";

        public static Dictionary<string, string> ServerList = new Dictionary<string, string> 
        {
            { "Vanilla", "62.133.174.133:19042" },
            { "Fantasy", "62.133.174.133:19022" },
            { "SkyNet CE+SRTS", "95.154.71.53:6666"},
            { "BIOnline", "62.133.174.133:19024"},
            { "Hi-Tech","62.133.174.133:19023"},
#if DEBUG
            { "localhost", "127.0.0.1" },
#endif
        };

        public static int MinCostForTrade = 25000;

        public static string CashlessThingDefName = "Silver";

        private static ThingDef _CashlessThingDef;
        public static ThingDef CashlessThingDef
        {
            get 
            {
                if (_CashlessThingDef == null)
                {
                    _CashlessThingDef = (ThingDef)GenDefDatabase.GetDef(typeof(ThingDef), CashlessThingDefName);
                }
                return _CashlessThingDef;
            }
        }

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
                        if (CultureFromGame != null && CultureFromGame.StartsWith("Russian"))
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

        public static int RandomSeed { get; } = new Random((int)(DateTime.UtcNow.Ticks & int.MaxValue)).Next(10000, 99999);
        public static int LockCode { get; } = int.Parse(DateTime.Now.ToString("HHmmssfff"));


        private static ConcurrentDictionary<string, string> TranslateCacheDic = new ConcurrentDictionary<string, string>();
        public static string TranslateCache(this string text)
        {
            return TranslateCacheDic.GetOrAdd(text, t => t.Translate());
        }
        public static string TranslateCache(this string text, params object[] args)
        {
            return string.Format(text.TranslateCache(), args);
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
            if (that == DateTime.MinValue) return that.ToString(Culture);
            var nowUtc = DateTime.Now - DateTime.UtcNow;
            return (that + nowUtc).ToString(Culture);
        }

        public static string ToGoodUtcString(this DateTime that, string format)
        {
            if (that == DateTime.MinValue) return that.ToString(format, Culture);
            var nowUtc = DateTime.Now - DateTime.UtcNow;
            return (that + nowUtc).ToString(format, Culture);
        }

        public static string NormalizeFileNameChars(this string fileName)
        {
            char[] invalidFileChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidFileChars) if (fileName.Contains(c)) fileName = fileName.Replace(c, '_');
            return fileName;
        }

    }
}

//  ⎘ ⎆ ⚐ ⚑ ⚙ ⚠ ⚡ ⚪ ⚫ ✓ ✅ ✔ ✘ ✕ ✗ ✎ ✏ ⬤  🪙 🤔 🏱 🏲 🏳 🏴 🆗 

