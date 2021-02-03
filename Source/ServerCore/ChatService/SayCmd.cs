using Model;
using OCUnion;
using OCUnion.Transfer.Types;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Transfer;
using Transfer.ModelMails;

namespace ServerOnlineCity.ChatService
{
    class SayCmd : IChatCmd
    {
        public string CmdID => "say";

        //todo: только для модераторов и админов?
        public Grants GrantsForRun => Grants.SuperAdmin | Grants.Moderator | Grants.DiscordBot;

        public string Help => ChatManager.prefix + "say {UserLogin | system} {/color} {Label} {text}";

        private readonly ChatManager _chatManager;

        public SayCmd(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM)
        {
            var ownLogin = player.Public.Login;

            PlayerServer targetPlayer = Repository.GetPlayerByLogin(argsM[1]);

            ModelMailMessadge.MessadgeTypes type = ModelMailMessadge.MessadgeTypes.Neutral;
            string label;
            string text = "";
            int argNum = 2;
            if(argsM[argNum][0] == '/')
            {
                var str = argsM[argNum].ToLower();
                switch (str)
                {
                    case "/treatbig":
                        type = ModelMailMessadge.MessadgeTypes.ThreatBig;
                        break;
                    case "/treatsmall":
                        type = ModelMailMessadge.MessadgeTypes.ThreatSmall;
                        break;
                    case "/death":
                        type = ModelMailMessadge.MessadgeTypes.Death;
                        break;
                    case "/negative":
                        type = ModelMailMessadge.MessadgeTypes.Negative;
                        break;
                    case "/positive":
                        type = ModelMailMessadge.MessadgeTypes.Positive;
                        break;
                    case "/visitor":
                        type = ModelMailMessadge.MessadgeTypes.Visitor;
                        break;
                }
                argNum++;
            }

            label = argsM[argNum++];

            while(argNum < argsM.Count())
            {
                text += argsM[argNum++] + " ";
            }

            //формируем пакет
            var packet = new ModelMailMessadge();
            packet.From = player.Public;
            packet.To = targetPlayer.Public;
            packet.type = type;
            packet.label = label;
            packet.text = text;
            packet.NeedSaveGame = true;

            Loger.Log("say to " + targetPlayer.Public.Login);

            lock (targetPlayer)
            { 
                targetPlayer.Mails.Add(packet);
            }

            return new ModelStatus() { Status = 0 };
        }
    }
}
