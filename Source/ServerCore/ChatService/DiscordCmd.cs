using Model;
using OCUnion;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServerOnlineCity.ChatService
{
    internal sealed class DiscordCmd : IChatCmd
    {
        public string CmdID => "discord";

        public Grants GrantsForRun => Grants.UsualUser;

        public string Help => ChatManager.prefix + "discord : Request Discord token";

        public void Execute(ref PlayerServer player, Chat chat, List<string> argsM)
        {
            var login = player.Public.Login;
            if ("discord".Equals(login.ToLower()))
            {
                ChatManager.PostCommandPrivatPostActivChat(player, chat, ChatManager.InvalidCommand);
                return;
            }

            if (argsM.Count == 0)
            {
                Loger.Log($"User {player.Public.Login} request Discord token");
                var playerServer = Repository.GetData.PlayersAll.Where(p1 => p1.Public.Login.Equals(login)).FirstOrDefault();
                if (playerServer.DiscordToken.Equals(Guid.Empty))
                {
                    playerServer.DiscordToken = Guid.NewGuid();
                }

                ChatManager.PostCommandPrivatPostActivChat(player, chat, playerServer.DiscordToken.ToString());
                return;
            }

            if (!player.IsAdmin)
            {
                ChatManager.PostCommandPrivatPostActivChat(player, chat, "Command only for admin");
                return;
            }

            if (argsM[0] == "servertoken")
            {
                Loger.Log($"User {player.Public.Login} request DiscordServer token");
                var ServerToken = Repository.GetData.PlayersAll.FirstOrDefault(p => "discord".Equals(p.Public.Login.ToLower()))?.Pass;
                ChatManager.PostCommandPrivatPostActivChat(player, chat, ServerToken);
                return;
            }

            ChatManager.PostCommandPrivatPostActivChat(player, chat, ChatManager.InvalidCommand);
        }
    }
}