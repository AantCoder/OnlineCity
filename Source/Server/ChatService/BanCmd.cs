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
    internal sealed class BanCmd : IChatCmd
    {
        public string CmdID => "ban";

        public Grants GrantsForRun => Grants.SuperAdmin | Grants.Moderator | Grants.DiscordBot;

        public string Help => ChatManager.prefix + "ban {UserLogin}: The player will be banned on the server by keys and login. Settlement will not be deleted";

        private readonly ChatManager _chatManager;

        public BanCmd(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM, ServiceContext context)
        {
            var ownLogin = player.Public.Login;

            if (argsM.Count < 1)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.PlayerNameEmpty, ownLogin, chat, "Player name is empty");
            }

            var banPlayer = Repository.GetPlayerByLogin(argsM[0]);
            if (banPlayer == null)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.UserNotFound, ownLogin, chat, "User " + argsM[0] + " not found");
            }
            if ((banPlayer.Public.Grants & Grants.SuperAdmin) == Grants.SuperAdmin)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.UserNotFound, ownLogin, chat, $"Can't ban {argsM[0]}");
            }

            //var msg = "User " + killPlayer.Public.Login + " deleted settlements.";
            //_chatManager.AddSystemPostToPublicChat(msg); //раскомментировать, чтобы постить в общем чате всем

            Loger.Log("Server BanCmd " + banPlayer.Public.Login + " by " + ownLogin);
            Repository.AddIntruder(new List<string>() { banPlayer.Public.Login }, " BanCmd by " + ownLogin);
            context.DisconnectLogin(banPlayer.Public.Login, "New BanCmd");

            return new ModelStatus() { Status = 0 };
        }
    }
}