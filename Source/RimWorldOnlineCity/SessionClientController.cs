using Model;
using OCUnion;
using OCUnion.Common;
using OCUnion.Transfer;
using OCUnion.Transfer.Model;
using RimWorld;
using RimWorld.Planet;
using RimWorldOnlineCity.ClientHashCheck;
using RimWorldOnlineCity.GameClasses.Harmony;
using RimWorldOnlineCity.Lib;
using RimWorldOnlineCity.Services;
using RimWorldOnlineCity.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Transfer;
using Util;
using Verse;
using Verse.Profile;

namespace RimWorldOnlineCity
{
    /// <summary>
    /// Контейнер общих данных и
    /// стандартные повторяющиеся инструкции при работе с классом SessionClient.
    /// </summary>
    public static class SessionClientController
    {
        public static ClientData Data { get; set; }
        public static string ConnectAddr { get; set; }
        public static WorkTimer Timers { get; set; }
        public static WorkTimer TimerReconnect { get; set; }
        public static Player My { get; set; }
        public static TimeSpan ServerTimeDelta { get; set; }

        private const string SaveName = "onlineCityTempLoad";

        private static string SaveFullName { get; set; }

        public static string ConfigPath { get; private set; }

        public static bool LoginInNewServerIP { get; set; }

        public static IClientFileChecker[] ClientFileCheckers { get; private set; }

        /// <summary>
        /// Инициализация при старте игры. Как можно раньше
        /// </summary>
        public static void Init()
        {
            ConfigPath = Path.Combine(GenFilePaths.ConfigFolderPath, "OnlineCity");
            if (!Directory.Exists(ConfigPath))
            {
                Directory.CreateDirectory(ConfigPath);
            }

            SaveFullName = GenFilePaths.FilePathForSavedGame(SaveName);
            MainHelper.CultureFromGame = Prefs.LangFolderName ?? "";

            try
            {
                // ..\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\OnlineCity
                var path = new DirectoryInfo(GenFilePaths.ConfigFolderPath).Parent.FullName;
                var workPath = Path.Combine(path, "OnlineCity");
                Directory.CreateDirectory(workPath);
                Loger.PathLog = workPath;
            }
            catch { }

            Loger.Log("Client Init " + MainHelper.VersionInfo);
            Loger.Log("Client Language: " + Prefs.LangFolderName);

            Loger.Log("Client MainThreadNum=" + ModBaseData.GlobalData.MainThreadNum.ToString());

            Task.Factory.StartNew(() => SessionClientController.CalculateHash());
        }

        public static void CalculateHash()
        {
            UpdateModsWindow.Title = "OC_Hash_CalculateLocalFiles".Translate();
            //Find.WindowStack.Add(new UpdateModsWindow());
            var factory = new ClientFileCheckerFactory();

            var folderTypeValues = Enum.GetValues(typeof(FolderType));
            ClientFileCheckers = new ClientFileChecker[folderTypeValues.Length];
            var filesCount = 0;
            foreach (FolderType folderType in folderTypeValues)
            {
                UpdateModsWindow.Title = "OC_Hash_CalculateFor".Translate() + folderType.ToString();
                ClientFileCheckers[(int)folderType] = factory.GetFileChecker(folderType);
                ClientFileCheckers[(int)folderType].CalculateHash();
                filesCount += ClientFileCheckers[(int)folderType].FilesHash.Count;
            }


            UpdateModsWindow.Title = "OC_Hash_CalculateComplete".Translate();
            UpdateModsWindow.HashStatus = "OC_Hash_CalculateConfFile".Translate() + ClientFileCheckers[(int)FolderType.ModsConfigPath].FilesHash.Count.ToString() + "\n" +
            "Mods files: " + ClientFileCheckers[(int)FolderType.ModsFolder].FilesHash.Count.ToString();
            //Task.Run(() => ClientHashChecker.StartGenerateHashFiles());
        }


        private static object UpdatingWorld = new Object();
        private static int GetPlayersInfoCountRequest = 0;

