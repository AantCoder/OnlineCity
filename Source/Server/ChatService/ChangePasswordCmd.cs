using Model;
using OCUnion;
using OCUnion.Transfer.Types;
using ServerOnlineCity;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Transfer;
using Util;

namespace ServerOnlineCity.ChatService
{
    internal sealed class ChangePasswordCmd : IChatCmd
    {
        public string CmdID => "changepassword";

        public Grants GrantsForRun => Grants.SuperAdmin | Grants.Moderator | Grants.DiscordBot;

        public string Help => ChatManager.prefix + "changepassword {UserLogin} {NewPassword}: Change password for UserLogin";

        private readonly ChatManager _chatManager;

        public ChangePasswordCmd(ChatManager chatManager)
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

            var actPlayer = Repository.GetPlayerByLogin(argsM[0]);
            if (actPlayer == null)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.UserNotFound, ownLogin, chat, "User " + argsM[0] + " not found");
            }
            if ((actPlayer.Public.Grants & Grants.SuperAdmin) == Grants.SuperAdmin
                && player != actPlayer)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.UserNotFound, ownLogin, chat, $"Can't change password {argsM[1]}");
            }

            if (argsM.Count < 2)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.PlayerNameEmpty, ownLogin, chat, "NewPassword name is empty");
            }

            var newPassword = argsM[1];
            var pass = new CryptoProvider().GetHash(newPassword);

            actPlayer.Pass = pass;

            Loger.Log("Server changepassword for " + actPlayer.Public.Login + " by " + ownLogin);

            return new ModelStatus() { Status = 0 };
        }
    }
}
