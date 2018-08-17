using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace OCUnion
{
    public static class MainHelper
    {
        public static bool DebugMode = false;

        public static CultureInfo Culture = CultureInfo.GetCultureInfo("ru-RU");

        public static string VersionInfo = "Версия 0.02.3a от 2018.08.17";

        public static string ToGoodString(this DateTime that)
        {
            return that.ToString(Culture);
        }
        public static string ToGoodUtcString(this DateTime that)
        {
            var nowUtc = DateTime.Now - DateTime.UtcNow;
            return (that + nowUtc).ToString(Culture);
        }
    }
}
