using OCUnion;
using ServerCore.Model;
using ServerOnlineCity.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using Transfer;
using Util;
using System.Text;
using OCUnion.Transfer.Model;
using OCUnion.Common;
using OCUnion.Transfer;
using ServerOnlineCity.Services;
using System.Net;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace ServerOnlineCity
{
    public class ServerManager
    {
        public int MaxActiveClientCount = 10000;
        public static ServerSettings ServerSettings = new ServerSettings();

        public static FileHashChecker HashChecker;

        private ConnectServer Connect = null;
        private int _ActiveClientCount;

        public ServerManager()
        {
            That = this;
            AppDomain.CurrentDomain.AssemblyResolve += Missing_AssemblyResolver;
        }

        private Assembly Missing_AssemblyResolver(object sender, ResolveEventArgs args)
        {
            // var asm = args.Name.Split(",")[0];
            var asm = args.RequestingAssembly == null 
                ? "Server"
                : args.RequestingAssembly.FullName.Split(',')[0]; //кто это делал? магия перестала работать т_т
            var a = Assembly.Load(asm);
            return a;
        }

        public int ActiveClientCount
        {
            get { return _ActiveClientCount; }
        }

        public string SendKeyCommandHelp = @"Available console commands: 
  Q - quit (shutdown)
  R - restart (shutdown and start)
  L - command to save players for shutdown (EverybodyLogoff)
  S - save player statistics file
  D - save debug info file
  С - run command from file command.txt";

        public static bool SendConsoleCommand(char key) => That.SendKeyCommand(key);
        public static void SendRunCommand(string text) => That.RunCommand(text);

        public bool SendKeyCommand(char key)
        {
            if (char.ToLower(key) == 'q') SaveAndQuit();
            if (char.ToLower(key) == 'r') SaveAndRestart();
            else if (char.ToLower(key) == 'l') EverybodyLogoff();
            else if (char.ToLower(key) == 's') SavePlayerStatisticsFile();
            else if (char.ToLower(key) == 'd') SaveDebugInfo();
            else if (char.ToLower(key) == 'c') RunCommandFile();
            else return false;
            return true;
        }

        public static bool SaveSettings(string storytaller, string difficulty)
        {
            var settings = ServerSettings;
            var general = ServerSettings.GeneralSettings;
            var path = ServerSettings.WorkingDirectory;

            general.Difficulty = difficulty;
            general.StorytellerDef = storytaller;
            settings.GeneralSettings = general;

            var jsonFile = Path.Combine(path, "Settings.json");
            using (StreamWriter file = File.CreateText(jsonFile))
            {
                var jsonText = JsonSerializer.Serialize(settings, new JsonSerializerOptions() { WriteIndented = true });
                file.WriteLine(jsonText);
            }
            return true;
        }

        private string GetSettingsFileName(string path)
        {
            return Path.Combine(path, "Settings.json");
        }

        private string GetWorldFileName(string path)
        {
            return Path.Combine(path, "World.dat");
        }

        public async Task<bool> StartPrepare(string path)
        {
            var jsonFile = GetSettingsFileName(path);
            if (!File.Exists(jsonFile))
            {
                using (StreamWriter file = File.CreateText(jsonFile))
                {
                    var jsonText = JsonSerializer.Serialize(ServerSettings, new JsonSerializerOptions() { WriteIndented = true });
                    file.WriteLine(jsonText);
                }

                Console.WriteLine("Created Settings.json, server was been stopped");
                Console.WriteLine($"RU: Настройте сервер, заполните {jsonFile}");
                Console.WriteLine("Enter some key");
                Console.ReadKey();
                return false;
            }
            else
            {
                try
                {
                    using (var fs = new StreamReader(jsonFile, Encoding.UTF8))
                    {
                        var jsonString = fs.ReadToEnd();
                        ServerSettings = JsonSerializer.Deserialize<ServerSettings>(jsonString);
                    }

                    ServerSettings.WorkingDirectory = path;
                    var results = new List<ValidationResult>();
                    var context = new ValidationContext(ServerSettings);
                    if (!Validator.TryValidateObject(ServerSettings, context, results, true))
                    {
                        foreach (var error in results)
                        {
                            Console.WriteLine(error.ErrorMessage);
                            Loger.Log(error.ErrorMessage, Loger.LogLevel.ERROR);
                        }

                        Console.ReadKey();
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine($"RU: Проверьте настройки сервера {jsonFile}");
                    Console.WriteLine($"EN: Check settings {jsonFile}");
                    Console.ReadKey();
                    return false;
                }
            }

            MainHelper.OffAllLog = false;
            Loger.PathLog = path;
            Loger.IsServer = true;
            Loger.Enable = true;

            var rep = Repository.Get;
            rep.SaveFileName = GetWorldFileName(path);
            rep.Load();
            CheckDiscrordUser();
            HashChecker = await FileHashChecker.FromServerSettings(ServerSettings);

            return true;
        }

        public void Start()
        {
            var rep = Repository.Get;

            //общее обслуживание
            rep.Timer.Add(1000, DoWorld);

            //сохранение, если были изменения
            rep.Timer.Add(ServerSettings.SaveInterval, () =>
            {
                rep.Save(true);
            });

            //ServerManager.ServerSettings.AutoSaveStatisticsFile SavePlayerStatisticsFile()

            //ActiveClientCount = 0;

            Connect = new ConnectServer();
            Connect.ConnectionAccepted = ConnectionAccepted;

            Loger.Log($"Server starting on port: {ServerSettings.Port}");
            Connect.Start(null, ServerSettings.Port);
        }
       
        /// <summary>
        /// check and create if it is necessary DiscrordUser
        /// </summary>
        private void CheckDiscrordUser()
        {
            var isDiscordBotUser = Repository.GetData.PlayersAll.Any(p => "discord" == p.Public.Login);

            if (isDiscordBotUser)
            {
                return;
            }

            var guid = Guid.NewGuid();
            var player = new PlayerServer("discord")
            {
                Pass = new CryptoProvider().GetHash(guid.ToString()),
                DiscordToken = guid,
                Approve = true,
            };

            player.Public.Grants = Grants.GameMaster | Grants.SuperAdmin | Grants.UsualUser | Grants.Moderator;
            player.Public.Id = Repository.GetData.GenerateMaxPlayerId(); // 0 - system, 1 - discord

            Repository.GetData.PlayersAll.Add(player);
            Repository.GetData.UpdatePlayersAllDic();
            Repository.Get.Save();
        }

        private long DoWorldCountRun = -1;

        /// <summary>
        /// Общее обслуживание мира
        /// </summary>
        private void DoWorld()
        {
            DoWorldCountRun++;

            HashSet<string> allLogins = null;
            //Есть ли какие-то изменения в списках пользователей
            bool changeInPlayers = false;

            ///Обновляем кто кого видит

            foreach (var player in Repository.GetData.GetPlayersAll)
            {
                var pl = ChatManager.Instance.PublicChat.PartyLogin;
                if (pl.Count == Repository.GetData.GetPlayersAll.Count()) continue;

                changeInPlayers = true;

                if (player.IsAdmin
                    || true //to do переделать это на настройки сервера "в чате доступны все, без учета зон контакта"
                    )
                {
                    if (allLogins == null) allLogins = new HashSet<string>(Repository.GetData.GetPlayersAll.Select(p => p.Public.Login));
                    lock (player)
                    {
                        ///админы видят всех: добавляем кого не хватает
                        var plAdd = new HashSet<string>(allLogins);
                        plAdd.ExceptWith(ChatManager.Instance.PublicChat.PartyLogin);

                        if (plAdd.Count > 0) pl.AddRange(plAdd);
                    }
                }
                else
                {
                    ///определяем кого видят остальные 
                    //админов
                    var plNeed = Repository.GetData.GetPlayersAll
                        .Where(p => p.IsAdmin)
                        .Select(p => p.Public.Login)
                        .ToList();

                    //те, кто запустил спутники
                    //to do когда сделаем, то потом, может быть, стоит это убрать для тех кто не построил ещё хотя бы консоль связи

                    //и те кто географически рядом
                    //to do

                    //себя и system
                    if (!plNeed.Any(p => p == player.Public.Login)) plNeed.Add(player.Public.Login);
                    if (!plNeed.Any(p => p == "system")) plNeed.Add("system");

                    ///синхронизируем
                    lock (player)
                    {
                        pl.RemoveAll((pp) => !plNeed.Any(p => p == pp));
                        pl.AddRange(plNeed.Where(p => !pl.Any(pp => p == pp)));
                    }
                }
            }

            /// Удаляем колонии за которые давно не заходили, игровое время которых меньше полугода и ценность в них меньше второй иконки

            if (DoWorldCountRun % 10 == 0)
            {
                if (ServerManager.ServerSettings.DeleteAbandonedSettlements)
                {
                    foreach (var player in Repository.GetData.GetPlayersAll)
                    {
                        if (player.Public.LastSaveTime == DateTime.MinValue) continue;
                        if (player.Public.LastTick > 3600000) continue;

                        if ((DateTime.UtcNow - player.Public.LastOnlineTime).TotalDays < 14) continue;
                        if ((DateTime.UtcNow - player.LastUpdateTime).TotalDays < 14) continue;
                        if ((DateTime.UtcNow - player.Public.LastSaveTime).TotalDays < 14) continue;

                        if (player.IsAdmin) continue;

                        var costAll = player.CostWorldObjects();
                        if (costAll.BaseCount + costAll.CaravanCount == 0) continue;
                        if (costAll.MarketValueTotal == 0) continue; //какой-то сбой отсутствия данных
                        if (costAll.MarketValueTotal > MainHelper.MinCostForTrade) continue;

                        var msg = $"User {player.Public.Login} deleted settlements (game abandoned): " +
                            $"cost {costAll.MarketValueTotal}, " +
                            $"game days {player.Public.LastTick / 60000}, " +
                            $"last online (day) {(int)(DateTime.UtcNow - player.Public.LastOnlineTime).TotalDays} ";

                        //блок удаления из AbandonHimSettlementCmd

                        //ChatManager.Instance.AddSystemPostToPublicChat(msg); // раскоментировать, для поста в общий чат

                        player.AbandonSettlement();
                        Loger.Log("Server " + msg);
                    }
                }
            }

            /// Завершение

            if (changeInPlayers)
            {
                Repository.GetData.UpdatePlayersAllDic();
            }
        }

        public void Stop()
        {
            Connect.Stop();
        }

        public void SaveAndQuit()
        {
            try
            {
                Loger.Log("Command SaveAndQuit");
                Thread.CurrentThread.IsBackground = false;
                Connect.Stop();
                Thread.Sleep(100);
                var rep = Repository.Get;
                rep.Save();
                Thread.Sleep(200);
                Loger.Log("Command SaveAndQuit done");
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                Loger.Log("Command Exception " + e.ToString());
            }
        }

        public void SaveAndRestart()
        {
            try
            {
                Loger.Log("Command SaveAndRestart");
                Thread.CurrentThread.IsBackground = false;
                Connect.Stop();
                Thread.Sleep(100);
                var rep = Repository.Get;
                rep.Save();
                Thread.Sleep(200);
                Loger.Log("Restart");
                Process.Start(Process.GetCurrentProcess().MainModule.FileName);
                Loger.Log("Command SaveAndRestart done");
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                Loger.Log("Command Exception " + e.ToString());
            }
        }

        public void EverybodyLogoff()
        {
            try
            {
                Loger.Log("Command EverybodyLogoff");
                //Ниже код из EverybodyLogoffCmd:

                var data = Repository.GetData;
                lock (data)
                {
                    data.EverybodyLogoff = true;
                }

                var msg = "Server is preparing to shut down (EverybodyLogoffCmd)";
                Loger.Log(msg);
            }
            catch (Exception e)
            {
                Loger.Log("Command Exception " + e.ToString());
            }
        }

        public void SavePlayerStatisticsFile()
        {
            try
            {
                var msg = "Command SaveListPlayerFileStats";
                Loger.Log(msg);

                var report = new ReportManager();
                var content = report.GetAllPlayerStatisticsFile();

                var fileName = Path.Combine(Path.GetDirectoryName(Repository.Get.SaveFileName)
                    , $"Players_{DateTime.Now.ToString("yyyy-MM-dd_hh-mm")}.csv");
                File.WriteAllText(fileName, content, Encoding.UTF8);
            }
            catch (Exception e)
            {
                Loger.Log("Command Exception " + e.ToString());
            }
        }

        public void SaveDebugInfo()
        {
            try
            {
                var msg = "Command SaveDebugInfo";
                Loger.Log(msg);

                var report = new ReportManager();
                var content = report.GetDebugInfo();

                var fileName = Path.Combine(Path.GetDirectoryName(Repository.Get.SaveFileName)
                    , $"DebugInfo_{DateTime.Now.ToString("yyyy-MM-dd_hh-mm")}.xml");
                File.WriteAllText(fileName, content, Encoding.UTF8);
            }
            catch (Exception e)
            {
                Loger.Log("Command Exception " + e.ToString());
            }
        }

        public void RunCommandFile()
        {
            try
            {
                Loger.Log("Command RunCommandFile");

                var fileName = Path.Combine(Path.GetDirectoryName(Repository.Get.SaveFileName), "command.txt");
                if (!File.Exists(fileName))
                {
                    Loger.Log("Command RunCommandFile not file " + fileName);
                    return;
                }
                var textCommands = File.ReadAllText(fileName);

                var fileNameReady = Path.Combine(Path.GetDirectoryName(Repository.Get.SaveFileName), "commandReady.txt");
                if (File.Exists(fileNameReady)) File.Delete(fileNameReady);
                File.Move(fileName, fileNameReady);

                RunCommand(textCommands);
            }
            catch (Exception e)
            {
                Loger.Log("Command Exception " + e.ToString());
            }
        }

        public void RunCommand(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                {
                    Loger.Log("Command RunCommandFile not commands");
                    return;
                }

                var commands = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                if (commands.Length == 0)
                {
                    Loger.Log("Command RunCommandFile not commands");
                    return;
                }

                var data = Repository.GetData;
                if (data.PlayersAll.Count < 3)
                {
                    Loger.Log("Command RunCommandFile not admin player");
                    return;
                }

                ServiceContext context = new ServiceContext()
                {
                    AddrIP = "localhost",
                    PossiblyIntruder = false,
                    Player = data.PlayersAll[2],
                    AllSessionAction = SessionsAction
                };
                if (Service.ServiceDictionary.TryGetValue((int)PackageType.Request19PostingChat, out IGenerateResponseContainer generateResponseContainer))
                {
                    var name = generateResponseContainer.GetType().Name;

                    for (int i = 0; i < commands.Length; i++)
                    {
                        var command = commands[i];
                        if (string.IsNullOrEmpty(command)) continue;

                        Loger.Log("Server RunCommandFile " + i + " start: " + (context.Player == null ? "     " : context.Player.Public.Login.PadRight(5))
                            + " " + name);
                        var inputPackage = new ModelContainer()
                        {
                            TypePacket = (int)PackageType.Response20PostingChat,
                            Packet = new ModelPostingChat() { IdChat = 1, Message = command }
                        };
                        var result = generateResponseContainer.GenerateModelContainer(inputPackage, context)?.Packet as ModelStatus;
                        Loger.Log("Server RunCommandFile " + i + " result: " + (result?.Message ?? "None"));
                    }
                }
            }
            catch (Exception e)
            {
                Loger.Log("Command Exception " + e.ToString());
            }
        }

        private void ConnectionAccepted(ConnectClient client)
        {
            if (ActiveClientCount > MaxActiveClientCount)
            {
                client.Dispose();
                return;
            }

            Interlocked.Increment(ref _ActiveClientCount);
            var thread = new Thread(() => DoClient(client));
            thread.IsBackground = true;
            thread.Start();
        }

        private List<SessionServer> Sessions = new List<SessionServer>();

        /// <summary>
        /// Обработать в событии все активные сессии. Корректно завершить выбранные сессии только через этот механизм
        /// </summary>
        /// <param name="act"></param>
        private void SessionsAction(Action<SessionServer> act)
        {
            lock (Sessions)
            {
                for(int i = 0; i < Sessions.Count; i++)
                {
                    if (Sessions[i].IsActive) act(Sessions[i]);
                    if (!Sessions[i].IsActive) Sessions.RemoveAt(i--);
                }
            }
        }

        private void DoClient(ConnectClient client)
        {
            SessionServer session = null;
            string addrIP = ((IPEndPoint)client.Client.Client.RemoteEndPoint).Address.ToString();
            try
            {
                try
                {
                    if (Repository.CheckIsBanIP(addrIP))
                    {
                        Loger.Log("Abort connect BanIP " + addrIP);
                    }
                    else
                    {
                        Loger.Log($"New connect {addrIP} (connects: {ActiveClientCount})");
                        session = new SessionServer();
                        lock (Sessions)
                        {
                            Sessions.Add(session);
                        }
                        session.Do(client, SessionsAction);
                    }
                }
                catch (ObjectDisposedException)
                {
                    Loger.Log("Abort connect Relogin " + addrIP);
                }
                catch (Transfer.ConnectClient.ConnectSilenceTimeOutException)
                {
                    Loger.Log("Abort connect TimeOut " + addrIP);
                }
                catch (Exception e)
                {
                    if (!(e is SocketException) && !(e.InnerException is SocketException)
                        && !(e is Transfer.ConnectClient.ConnectNotConnectedException) && !(e.InnerException is Transfer.ConnectClient.ConnectNotConnectedException))
                    {
                        ExceptionUtil.ExceptionLog(e, "Server Exception");
                    }
                }
                //if (LogMessage != null) LogMessage("End connect");
            }
            finally
            {
                Interlocked.Decrement(ref _ActiveClientCount);
                if (session == null || !session.IsAPI)
                    Loger.Log($"Close connect {addrIP}{(session == null ? "" : " " + session?.GetNameWhoConnect())} (connects: {ActiveClientCount})");
                try
                {
                    if (session != null)
                    {
                        session.Dispose();
                    }
                }
                catch
                { }
            }
        }

    }
}
