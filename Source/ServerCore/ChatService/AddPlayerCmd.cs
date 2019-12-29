using Model;
using OCUnion;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System.Collections.Generic;

namespace ServerOnlineCity.ChatService
{
    internal sealed class AddPlayerCmd : IChatCmd
    {
        public string CmdID => "addplayer";

        public Grants GrantsForRun => Grants.UsualUser;

        public string Help => ChatManager.prefix + "addplayer :Add player to private chat";

        public void Execute(ref PlayerServer player, Chat chat, List<string> argsM)
        {
            //Loger.Log("Server addPlayer");
            if (argsM.Count < 1)
            {
                ChatManager.PostCommandPrivatPostActivChat(player, chat, "Player name is empty");
            }
            else
            {
                ChatManager.PostCommandAddPlayer(player, chat, argsM[0]);
            }
        }
    }
}
