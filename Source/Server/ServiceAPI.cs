using Model;
using ServerOnlineCity.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerOnlineCity
{
    internal class ServiceAPI
    {
        internal APIResponse GetPackage(APIRequest package)
        {
            try
            {
                switch (package?.Q?.Trim()?.ToLower() ?? "")
                {
                    case "allplayer":
                    case "a":
                        return GetPlayers();
                    case "player":
                    case "p":
                        return GetPlayers(package.Login);
                    case "status":
                    case "s":
                    default:
                        return GetStatus();
                }
            }
            catch
            {
                return new APIResponseError()
                {
                    Error = "Exception"
                };
            }
        }

        private APIResponse GetPlayers()
        {
            var res = new APIResponsePlayers();
            res.Players = Repository.GetData.PlayersAll
                .Where(p => p.Public.Login != "system" && p.Public.Login != "discord")
                .Select(p => ToAPIPlayer(p)).ToList();
            return res;
        }

        private APIResponse GetPlayers(string login)
        {
            var player = Repository.GetPlayerByLogin(login);
            if (player == null)
            {
                return new APIResponseError()
                {
                    Error = "Player not found"
                };
            }
            var res = new APIResponsePlayers()
            {
                Players = new List<APIPlayer>() { ToAPIPlayer(player) }
            };
            return res;
        }

        private APIResponse GetStatus()
        {
            var onlines = GetOnline();
            var res = new APIResponseStatus()
            {
                OnlineCount = onlines.Count,
                PlayerCount = Repository.GetData.PlayersAll.Count - 2,
                Onlines = onlines.Select(p => p.Public.Login).ToList(),
            };
            return res;
        }

        private APIPlayer ToAPIPlayer(PlayerServer player)
        {
            var attCosts = player.CostWorldObjectsWithCache();
            return new APIPlayer()
            {
                Login = player.Public.Login,
                DiscordUserName = player.Public.DiscordUserName,
                LastOnlineTime = player.Public.LastOnlineTime,
                Days = player.Public.LastTick / 60000,
                BaseCount = attCosts.BaseCount,
                CaravanCount = attCosts.CaravanCount,
                MarketValueTotal = attCosts.MarketValueTotal,
            };
        }

        private HashSet<PlayerServer> OnlineCache = null;
        private DateTime OnlineCacheDate;
        private HashSet<PlayerServer> GetOnline()
        { 
            if (DateTime.UtcNow > OnlineCacheDate)
            {
                var data = Repository.GetData;
                OnlineCache = data.PlayersAllDic.Values.Where(p => p.Online).ToHashSet();
                OnlineCacheDate = DateTime.UtcNow.AddSeconds(10);
            }
            return OnlineCache;
        }

    }
}
