using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// for using SQL Lite DB install https://marketplace.visualstudio.com/items?itemName=ErikEJ.SQLServerCompactSQLiteToolbox
/// </summary>
namespace DiscordChatBotServer.Helpers
{ 
    public static class Helper
    {
        public const string PREFIX = "!OC"; // Online City 
        private static ConcurrentDictionary<int, string> DiscordChanel2IPServer = new ConcurrentDictionary<int, string>();
        private static ConcurrentDictionary<string, int> IPServer2DiscordChanel = new ConcurrentDictionary<string, int>();

        public static bool RegServerOnline(int discordChanel, string ip)
        {
            return
            DiscordChanel2IPServer.TryAdd(discordChanel, ip) &&
            IPServer2DiscordChanel.TryAdd(ip, discordChanel);
        }

        /// <summary>
        /// Save to DB
        /// </summary>
        /// <param name="idChanel"></param>
        /// <returns></returns>
        internal static bool RegServerOffline(int idChanel, string ip)
        {
            var t = System.Data.SQLite.EF6.SQLiteProviderFactory.Instance;
            var da = t.CreateDataAdapter();

            return false;
        }
    }
}
