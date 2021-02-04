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

        //todo: только для модераторов и админов?
        public Grants GrantsForRun => Grants.UsualUser | Grants.SuperAdmin | Grants.Moderator | Grants.DiscordBot;

        public string Help => ChatManager.prefix + "call {raid|caravan|...} {UserLogin} [{params}]";
        //  /call raid Aant 4 air

        private readonly ChatManager _chatManager;

        public CallIncidentCmd(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM)
        {
            var ownLogin = player.Public.Login;
            
            //базовая проверка аргументов
            if (argsM.Count < 2)
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat,
                    "OC_Incidents_CallIncidents_Err1".NeedTranslate());

            //собираем данные
            var type = CallIncident.ParseIncidentTypes(argsM[0]); 

            PlayerServer targetPlayer = Repository.GetPlayerByLogin(argsM[1]);
            if (targetPlayer == null)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.UserNotFound, ownLogin, chat, "User " + argsM[1] + " not found");
            }

            int mult = 1;
            if (argsM.Count > 2)
            {
                mult = Int32.Parse(argsM[2]);
            }

            //  walk, random, air
            IncidentArrivalModes? arrivalMode = null;
            if (argsM.Count > 3)
            {
                arrivalMode = CallIncident.ParseArrivalMode(argsM[3]);
            }

            string faction = null;
            if (argsM.Count > 4)
            {
                faction = CallIncident.ParseFaction(argsM[4]);
            }

            var error = CallIncident.CreateIncident(player, targetPlayer, type, mult, arrivalMode, faction);
            if (error != null) return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat, error);

            //var msg = argsM[0] + " lvl " + mult + " for user " + targetPlayer.Public.Login + " from " + ownLogin;
            //_chatManager.AddSystemPostToPublicChat(msg);

            return new ModelStatus() { Status = 0 };
        }
    }
}
