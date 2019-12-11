﻿using Discord.Commands;
using OC.DiscordBotServer.Commands;
using OC.DiscordBotServer.Helpers;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OC.DiscordBotServer.Modules
{
    public sealed class UserModule : ModuleBase<SocketCommandContext>
    {
        private readonly RegmeCommand _regUserCmd;
        private readonly CommandService _commandService;
        private readonly ApplicationContext _app;

        public UserModule(RegmeCommand regUserCmd, CommandService commandService, ApplicationContext app)
        {
            _regUserCmd = regUserCmd;
            _commandService = commandService;
            _app = app;
        }

        [Description("Status server: when started, online player e.t.c ")]
        // RU: Сообщает статус сервера, когда запущен, сколько игроков онлайн и т.д.
        [Command("status")]
        public async Task StatusAsync()
        {
            var allCount = _app.DiscrordToOCServer.Count;
            var onlineCount = _app.DiscrordToOCServer.Values.Count(x => x.IsLogined);
            await ReplyAsync($"Server online\\all: {allCount}\\{onlineCount}");
        }

        [Command("regme")]
        [Description("Linking account discord user to RimWorld Online City :" + Helper.PREFIX + "regme [OCLogin]")]
        // RU: Привязывает аккаунт пользователя Discrod к серверу RimworldOnlineCity: !regme Логин_в_RimworldonlineCity
        public async Task RegmeAsync(string userToken)
        {
            await ReplyAsync(_regUserCmd.Execute(Context, userToken));
        }

        [Command("help")]
        [Description("List of all commands: " + Helper.PREFIX + "help")]
        //RU: Выводит список команд
        public async Task Helpsync()
        {
            var result = new StringBuilder();
            var list = _commandService.Commands.OrderBy(x => x.Name);
            foreach (var c in list)
            {
                var descr = c.Attributes.FirstOrDefault(x => x.GetType().Equals(typeof(DescriptionAttribute))) as DescriptionAttribute;
                if (descr != null)
                {
                    result.AppendLine(descr.Description);
                }
            }

            await ReplyAsync(result.ToString());
        }
    }
}