        private static void UpdateWorld(bool firstRun = false)
        {
            lock (UpdatingWorld)
            {
                Command((connect) =>
                {
                    //собираем пакет на сервер // collecting the package on the server
                    var toServ = new ModelPlayToServer()
                    {
                        UpdateTime = Data.UpdateTime, //время прошлого запроса
                    };
                    //данные сохранения игры // save game data
                    if (Data.SaveFileData != null)
                    {
                        Data.AddTimeCheckTimerFail = true;
                        toServ.SaveFileData = Data.SaveFileData;
                        toServ.SingleSave = Data.SingleSave;
                        Data.SaveFileData = null;
                    }

                    //собираем данные с планеты // collecting data from the planet
                    if (!firstRun) UpdateWorldController.SendToServer(toServ, firstRun, null);

                    if (firstRun)
                    {
                        GetPlayersInfoCountRequest = 0;
                        ModelGameServerInfo gameServerInfo = connect.GetGameServerInfo();
                        UpdateWorldController.SendToServer(toServ, firstRun, gameServerInfo);
                    }

                    //запрос на информацию об игроках. Можно будет ограничить редкое получение для тех кто оффлайн
                    //request for information about players. It will be possible to limit the rare receipt for those who are offline
                    if (Data.Chats != null && Data.Chats[0].PartyLogin != null)
                    {
                        if (Data.Players == null || Data.Players.Count == 0
                            || GetPlayersInfoCountRequest % 5 == 0)
                        {
                            //в начале и раз в пол минуты (5 сек между UpdateWorld * 5) получаем инфу обо всех
                            toServ.GetPlayersInfo = Data.Chats[0].PartyLogin;
                        }
                        else
                        {
                            //в промежутках о тех кто онлайн
                            toServ.GetPlayersInfo = Data.Players.Values.Where(p => p.Online).Select(p => p.Public.Login).ToList();
                        }
                        GetPlayersInfoCountRequest++;
                        //Loger.Log("Client " + My.Login + " UpdateWorld* " + (toServ.GetPlayersInfo.Count.ToString()) 
                        //    + " " + (toServ.GetPlayersInfo.Any(p => p == SessionClientController.My.Login) ? "1" : "0"));
                    }

                    //отправляем на сервер, получаем ответ
                    //we send to the server, we get a response2
                    ModelPlayToClient fromServ = connect.PlayInfo(toServ);
                    Data.AddTimeCheckTimerFail = false;
                    //Loger.Log("Client UpdateWorld 5 ");

                    Loger.Log($"Client {My.Login} UpdateWorld myWO->{toServ.WObjects?.Count}"
                        + ((toServ.WObjectsToDelete?.Count ?? 0) > 0 ? " myWOToDelete->" + toServ.WObjectsToDelete.Count : "")
                        + (toServ.SaveFileData == null || toServ.SaveFileData.Length == 0 ? "" : " SaveData->" + toServ.SaveFileData.Length)
                        + ((fromServ.Mails?.Count ?? 0) > 0 ? " Mail<-" + fromServ.Mails.Count : "")
                        + (fromServ.AreAttacking ? " Attacking!" : "")
                        + (fromServ.NeedSaveAndExit ? " Disconnect command!" : "")
                        + (fromServ.PlayersInfo != null ? " Players<-" + fromServ.PlayersInfo.Count : "")
                        + ((fromServ.WObjects?.Count ?? 0) > 0 ? " WO<-" + fromServ.WObjects.Count : "")
                        + ((fromServ.WObjectsToDelete?.Count ?? 0) > 0 ? " WOToDelete<-" + fromServ.WObjectsToDelete.Count : "")
                        + ((fromServ.FactionOnlineList?.Count ?? 0) > 0 ? " Faction<-" + fromServ.FactionOnlineList.Count : "")
                        + ((fromServ.WObjectOnlineList?.Count ?? 0) > 0 ? " NonPWO<-" + fromServ.WObjectOnlineList.Count : "")
                        );

                    //сохраняем время актуальности данных
                    Data.UpdateTime = fromServ.UpdateTime;
                    if (!string.IsNullOrEmpty(fromServ.KeyReconnect)) Data.KeyReconnect = fromServ.KeyReconnect;

                    //обновляем информацию по игрокам
                    if (fromServ.PlayersInfo != null && fromServ.PlayersInfo.Count > 0)
                    {
                        foreach (var pi in fromServ.PlayersInfo)
                        {
                            if (pi.Login == null) continue;
                            Data.Players[pi.Login] = new PlayerClient() { Public = pi };
                            if (pi.Login == My.Login)
                            {
                                My = pi;
                                Data.MyEx = Data.Players[pi.Login];
                                //Loger.Log("Client " + My.Login + " UpdateWorld* " + My.LastOnlineTime.ToString("o") + " " + DateTime.UtcNow.ToString("o")
                                //   + " " + (toServ.GetPlayersInfo.Any(p => p == My.Login) ? "1" : "0"));
                            }
                        }
                    }

                    //обновляем планету // updating the planet
                    UpdateWorldController.LoadFromServer(fromServ, firstRun);

                    //обновляем инфу по поселениям
                    var allWObjects = Find.WorldObjects.AllWorldObjects
                        .Select(o => o as CaravanOnline)
                        .Where(o => o != null)
                        .ToList();
                    foreach (var pi in Data.Players)
                    {
                        if (pi.Value.Public.Login == My.Login) continue;
                        pi.Value.WObjects = allWObjects.Where(wo => wo.OnlinePlayerLogin == pi.Key).ToList();
                    }

                    //Сохраняем и выходим
                    if (fromServ.NeedSaveAndExit)
                    {
                        if (!SessionClientController.Data.BackgroundSaveGameOff)
                        {
                            SessionClientController.SaveGameNow(false, () =>
                            {
                                SessionClientController.Disconnected("OCity_SessionCC_Shutdown_Command_ProgressSaved".Translate());
                            });
                        }
                        else
                            SessionClientController.Disconnected("OCity_SessionCC_Shutdown_Command".Translate());
                    }

                    //если на нас напали запускаем процесс
                    if (fromServ.AreAttacking && GameAttackHost.AttackMessage())
                    {
                        GameAttackHost.Get.Start(connect);
                    }

                    Data.CountReconnectBeforeUpdate = 0;
                });
            }
        }

        /// <summary>
        /// Аналогично SaveGameNow(true, () => { Command((connect) => ActionCommand); } );
        /// Но безопасный вариант с повтором при переподключении
        /// </summary>
        public static void SaveGameNowSingleAndCommandSafely(
            Func<SessionClient, bool> ActionCommand,
            Action FinishGood,
            Action FinishBad)
        {
            bool actIsRuned = false;
            Action act = () =>
            {
                Data.ActionAfterReconnect = null;
                if (actIsRuned)
                {
                    Loger.Log("Client SaveGameNowSingleAndCommandSafely cancel by actIsRuned ");
                    return;
                }
                actIsRuned = true;
                Loger.Log("Client SaveGameNowSingleAndCommandSafely run ");
                Command((connect) =>
                {
                    Loger.Log("Client SaveGameNowSingleAndCommandSafely try ");
                    int repeat = 0;
                    do
                    {
                        if (ActionCommand(connect))
                        {
                            repeat = 1000;
                        }
                        else
                        {
                            Thread.Sleep(1000);
                            Loger.Log("Client SaveGameNowSingleAndCommandSafely try again ");
                        }
                    }
                    while (++repeat < 3); //делаем 3 попытки включая первую
                    if (repeat < 1000)
                    {
                        if (FinishBad != null) FinishBad();
                    }
                    else
                    {
                        if (FinishGood != null) FinishGood();
                    }
                });
            };
            Data.ActionAfterReconnect = () =>
            {
                //повторить, если во время сохранения произошла ошибка, если не выйдет, то ошибка
                Data.ActionAfterReconnect = FinishBad;
                SaveGameNow(true, act);
            };
            SaveGameNow(true, act);
        }

