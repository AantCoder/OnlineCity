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
        public string CmdID => "help";

        public Grants GrantsForRun => Grants.UsualUser | Grants.DiscordBot;

        public string Help => "list of all commands";

        private readonly ChatManager _chatManager;

        public HelpCmd(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }
        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> param, ServiceContext context)
        {
            var userGrants = player.Public.Grants;

            var sb = new StringBuilder();
            foreach (var cmd in ChatManager.ChatCmds.Values.OrderBy(x => x.CmdID))
            {
                if (!Grants.NoPermissions.Equals(cmd.GrantsForRun & userGrants))
                {
                    sb.AppendLine(cmd.Help);
                }
            }

            return _chatManager.PostCommandPrivatPostActivChat(0, player.Public.Login, chat, sb.ToString());
        }
    }
}
