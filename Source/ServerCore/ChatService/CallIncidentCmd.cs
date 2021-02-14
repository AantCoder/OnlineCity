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
        //  /call raid Aant 4 air

        private readonly ChatManager _chatManager;

        public CallIncidentCmd(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM)
        {
            Loger.Log("IncidentLod CallIncidentCmd Execute 1");
            var ownLogin = player.Public.Login;
            
            //базовая проверка аргументов
            if (argsM.Count < 2)
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat,
                    "OC_Incidents_CallIncidents_Err1");

            //собираем данные
            var type = CallIncident.ParseIncidentTypes(argsM[0]); 

            PlayerServer targetPlayer = Repository.GetPlayerByLogin(argsM[1]);
            if (targetPlayer == null)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.UserNotFound, ownLogin, chat, "User " + argsM[1] + " not found");
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
            if (argsM.Count > 4)
            {
                arrivalMode = CallIncident.ParseArrivalMode(argsM[4]);
            }

            string faction = null;
            if (argsM.Count > 5)
            {
                faction = CallIncident.ParseFaction(argsM[5]);
            }

            Loger.Log("IncidentLod CallIncidentCmd Execute 2");
            var error = CallIncident.CreateIncident(player, targetPlayer, serverId, type, mult, arrivalMode, faction);
            if (error != null)
            {
                Loger.Log("IncidentLod CallIncidentCmd Execute error: " + error);
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat, error);
            }

            //var msg = argsM[0] + " lvl " + mult + " for user " + targetPlayer.Public.Login + " from " + ownLogin;
            //_chatManager.AddSystemPostToPublicChat(msg);

            Loger.Log("IncidentLod CallIncidentCmd Execute 3");

            return new ModelStatus() { Status = 0 };
        }
    }
}
