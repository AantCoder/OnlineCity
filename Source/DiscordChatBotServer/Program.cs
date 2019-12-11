using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using OCUnion;
using OC.DiscordBotServer.Models;
using OC.DiscordBotServer.Repositories;
using Microsoft.EntityFrameworkCore;
using OC.DiscordBotServer.Commands;
using System.IO;
using System.Text;

//https://discord.foxbot.me/docs/api/
namespace OC.DiscordBotServer
{
    class Program
    {
        private DiscordSocketClient _discordClient;
        private CommandService _commands;
        private IServiceProvider _services;
        private ApplicationContext _appContext;
        private MessageParser _messageParser;

        /// <summary>
        /// Prefix OC Online City 
        /// </summary>
        public const string PX = "!OC";
        public const string PathToDb = "Filename=..\\BotDB.sqlite3";
        static void Main(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                Console.WriteLine("DiscordChatBotServer.exe BotToken [PathToLog]");
                return;
            }

            if (args.Length == 2)
            {
                Loger.PathLog = args[1];
            }
            else
            {
                Loger.PathLog = Environment.CurrentDirectory;
            }

            new Program().RunBotAsync(args[0]).GetAwaiter().GetResult();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            var date = DateTime.Now.ToString("yyyy-MM-dd-hh-mm");
            var fileName = Path.Combine(path, "!UnhandledException" + date + ".log");
            File.WriteAllText(fileName, e.ExceptionObject.ToString(), Encoding.UTF8);
        }

        public async Task RunBotAsync(string botToken)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            _discordClient = new DiscordSocketClient();
            _commands = new CommandService();
            var optionsBuilder = new DbContextOptionsBuilder<BotDataContext>();
            var options = optionsBuilder
                .UseSqlite(PathToDb)
                .Options;

            var services = new ServiceCollection()
                .AddSingleton<DiscordSocketClient>(_discordClient)
                .AddSingleton<ApplicationContext>()
                .AddSingleton<BotDataContext>(new BotDataContext(options))
                .AddSingleton<CommandService>(_commands)
                .AddSingleton<OCUserRepository>()
                .AddSingleton<Chanel2ServerRepository>()
                .AddSingleton<IRepository<OCUser>>(x => x.GetService<OCUserRepository>())
            .AddSingleton<IRepository<Chanel2Server>>(x => x.GetService<Chanel2ServerRepository>());

            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (!type.IsClass)
                {
                    continue;
                }

                if (type.GetInterface("ICommand") != null)
                {
                    services.AddSingleton(type);
                }
            }
            _services = services
                .AddSingleton<Listener>()
                .AddTransient<ChannelDestroyedCommand>()
                .BuildServiceProvider();

            _discordClient.Log += _discordClient_Log;
            _discordClient.ChannelDestroyed += _discordClient_ChannelDestroyed;

            await RegisterCommandAsync();
            await _discordClient.LoginAsync(Discord.TokenType.Bot, botToken);
            await _discordClient.StartAsync();

            var listener = _services.GetService<Listener>();
            const int WAIT_LOGIN_DISCORD_TIME = 3000;
            const int REFRESH_TIME = 500;
            var t = new System.Threading.Timer((a) => { listener.UpdateChats(); }, null, WAIT_LOGIN_DISCORD_TIME, REFRESH_TIME);

            await Task.Delay(-1);
        }

        private Task _discordClient_ChannelDestroyed(SocketChannel channel)
        {
            var cmd = _services.GetService<ChannelDestroyedCommand>();
            cmd.Execute(channel.Id);
            return Task.CompletedTask;
        }

        private Task _discordClient_Log(Discord.LogMessage arg)
        {
            if (arg.Message != null)
            {
                Task.Factory.StartNew(() => { Loger.Log(arg.Message); });
                Console.WriteLine(arg.Message);
            }

            if (arg.Exception != null)
            {
                Task.Factory.StartNew(() => { Loger.Log(arg.Exception.ToString()); });
            }

            return Task.CompletedTask;
        }

        public async Task RegisterCommandAsync()
        {
             _messageParser = new MessageParser(_services);
             _discordClient.MessageReceived += _messageParser.Execute;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }
    }
}