using Discord.Commands;
using DiscordChatBotServer.Commands;
using System.Threading.Tasks;

namespace DiscordChatBotServer.Modules
{
    public abstract class BaseModule : ModuleBase<SocketCommandContext>
    {
        protected async Task ExecuteCommand(ICommand command)
        {
            if (!command.CanExecute(this.Context, out string message))
            {
                await ReplyAsync(message);
                return;
            }

            await ReplyAsync(command.Execute(this.Context));
        }
    }
}