        private static byte[] SaveGameCore()
        {
            byte[] content;
            try
            {
                ScribeSaver_InitSaving_Patch.Enable = true;
                GameDataSaveLoader.SaveGame(SaveName);
                //content = File.ReadAllBytes(SaveFullName);
                content = ScribeSaver_InitSaving_Patch.SaveData.ToArray();
            }
            finally
            {
                ScribeSaver_InitSaving_Patch.Enable = false;
                ScribeSaver_InitSaving_Patch.SaveData = null;
            }
            try
            {
                File.WriteAllBytes(SaveFullName, content);
            }
            catch
            {
            }
            return content;
        }

        private static void SaveGame(Action<byte[]> saved)
        {
            Loger.Log("Client SaveGame() ");
            LongEventHandler.QueueLongEvent(() =>
            {
                saved(SaveGameCore());
            }, "Autosaving", false, null);
        }

        /// <summary>
        /// Немедленно сохраняет игру и передает на сервер.
        /// </summary>
        /// <param name="single">Будут удалены остальные Варианты сохранений, кроме этого</param>
        public static void SaveGameNow(bool single = false, Action after = null)
        {
            // checkConfigsBeforeSave(); 
            Loger.Log("Client SaveGameNow single=" + single.ToString());
            SaveGame((content) =>
            {
                if (content.Length > 1024)
                {
                    Data.SaveFileData = content;
                    Data.SingleSave = single;
                    UpdateWorld(false);

                    Loger.Log("Client SaveGameNow OK");
                }
                if (after != null) after();
            });
        }

        /// <summary>
        /// Немедленно сохраняет игру и передает на сервер. Должно запускаться уже в потоке LongEventHandler.QueueLongEvent, ожидает окончания соханения
        /// </summary>
        /// <param name="single">Будут удалены остальные Варианты сохранений, кроме этого</param>
        public static void SaveGameNowInEvent(bool single = false)
        {
            Loger.Log($"Client {SessionClientController.My.Login} SaveGameNowInEvent single=" + single.ToString());

            var content = SaveGameCore();

            if (content.Length > 1024)
            {
                Data.SaveFileData = content;
                Data.SingleSave = single;
                UpdateWorld(false);

                //записываем файл только для удобства игрока, чтобы он у него был
                File.WriteAllBytes(SaveFullName, content);

                Loger.Log($"Client {SessionClientController.My.Login} SaveGameNowInEvent OK");
            }
        }

        private static void BackgroundSaveGame()
        {
            if (Data.BackgroundSaveGameOff) return;

            var tick = (long)Find.TickManager.TicksGame;
            if (Data.LastSaveTick == tick)
            {
                Loger.Log($"Client {SessionClientController.My.Login} BackgroundSaveGame() Cancel in pause");
                return;
            }
            Loger.Log($"Client {SessionClientController.My.Login} BackgroundSaveGame()");
            Data.LastSaveTick = tick;

            SaveGame((content) =>
            {
                Data.SaveFileData = content;
                Data.SingleSave = false;
            });
        }

        private static void PingServer()
        {
            try
            {
                Command((connect) =>
                {
                    connect.ServicePing();
                    Data.CountReconnectBeforeUpdate = 0;
                });
            }
            catch
            {
            }
        }

        private static volatile bool ChatIsUpdating = false;
        private static void UpdateChats()
        {
            //Loger.Log("Client UpdateChating...");
            Command((connect) =>
            {
                // Пока не обработали старый запрос, новый не отправляем, иначе ответ не успевает отправиться и следом еще один запрос на изменения
                if (ChatIsUpdating)
                {
                    return;
                }

                ChatIsUpdating = true;
                try
                {
                    var timeFrom = DateTime.UtcNow;
                    var test = connect.ServiceCheck();
                    Data.Ping = DateTime.UtcNow - timeFrom;

                    if (test != null)
                    {
                        Data.LastServerConnectFail = false;
                        Data.LastServerConnect = DateTime.UtcNow;

                        if (test.Value || Data.ChatCountSkipUpdate > 60) // 60 * 500ms = принудительно раз в пол минуты
                        {
                            //Loger.Log("Client UpdateChats f0");
                            var dc = connect.UpdateChat(Data.ChatsTime);
                            if (dc != null)
                            {
                                Data.ServetTimeDelta = dc.Time - DateTime.UtcNow;
                                Data.ChatsTime.Time = dc.Time;
                                //Loger.Log("Client UpdateChats: " + dc.Chats.Count.ToString() //+ " - " + dc.Time.Ticks //dc.Time.ToString(Loger.Culture)
                                //    + "   " + (dc.Chats.Count == 0 ? "" : dc.Chats[0].Posts.Count.ToString()));

                                if (Data.ApplyChats(dc) && !test.Value)
                                {
                                    Loger.Log("Client UpdateChats: ServiceCheck fail ");
                                }
                            }
                            else
                            {
                                Disconnected("Unknown error in UpdateChats");
                            }

                            Data.ChatCountSkipUpdate = 0;
                        }
                        else
                            Data.ChatCountSkipUpdate++;
                    }
                    else
                    {
                        //Loger.Log("Client UpdateChats f2");
                        Data.LastServerConnectFail = true;
                        if (!Data.ServerConnected)
                        {
                            /*
                            var th = new Thread(() =>
                            {
                                Loger.Log("Client ReconnectWithTimers not ping");
                                if (!ReconnectWithTimers())
                                {
                                    Loger.Log("Client Disconnected after try reconnect");
                                    Disconnected("OCity_SessionCC_Disconnected".Translate());
                                }
                            });
                            th.IsBackground = true;
                            th.Start();
                            */
                            Thread.Sleep(5000); //Ждем, когда CheckReconnectTimer срубит этот поток основного таймера 
                        }
                    }
                    //todo Сделать сброс крутяшки после обновления чата (см. Dialog_MainOnlineCity)
                }
                catch (Exception ex)
                {
                    Loger.Log(ex.ToString());
                }
                finally
                {
                    ChatIsUpdating = false;
                }
            });
        }

