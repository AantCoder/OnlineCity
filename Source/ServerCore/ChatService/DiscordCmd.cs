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
    internal sealed class DiscordCmd : IChatCmd
    {
        public string CmdID => "discord";

        public Grants GrantsForRun => Grants.UsualUser;

        public string Help => ChatManager.prefix + "discord : Request Discord token";

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM)
        {
            var myLogin = player.Public.Login;
            if ("discord".Equals(myLogin.ToLower()))
            {
                return ChatManager.PostCommandPrivatPostActivChat(ChatCmdResult.AccessDeny, myLogin, chat, "impossible get information for this user");
            }

            if (argsM.Count == 0)
            {
                Loger.Log($"User {player.Public.Login} request Discord token");
                var playerServer = Repository.GetPlayerByLogin(myLogin);
                if (playerServer.DiscordToken.Equals(Guid.Empty))
                {
                    playerServer.DiscordToken = Guid.NewGuid();
                    Repository.Get.ChangeData = true;
                }

                return ChatManager.PostCommandPrivatPostActivChat(0, myLogin, chat, playerServer.DiscordToken.ToString());
            }

            if (!player.Public.Grants.HasFlag(Grants.SuperAdmin))
            {
                return ChatManager.PostCommandPrivatPostActivChat(ChatCmdResult.AccessDeny, myLogin, chat, "Command only for admin");
            }

            if (argsM[0] == "servertoken")
            {
                Loger.Log($"User {player.Public.Login} request DiscordServer token");
                var serverToken = Repository.GetPlayerByLogin("discord").DiscordToken;
                return ChatManager.PostCommandPrivatPostActivChat(0, myLogin, chat, serverToken.ToString());
            }

            return ChatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, ChatManager.InvalidCommand);
        }
    }
}