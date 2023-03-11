using Model;
using OCUnion;
using OCUnion.Transfer.Types;
using ServerOnlineCity.Mechanics;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Transfer;
using Transfer.ModelMails;

namespace ServerOnlineCity.ChatService
{
    class CallIncidentCmd : IChatCmd
    {
        public string CmdID => "call";

        public Grants GrantsForRun => Grants.UsualUser | Grants.SuperAdmin | Grants.Moderator | Grants.DiscordBot;

        public string Help => ChatManager.prefix + "call {raid|caravan|...} {UserLogin} {serverId|0} {level 1-10} [{params}]";
        //  /call raid Aant 4 1 air
        //  /call def online 0 0 ThrumboPasses

        private readonly ChatManager _chatManager;

        public CallIncidentCmd(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }
        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM, ServiceContext context)
        {
            return StartIncident(player, argsM, chat, _chatManager);
        }
        public static ModelStatus StartIncident(PlayerServer player, List<string> argsM, Chat chat, ChatManager chatManager)
        {
            Loger.Log("IncidentLod CallIncidentCmd Execute 1");
            var ownLogin = player.Public.Login;

            int cost = 0;
            for(int i = 0; i < argsM.Count; i++)
            {
                if (argsM[i]?.Contains("cost=") == true)
                {
                    var cs = argsM[i].Substring(argsM[i].IndexOf("cost=") + 5);
                    cost = int.Parse(cs);
                    argsM.RemoveAt(i);
                    break;
                }
            }
            
            //базовая проверка аргументов
            if (argsM.Count < 2)
                return chatManager?.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat,
                    "OC_Incidents_CallIncidents_Err1");

