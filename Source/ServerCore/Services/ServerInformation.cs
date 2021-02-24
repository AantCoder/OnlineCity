using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using Model;
using OCUnion;
using OCUnion.Transfer;
using OCUnion.Transfer.Model;
using ServerCore.Model;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class ServerInformation : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request5UserInfo;

        public int ResponseTypePackage => (int)PackageType.Response6UserInfo;

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
                    case (long)ServerInfoType.Full:
                        {
                            var result = GetModelInfo(context.Player);
                            return result;
                        }

                    case (long)ServerInfoType.SendSave:
                        {
                            if (context.PossiblyIntruder)
                            {
                                context.Disconnect("Possibly intruder");
                                return null;
                            }
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

                            result.SaveFileData = Repository.GetSaveData.LoadPlayerData(context.Player.Public.Login, 1);

                            if (result.SaveFileData != null)
                            {
                                if (context.Player.MailsConfirmationSave.Count > 0)
                                {
                                    for (int i = 0; i < context.Player.MailsConfirmationSave.Count; i++) 
                                        context.Player.MailsConfirmationSave[i].NeedSaveGame = false;

                                    Loger.Log($"MailsConfirmationSave add {context.Player.MailsConfirmationSave.Count} (mails={context.Player.Mails.Count})");
                                    //Ого! Игрок не сохранился после приема письма, с обязательным сохранением после получения
                                    //Отправляем письма ещё раз
                                    if (context.Player.Mails.Count == 0)
                                    {
                                        context.Player.Mails = context.Player.MailsConfirmationSave.ToList();
                                    }
                                    else
                                    {
                                        var ms = context.Player.MailsConfirmationSave
                                            .Where(mcs => context.Player.Mails.Any(m => m.GetHashBase() != mcs.GetHashBase()))
                                            .ToList();
                                        context.Player.Mails.AddRange(ms);
                                    }
                                    Loger.Log($"MailsConfirmationSave (mails={context.Player.Mails.Count})");
                                }
                            }
                            Loger.Log($"Load World for {context.Player.Public.Login}. (mails={context.Player.Mails.Count}, fMails={context.Player.FunctionMails.Count})");

                            return result;
                        }

                    case (long)ServerInfoType.FullWithDescription:
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
                    case (long)ServerInfoType.Short:
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
            var needCreateWorld = Repository.GetSaveData.GetListPlayerDatas(player.Public.Login).Count == 0;
            if (needCreateWorld) BeforeBeginSettlement(player);

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
                    NeedCreateWorld = needCreateWorld,
                    ServerTime = DateTime.UtcNow,
                    IsModsWhitelisted = ServerManager.ServerSettings.IsModsWhitelisted,
                    ServerName = ServerManager.ServerSettings.ServerName,
                    DelaySaveGame = player.SettingDelaySaveGame,
                    DisableDevMode = ServerManager.ServerSettings.DisableDevMode,
                    MinutesIntervalBetweenPVP = ServerManager.ServerSettings.MinutesIntervalBetweenPVP,
                    EnableFileLog = player.SettingEnableFileLog,
                    TimeChangeEnablePVP = player.TimeChangeEnablePVP,
                    GeneralSettings = ServerManager.ServerSettings.GeneralSettings,
                    ProtectingNovice = ServerManager.ServerSettings.ProtectingNovice,
                };

            return result;
        }

        private void BeforeBeginSettlement(PlayerServer player)
        {
            player.GameProgress = new PlayerGameProgress();
            player.AttacksWonCount = 0;
            player.AttacksInitiatorCount = 0;

            player.StartMarketValue = 0;
            player.StartMarketValuePawn = 0;
            player.TotalRealSecond = 0;
            /* //если сбрасывать для новых поселений, то можно не заметить следов абуза в отчете
            player.MaxDeltaGameMarketValue = 0;
            player.MaxDeltaGameMarketValuePawn = 0;
            player.MaxDeltaRealMarketValue = 0;
            player.MaxDeltaRealMarketValuePawn = 0;
            */
        }
    }
}