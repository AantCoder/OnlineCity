using Discord.Commands;
using Discord.WebSocket;
using OC.DiscordBotServer.Models;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace OC.DiscordBotServer.Commands
{
    public class MessageParser
    {
        private readonly DiscordSocketClient _discordClient;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;
        private readonly ApplicationContext _app;

        public MessageParser(IServiceProvider services)
        {
            _services = services;
            _discordClient = services.GetService(typeof(DiscordSocketClient)) as DiscordSocketClient;
            _commands = services.GetService(typeof(CommandService)) as CommandService;
            _app = services.GetService(typeof(ApplicationContext)) as ApplicationContext;
        }

        public async Task Execute(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;

            if (message is null || message.Author.IsBot)
            {
                return;
            }

            int argPos = 0;

            // обработка команды явно обращенной к боту. 
            if (message.HasStringPrefix(Program.PX, ref argPos) || message.HasMentionPrefix(_discordClient.CurrentUser, ref argPos))
            {
                var context = new SocketCommandContext(_discordClient, message);

                var result = await _commands.ExecuteAsync(context, argPos + 1, _services);
                if (!result.IsSuccess)
                {
                    Console.WriteLine(result.ErrorReason);
                }
            }
            else
            {
                var id = message.Channel.Id;
                // проверяем что сообщение находится в зарегестрированном канале, и если да, то обрабатываем его дальше
                if (!_app.UserOnServers.TryGetValue(message.Channel.Id, out ConcurrentDictionary<ulong, OCUser> users))
                {
                    // отправим приватное сообщение пользователю что он не зарегистрирован и удалим его сообщение с канала

                    // await message.DeleteAsync();
                    //var  privateChannel = _discordClient.GetChannel(message.Author.Id) ;
                    // IMessageChannel
                    // IMessageChannel
                    // _discordClient
                    // privateChannel.\Send


                }
            }
        }
    }
}