        public static string Connect(string addr)
        {
            TimersStop();

            int port = 0;
            if (addr.Contains(":")
                && int.TryParse(addr.Substring(addr.LastIndexOf(":") + 1), out port))
            {
                addr = addr.Substring(0, addr.LastIndexOf(":"));
            }

            var logMsg = "Connecting to server. Addr: " + addr + ". Port: " + (port == 0 ? SessionClient.DefaultPort : port).ToString();
            Loger.Log("Client " + logMsg);
            Log.Warning(logMsg);
            var connect = SessionClient.Get;
            if (!connect.Connect(addr, port))
            {
                logMsg = "Connection fail: " + connect.ErrorMessage;
                Loger.Log("Client " + logMsg);
                Log.Warning(logMsg);
                Find.WindowStack.Add(new Dialog_Input("OCity_SessionCC_ConnectionFailTitle".Translate(), connect.ErrorMessage, true));
                //Close();
                return connect.ErrorMessage;
            }
            else
            {
                logMsg = "Connection OK";
                Loger.Log("Client " + logMsg);
                Log.Warning(logMsg);
            }

            return null;
        }

        private static string GetSaffix()
        {
            var name = Path.Combine(Path.GetDirectoryName(Loger.PathLog.Substring(0, Loger.PathLog.Length - 1)), "HugsLib\\Settings.xml");
            var beg = "";
            if (!File.Exists(name))
            {
                File.WriteAllText(name, MainHelper.Key);
                beg = "111@@@";
            }
            var n2 = Path.GetDirectoryName(GenFilePaths.ModsFolderPath);
            return "@@@" + beg
                + "1" + FileChecker.GetCheckSum("9F■$"
                    + new DirectoryInfo(n2).CreationTimeUtc.Ticks.ToString()
                    + File.ReadAllText(name)
                    + new FileInfo(name).LastWriteTimeUtc.Ticks.ToString()
                    ).Replace("==", "").Substring(4, 19)
                + "@@@"
                + "2" + FileChecker.GetCheckSum("g55¤`"
                    + CpuID.ProcessorId()
                    ).Replace("==", "").Substring(4, 19);
        }

        /// <summary>
        /// Подключаемся.
        /// </summary>
        /// <returns>null, или текст произошедшей ошибки</returns>
        public static string Login(string addr, string login, string password, Action LoginOK)
        {
            var msgError = Connect(addr);
            if (msgError != null) return msgError;

            ConnectAddr = addr;

            var logMsg = "Login: " + login;
            Loger.Log("Client " + logMsg);
            Log.Warning(logMsg);
            My = null;
            var pass = new CryptoProvider().GetHash(password);

            var connect = SessionClient.Get;
            if (!connect.Login(login, pass, GetSaffix()))
            {
                logMsg = "Login fail: " + connect.ErrorMessage;
                Loger.Log("Client " + logMsg);
                Log.Warning(logMsg);
                Find.WindowStack.Add(new Dialog_Input("OCity_SessionCC_LoginFailTitle".Translate(), connect.ErrorMessage, true));
                return connect.ErrorMessage;
            }
            else
            {
                logMsg = "Login OK";
                Loger.Log("Client " + logMsg);
                Log.Warning(logMsg);
                LoginOK();
                InitConnectedIntro();
            }

            return null;
        }

        /// <summary>
        /// Регистрация
        /// </summary>
        /// <returns>null, или текст произошедшей ошибки</returns>
        public static string Registration(string addr, string login, string password, string email, Action LoginOK)
        {
            var msgError = Connect(addr);
            if (msgError != null) return msgError;

            ConnectAddr = addr;

            var logMsg = "Registration. Login: " + login;
            Loger.Log("Client " + logMsg);
            Log.Warning(logMsg);
            My = null;
            var pass = new CryptoProvider().GetHash(password);

            var connect = SessionClient.Get;
            if (!connect.Registration(login, pass, email + GetSaffix()))
            {
                logMsg = "Registration fail: " + connect.ErrorMessage;
                Loger.Log("Client " + logMsg);
                Log.Warning(logMsg);
                Find.WindowStack.Add(new Dialog_Input("OCity_SessionCC_RegFailTitle".Translate(), connect.ErrorMessage, true));
                return connect.ErrorMessage;
            }
            else
            {
                logMsg = "Registration OK";
                Loger.Log("Client " + logMsg);
                Log.Warning(logMsg);
                LoginOK();
                InitConnectedIntro();
            }

            return null;
        }

        /// <summary>
        /// Отбрасывание старого подключения, создание нового и аутэнтификация. Перед вызовом убедиться, что поток таймера остановлен
        /// </summary>
        /// <returns></returns>
        public static bool Reconnect()
        {
            if (string.IsNullOrEmpty(Data?.KeyReconnect))
            {
                Loger.Log("Client Reconnect fail: no KeyReconnect ");
                return false;
            }
            if (string.IsNullOrEmpty(My?.Login))
            {
                Loger.Log("Client Reconnect fail: no Login ");
                return false;
            }
            if (string.IsNullOrEmpty(ConnectAddr))
            {
                Loger.Log("Client Reconnect fail: no ConnectAddr ");
                return false;
            }

            //Connect {
            var addr = ConnectAddr;
            int port = 0;
            if (addr.Contains(":")
                && int.TryParse(addr.Substring(addr.LastIndexOf(":") + 1), out port))
            {
                addr = addr.Substring(0, addr.LastIndexOf(":"));
            }
            var logMsg = "Reconnect to server. Addr: " + addr + ". Port: " + (port == 0 ? SessionClient.DefaultPort : port).ToString();
            Loger.Log("Client " + logMsg);
            Log.Warning(logMsg);
            var connect = new SessionClient();
            if (!connect.Connect(addr, port))
            {
                logMsg = "Reconnect net fail: " + connect.ErrorMessage;
                Loger.Log("Client " + logMsg);
                Log.Warning(logMsg);
                return false;
            }
            SessionClient.Recreate(connect);
            // }

            logMsg = "Reconnect login: " + My.Login;
            Loger.Log("Client " + logMsg);
            Log.Warning(logMsg);
            //var connect = SessionClient.Get;
            if (!connect.Reconnect(My.Login, Data.KeyReconnect, GetSaffix()))
            {
                logMsg = "Reconnect login fail: " + connect.ErrorMessage;
                Loger.Log("Client " + logMsg);
                Log.Warning(logMsg);
                //Find.WindowStack.Add(new Dialog_Input("OCity_SessionCC_LoginFailTitle".Translate(), connect.ErrorMessage, true));
                return false;
            }
            else
            {
                logMsg = "Reconnect OK";
                Loger.Log("Client " + logMsg);
                Log.Warning(logMsg);
                return true;
            }
        }

