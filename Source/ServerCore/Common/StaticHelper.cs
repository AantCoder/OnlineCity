using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System.Collections.Generic;
using System.Linq;

namespace ServerOnlineCity.Common
{
    internal class StaticHelper
    {
        /// <summary>
        /// Ники тех кого мы видим
        /// </summary>
        /// <returns></returns>
        public static List<string> PartyLoginSee(PlayerServer player)
        {
            var ps = player.IsAdmin
               ? Repository.GetData.PlayersAll.Select(p => p.Public.Login)
           : ChatManager.Instance.PublicChat.PartyLogin;
            return ps.ToList();
        }
    }
}