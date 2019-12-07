using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OC.DiscordBotServer.Commands
{
    public class MessageParser
    {
        private readonly DiscordSocketClient _discordClient;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;

        public MessageParser(IServiceProvider services)
        {
            _services = services;
            _discordClient = services.GetService(typeof (DiscordSocketClient)) as DiscordSocketClient;
            _commands = services.GetService(typeof (CommandService)) as CommandService;
        }

        public async Task Execute(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;

            if (message is null || message.Author.IsBot)
            {
                return;
            }
            int argPos = 0;

            if (message.HasStringPrefix(Program.PX, ref argPos) || message.HasMentionPrefix(_discordClient.CurrentUser, ref argPos))
            {
                var context = new SocketCommandContext(_discordClient, message);

                var result = await _commands.ExecuteAsync(context, argPos + 1, _services);
                if (!result.IsSuccess)
                {
                    Console.WriteLine(result.ErrorReason);
                }
            }
        }
    }
}
