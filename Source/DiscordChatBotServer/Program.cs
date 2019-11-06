using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DiscordChatBotServer
{
    class Program
    {
        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();       

        private DiscordSocketClient _discordClient;
        private CommandService _commands;
        private IServiceProvider _services;
        public async Task RunBotAsync()
        {
            _discordClient = new DiscordSocketClient();
            _commands = new CommandService();
            _services = new ServiceCollection()
                .AddSingleton(_discordClient)
                .AddSingleton(_commands)
                .BuildServiceProvider();

            string botToken = "NjM4MDAwNDY1NzQwODI0NjE0.Xbhuzw.5NLkzWao9Hn_TotrUF6qR2bclzI";

            // event subscription
            _discordClient.Log += _discordClient_Log;

            await RegisterCommandAsync();
            await _discordClient.LoginAsync(Discord.TokenType.Bot, botToken);
            await _discordClient.StartAsync();
            await Task.Delay(-1);
        }

        private Task _discordClient_Log(Discord.LogMessage arg)
        {
            Console.WriteLine(arg);

            return Task.CompletedTask;
        }

        public async Task RegisterCommandAsync()
        {
            _discordClient.MessageReceived += _discordClient_MessageReceived;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task _discordClient_MessageReceived(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;

            if (message is null || message.Author.IsBot) return ;
            int argumentPosition;
            int argPos = 0;

            if (message.HasStringPrefix("!", ref argPos) || message.HasMentionPrefix(_discordClient.CurrentUser, ref argPos))
            {
                var context = new SocketCommandContext(_discordClient, message);

                var result = await _commands.ExecuteAsync(context, argPos, _services);

                if (!result.IsSuccess) 
                {
                    Console.WriteLine(result.ErrorReason);
                }
            }
        }
    }
}