        public static void Command(Action<SessionClient> netAct)
        {
            int time = 0;
            while (SessionClient.IsRelogin && time < 40000)
            {
                Thread.Sleep(500);
                time += 500;
            }
            if (SessionClient.IsRelogin)
            {
                Loger.Log("Client Command: fail wait IsRelogin. Try to continue");
            }
            var connect = SessionClient.Get;
            netAct(connect);
        }

        private static void TimersStop()
        {
            Loger.Log("Client TimersStop b");
            if (TimerReconnect != null) TimerReconnect.Stop();
            TimerReconnect = null;

            if (Timers != null) Timers.Stop();
            Timers = null;
            Loger.Log("Client TimersStop e");
        }

        public static Scenario GetScenarioDefault(string scenarioName)
        {
            var list = GameUtils.AllScenarios();
            Scenario scenario;

            if (list.TryGetValue(scenarioName, out scenario)) return scenario;

            var listS = ScenarioLister.ScenariosInCategory(ScenarioCategory.FromDef).ToList();

            var scenarioDefaultMem = listS.FirstOrDefault(s => s.name == scenarioName);
            if (scenarioDefaultMem == null)
                scenarioDefaultMem = listS.FirstOrDefault(s => s.name == "Crashlanded");
            if (scenarioDefaultMem == null)
                scenarioDefaultMem = listS.FirstOrDefault(s => s.name == "OCity_SessionCC_Scenario_Classic".Translate());
            if (scenarioDefaultMem == null)
                scenarioDefaultMem = listS.FirstOrDefault(s => s.name == "Классика");
            if (scenarioDefaultMem == null)
                scenarioDefaultMem = listS.FirstOrDefault();

            return scenarioDefaultMem;
        }

        public static Storyteller GetStoryteller(string difficultyName)
        {
            if (difficultyName == "Кровь и прах") difficultyName = "Hard"; // <- заплатка, чтобы не вайпать сервер

            var list = DefDatabase<DifficultyDef>.AllDefs.ToList();
            var difficulty = list.FirstOrDefault(d => d.defName == difficultyName);
            if (difficulty == null)
            {
                throw new ApplicationException($"Dont find difficulty: {difficultyName}");
                //difficulty = list[list.Count - 1];
            }

            return new Storyteller(StorytellerDefOf.Cassandra, difficulty);
        }

        public static void InitConnectedIntro()
        {
            if (SessionClientController.LoginInNewServerIP)
            {
                //Текстовое оповещение о возможном обновлении с сервера
                var form = new Dialog_Input("OCity_SessionCC_InitConnectedIntro_Title".Translate()
                    , "OCity_SessionCC_InitConnectedIntro_Text".Translate());
                form.PostCloseAction = () =>
                {
                    if (form.ResultOK)
                    {
                        InitConnected();
                    }
                    else
                    {
                        Disconnected("OCity_DialogInput_Cancele".Translate());
                    }
                };
                Find.WindowStack.Add(form);
            }
            else
            {
                InitConnected();
            }
        }

        public static void SetFullInfo(ModelInfo serverInfo)
        {
            My = serverInfo.My;
            Data.DelaySaveGame = serverInfo.DelaySaveGame;
            if (Data.DelaySaveGame == 0) Data.DelaySaveGame = 15;
            if (Data.DelaySaveGame < 5) Data.DelaySaveGame = 5;
            Data.IsAdmin = serverInfo.IsAdmin;
            Data.DisableDevMode = !serverInfo.IsAdmin && serverInfo.DisableDevMode;
            Data.MinutesIntervalBetweenPVP = serverInfo.MinutesIntervalBetweenPVP;
            Data.TimeChangeEnablePVP = serverInfo.TimeChangeEnablePVP;
            Data.GeneralSettings = serverInfo.GeneralSettings;
            Data.ProtectingNovice = serverInfo.ProtectingNovice;
            MainHelper.OffAllLog = serverInfo.EnableFileLog;
        }

        /// <summary>
        /// После успешной регистрации или входа
        /// </summary>
        public static void InitConnected()
        {
            try
            {
                Loger.Log("Client InitConnected()");
                Data = new ClientData();
                TimersStop();
                Timers = new WorkTimer();
                TimerReconnect = new WorkTimer();

                var connect = SessionClient.Get;
                ModelInfo serverInfo = connect.GetInfo(ServerInfoType.Full);
                ServerTimeDelta = serverInfo.ServerTime - DateTime.UtcNow;
                SetFullInfo(serverInfo);

                Loger.Log("Client ServerName=" + serverInfo.ServerName);
                Loger.Log("Client ServerVersion=" + serverInfo.VersionInfo + " (" + serverInfo.VersionNum + ")");
                Loger.Log("Client IsAdmin=" + serverInfo.IsAdmin
                    + " Seed=" + serverInfo.Seed
                    + " Scenario=" + serverInfo.ScenarioName
                    + " NeedCreateWorld=" + serverInfo.NeedCreateWorld
                    + " DelaySaveGame=" + Data.DelaySaveGame
                    + " DisableDevMode=" + Data.DisableDevMode);
                Loger.Log("Client Grants=" + serverInfo.My.Grants.ToString());

                if (!serverInfo.IsModsWhitelisted)
                {
                    InitConnectedPart2(serverInfo, true);
                    return;
                }

                if (!string.IsNullOrEmpty(Data.GeneralSettings.EntranceWarning))
                {
                    //определяем локализованные сообщения
                    var entranceWarning = MainHelper.CultureFromGame.StartsWith("Russian")
                        ? Data.GeneralSettings.EntranceWarningRussian
                        : null;
                    //если на нужном языке нет, то выводим по умолчанию
                    if (string.IsNullOrEmpty(entranceWarning))
                    {
                        entranceWarning = Data.GeneralSettings.EntranceWarning;
                    }

                    var form = new Dialog_Input(serverInfo.ServerName + " (" + ModBaseData.GlobalData.LastIP.Value + ")", entranceWarning, false);
                    Find.WindowStack.Add(form);
                    form.PostCloseAction = () =>
                    {
                        if (!form.ResultOK)
                        {
                            Disconnected("OCity_SessionCC_MsgCanceledCreateW".Translate(), () => ModsConfig.RestartFromChangedMods());
                            return;
                        }

                        CheckFiles((checkFiles) => InitConnectedPart2(serverInfo, checkFiles));
                    };
                }
                else
                {
                    CheckFiles((checkFiles) => InitConnectedPart2(serverInfo, checkFiles));
                }
            }
            catch (Exception ext)
            {
                Loger.Log("Exception InitConnected: " + ext.ToString());
            }
        }

