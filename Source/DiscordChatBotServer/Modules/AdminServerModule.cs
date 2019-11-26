using Discord.Commands;
using OC.DiscordBotServer.Commands;
using OC.DiscordBotServer.Helpers;
using OCUnion;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

// How fix: A MessageReceived handler is blocking the gateway task
// https://github.com/programming-with-peter/common-issues/blob/master/Issues/GatewayTaskBlocked.md
namespace OC.DiscordBotServer.Modules
{
    /// <summary>
    /// RU: Команды только для администратора сервера RimworldOnlineCity
    /// EN: Commands only for admin server RimworldOnlineCity
    /// </summary>
    public sealed class AdminServerModule : ModuleBase<SocketCommandContext>
    {
        private readonly RegCommand _regCmd;
        private readonly UnRegCommand _unRegCommand;
        private readonly SqlLiteDataContext _appContext;

        public AdminServerModule(SqlLiteDataContext appContext,  RegCommand reg, UnRegCommand unRegCommand)
        {
            _regCmd = reg;
            _unRegCommand = unRegCommand;
            _appContext = appContext;
        }

        [Command("reg", ignoreExtraArgs: false, RunMode = RunMode.Async)]
        [Description("Register a new server RimWorldOnlineCity on the Discord channel: " +
            "\n type \"/Discord ServerToken\" in Game for get token. Remember! " +
            "\n Token is Secret! "
            + Helper.PREFIX + "reg IP_Server DiscordToken")]
        //RU: Регистрирует новый сервер RimWorldOnlineCity на канале Discord: reg IP_server
        public async Task RegAsync(string ip, string token)
        {
           
         
            var s = _regCmd.Execute(Context , ip, token);
            await ReplyAsync(s);
        }

        [Command("killhimplease")]
        [Description("killhimplease")]
        public async Task KillmyallpleaseAsync()
        {
            await ReplyAsync("This command apply only in Game");
        }
    }
}
