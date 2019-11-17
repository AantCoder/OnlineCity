using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DiscordChatBotServer.Commands
{
    public interface ICommand
    {
        bool CanExecute(SocketCommandContext parameter, out string message);
        string Execute(SocketCommandContext parameter);
    }
}
