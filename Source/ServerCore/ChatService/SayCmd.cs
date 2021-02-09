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

        //только для модераторов и админов
        public Grants GrantsForRun => Grants.SuperAdmin | Grants.Moderator | Grants.DiscordBot;

        public string Help => ChatManager.prefix + "/say {UserLogin | system} {/color} {Label} {text}";

        private readonly ChatManager _chatManager;

        public SayCmd(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM)
        {
            var ownLogin = player.Public.Login;
            if (argsM.Count < 3)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat,
                   "Необходимо минимум 3 аргумента: имя игрока, заголовок, текст".NeedTranslate());
            }
            int argNum = 0;

            PlayerServer targetPlayer = Repository.GetPlayerByLogin(argsM[argNum++]);
            if (targetPlayer == null)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.UserNotFound, ownLogin, chat, "User " + argsM[1] + " not found");
            }
            if (targetPlayer == player)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat,
                "Нельзя указывать самого себя".NeedTranslate());
            }

            ModelMailMessadge.MessadgeTypes type = ModelMailMessadge.MessadgeTypes.Neutral;
            string label;
            string text = "";
            
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
                    case "/neutral":
                        type = ModelMailMessadge.MessadgeTypes.Neutral;
                        break;
                    default:
                        return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, ownLogin, chat,
                        "Неверный тип сообщения".NeedTranslate());
                }
                argNum++;
            }

            label = argsM[argNum++];

            while(argNum < argsM.Count)
            {
                text += argsM[argNum++] + " ";
            }

            //формируем пакет
            var packet = new ModelMailMessadge()
            {
                From = player.Public,
                To = targetPlayer.Public,
                type = type,
                label = label,
                text = text,
            };

            Loger.Log("say to " + targetPlayer.Public.Login);

            lock (targetPlayer)
            { 
                targetPlayer.Mails.Add(packet);
            }

            return new ModelStatus() { Status = 0 };
        }
    }
}
