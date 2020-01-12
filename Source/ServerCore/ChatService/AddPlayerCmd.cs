using Model;
using OCUnion;
using OCUnion.Transfer.Types;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System.Collections.Generic;
using Transfer;

namespace ServerOnlineCity.ChatService
{
    internal sealed class AddPlayerCmd : IChatCmd
    {
        public string CmdID => "addplayer";

        public Grants GrantsForRun => Grants.UsualUser;

        public string Help => ChatManager.prefix + "addplayer :Add player to private chat";

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM)
        {
            if (argsM.Count < 1)
            {
                return ChatManager.PostCommandPrivatPostActivChat(ChatCmdResult.PlayerNameEmpty, player.Public.Login, chat, "Player name is empty");
            }

            return ChatManager.PostCommandAddPlayer(player, chat, argsM[0]);
        }
    }
}
