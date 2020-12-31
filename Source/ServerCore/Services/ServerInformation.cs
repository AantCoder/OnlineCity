using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using OCUnion;
using OCUnion.Transfer;
using ServerCore.Model;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class ServerInformation : IGenerateResponseContainer
    {
        public int RequestTypePackage => 5;

        public int ResponseTypePackage => 6;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = GetInfo((ModelInt)request.Packet, context);
            return result;
        }

        public ModelInfo GetInfo(ModelInt packet, ServiceContext context)
        {
            lock (context.Player)
            {
                switch (packet.Value)
                {
                    case 1:
                        {
                            var result = GetModelInfo(context.Player);
                            return result;
                        }

                    case 3:
                        {
                            var result = new ModelInfo();
                            //передача файла игры, для загрузки WorldLoad();
                            // файл передать можно только в том случае если файлы прошли проверку

                            //!Для Pvp проверка нужна всегда, в PvE нет
                            if (ServerManager.ServerSettings.IsModsWhitelisted)
                            {
                                if ((int)context.Player.ApproveLoadWorldReason > 0)
                                {
                                    context.Player.ExitReason = DisconnectReason.FilesMods;
                                    Loger.Log($"Login : {context.Player.Public.Login} not all files checked,{context.Player.ApproveLoadWorldReason.ToString() } Disconnect");
                                    result.SaveFileData = null;
                                    return result;
                                }
                            }

                            Loger.Log($"Load World for {context.Player.Public.Login}");
                            result.SaveFileData = Repository.GetSaveData.LoadPlayerData(context.Player.Public.Login, 1);
                            return result;
                        }

                    case 4:
                        {
                            var result = GetModelInfo(context.Player);
                            //result.Description = "";
                            var displayAttributes = new List<Tuple<int, string>>();

                            foreach (var prop in typeof(ServerSettings).GetFields())
                            {
                                var attribute = prop.GetCustomAttributes(typeof(DisplayAttribute)).FirstOrDefault();
                                if (attribute is null || !prop.IsPublic)
                                {
                                    continue;
                                }

                                var dispAtr = (DisplayAttribute)attribute;
                                var strvalue = string.IsNullOrEmpty(dispAtr.GetDescription()) ? prop.Name : dispAtr.GetDescription();
                                strvalue = strvalue + "=" + prop.GetValue(ServerManager.ServerSettings).ToString();
                                var order = dispAtr.GetOrder().HasValue ? dispAtr.GetOrder().Value : 0;
                                displayAttributes.Add(Tuple.Create(order, strvalue));
                            }

                            var sb = new StringBuilder();
                            var sorte = new List<string>(displayAttributes.OrderBy(x => x.Item1).Select(y => y.Item2)).AsReadOnly();
                            foreach (var prop in sorte)
                            {
                                sb.AppendLine(prop);
                            }

                            //result.Description = sb.ToString();
                            return result;
                        }
                    default:
                        {
                            // краткая (зарезервированно, пока не используется) fullInfo = false
                            var result = new ModelInfo();
                            return result;
                        }
                }
            }
        }

        private ModelInfo GetModelInfo(PlayerServer player)
        {
            var data = Repository.GetData;
            var result =
            new ModelInfo()
            {
                My = player.Public,
                IsAdmin = player.IsAdmin,
                VersionInfo = MainHelper.VersionInfo,
                VersionNum = MainHelper.VersionNum,
                Seed = data.WorldSeed ?? "",
                ScenarioName = data.WorldScenarioName,
                MapSize = data.WorldMapSize,
                PlanetCoverage = data.WorldPlanetCoverage,
                Difficulty = data.WorldDifficulty,
                NeedCreateWorld = Repository.GetSaveData.GetListPlayerDatas(player.Public.Login).Count == 0,
                ServerTime = DateTime.UtcNow,
                IsModsWhitelisted = ServerManager.ServerSettings.IsModsWhitelisted,
                ServerName = ServerManager.ServerSettings.ServerName,
                DelaySaveGame = player.SettingDelaySaveGame,
                DisableDevMode = ServerManager.ServerSettings.DisableDevMode,
                MinutesIntervalBetweenPVP = ServerManager.ServerSettings.MinutesIntervalBetweenPVP,
                EnableFileLog = player.SettingEnableFileLog,
                TimeChangeEnablePVP = player.TimeChangeEnablePVP,
                ProtectingNovice = ServerManager.ServerSettings.ProtectingNovice,
            };

            return result;
        }
    }
}