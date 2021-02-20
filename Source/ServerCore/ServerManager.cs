using OCUnion;
using ServerCore.Model;
using ServerOnlineCity.Model;
using System;
using System.Text.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

namespace ServerOnlineCity
{
    public class ServerManager
    {
        public int MaxActiveClientCount = 10000; //todo провверить корректность дисконнекта
        public static ServerSettings ServerSettings = new ServerSettings();

        public static FileHashChecker FileHashChecker;
       
        private ConnectServer Connect = null;
        private int _ActiveClientCount;

        public ServerManager()
        {
            AppDomain.CurrentDomain.AssemblyResolve += Missing_AssemblyResolver;
        }

        private Assembly Missing_AssemblyResolver(object sender, ResolveEventArgs args)
        {
            // var asm = args.Name.Split(",")[0];
            var asm = args.RequestingAssembly.FullName.Split(",")[0];
            var a = Assembly.Load(asm);
            return a;
        }

        public int ActiveClientCount
        {
            get { return _ActiveClientCount; }
        }

        private string GetSettingsFileName(string path)
        {
            return Path.Combine(path, "Settings.json");
        }

        private string GetWorldFileName(string path)
        {
            return Path.Combine(path, "World.dat");
        }

        public bool StartPrepare(string path)
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
                            Loger.Log(error.ErrorMessage);
                        }

                        Console.ReadKey();
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine($"RU: Проверьте настройки сервера {jsonFile}");
                    Console.WriteLine("EN: Check Settings.json");
                    Console.ReadKey();
                    return false;
                }
            }

            MainHelper.OffAllLog = false;
            Loger.PathLog = path;
            Loger.IsServer = true;

            var rep = Repository.Get;
            rep.SaveFileName = GetWorldFileName(path);
            rep.Load();
            CheckDiscrordUser();
            FileHashChecker = new FileHashChecker(ServerSettings);

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
                IsAdmin = true, // возможно по умолчанию запретить ?                
            };

            player.Public.Grants = Grants.GameMaster | Grants.SuperAdmin | Grants.UsualUser | Grants.Moderator;
            player.Public.Id = Repository.GetData.GenerateMaxPlayerId(); // 0 - system, 1 - discord

            Repository.GetData.PlayersAll.Add(player);
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

            foreach (var player in Repository.GetData.PlayersAll)
            {
                var pl = ChatManager.Instance.PublicChat.PartyLogin;
                if (pl.Count == Repository.GetData.PlayersAll.Count) continue;

                changeInPlayers = true;

                if (player.IsAdmin
                    || true //to do переделать это на настройки сервера "в чате доступны все, без учета зон контакта"
                    )
                {
                    if (allLogins == null) allLogins = new HashSet<string>(Repository.GetData.PlayersAll.Select(p => p.Public.Login));
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
                    var plNeed = Repository.GetData.PlayersAll
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
                    var minCostForTrade = 25000; // эту цифру изменять вместе с CaravanOnline.GetFloatMenuOptions()
                    foreach (var player in Repository.GetData.PlayersAll)
                    {
                        if (player.Public.LastSaveTime == DateTime.MinValue) continue;
                        if (player.Public.LastTick > 3600000 / 2) continue;

                        if ((DateTime.UtcNow - player.Public.LastOnlineTime).TotalDays < 7) continue;
                        if ((DateTime.UtcNow - player.LastUpdateTime).TotalDays < 7) continue;
                        if ((DateTime.UtcNow - player.Public.LastSaveTime).TotalDays < 7) continue;

                        if (player.IsAdmin) continue;

                        var costAll = player.CostWorldObjects();
                        if (costAll.BaseCount + costAll.CaravanCount == 0) continue;
                        if (costAll.MarketValue + costAll.MarketValuePawn == 0) continue; //какой-то сбой отсутствия данных
                        if (costAll.MarketValue + costAll.MarketValuePawn > minCostForTrade) continue;

                        var msg = $"User {player.Public.Login} deleted settlements (game abandoned): " +
                            $"cost {costAll.MarketValue + costAll.MarketValuePawn}, " +
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

                Func<DateTime, string> dateTimeToStr = dt => dt == DateTime.MinValue ? "" : dt.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture);

                var content = $"Login;LastOnlineTime;LastOnlineDay;GameDays;BaseCount;CaravanCount;MarketValue;MarketValuePawn" +
                    $";Grants;EnablePVP;EMail;DiscordUserName;IntruderKeys;StartMarketValue;StartMarketValuePawn" +
                    $";MarketValueBy15Day;MarketValuePawnBy15Day;MarketValueByHour;MarketValuePawnByHour;TicksByHour;HourInGame" + Environment.NewLine;
                foreach (var player in Repository.GetData.PlayersAll)
                {
                    var costAll = player.CostWorldObjects();

                    var newLine = $"{player.Public.Login};" +
                        $"{dateTimeToStr(player.Public.LastOnlineTime)};" +
                        $"{(int)(DateTime.UtcNow - player.Public.LastOnlineTime).TotalDays};" +
                        $"{(int)(player.Public.LastTick / 60000)};" +
                        $"{costAll.BaseCount};" +
                        $"{costAll.CaravanCount};" +
                        $"{costAll.MarketValue};" +
                        $"{costAll.MarketValuePawn};" +
                        $"{player.Public.Grants.ToString()};" +
                        $"{(player.Public.EnablePVP ? 1 : 0)};" +
                        $"{player.Public.EMail};" +
                        $"{player.Public.DiscordUserName};" +
                        $"{player.IntruderKeys};" +
                        $"{player.StartMarketValue};" +
                        $"{player.StartMarketValuePawn};" +
                        $"{player.StatMaxDeltaGameMarketValue};" +
                        $"{player.StatMaxDeltaGameMarketValuePawn};" +
                        $"{player.StatMaxDeltaRealMarketValue};" +
                        $"{player.StatMaxDeltaRealMarketValuePawn};" +
                        $"{player.StatMaxDeltaRealTicks};" +
                        $"{player.TotalRealSecond / 60f / 60f};"
                        ;
                    newLine = newLine.Replace(Environment.NewLine, " ")
                        .Replace("/r", "").Replace("/n", "");

                    content += newLine + Environment.NewLine;
                }

                var fileName = Path.Combine(Path.GetDirectoryName(Repository.Get.SaveFileName)
                    , $"Players_{DateTime.Now.ToString("yyyy-MM-dd_hh-mm")}.csv");
                File.WriteAllText(fileName, content, Encoding.UTF8);
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
