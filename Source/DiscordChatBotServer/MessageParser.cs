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

            var context = new SocketCommandContext(_discordClient, message);
            // обработка команды явно обращенной к боту. 
            if (message.HasStringPrefix(Program.PX, ref argPos) || message.HasMentionPrefix(_discordClient.CurrentUser, ref argPos))
            {
                var result = await _commands.ExecuteAsync(context, argPos + 1, _services);
                if (!result.IsSuccess)
                {
                    message.Channel.SendMessageAsync(result.ErrorReason);
                    Console.WriteLine(result.ErrorReason);
                }

                return;
            }

            var idServer = message.Channel.Id;
            // проверяем что сообщение находится в заригестрированном канале, и если да, то обрабатываем его дальше
            // check message: typed in registred channel  if not  exit

            if (!_app.UserOnServers.TryGetValue(idServer, out ConcurrentDictionary<ulong, OCUser> users))
            {
                return;
            }

            var privateChannel = await message.Author.GetOrCreateDMChannelAsync();
            if (!users.TryGetValue(message.Author.Id, out OCUser user))
            {
                // отправим приватное сообщение пользователю что он не зарегистрирован и удалим его сообщение с канала
                await privateChannel.SendMessageAsync(Translator.InfUserNotFound);
                await message.DeleteAsync();
                return;
            }


            if (_app.DiscrordToOCServer.TryGetValue(idServer, out var sessionClient))
            {
                if (!sessionClient.IsLogined)
                {
                    await privateChannel.SendMessageAsync(Translator.ErrServerNotAvailable);
                }

                var res = sessionClient.PostingChat(user.OCLogin, message.Content, message.Id, context.IsPrivate);
                if (res == null)
                {
                    sessionClient.Disconnected("Error ");
                }

                if (!string.IsNullOrEmpty(res.Message))
                {
                    await privateChannel.SendMessageAsync(res.Message);
                }
            }
        }
    }
}