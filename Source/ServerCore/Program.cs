﻿using System;
using System.Diagnostics;
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
            Thread.Sleep(3000);
            Console.WriteLine(@"Available console commands: Q - quit (shutdown)
R - restart (shutdown and start), 
L - command to save players for shutdown (EverybodyLogoff)
S - save player statistics file");

            writeDiscordTokenToConsole();

            while (true)
            {
                Thread.Sleep(500);
                var key = Console.ReadKey(true);
                if (char.ToLower(key.KeyChar) == 'q') Manager.SaveAndQuit();
                if (char.ToLower(key.KeyChar) == 'r') Manager.SaveAndRestart();
                else if (char.ToLower(key.KeyChar) == 'l') Manager.EverybodyLogoff();
                else if (char.ToLower(key.KeyChar) == 's') Manager.SavePlayerStatisticsFile();
                else Console.WriteLine($"Unknow command: {char.ToUpper(key.KeyChar)}");
            }
        }

        private static void writeDiscordTokenToConsole()
        {
            Console.WriteLine("Add discord bot to you server, with grants for read, delete and writre messages");
            Console.WriteLine("Then type this in a discord channel to synchronize messages in the discord");
            Console.WriteLine("Working rule: one discord channel for one Onlyne city server");
            var discordServerToken = Repository.GetPlayerByLogin("discord").DiscordToken;
            Console.WriteLine("OC! Reg IP_Server:Port Bot_Token");
            // Получение имени компьютера.
            String host = System.Net.Dns.GetHostName();
            // Получение ip-адреса.
            System.Net.IPAddress ip = System.Net.Dns.GetHostEntry(host).AddressList.First(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            Console.WriteLine($"OC! Reg {ip}:{ServerManager.ServerSettings.Port} {discordServerToken}");
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd-hh-mm");
            var fileName = Path.Combine(Directory.GetCurrentDirectory(), "!UnhandledException" + date + ".log");
            File.WriteAllText(fileName, e.ExceptionObject.ToString(), Encoding.UTF8);
        }
    }
}
