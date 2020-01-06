using Model;
using OCUnion;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServerOnlineCity.ChatService
{
    internal sealed class EverybodyLogoffCmd : IChatCmd
    {
        public string CmdID => "everybodylogoff";

        public Grants GrantsForRun => Grants.SuperAdmin;

        public string Help => ChatManager.prefix + "everybodylogoff: all online users will be given a command to save and disconnect, except for the admin, until the server is rebooted";

        public void Execute(ref PlayerServer player, Chat chat, List<string> argsM)
        {
            if (!player.IsAdmin)
            {
                ChatManager.PostCommandPrivatPostActivChat(player, chat, "Command only for admin");
                return;
            }

            var data = Repository.GetData;
            lock (data)
            {
                data.EverybodyLogoff = true;
            }
            Loger.Log("Server is preparing to shut down (EverybodyLogoffCmd)");
        }
    }
}