using Model;
using OCUnion;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Transfer;

namespace ServerOnlineCity.ChatService
{
    internal sealed class HelpCmd : IChatCmd
    {
        public string CmdID => ChatManager.prefix + "help";

        public Grants GrantsForRun => Grants.UsualUser | Grants.DiscordBot;

        public string Help => "list of all commands";

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> param)
        {
            var userGrants = player.Public.Grants;

            var sb = new StringBuilder();
            foreach (var cmd in ChatManager.ChatCmds.Values.OrderBy(x => x.CmdID))
            {
                if (cmd.GrantsForRun.HasFlag(userGrants))
                {
                    sb.AppendLine(cmd.Help);
                }
            }

            return ChatManager.PostCommandPrivatPostActivChat(0, player.Public.Login, chat, sb.ToString());
        }
    }
}
