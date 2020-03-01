using HugsLib.Utils;
using Model;
using OCUnion;
using OCUnion.Common;
using OCUnion.Transfer;
using RimWorld;
using RimWorld.Planet;
using RimWorldOnlineCity.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Transfer;
using Util;
using Verse;
using Verse.Profile;
using Verse.Steam;

namespace RimWorldOnlineCity
{
    /// <summary>
    /// Контейнер общих данных и
    /// стандартные повторяющиеся инструкции при работе с классом SessionClient.
    /// </summary>
    public static class SessionClientController
    {
        public static ClientData Data { get; set; }
        public static WorkTimer Timers { get; set; }
        public static Player My { get; set; }
        public static TimeSpan ServerTimeDelta { get; set; }

        private const string SaveName = "onlineCityTempLoad";

        private static string SaveFullName { get; set; }

        public static string ConfigPath { get; private set; }

        public static bool LoginInNewServerIP { get; set; }

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
                var workPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                    , @"..\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\OnlineCity");
                Directory.CreateDirectory(workPath);
                Loger.PathLog = workPath;
            }
            catch { }

            Loger.Log("Client Init " + MainHelper.VersionInfo);
            Loger.Log("Client Language: " + Prefs.LangFolderName);

            Loger.Log("Client MainThreadNum=" + ModBaseData.GlobalData.MainThreadNum.ToString());
        }

        private static object UpdatingWorld = new Object();

        private static void UpdateWorld(bool firstRun = false)
        {
            lock (UpdatingWorld)
            {
                Command((connect) =>
                {
                    //собираем пакет на сервер
                    var toServ = new ModelPlayToServer()
                    {
                        UpdateTime = Data.UpdateTime, //время прошлого запроса
                    };
                    //данные сохранения игры
                    if (Data.SaveFileData != null)
                    {
                        toServ.SaveFileData = Data.SaveFileData;
                        toServ.SingleSave = Data.SingleSave;
                        Data.SaveFileData = null;
                    }

                    //собираем данные с планеты
                    if (!firstRun) UpdateWorldController.SendToServer(toServ);

                    //запрос на информацию об игроках. Можно будет ограничить редкое получение для тех кто оффлайн
                    if (Data.Chats != null && Data.Chats[0].PartyLogin != null)
                    {
                        toServ.GetPlayersInfo = Data.Chats[0].PartyLogin;
                        //Loger.Log("Client " + My.Login + " UpdateWorld* " + (toServ.GetPlayersInfo.Count.ToString()) 
                        //    + " " + (toServ.GetPlayersInfo.Any(p => p == SessionClientController.My.Login) ? "1" : "0"));
                    }

                    //отправляем на сервер, получаем ответ
                    ModelPlayToClient fromServ = connect.PlayInfo(toServ);

                    //Loger.Log("Client UpdateWorld 5 ");
                    Loger.Log("Client " + My.Login + " UpdateWorld "
                        + string.Format("Отпр. свои {0}, своиDel {1}{5}. Пришло {2}, del {3}, посылок {4}{6}{7}"
                            , toServ.WObjects == null ? 0 : toServ.WObjects.Count
                            , toServ.WObjectsToDelete == null ? 0 : toServ.WObjectsToDelete.Count
                            , fromServ.WObjects == null ? 0 : fromServ.WObjects.Count
                            , fromServ.WObjectsToDelete == null ? 0 : fromServ.WObjectsToDelete.Count
                            , fromServ.Mails == null ? 0 : fromServ.Mails.Count
                            , toServ.SaveFileData == null || toServ.SaveFileData.Length == 0 ? "" : ", сейв"
                            , fromServ.AreAttacking ? " Атакуют!" : ""
                            , fromServ.NeedSaveAndExit ? " Команда на отключение" : ""
                            ));

                    //сохраняем время актуальности данных
                    Data.UpdateTime = fromServ.UpdateTime;

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

                    //обновляем планету
                    UpdateWorldController.LoadFromServer(fromServ, firstRun);

                    //обновляем инфу по поселениям
                    var allWObjects = Find.WorldObjects.AllWorldObjects
                        .Select(o => o as CaravanOnline)
                        .Where(o => o != null)
                        .ToList();
                    foreach (var pi in Data.Players)
                    {
                        pi.Value.WObjects = allWObjects.Where(wo => wo.OnlinePlayerLogin == pi.Key).ToList();
                    }
                    //свои поселения заполняем отдельно фиктивными CaravanOnline
                    if (Data.Players.ContainsKey(My.Login))
                    {
                        Data.Players[My.Login].WObjects = UpdateWorldController.MyWorldObjectEntrys
                            .Select(wo => wo.Type == WorldObjectEntryType.Base
                                ? (CaravanOnline)new BaseOnline() { Tile = wo.Tile, OnlineWObject = wo }
                                : new CaravanOnline() { Tile = wo.Tile, OnlineWObject = wo })
                            .ToList();
                        //todo test it (Нет цены своих колоний)
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

                });
            }
        }

        private static void SaveGame(Action<byte[]> saved)
        {
            Loger.Log("Client SaveGame() " + SaveFullName);
            LongEventHandler.QueueLongEvent(() =>
            {
                GameDataSaveLoader.SaveGame(SaveName);
                var content = File.ReadAllBytes(SaveFullName);
                saved(content);
            }, "Autosaving", false, null);
        }

        /// <summary>
        /// Немедленно сохраняет игру и передает на сервер.
        /// </summary>
        /// <param name="single">Будут удалены остальные Варианты сохранений, кроме этого</param>
        public static void SaveGameNow(bool single = false, Action after = null)
        {
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

            GameDataSaveLoader.SaveGame(SaveName);
            var content = File.ReadAllBytes(SaveFullName);

            if (content.Length > 1024)
            {
                Data.SaveFileData = content;
                Data.SingleSave = single;
                UpdateWorld(false);

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
                });
            }
            catch
            { }
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
                            Loger.Log("Client UpdateChats f0");
                            var dc = connect.UpdateChat(Data.ChatsTime);
                            if (dc != null)
                            {
                                Data.ServetTimeDelta = dc.Time - DateTime.UtcNow;
                                Loger.Log("Client UpdateChats: " + dc.Chats.Count.ToString() + " - " + dc.Time.Ticks //dc.Time.ToString(Loger.Culture)
                                    + "   " + (dc.Chats.Count == 0 ? "" : dc.Chats[0].Posts.Count.ToString()));

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
                        if (!Data.ServerConnected) Disconnected("OCity_SessionCC_Disconnected".Translate());
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
            //todo Предварительная проверка корректности
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
                Find.WindowStack.Add(new Dialog_Message("OCity_SessionCC_ConnectionFailTitle".Translate(), connect.ErrorMessage));
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

        /// <summary>
        /// Подключаемся.
        /// </summary>
        /// <returns>null, или текст произошедшей ошибки</returns>
        public static string Login(string addr, string login, string password, Action LoginOK)
        {
            var msgError = Connect(addr);
            if (msgError != null) return msgError;

            var logMsg = "Login: " + login;
            Loger.Log("Client " + logMsg);
            Log.Warning(logMsg);
            My = null;
            var pass = new CryptoProvider().GetHash(password);

            var connect = SessionClient.Get;
            if (!connect.Login(login, pass))
            {
                logMsg = "Login fail: " + connect.ErrorMessage;
                Loger.Log("Client " + logMsg);
                Log.Warning(logMsg);
                Find.WindowStack.Add(new Dialog_Message("OCity_SessionCC_LoginFailTitle".Translate(), connect.ErrorMessage));
                //Close();
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
        public static string Registration(string addr, string login, string password, Action LoginOK)
        {
            var msgError = Connect(addr);
            if (msgError != null) return msgError;

            var logMsg = "Registration. Login: " + login;
            Loger.Log("Client " + logMsg);
            Log.Warning(logMsg);
            My = null;
            var pass = new CryptoProvider().GetHash(password);

            var connect = SessionClient.Get;
            if (!connect.Registration(login, pass))
            {
                logMsg = "Registration fail: " + connect.ErrorMessage;
                Loger.Log("Client " + logMsg);
                Log.Warning(logMsg);
                Find.WindowStack.Add(new Dialog_Message("OCity_SessionCC_RegFailTitle".Translate(), connect.ErrorMessage));
                //Close();
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

        public static void Command(Action<SessionClient> netAct)
        {
            var connect = SessionClient.Get;
            netAct(connect);
        }

        private static void TimersStop()
        {
            if (Timers != null) Timers.Stop();
            Timers = null;
        }

        public static Scenario GetScenarioDefault()
        {
            var listS = ScenarioLister.ScenariosInCategory(ScenarioCategory.FromDef).ToList();

            var scenarioDefaultMem = listS.FirstOrDefault(s => s.name == "Crashlanded");
            if (scenarioDefaultMem == null)
                scenarioDefaultMem = listS.FirstOrDefault(s => s.name == "OCity_SessionCC_Scenario_Classic".Translate());
            if (scenarioDefaultMem == null)
                scenarioDefaultMem = listS.FirstOrDefault(s => s.name == "Классика");
            if (scenarioDefaultMem == null)
                scenarioDefaultMem = listS.FirstOrDefault();

            return scenarioDefaultMem;
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

        /// <summary>
        /// После успешной регистрации или входа
        /// </summary>
        public static void InitConnected()
        {
            Loger.Log("Client InitConnected()");
            Data = new ClientData();
            TimersStop();
            Timers = new WorkTimer();

            var connect = SessionClient.Get;
            var serverInfo = connect.GetInfo(ServerInfoType.Full);
            My = serverInfo.My;
            ServerTimeDelta = serverInfo.ServerTime - DateTime.UtcNow;
            Data.DelaySaveGame = serverInfo.DelaySaveGame;
            if (Data.DelaySaveGame == 0) Data.DelaySaveGame = 15;
            if (Data.DelaySaveGame < 5) Data.DelaySaveGame = 5;
            Data.DisableDevMode = !serverInfo.IsAdmin && serverInfo.DisableDevMode;
            MainHelper.OffAllLog = serverInfo.EnableFileLog;

            Loger.Log("Client ServerName=" + serverInfo.ServerName);
            Loger.Log("Client ServerVersion=" + serverInfo.VersionInfo + " (" + serverInfo.VersionNum + ")");
            Loger.Log("Client IsAdmin=" + serverInfo.IsAdmin
                + " Seed=" + serverInfo.Seed
                + " NeedCreateWorld=" + serverInfo.NeedCreateWorld
                + " DelaySaveGame=" + Data.DelaySaveGame
                + " DisableDevMode=" + Data.DisableDevMode);
            Loger.Log("Client Grants=" + serverInfo.My.Grants.ToString());

            if (MainHelper.VersionNum < serverInfo.VersionNum)
            {
                Disconnected("OCity_SessionCC_Client_UpdateNeeded".Translate() + serverInfo.VersionInfo);
                return;
            }

            if (serverInfo.IsModsWhitelisted && !CheckFiles())
            {
                var msg = "Not all files are resolve hash check, they was been updated, Close and Open Game".NeedTranslate();
                //Не все файлы прошли проверку, надо инициировать перезагрузку всех модов
                Disconnected(msg, () => ModsConfig.RestartFromChangedMods());
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
                        Disconnected("OCity_SessionCC_MsgCanceledCreateW".Translate());
                        return;
                    }

                    GameStarter.SetMapSize = int.Parse(form.InputMapSize);
                    GameStarter.SetPlanetCoverage = float.Parse(form.InputPlanetCoverage) / 100f;
                    GameStarter.SetSeed = form.InputSeed;
                    GameStarter.SetDifficulty = int.Parse(form.InputDifficulty);
                    GameStarter.SetScenario = GetScenarioDefault();

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

        private static void LoadPlayerWorld()
        {
            Loger.Log("Client LoadPlayerWorld");

            var connect = SessionClient.Get;
            var worldData = connect.WorldLoad();

            File.WriteAllBytes(SaveFullName, worldData.SaveFileData);
            Action loadAction = () =>
            {
                LongEventHandler.QueueLongEvent(delegate
                {
                    Current.Game = new Game { InitData = new GameInitData { gameToLoad = SaveName } };
                    GameLoades.AfterLoad = () =>
                    {
                        GameLoades.AfterLoad = null;

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
            GameStarter.SetScenario = GetScenarioDefault();
            GameStarter.AfterStart = CreatePlayerMap;

            GameStarter.GameGeneration(false);

            //выбор места на планете. Код из события завершения выбора параметров планеты Page_CreateWorldParams
            Loger.Log("Client InitConnected() ExistMap1");

            Current.Game = new Game();
            Current.Game.InitData = new GameInitData();
            Current.Game.Scenario = GameStarter.SetScenario;
            Current.Game.Scenario.PreConfigure();
            Current.Game.storyteller = new Storyteller(StorytellerDefOf.Cassandra
                , GameStarter.SetDifficulty == 0 ? DifficultyDefOf.Easy
                    : DifficultyDefOf.Rough);

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

            Timers.Add(20000, PingServer);

            Loger.Log("Client InitConnected() ExistMap4");
            var form = GetFirstConfigPage();
            Find.WindowStack.Add(form);

            Loger.Log("Client InitConnected() ExistMap5");

            MemoryUtility.UnloadUnusedUnityAssets();

            Loger.Log("Client InitConnected() ExistMap6");
            Find.World.renderer.RegenerateAllLayersNow();

            Loger.Log("Client InitConnected() ExistMap7");

        }

        public static bool CheckFiles()
        {
            // 1. Шаг Проверяем что список модов совпадает и получены файлы для проверки
            var ip = ModBaseData.GlobalData.LastIP.Value.Replace(":", "_");
            var ap = new GetApproveFolders(SessionClient.Get);
            var approveModList = ap.GenerateRequestAndDoJob(ip);

            var modsCheckFileName = GetApproveFolders.GetModsApprovedFoldersFileName(ip);
            var steamCheckFileName = GetApproveFolders.GetSteamApprovedFoldersFileName(ip);

            Loger.Log(modsCheckFileName);
            Loger.Log(steamCheckFileName);

            // 2. Запускаем пересчет хеша после получения папок проверки с сервера.
            // Если файлы которые содержат папки для проверки,то При инициализации мода сразу же запускаем подсчет контрольной суммы файлов
            ClientHashChecker.StartGenerateHashFiles(modsCheckFileName, steamCheckFileName);

            // 3. Полученный хеш отправляем серверу для проверки
            var fc = new ClientHashChecker(SessionClient.Get);
            var res = fc.GenerateRequestAndDoJob(null);

            Loger.Log(res.ToString());
            return approveModList && res == 0;
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
            //todo Удаление лишнего, добавление того, что нужно в пустом новом мире на сервере

            //to do

            //передаем полученное
            var toServ = new ModelCreateWorld();
            toServ.MapSize = GameStarter.SetMapSize;
            toServ.PlanetCoverage = GameStarter.SetPlanetCoverage;
            toServ.Seed = GameStarter.SetSeed;
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

            Find.WindowStack.Add(new Dialog_Message("OCity_SessionCC_MsgCreatingServer".Translate(), msg, null, () =>
                {
                    GenScene.GoToMainMenu();
                }));
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

            Loger.Log("Client Disconected :( ");
            GameExit.BeforeExit = null;
            TimersStop();
            SessionClient.Get.Disconnect();
            if (msg == null)
                GenScene.GoToMainMenu();
            else
                Find.WindowStack.Add(new Dialog_Message("OCity_SessionCC_Disconnect".Translate(), msg, null, actionOnDisctonnect));
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
                Data.UpdateTime = DateTime.MinValue;
                UpdateWorld(true);

                Data.LastServerConnect = DateTime.MinValue;

                Timers.Add(100, UpdateFastTimer);
                Timers.Add(500, UpdateChats);
                Timers.Add(5000, () => UpdateWorld(false));
                Timers.Add(60000 * Data.DelaySaveGame, BackgroundSaveGame);

                //устанавливаем событие на выход из игры
                GameExit.BeforeExit = () =>
                {
                    Loger.Log("Client BeforeExit ");
                    GameExit.BeforeExit = null;
                    TimersStop();
                    if (Current.Game == null) return;

                    if (!Data.BackgroundSaveGameOff)
                    {
                        Loger.Log($"Client {SessionClientController.My.Login} SaveGameBeforeExit " + SaveFullName);
                        GameDataSaveLoader.SaveGame(SaveName);
                        var content = File.ReadAllBytes(SaveFullName);
                        if (content.Length > 1024)
                        {
                            Data.SaveFileData = content;
                            Data.SingleSave = false;
                            UpdateWorld(false);

                            Loger.Log($"Client {SessionClientController.My.Login} SaveGameBeforeExit OK");
                        }
                    }
                    SessionClient.Get.Disconnect();
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
