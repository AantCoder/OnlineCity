using HugsLib.Utils;
using Model;
using OCUnion;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        public static WorkTimer Timers { get; set; }
        public static Player My { get; set; }
        public static TimeSpan ServerTimeDelta { get; set; }


        private const string SaveName = "onlineCityTempLoad";

        private static string SaveFullName { get; set; }
        
        /// <summary>
        /// Инициализация при старте игры. Как можно раньше
        /// </summary>
        public static void Init()
        {
            SaveFullName = GenFilePaths.FilePathForSavedGame(SaveName);
            MainHelper.CultureFromGame = Prefs.LangFolderName ?? "";
            //if (MainHelper.DebugMode) 
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
        }

        private static object UpdatingWorld = new Object();

        private static void UpdateWorld(bool firstRun = false)
        {
            //Loger.Log("Client UpdateWorld 1 ");
            lock (UpdatingWorld)
            {
                //Loger.Log("Client UpdateWorld 2 ");
                Command((connect) =>
                {
                    //Loger.Log("Client Init " + MainHelper.VersionInfo);
                    //собираем пакет на сервер
                    var toServ = new ModelPlayToServer()
                    {
                        UpdateTime = Data.UpdateTime, //время прошлого запроса
                    };
                    //данные сохранения игры
                    if (Data.SaveFileData != null)
                    {
                        toServ.SaveFileData = Data.SaveFileData;
                        Data.SaveFileData = null;
                    }

                    //Loger.Log("Client UpdateWorld 3 ");
                    //собираем данные с планеты
                    if (!firstRun) UpdateWorldController.SendToServer(toServ);

                    //Loger.Log("Client UpdateWorld 4 ");

                    //запрос на информацию об игроках. Можно будет ограничить редкое получение для тех кто оффлайн
                    if (Data.Chats != null && Data.Chats[0].PartyLogin != null)
                    {
                        toServ.GetPlayersInfo = Data.Chats[0].PartyLogin;
                    }

                    //отправляем на сервер, получаем ответ
                    ModelPlayToClient fromServ = connect.PlayInfo(toServ);

                    //Loger.Log("Client UpdateWorld 5 ");
                    Loger.Log("Client " + My.Login + " UpdateWorld "
                        + string.Format("Отпр. свои {0}, своиDel {1}{5}. Пришло {2}, del {3}, посылок {4}{6}"
                            , toServ.WObjects == null ? 0 : toServ.WObjects.Count
                            , toServ.WObjectsToDelete == null ? 0 : toServ.WObjectsToDelete.Count
                            , fromServ.WObjects == null ? 0 : fromServ.WObjects.Count
                            , fromServ.WObjectsToDelete == null ? 0 : fromServ.WObjectsToDelete.Count
                            , fromServ.Mails == null ? 0 : fromServ.Mails.Count
                            , toServ.SaveFileData == null || toServ.SaveFileData.Length == 0 ? "" : ", сейв"
                            , fromServ.AreAttacking ? " Атакуют!":""
                            ));

                    //сохраняем время актуальности данных
                    Data.UpdateTime = fromServ.UpdateTime;

                    //обновляем информацию по игрокам
                    if (fromServ.PlayersInfo != null && fromServ.PlayersInfo.Count > 0)
                    {
                        foreach (var pi in fromServ.PlayersInfo)
                        {
                            if (pi.Login == null) continue;
                            if (Data.Players.ContainsKey(pi.Login))
                                Data.Players[pi.Login] = new PlayerClient() { Public = pi };
                            else
                                Data.Players.Add(pi.Login, new PlayerClient() { Public = pi });
                        }
                    }
                    var allWObjects = Find.WorldObjects.AllWorldObjects
                        .Select(o => o as CaravanOnline)
                        .Where(o => o != null)
                        .ToList();
                    foreach (var pi in Data.Players)
                    {
                        pi.Value.WObjects = allWObjects.Where(wo => wo.OnlinePlayerLogin == pi.Key).ToList();
                    }

                    //обновляем планету
                    UpdateWorldController.LoadFromServer(fromServ, firstRun);

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

        private static void BackgroundSaveGame()
        {
            var tick = (long)Find.TickManager.TicksGame;
            if (Data.LastSaveTick == tick)
            {
                Loger.Log("Client BackgroundSaveGame() Cancel in pause");
                return;
            }
            Loger.Log("Client BackgroundSaveGame()");
            Data.LastSaveTick = tick;

            SaveGame((content) =>
            {
                Data.SaveFileData = content;
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

        private static void UpdateChats()
        {
            //Loger.Log("Client UpdateChating...");
            Command((connect) =>
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
                        //Loger.Log("Client T4");
                        ModelUpdateChat dc;
                        Loger.Log("Client UpdateChats f0");
                        dc = connect.UpdateChat(Data.ChatsTime);
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
        public static string Login(string addr, string login, string password)
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
                InitConnected();
            }
            return null;
        }

        /// <summary>
        /// Регистрация
        /// </summary>
        /// <returns>null, или текст произошедшей ошибки</returns>
        public static string Registration(string addr, string login, string password)
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
                InitConnected();
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
            var listS = ScenarioLister.ScenariosInCategory(ScenarioCategory.FromDef);

            var scenarioDefaultMem = listS.FirstOrDefault(s => s.name == "Crashlanded");
            if (scenarioDefaultMem == null)
                scenarioDefaultMem = listS.FirstOrDefault(s => s.name == "Классика");
            if (scenarioDefaultMem == null)
                scenarioDefaultMem = listS.FirstOrDefault();

            return scenarioDefaultMem;
        }

        /// <summary>
        /// После успешной регистрации или входа
        /// </summary>
        /// 
        public static void InitConnected()
        {
            Loger.Log("Client InitConnected()");
            Data = new ClientData();
            TimersStop();
            Timers = new WorkTimer();
            
            var connect = SessionClient.Get;
            var serverInfo = connect.GetInfo(true);
            My = serverInfo.My;
            ServerTimeDelta = serverInfo.ServerTime - DateTime.UtcNow;

            Loger.Log("Client ServerVersion=" + serverInfo.VersionInfo + " (" + serverInfo.VersionNum + ")");
            Loger.Log("Client IsAdmin=" + serverInfo.IsAdmin + " Seed=" + serverInfo.Seed + " NeedCreateWorld=" + serverInfo.NeedCreateWorld);

            if (MainHelper.VersionNum < serverInfo.VersionNum)
            {
                Disconnected("Обновите клиент! Версия для данного сервера: ".NeedTranslate() + serverInfo.VersionInfo);
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

                    GameStarter.AfterStart = CreatingWorld;
                    GameStarter.GameGeneration();
                };
                Find.WindowStack.Add(form);
                return;
            }

            if (serverInfo.NeedCreateWorld)
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
                    GameStarter.SetOverallTemperature);

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

                return;
            }
            
            Loger.Log("Client InitConnected() WorldLoad");

            var worldData = connect.WorldLoad();

            File.WriteAllBytes(SaveFullName, worldData.SaveFileData);
            Action loadAction = () => {
                LongEventHandler.QueueLongEvent(delegate {
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
        private static void CreatingWorld()
        {
            Loger.Log("Client CreatingWorld()");
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
                InitGame();
            });
        }

        public static void Disconnected(string msg)
        {
            Loger.Log("Client Disconected :( ");
            GameExit.BeforeExit = null;
            TimersStop();
            SessionClient.Get.Disconnect();
            if (msg == null)
                GenScene.GoToMainMenu();
            else
                Find.WindowStack.Add(new Dialog_Message("OCity_SessionCC_Disconnect".Translate(), msg, null, () =>
            {
                GenScene.GoToMainMenu();
            }));
        }

        /// <summary>
        /// Инициализация после получения всех данных и уже запущенной игре
        /// </summary>
        public static void InitGame()
        {
            try
            {
                Loger.Log("Client InitGame()");
                Data.ChatsTime = (DateTime.UtcNow + ServerTimeDelta).AddDays(-1); //без этого указания будут получены все сообщения с каналов

                MainButtonWorker_OC.ShowOnStart();
                UpdateWorldController.ClearWorld();
                UpdateWorldController.InitGame();
                Data.UpdateTime = DateTime.MinValue;
                UpdateWorld(true);

                Data.LastServerConnect = DateTime.MinValue;

                Timers.Add(500, UpdateChats);
                Timers.Add(5000, () => UpdateWorld(false));
                Timers.Add(60000 * 15, BackgroundSaveGame);

                //устанавливаем событие на выход из игры
                GameExit.BeforeExit = () =>
                {
                    Loger.Log("Client BeforeExit ");
                    GameExit.BeforeExit = null;
                    TimersStop();
                    if (Current.Game == null) return;

                    Loger.Log("Client SaveGameBeforeExit " + SaveFullName);
                    GameDataSaveLoader.SaveGame(SaveName);
                    var content = File.ReadAllBytes(SaveFullName);
                    if (content.Length > 1024)
                    {
                        Data.SaveFileData = content;
                        UpdateWorld(false);

                        Loger.Log("Client SaveGameBeforeExit OK");
                    }
                    SessionClient.Get.Disconnect();
                };
            }
            catch(Exception e)
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
