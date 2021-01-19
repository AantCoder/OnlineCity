using Model;
using OCUnion;
using OCUnion.Transfer.Types;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Transfer;

namespace ServerOnlineCity.ChatService
{
    class EventCmd : IChatCmd
    {
        public string CmdID => "callraid";

        public Grants GrantsForRun => Grants.UsualUser | Grants.SuperAdmin | Grants.Moderator | Grants.DiscordBot;

        public string Help => ChatManager.prefix + "callraid {UserLogin}";

        private readonly ChatManager _chatManager;

        public EventCmd(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM)
        {

            var ownLogin = player.Public.Login;
            PlayerServer targetPlayer = player;
            
            if (argsM.Count > 0)
            {
               targetPlayer = Repository.GetPlayerByLogin(argsM[0]);
            }

            int mult = 1;
            if(argsM.Count > 1)
            {
                mult = Int32.Parse(argsM[1]);
            }
            mult = mult > 10 ? 10 : mult;

            if (targetPlayer == null)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.UserNotFound, ownLogin, chat, "User " + argsM[0] + " not found");
            }

            var msg = "raid lvl " + mult + " for user " + targetPlayer.Public.Login + " from " + ownLogin;
            _chatManager.AddSystemPostToPublicChat(msg);

            //вызвать у клиента метод TryExecuteEvent через Mail. надо это делать в другом месте

            var packet = new ModelMailTrade();
            packet.Type = ModelMailTradeType.StartEvent;
            packet.To = targetPlayer.Public;
            packet.Tile = mult;

            Loger.Log("Server test raid " + targetPlayer.Public.Login);

            lock (targetPlayer)
            {
                targetPlayer.Mails.Add(packet);
            }

            return new ModelStatus() { Status = 0 };
        }
    }
}
