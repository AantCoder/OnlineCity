using OCUnion;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ServerOnlineCity
{
    class Program
    {
        private static ServerManager Manager;

        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // fix error encoding 1252 from encoding with netcore framework

            var defaultPath = args.Length > 0 ? args[0] : "World";
            var workPath = Path.Combine(Directory.GetCurrentDirectory(), defaultPath);
            Directory.CreateDirectory(workPath);

            //Loger.LogMessage += (msg) => Console.WriteLine(msg);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            Manager = new ServerManager();
            if (!Manager.StartPrepare(workPath)) return;

            var th = new Thread(CommandInput);
            th.IsBackground = true;
            th.Start();

            Manager.Start();
        }

        private static void CommandInput()
        {
            Thread.Sleep(500);
            Console.WriteLine(Manager.SendKeyCommandHelp);

            writeDiscordTokenToConsole();
            while (true)
            {
                Thread.Sleep(500);
                var key = Console.ReadKey(true);

                if (!Manager.SendKeyCommand(key.KeyChar)) Console.WriteLine($"Unknow command: {char.ToUpper(key.KeyChar)}");
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd-hh-mm");
            var fileName = Path.Combine(Directory.GetCurrentDirectory(), "!UnhandledException" + date + ".log");
            File.WriteAllText(fileName, e.ExceptionObject.ToString(), Encoding.UTF8);
        }

        private static void writeDiscordTokenToConsole()
        {
            Console.WriteLine("");
            Console.WriteLine("Add discord bot client \"Rimworld Online Bot\" to you Discord server, with grants for read, delete and wrire messages");
            Console.WriteLine("https://discordapp.com/oauth2/authorize?&client_id=638000465740824614&scope=bot&permissions=397284551680");
            Console.WriteLine("Then type this in a discord channel to synchronize messages in the discord");
            Console.WriteLine("Working rule: one discord channel for one Onlyne city server");
            var discordServerToken = Repository.GetPlayerByLogin("discord").DiscordToken;            
            Console.WriteLine("OC! Reg IP_Server:Port Bot_Token");
            // Получение имени компьютера.
            String host = System.Net.Dns.GetHostName();
            // Получение ip-адреса.
            System.Net.IPAddress ip = System.Net.Dns.GetHostEntry(host).AddressList.First(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            Console.WriteLine("May be ip server need for change: ");
            Console.WriteLine($"OC! Reg {ip}:{ServerManager.ServerSettings.Port} {discordServerToken}");
            Console.WriteLine("");
        }
    }
}
