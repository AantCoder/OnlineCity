using Model;
using OCUnion;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerOnlineCity
{
    internal class ServiceAPI
    {
        //to do:
        //  получить данные с чата, максимум последнии 10 мин (возвращает текст и время в тиках посл сообщения; требуется время в тиках после которого нужны сообщения, при 0 возвращает только время посл сообщения)
        //  получать информацию об игроке
        //  получать информацию о бирже
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
                    case "playerstatistics":
                    case "playerstatistic":
                    case "playerstat":
                        return CheckSecretKey(package.Key) ?? GetPlayerStatistics();
                    case "blockkey":
                        return CheckSecretKey(package.Key) ?? GetBlockKey();
                    case "consolecommand":
                    case "cc":
                        return CheckSecretKey(package.Key) ?? ConsoleCommand(package.N);
                    case "chatpost":
                    case "cp":
                        return CheckSecretKey(package.Key) ?? ChatPost(package.N);
                    case "playerapprove":
                    case "pa":
                        return CheckSecretKey(package.Key) ??
                            SetPlayerApprove(package.Key
                                , (package.Login ?? "").Split(new string[] { "*" }, StringSplitOptions.RemoveEmptyEntries)
                                , (package.N ?? "").Split(new string[] { "*" }, StringSplitOptions.RemoveEmptyEntries));
                    case "loadimage":
                    case "li":
                        return CheckSecretKey(package.Key) ?? GetLoadImage(package.N);
                    case "saveimage":
                    case "si":
                        return CheckSecretKey(package.Key) ?? GetSaveImage(package.N, package.Data);
                    case "test":
                        return new APIResponseRawData() { Data = package.Data };
                    case "status":
                    case "s":
                    default:
                        return GetStatus(package.Key);
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
            res.Players = Repository.GetData.GetPlayersAll
                .Where(p => p.Public.Login != "system" && p.Public.Login != "discord")
                .Select(p => ToAPIPlayer(p)).ToList();
            return res;
        }

        private List<string> GetPlayersNeedApprove()
        {
            if (!ServerManager.ServerSettings.PlayerNeedApproveInDiscord) return null;
            var res = Repository.GetData.PlayersAll
                .Where(p => p.Public.Login != "system" && p.Public.Login != "discord")
                .Where(p => !p.Approve)
                .Select(p => p.Public.Login + "*" + p.Public.DiscordUserName).ToList();
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

        private APIResponse GetStatus(string key = null)
        {
            var onlines = GetOnline();
            var res = new APIResponseStatus()
            {
                OnlineCount = onlines.Count,
                PlayerCount = Repository.GetData.GetPlayersAll.Count - 2,
                Onlines = onlines.Select(p => p.Public.Login).ToList(),
                NeedApprove = key != null && CheckSecretKey(key) == null ? GetPlayersNeedApprove() : null,
            };
            return res;
        }

        public APIResponse CheckSecretKey(string key) => 
            string.IsNullOrEmpty(key) 
            || string.IsNullOrEmpty(ServerManager.ServerSettings.SecretKey)
            || ServerManager.ServerSettings.SecretKey != key
            ? new APIResponseError()
            {
                Error = "No access"
            }
            : null;

        public APIResponse GetPlayerStatistics()
        {
            var report = new ReportManager();
            var content = report.GetAllPlayerStatisticsFile();
            return new APIResponseRawData() { Data = Encoding.UTF8.GetBytes(content) };
        }

        public APIResponse GetBlockKey()
        {
            var content = "";
            try
            {
                var fileName = Path.Combine(Path.GetDirectoryName(Repository.Get.SaveFileName), "blockkey.txt");
                if (File.Exists(fileName))
                {
                    content = File.ReadAllText(fileName);
                }
            }
            catch { }

            return new APIResponseRawData() { Data = Encoding.UTF8.GetBytes(content ?? "") };
        }

        private APIResponse ConsoleCommand(string command)
        {
            if (string.IsNullOrEmpty(command)
                || !ServerManager.SendConsoleCommand(command[0]))
                return new APIResponseError()
                {
                    Error = $"Unknow command: {command}"
                };
            else
                return new APIResponseError()
                {
                    Error = null
                };
        }

        private APIResponse ChatPost(string command)
        {
            ServerManager.SendRunCommand(command);
            return new APIResponseError()
            {
                Error = null
            };
        }

        public APIResponse SetPlayerApprove(string key, IEnumerable<string> loginApproved, IEnumerable<string> loginDelete)
        {
            var data = Repository.GetData;
            lock (data)
            {
                foreach (var login in loginApproved)
                {
                    if (string.IsNullOrEmpty(login) || login == "system" || login == "discord") continue;
                    var player = Repository.GetPlayerByLogin(login, true);
                    if (player == null) continue;

                    player.Approve = true;
                    Loger.Log("Server Set approve player " + player.Public.Login);
                }

                foreach (var login in loginDelete)
                {
                    if (string.IsNullOrEmpty(login) || login == "system" || login == "discord") continue;
                    var player = Repository.GetPlayerByLogin(login, true);
                    if (player == null) continue;

                    if (!player.Approve)
                    {
                        player.Delete();
                        Loger.Log($"Server Delete not approve player " + player.Public.Login);
                    }
                }

                if (loginApproved.Count() > 0 || loginDelete.Count() > 0) Repository.Get.ChangeData = true;
            }
            return GetStatus(key);
        }

        public APIResponse GetLoadImage(string name)
        {
            APIResponse response = new APIResponseError()
            {
                Error = "Error N"
            };
            if (name.Length < 4) return response;
            var sendName = name.Substring(3);

            FileSharingCategory category;
            if (name.StartsWith("pl_")) category = FileSharingCategory.PlayerIcon;
            else if (name.StartsWith("cs_")) category = FileSharingCategory.ColonyScreen;
            else return response;

            var fileSharing = new ModelFileSharing()
            {
                Category = category,
                Name = sendName
            };

            //запрос на чтение с сервера из FileSharing
            Repository.GetFileSharing.LoadFileSharing(Repository.GetData.PlayerSystem, fileSharing);

            response = fileSharing.Data == null
                ? (APIResponse)new APIResponseError()
                {
                    Error = "No data"
                }
                : new APIResponseRawData() { Data = fileSharing.Data };

            return response;
        }

        public APIResponse GetSaveImage(string name, byte[] data)
        {
            if (data == null) return new APIResponseError()
            { 
                Error = "No Data" 
            };

            APIResponse response = new APIResponseError()
            {
                Error = "Error N"
            };
            if (name.Length < 4) return response;
            var sendName = name.Substring(3);

            FileSharingCategory category;
            if (name.StartsWith("pl_")) category = FileSharingCategory.PlayerIcon;
            else if (name.StartsWith("cs_")) category = FileSharingCategory.ColonyScreen;
            else return response;

            var fileSharing = new ModelFileSharing()
            {
                Category = category,
                Name = sendName,
                Data = data,
            };

            //запрос на запись файла на сервер из FileSharing
            if (!Repository.GetFileSharing.SaveFileSharing(Repository.GetData.PlayerSystem, fileSharing))
                return new APIResponseError()
                {
                    Error = "Error save"
                };

            return new APIResponseError()
            {
                Error = "OK"
            };
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
                BaseServerIds = attCosts.BaseServerIds,
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
