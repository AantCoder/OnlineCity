using Discord.Commands;
using DiscordChatBotServer.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DiscordChatBotServer.Modules
{
    /// <summary>
    /// RU: Команды только для администратора сервера RimworldOnlineCity
    /// EN: Commands only for admin server RimworldOnlineCity
    /// </summary>
    public class AdminServerModule : ModuleBase<SocketCommandContext>
    {
        [Command("reg")]
        [Description("Register a new server RimWorldOnlineCity on the Discord channel: " + Helper.PREFIX + "reg [DiscordLogin] [password]")]        
        //RU: Регистрирует новый сервер RimWorldOnlineCity на канале Discord: reg IP_server
        public async Task RegAsync()
        {
            // RU: Проверка на дурака: такой сервер не зарегистрирован, и сервер живой, есть необходимые права,
            // RU: Регистрируем сервер и сохраняем параметры: в виде таблицы: IDканала, IPserver
            var ipServer = Context.Message;
            if (!true)
            {
                await ReplyAsync("Hello world");
            }

            int idChanel = -1;
            string ip = string.Empty;

            if (Helper.RegServerOnline(idChanel, ip) && Helper.RegServerOffline(idChanel, ip))
            {
                this.
                await ReplyAsync($"Сongratulation! Server ({ipServer}) related to the channel {this.Context.Channel.Name}");
            }
            else
            {
                await ReplyAsync($"Server not related");
            }
        }

        [Command("unreg")]
        [Description("unreg сервер на канале: unreg")]
        public async Task UnregAsync()
        {
            await ReplyAsync("Hello world");
        }

        [Command("killhimplease")]
        [Description("killhimplease")]
        public async Task KillmyallpleaseAsync()
        {
            await ReplyAsync("Hello world");
        }
    }
}
