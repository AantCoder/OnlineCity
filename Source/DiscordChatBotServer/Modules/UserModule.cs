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
    public class UserModule : ModuleBase<SocketCommandContext>
    {
        [Description("Status server: where started, online player e.t.c ")]
        // RU: Сообщает статус сервера, когда запущен, сколько игроков онлайн и т.д.
        [Command("status")]
        public async Task StatusAsync()
        {
            this.
            // TO DO
            //await ReplyAsync("");
        }

        [Command("regme")]
        [Description("Linking account discord user to RimWorld Online City :" + Helper.PREFIX + "regme [OCLogin]")]
        // RU: Привязывает аккаунт пользователя Discrod к серверу RimworldOnlineCity: !regme Логин_в_RimworldonlineCity
        public async Task RegmeAsync()
        {
            await ReplyAsync("Hello world");
        }

        [Command("help")]
        [Description("List of all commands: " + Helper.PREFIX + "help")]
        //RU: Выводит список команд
        public async Task Helpsync()
        {
            await ReplyAsync("Hello world");
        }
    }
}
