using Model;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System.Collections.Generic;
using System.Linq;

namespace ServerOnlineCity.Common
{
    internal static class StaticHelper
    {
        /// <summary>
        /// Ники тех кого мы видим
        /// </summary>
        /// <returns></returns>
        public static HashSet<string> PartyLoginSee(PlayerServer player)
        {
            var ps = player.IsAdmin
                ? Repository.GetData.GetPlayerLoginsAll
                : ChatManager.Instance.PublicChat.PartyLogin;
            return ps.ToHashSet();
        }

        public static PlayerServer GetPlayerServer(this Player player)
        {
            return Repository.GetPlayerByLogin(player.Login);
        }
    }
}