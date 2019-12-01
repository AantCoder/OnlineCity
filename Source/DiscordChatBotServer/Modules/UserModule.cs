using Discord.Commands;
using OC.DiscordBotServer.Commands;
using OC.DiscordBotServer.Helpers;
using System.ComponentModel;
using System.Threading.Tasks;

namespace OC.DiscordBotServer.Modules
{
    public sealed class UserModule : ModuleBase<SocketCommandContext>
    {
        private readonly RegisterUserCommand _regUserCmd; 

        public UserModule(RegisterUserCommand regUserCmd) 
        {
            _regUserCmd = regUserCmd;
        }

        [Description("Status server: where started, online player e.t.c ")]
        // RU: Сообщает статус сервера, когда запущен, сколько игроков онлайн и т.д.
        [Command("status")]
        public async Task StatusAsync()
        {
            // TO DO
            //await ReplyAsync("");
        }

        [Command("regme")]
        [Description("Linking account discord user to RimWorld Online City :" + Helper.PREFIX + "regme [OCLogin]")]
        // RU: Привязывает аккаунт пользователя Discrod к серверу RimworldOnlineCity: !regme Логин_в_RimworldonlineCity
        public async Task RegmeAsync(string userToken)
        {            
            await ReplyAsync(_regUserCmd.Execute (Context, userToken));
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