            //собираем данные
            var type = CallIncident.ParseIncidentTypes(argsM[0]); 
            if (type == null)
            {
                return chatManager?.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, ownLogin, chat, "Command " + argsM[0] + " not found");
            }
            if (type == IncidentTypes.Def && !player.IsAdmin)
            {
                return chatManager?.PostCommandPrivatPostActivChat(ChatCmdResult.AccessDeny, ownLogin, chat, "Command only for admin");
            }

            bool online = false;
            bool all = false;
            PlayerServer targetPlayer = null;
            if (argsM[1] == "online")
            {
                online = true;
            }
            else if (argsM[1] == "all")
            {
                all = true;
            }
            else
            {
                targetPlayer = Repository.GetPlayerByLogin(argsM[1]);
                if (targetPlayer == null)
                {
                    return chatManager?.PostCommandPrivatPostActivChat(ChatCmdResult.UserNotFound, ownLogin, chat, "User " + argsM[1] + " not found");
                }
            }
            if (targetPlayer == null && !player.IsAdmin)
            {
                return chatManager?.PostCommandPrivatPostActivChat(ChatCmdResult.UserNotFound, ownLogin, chat, "User " + argsM[1] + " not found");
            }

            long serverId = 0;
            if (argsM.Count > 2)
            {
                serverId = Int64.Parse(argsM[2]);
            }

            int mult = 1;
            if (argsM.Count > 3)
            {
                mult = Int32.Parse(argsM[3]);
            }

            //  walk, random, air
            IncidentArrivalModes? arrivalMode = null;
            string defName = null;
            if (argsM.Count > 4)
            {
                if (type == IncidentTypes.Def)
                {
                    defName = argsM[4];
                }
                else
                    arrivalMode = CallIncident.ParseArrivalMode(argsM[4]);
            }

            string faction = null;
            if (argsM.Count > 5)
            {
                faction = CallIncident.ParseFaction(argsM[5]);
            }

            Loger.Log("IncidentLod CallIncidentCmd Execute 2 " + argsM[1]);
            string error = null;
            if (targetPlayer != null)
                error = CallIncident.CreateIncident(player, targetPlayer, serverId, type, mult, arrivalMode, faction, defName, true);

            if (error != null)
            {
                Loger.Log("IncidentLod CallIncidentCmd Execute error: " + error, Loger.LogLevel.ERROR);
                return chatManager?.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat, error);
            }

            Loger.Log("IncidentLod CallIncidentCmd Execute 3 cost=" + cost);
            var execute = cost > 0
                || (player.Public.Grants & Grants.SuperAdmin) == Grants.SuperAdmin
                || (player.Public.Grants & Grants.Moderator) == Grants.Moderator;

            if (cost > 0 && targetPlayer != null)
            {
                //изымаем золото со склада
                lock (player)
                {
                    var data = Repository.GetData;
                    lock (data)
                    {
                        var thingTemplate = ThingTrade.CreateTradeServer("Gold", 1);
                        var thingRaw = data.OrderOperator.FindThingDef(player, thingTemplate);
                        var thingSort = thingRaw
                            .GroupBy(p => p.Value)
                            .OrderByDescending(g => g.Sum(p => p.Key.Count))
                            .Select(g => g.ToList())
                            .ToList();

                        var thingCount = thingRaw.Sum(p => p.Key.Count);
                        if (cost > thingCount)
                        {
                            Loger.Log($"Server IncidentLod CallIncidentCmd Operation not possible! thingTemplate=" + thingTemplate.PackToString() + " storage=" + thingRaw.Select(p => p.Key).ToStringThing(), Loger.LogLevel.EXCHANGE);

                            return new ModelStatus()
                            {
                                Status = 1,
                                Message = "Operation not possible"
                            };
                        }

                        var needCount = cost;
                        foreach (var things in thingSort)
                        {
                            var thingsStorage = things[0].Value;
                            var thingsCount = things.Sum(t => t.Key.Count);
                            if (needCount > thingsCount) thingTemplate.Count = thingsCount;
                            else thingTemplate.Count = needCount;
                            needCount -= thingTemplate.Count;

                            if (data.OrderOperator.GetFromStorage(thingsStorage.Tile, player, new List<ThingTrade>() { thingTemplate }) == null)
                            {
                                //только для логов:
                                var storage = data.OrderOperator.GetStorage(thingsStorage.Tile, player.Public, false);
                                Loger.Log($"Server IncidentLod CallIncidentCmd Operation not possible! Error! thingTemplate=" + thingTemplate.PackToString() + " storage=" + storage.Things.ToStringThing(), Loger.LogLevel.EXCHANGE);

                                return new ModelStatus()
                                {
                                    Status = 2,
                                    Message = "Operation not possible"
                                };
                            }

                            if (needCount == 0) break;
                        }

                        if (needCount > 0)
                        {
                            Loger.Log($"Server IncidentLod CallIncidentCmd Operation not possible! Error!! thingTemplate=" + thingTemplate.PackToString() + " storage=" + thingRaw.Select(p => p.Key).ToStringThing(), Loger.LogLevel.EXCHANGE);

                            return new ModelStatus()
                            {
                                Status = 3,
                                Message = "Operation not possible"
                            };
                        }

                        Repository.Get.ChangeData = true;
                    }
                }
            }

            
            error = null;
            if (targetPlayer != null)
            {
                Loger.Log("IncidentLod CallIncidentCmd Execute 4 testMode=" + (!execute).ToString());
                error = CallIncident.CreateIncident(player, targetPlayer, serverId, type, mult, arrivalMode, faction, defName, !execute);
            }
            else
            {
                Loger.Log("IncidentLod CallIncidentCmd Execute 4 mode=" + (all ? "all" : online ? "online" : "error"));
                foreach (PlayerServer pl in Repository.GetData.PlayersAll)
                {
                    if (pl.Public.Login == ownLogin) continue;
                    if (player.Public.LastSaveTime == DateTime.MinValue) continue;
                    string err = null;
                    bool allError = true;
                    if (all || pl.Online)
                    {
                        err = CallIncident.CreateIncident(player, pl, serverId, type, mult, arrivalMode, faction, defName, false);
                        if (err == null) allError = false;
                    }
                    if (allError) error = err;
                }
            }
            if (error != null)
            {
                Loger.Log("IncidentLod CallIncidentCmd Execute error: " + error, Loger.LogLevel.ERROR);
                return chatManager?.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat, error);
            }

            //var msg = argsM[0] + " lvl " + mult + " for user " + targetPlayer.Public.Login + " from " + ownLogin;
            //_chatManager.AddSystemPostToPublicChat(msg);

            Loger.Log("IncidentLod CallIncidentCmd Execute 3.");

            return new ModelStatus() { Status = 0 };
        }
    }
}
