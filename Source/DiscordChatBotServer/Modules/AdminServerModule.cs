using Discord.Commands;
using DiscordChatBotServer.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordChatBotServer.Commands;


namespace DiscordChatBotServer.Modules
{
    /// <summary>
    /// RU: Команды только для администратора сервера RimworldOnlineCity
    /// EN: Commands only for admin server RimworldOnlineCity
    /// </summary>
    public class AdminServerModule : BaseModule
    {
        private readonly RegCommand _regCmd;
        private readonly UnRegCommand _unRegCommand;

        public AdminServerModule(RegCommand reg, UnRegCommand unRegCommand)
        {
            _regCmd = reg;
            _unRegCommand = unRegCommand;
        }

        [Command("reg")]
        [Description("Register a new server RimWorldOnlineCity on the Discord channel: " +
            "\n type /Discord in Game for get token. Remember! " +
            "\n Token is Secret! "
            + Helper.PREFIX + "reg [DiscordToken]")]
        //RU: Регистрирует новый сервер RimWorldOnlineCity на канале Discord: reg IP_server
        public async Task RegAsync()
        {
            await ExecuteCommand(_regCmd);
        }

        [Command("unreg")]
        [Description("unreg сервер на канале: unreg")]
        public async Task UnregAsync()
        {
            await ExecuteCommand(_unRegCommand);
        }

        [Command("killhimplease")]
        [Description("killhimplease")]
        public async Task KillmyallpleaseAsync()
        {
            await ReplyAsync("This command apply only in Game");
        }
    }
}