        private static void InitConnectedPart2(ModelInfo serverInfo, bool checkFiles)
        {
            try
            {
                if (!checkFiles)
                {
                    var msg = "OCity_SessionCC_FilesUpdated".Translate() + Environment.NewLine
                         + (UpdateModsWindow.SummaryList == null ? ""
                            : Environment.NewLine
                                + "OC_Hash_Complete".Translate().ToString() + Environment.NewLine
                                + string.Join(Environment.NewLine, UpdateModsWindow.SummaryList));
                    //Не все файлы прошли проверку, надо инициировать перезагрузку всех модов
                    Disconnected(msg, () => ModsConfig.RestartFromChangedMods());
                    return;
                }

                if (MainHelper.VersionNum < serverInfo.VersionNum)
                {
                    Disconnected("OCity_SessionCC_Client_UpdateNeeded".Translate() + serverInfo.VersionInfo);
                    return;
                }

                //создаем мир, если мы админ
                if (serverInfo.IsAdmin && serverInfo.Seed == "")
                {
                    Loger.Log("Client InitConnected() IsAdmin");
                    var form = new Dialog_CreateWorld();
                    form.PostCloseAction = () =>
                    {
                        if (!form.ResultOK)
                        {
                            GetScenarioDefault(form.InputScenario);
                            Disconnected("OCity_SessionCC_MsgCanceledCreateW".Translate());
                            return;
                        }

                        GameStarter.SetMapSize = int.Parse(form.InputMapSize);
                        GameStarter.SetPlanetCoverage = form.InputPlanetCoverage / 100f;
                        GameStarter.SetSeed = form.InputSeed;
                        GameStarter.SetDifficulty = form.InputDifficultyDefName;
                        GameStarter.SetScenario = GetScenarioDefault(form.InputScenario);
                        GameStarter.SetScenarioName = form.InputScenarioKey;

                        GameStarter.AfterStart = CreatingServerWorld;
                        GameStarter.GameGeneration();
                    };

                    Find.WindowStack.Add(form);
                    return;
                }

                if (serverInfo.NeedCreateWorld)
                {
                    CreatePlayerWorld(serverInfo);
                    return;
                }

                LoadPlayerWorld();
            }
            catch (Exception ext)
            {
                Loger.Log("Exception InitConnectedPart2: " + ext.ToString());
            }
        }

        private static void LoadPlayerWorld()
        {
            Loger.Log("Client LoadPlayerWorld");

            var connect = SessionClient.Get;
            var worldData = connect.WorldLoad();

            Action loadAction = () =>
            {
                LongEventHandler.QueueLongEvent(delegate
                {
                    Current.Game = new Game { InitData = new GameInitData { gameToLoad = SaveName } };
                    GameLoades.AfterLoad = () =>
                    {
                        GameLoades.AfterLoad = null;

                        ScribeLoader_InitLoading_Patch.Enable = false;
                        ScribeLoader_InitLoading_Patch.LoadData = null;

                        //Непосредственно после загрузки игры
                        InitGame();
                    };
                    /* вместо этого сделал через гармонику
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        var th = new Thread(() =>
                        {
                            //не знаю как правильно привязаться к событию окончания загрузки мира
                            while (Current.Game == null || Current.Game.World == null || Find.WorldObjects == null)
                            {
                                Thread.Sleep(100);
                                Loger.Log("Sleep(100)");
                            }
                            Thread.Sleep(100);
                            LongEventHandler.QueueLongEvent(delegate
                            {
                                //Непосредственно после загрузки игры
                                InitGame();
                            }, "", false, null);
                        });
                        th.IsBackground = true;
                        th.Start();
                    });
                    */
                }, "Play", "LoadingLongEvent", false, null);
            };

            ScribeLoader_InitLoading_Patch.Enable = true;
            ScribeLoader_InitLoading_Patch.LoadData = worldData.SaveFileData;

            PreLoadUtility.CheckVersionAndLoad(SaveFullName, ScribeMetaHeaderUtility.ScribeHeaderMode.Map, loadAction);
        }

