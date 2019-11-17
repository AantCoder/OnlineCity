using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DiscordChatBotServer
{
    class Program
    {
        /// <summary>
        /// Prefix OC Online City 
        /// </summary>
        public  const string PX = "/OC"; 
        static void Main(string[] args) 
        {
            if (args == null || args.Length !=1) 
            {
                Console.WriteLine("DiscordChatBotServer.exe [tokenBot]");
                return;
            }

            new Program().RunBotAsync(args[0]).GetAwaiter().GetResult();
        }     

        private DiscordSocketClient _discordClient;
        private CommandService _commands;
        //private SqlLiteProvider _sqlLiteProvider;
        private IServiceProvider _services;

        public async Task RunBotAsync(string botToken)
        {
            _discordClient = new DiscordSocketClient();
            _commands = new CommandService();
            //_sqlLiteProvider = new SqlLiteProvider("BotDatabase.sqlite3");
            _services = new ServiceCollection()
                .AddSingleton(_discordClient)
                .AddSingleton(_commands)
                .AddSingleton<ApplicationContext>()
                //.AddSingleton(_sqlLiteProvider)
                .BuildServiceProvider();

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
            int argPos = 0;

            if (message.HasStringPrefix(PX, ref argPos) || message.HasMentionPrefix(_discordClient.CurrentUser, ref argPos))
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
