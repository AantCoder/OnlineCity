using Discord.Commands;
using Discord.WebSocket;
using OC.DiscordBotServer.Languages;
using OC.DiscordBotServer.Models;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace OC.DiscordBotServer
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

                return;
            }

            var id = message.Channel.Id;
            // проверяем что сообщение находится в заригестрированном канале, и если да, то обрабатываем его дальше
            if (!_app.UserOnServers.TryGetValue(message.Channel.Id, out ConcurrentDictionary<ulong, OCUser> users))
            {
                return;
            }

            if (!users.TryGetValue(message.Author.Id, out OCUser user))
            {
                var privateChannel = await message.Author.GetOrCreateDMChannelAsync();
                // отправим приватное сообщение пользователю что он не зарегистрирован и удалим его сообщение с канала
                await privateChannel.SendMessageAsync(Translator.InfUserNotFound);
                await message.DeleteAsync();
                return;
            }

            // Here may be every commands for a game
        }
    }
}