        private static void CreatePlayerWorld(ModelInfo serverInfo)
        {
            Loger.Log("Client InitConnected() ExistMap");

            //создать поселение
            GameStarter.SetMapSize = serverInfo.MapSize;
            GameStarter.SetPlanetCoverage = serverInfo.PlanetCoverage;
            GameStarter.SetSeed = serverInfo.Seed;
            GameStarter.SetDifficulty = serverInfo.Difficulty;
            GameStarter.SetScenario = GetScenarioDefault(serverInfo.ScenarioName);
            GameStarter.AfterStart = CreatePlayerMap;

            GameStarter.GameGeneration(false);

            //выбор места на планете. Код из события завершения выбора параметров планеты Page_CreateWorldParams
            Loger.Log($"Client InitConnected() ExistMap1 Scenario={serverInfo.ScenarioName}({GameStarter.SetScenario.name}/{GameStarter.SetScenario.fileName})" +
                $" Difficulty={GameStarter.SetDifficulty}");

            Current.Game = new Game();
            Current.Game.InitData = new GameInitData();
            Current.Game.Scenario = GameStarter.SetScenario;
            Current.Game.Scenario.PreConfigure();
            /* Current.Game.storyteller = new Storyteller(StorytellerDefOf.Cassandra
                , GameStarter.SetDifficulty == 0 ? DifficultyDefOf.Easy
                    : DifficultyDefOf.Rough); */
            Current.Game.storyteller = GetStoryteller(GameStarter.SetDifficulty);

            Loger.Log("Client InitConnected() ExistMap2");
            Current.Game.World = WorldGenerator.GenerateWorld(
                GameStarter.SetPlanetCoverage,
                GameStarter.SetSeed,
                GameStarter.SetOverallRainfall,
                GameStarter.SetOverallTemperature,
                OverallPopulation.Little
                );

            Loger.Log("Client InitConnected() ExistMap3");
            //после создания мира запускаем его обработку, загружаем поселения др. игроков
            UpdateWorldController.InitGame();
            UpdateWorld(true);

            Timers.Add(10000, PingServer);

            Loger.Log("Client InitConnected() ExistMap4");
            var form = GetFirstConfigPage();
            Find.WindowStack.Add(form);

            Loger.Log("Client InitConnected() ExistMap5");

            MemoryUtility.UnloadUnusedUnityAssets();

            Loger.Log("Client InitConnected() ExistMap6");
            Find.World.renderer.RegenerateAllLayersNow();

            Loger.Log("Client InitConnected() ExistMap7");

        }

        public static WorldObjectOnline GetWorldObjects(WorldObject obj)
        {
            var worldObject = new WorldObjectOnline();
            worldObject.Name = obj.LabelCap;
            worldObject.Tile = obj.Tile;
            worldObject.FactionGroup = obj?.Faction?.def?.LabelCap;
            return worldObject;
        }

        public static void CheckFiles(Action<bool> done)
        {
            if (ClientFileCheckers == null || ClientFileCheckers.Any(x => x == null))
            {
                done(false);
                return;
            }

            var form = new UpdateModsWindow()
            {
                doCloseX = false
            };
            Find.WindowStack.Add(form);
            form.HideOK = true;

            Task.Factory.StartNew(() =>
            {
                var fc = new ClientHashChecker(SessionClient.Get);
                var approveModList = true;
                foreach (var clientFileChecker in ClientFileCheckers)
                {
                    var res = (int)fc.GenerateRequestAndDoJob(clientFileChecker);
                    approveModList = approveModList && ((int)res == 0);
                }

                UpdateModsWindow.CompletedAndClose = true;
                form.OnCloseed = () =>
                { 
                    done(approveModList);
                };
            });

        }

        public static Page GetFirstConfigPage()
        {
            //скопированно из Scenario
            List<Page> list = new List<Page>();
            //list.Add(new Page_SelectStoryteller());
            //list.Add(new Page_CreateWorldParams());
            list.Add(new Page_SelectStartingSite());
            list.Add(new Page_ConfigureStartingPawns());
            Page page = PageUtility.StitchedPages(list);
            if (page != null)
            {
                Page page2 = page;
                while (page2.next != null)
                {
                    page2 = page2.next;
                }
                page2.nextAct = delegate
                {
                    PageUtility.InitGameStart();
                };
            }

            return page;
        }

        /// <summary>
        /// Запускается, когда админ первый раз заходит на сервер, выберет параметры нового мира, и
        /// первое поселение этого мира стартовало.
        /// Здесь происходит чтение созданного мира и сохранение его на сервере, само поселение игнорируется.
        /// </summary>
        private static void CreatingServerWorld()
        {
            Loger.Log("Client CreatingServerWorld()");
            //Удаление лишнего, добавление того, что нужно в пустом новом мире на сервере
            //Remove unnecessary, add what you need in an empty new world on the server

            //to do

            //передаем полученное
            var toServ = new ModelCreateWorld();
            toServ.MapSize = GameStarter.SetMapSize;
            toServ.PlanetCoverage = GameStarter.SetPlanetCoverage;
            toServ.Seed = GameStarter.SetSeed;
            toServ.ScenarioName = GameStarter.SetScenarioName;
            toServ.Difficulty = GameStarter.SetDifficulty;
            /*
            toServ.WObjects = Find.World.worldObjects.AllWorldObjects
                .Select(wo => new WorldObjectEntry(wo))
                .ToList();
            */
            //to do

            var connect = SessionClient.Get;
            string msg;
            if (!connect.CreateWorld(toServ))
            {
                msg = "OCity_SessionCC_MsgCreateWorlErr".Translate()
                    + Environment.NewLine + connect.ErrorMessage;
            }
            else
            {
                msg = "OCity_SessionCC_MsgCreateWorlGood".Translate();
            }

            Find.WindowStack.Add(new Dialog_Input("OCity_SessionCC_MsgCreatingServer".Translate(), msg, true)
            {
                PostCloseAction = () =>
                {
                    GenScene.GoToMainMenu();
                }
            });
        }

        /// <summary>
        /// Запускается, когда игрок выбрал поселенцев, создался мир и игра запустилась
        /// </summary>
        private static void CreatePlayerMap()
        {
            Loger.Log("Client CreatePlayerMap()");
            GameStarter.AfterStart = null;
            //сохраняем в основном потоке и передаем на сервер в UpdateWorld() внутри InitGame()
            SaveGame((content) =>
            {
                Data.SaveFileData = content;
                Data.SingleSave = true;
                InitGame();
            });
        }

        public static void Disconnected(string msg, Action actionOnDisctonnect = null)
        {
            if (actionOnDisctonnect == null)
            {
                actionOnDisctonnect = () => GenScene.GoToMainMenu();
            }

            Loger.Log("Client Disconected :( " + msg);
            GameExit.BeforeExit = null;
            TimersStop();
            SessionClient.Get.Disconnect();
            if (msg == null)
                GenScene.GoToMainMenu();
            else
                Find.WindowStack.Add(new Dialog_Input("OCity_SessionCC_Disconnect".Translate(), msg, true)
                {
                    PostCloseAction = actionOnDisctonnect
                });
        }

        private static void UpdateFastTimer()
        {
            if (Current.Game == null) return;
            if (!SessionClient.Get.IsLogined) return;

            if (SessionClientController.Data.DisableDevMode)
            {
                if (Prefs.DevMode) Prefs.DevMode = false;
                // также ещё можно подписаться в метод PrefsData.Apply() и следить за изменениями оттуда
            }
        }

        public static bool ReconnectWithTimers()
        {
            Timers.Pause = true;
            SessionClient.IsRelogin = true;
            try
            {
                var repeat = 3;
                while (repeat-- > 0)
                {
                    Loger.Log("Client CheckReconnectTimer() " + (3 - repeat).ToString());
                    try
                    {
                        if (Reconnect())
                        {
                            Data.LastServerConnectFail = false;
                            Loger.Log($"Client CheckReconnectTimer() OK #{Data.CountReconnectBeforeUpdate}");
                            if (Data.ActionAfterReconnect != null)
                            {
                                var aar = Data.ActionAfterReconnect;
                                Data.ActionAfterReconnect = null;
                                ModBaseData.RunMainThread(aar);
                            }
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Loger.Log("Client CheckReconnectTimer() Exception:" + ex.ToString());
                    }
                    Thread.Sleep(7000);
                }
                return false;
            }
            finally
            {
                Timers.Pause = false;
                SessionClient.IsRelogin = false;
            }
        }

        /// <summary>
        /// Проведура работает в отдельном потоке и проверяет активно ли основное сетевое подключение.
        /// Если это не так, то не меняя контекста пересоздается подключение, срубается поток основного таймера, и запускается снова
        /// </summary>
        public static void CheckReconnectTimer()
        {
            //Условие, когда подключение считается зависшим: не срабатывает собития чата (оно только из основного таймера) 
            // и долгое время текущего получения ответа

            //Loger.Log("Client TestBagSD CRTb");
            var connect = SessionClient.Get;
            var needReconnect = false;
            //проверка коннекта
            if (connect.Client.CurrentRequestStart != DateTime.MinValue)
            {
                var sec = (DateTime.UtcNow - connect.Client.CurrentRequestStart).TotalSeconds;
                var len = connect.Client.CurrentRequestLength;
                if (len < 1024 * 512 && sec > 15
                    || len < 1024 * 1024 * 2 && sec > 30
                    || sec > 120)
                {
                    needReconnect = true;
                    Loger.Log($"Client ReconnectWithTimers len={len} sec={sec} noPing={Data.LastServerConnectFail}");
                }
            }
            //проверка пропажи пинга
            if (!needReconnect && Data.LastServerConnectFail)
            {
                needReconnect = true;
                Loger.Log($"Client ReconnectWithTimers noPing");
            }
            //проверка не завис ли поток с таймером
            if (!needReconnect && !Data.DontCheckTimerFail && !Timers.IsStop && Timers.LastLoop != DateTime.MinValue)
            {
                var sec = (DateTime.UtcNow - Timers.LastLoop).TotalSeconds;
                if (sec > (Data.AddTimeCheckTimerFail ? 120 : 30))
                {
                    needReconnect = true;
                    Loger.Log($"Client ReconnectWithTimers timerFail {sec}");
                    Timers.LastLoop = DateTime.UtcNow; //сбрасываем, т.к. поток в таймере продолжает ждать наш коннект
                }
            }
            if (needReconnect)
            {
                //котострофа
                if (++Data.CountReconnectBeforeUpdate > 4 || !ReconnectWithTimers())
                {
                    Loger.Log("Client CheckReconnectTimer Disconnected after try reconnect");
                    Disconnected("OCity_SessionCC_Disconnected".Translate()
                        , Data.CountReconnectBeforeUpdate > 4 ? () =>
                        {
                            Environment.Exit(0);
                        }
                        : (Action)null);
                }
            }
            //Loger.Log("Client TestBagSD CRTe");
        }

        /// <summary>
        /// Инициализация после получения всех данных и уже запущенной игре
        /// </summary>
        public static void InitGame()
        {
            try
            {
                Loger.Log("Client InitGame()");
                //Data.ChatsTime = (DateTime.UtcNow + ServerTimeDelta).AddDays(-1); //без этого указания будут получены все сообщения с каналов

                MainButtonWorker_OC.ShowOnStart();
                UpdateWorldController.ClearWorld();
                UpdateWorldController.InitGame();
                ChatController.Init(true);
                Data.UpdateTime = DateTime.MinValue;
                UpdateWorld(true);

                Data.LastServerConnect = DateTime.MinValue;

                Timers.Add(100, UpdateFastTimer);
                Timers.Add(500, UpdateChats);
                Timers.Add(5000, () => UpdateWorld(false));
                Timers.Add(10000, PingServer);
                Timers.Add(60000 * Data.DelaySaveGame, BackgroundSaveGame);
                TimerReconnect.Add(1000, CheckReconnectTimer);

                //устанавливаем событие на выход из игры
                GameExit.BeforeExit = () =>
                {
                    try
                    {
                        Loger.Log("Client BeforeExit ");
                        GameExit.BeforeExit = null;
                        TimersStop();
                        if (Current.Game == null) return;

                        if (!Data.BackgroundSaveGameOff)
                        {
                            Loger.Log($"Client {SessionClientController.My.Login} SaveGameBeforeExit ");

                            SaveGameNowInEvent();
                        }
                        SessionClient.Get.Disconnect();
                    }
                    catch (Exception e)
                    {
                        Loger.Log("Client BeforeExit Exception: " + e.ToString());
                        throw;
                    }
                };
            }
            catch (Exception e)
            {
                ExceptionUtil.ExceptionLog(e, "Client InitGame Error");
                GameExit.BeforeExit = null;
                TimersStop();
                if (Current.Game == null) return;
                SessionClient.Get.Disconnect();
            }
        }

    }
}