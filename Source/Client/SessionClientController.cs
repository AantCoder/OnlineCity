using HugsLib;
using Model;
using OCUnion;
using OCUnion.Common;
using OCUnion.Transfer;
using OCUnion.Transfer.Model;
using RimWorld;
using RimWorld.Planet;
using RimWorldOnlineCity.ClientHashCheck;
using RimWorldOnlineCity.GameClasses;
using RimWorldOnlineCity.GameClasses.Harmony;
using RimWorldOnlineCity.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Transfer;
using UnityEngine;
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
        //Локальные данные текущей игры
        public static string ConnectAddr { get; set; }
        
        public static WorkTimer Timers { get; set; }
        public static WorkTimer TimerReconnect { get; set; }
        public static Player My { get; set; }
        
        public static TimeSpan ServerTimeDelta { get; set; }
        
        private const string SaveNameBase = "onlineCity";
        private static string SaveName => SaveNameBase + "_" + Data.ServerName.NormalizeFileNameChars();
        private static string SaveFullName => GenFilePaths.FilePathForSavedGame(SaveName);

        public static StorytellerDef ChoosedTeller { get; set; }

        public static string ConfigPath { get; private set; }

        public static bool LoginInNewServerIP { get; set; }

        public static ClientFileChecker ClientFileChecker { get; private set; }

        public static Action UpdateWorldSafelyRun { get; set; }

        /// <summary>
        /// Инициализация при старте игры. Как можно раньше
        /// </summary>
        public static void Init()
        {
            MainHelper.InGame = true;

            ConfigPath = Path.Combine(GenFilePaths.ConfigFolderPath, "OnlineCity");
            if (!Directory.Exists(ConfigPath))
            {
                Directory.CreateDirectory(ConfigPath);
            }

            MainHelper.CultureFromGame = Prefs.LangFolderName ?? "";

            try
            {
                // ..\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\OnlineCity
                var path = new DirectoryInfo(GenFilePaths.ConfigFolderPath).Parent.FullName;
                var workPath = Path.Combine(path, "OnlineCity");
                Directory.CreateDirectory(workPath);
                Loger.PathLog = workPath;
                Loger.Enable = true;

                //удаляем логи старше 7 дней
                var now = DateTime.UtcNow;
                foreach (var old in Directory.GetDirectories(workPath, "Log_*", SearchOption.TopDirectoryOnly))
                {
                    var info = new DirectoryInfo(old);
                    //Loger.Log("Client Delete Directory " + old + " " + (now - info.CreationTimeUtc).TotalDays);
                    if (info.Exists && (now - info.CreationTimeUtc).TotalDays > 7d) info.Delete(true);
                }
                foreach (var old in Directory.GetFiles(workPath, "Log_*.*", SearchOption.TopDirectoryOnly))
                {
                    var info = new FileInfo(old);
                    //Loger.Log("Client Delete File " + old + " " + (now - info.CreationTimeUtc).TotalDays);
                    if (info.Exists && (now - info.CreationTimeUtc).TotalDays > 7d) info.Delete();
                }
            }
            catch { }

            Loger.Log("Client Init " + MainHelper.VersionInfo);
            Loger.Log("Client Language: " + Prefs.LangFolderName);

            Loger.Log($"Client local time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}");
            Loger.Log($"The difference between time zones: {(DateTime.UtcNow - DateTime.Now).ToString("g")}");

            //Loger.Log("Client MainThreadNum=" + ModBaseData.GlobalData.MainThreadNum.ToString()); всегда строго = 1

        }
        

        private static object UpdatingWorld = new object();
        private static int GetPlayersInfoCountRequest = 0;

        private static void UpdateWorld(bool firstRun = false)
        {
            lock (UpdatingWorld)
            {
                Command((connect) =>
                {
                    var errorNum = "0 ";
                    try
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
                        errorNum += "00 ";

                        //метод не выполняется когда игра свернута
                        if (!ModBaseData.RunMainThreadSync(UpdateWorldController.PrepareInMainThread, 1, true)) return;

                        errorNum += "1 ";
                        //собираем данные с планеты // collecting data from the planet
                        if (!firstRun) UpdateWorldController.SendToServer(toServ, firstRun, null);
                        //Послать на серверв
                        errorNum += "2 ";
                        if (firstRun)
                        {
                            GetPlayersInfoCountRequest = 0;
                            ModelGameServerInfo gameServerInfo = connect.GetGameServerInfo();
                            errorNum += "3 ";
                            UpdateWorldController.SendToServer(toServ, firstRun, gameServerInfo);
                            errorNum += "4 ";
                        }

                        errorNum += "5 ";
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

                        errorNum += "6 ";
                        //отправляем на сервер, получаем ответ
                        //we send to the server, we get a response2
                        ModelPlayToClient fromServ = connect.PlayInfo(toServ);
                        if (Data.AddTimeCheckTimerFail)
                        {
                            if (Timers != null) Timers.LastLoop = DateTime.UtcNow; //сбрасываем, чтобы проверка по диссконекту не сбросла подключение
                            Data.AddTimeCheckTimerFail = false;
                        }
                        //Loger.Log("Client UpdateWorld 5 ");

                        errorNum += "7 ";
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
                        Data.UpdateTimeLocalTime = DateTime.UtcNow;

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
                        //приватная информация про самого игрока
                        Data.CashlessBalance = fromServ.CashlessBalance;
                        Data.StorageBalance = fromServ.StorageBalance;

                        errorNum += "8 ";
                        //обновляем планету // updating the planet
                        UpdateWorldController.LoadFromServer(fromServ, firstRun);

                        errorNum += "9 ";
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

                        errorNum += "10 ";
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
                    }
                    catch
                    {
                        Loger.Log("Client  Exception errorNum = " + errorNum);
                        throw;
                    }
                });
            }
        }

        /// <summary>
        /// Аналогично Command((connect) => ActionCommand);
        /// Но с 3 повторами. При неудаче в некоторых случая лучше вызвать Disconnected("OCity_SessionCC_Disconnected".Translate() + " " + errorMessage);
        /// </summary>
        /// <param name="ActionCommand"></param>
        /// <returns></returns>
        public static string CommandSafely(Func<SessionClient, bool> ActionCommand)
        {
            var errorMessage = "";
            Loger.Log("Client CommandSafely try ");
            int repeat = 0;
            do
            {
                Command((connect) =>
                {
                    if (ActionCommand(connect))
                    {
                        repeat = 1000;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(connect.ErrorMessage)) errorMessage = connect.ErrorMessage?.ServerTranslate();
                        Thread.Sleep(1000);
                        if (repeat > 0) Thread.Sleep(3000);
                        Loger.Log($"Client CommandSafely try again (error: {errorMessage})");
                    }
                });
            }
            while (++repeat < 3); //делаем 3 попытки включая первую
            return repeat >= 1000 ? null : errorMessage;
        }

        /// <summary>
        /// Аналогично SaveGameNow(true, () => { Command((connect) => ActionCommand); } );
        /// Но безопасный вариант с повтором при переподключении
        /// </summary>
        public static void SaveGameNowSingleAndCommandSafely(
            Func<SessionClient, bool> ActionCommand,
            Action FinishGood,
            Action FinishBad,
            bool single = true)
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

                if (CommandSafely(ActionCommand) != null)
                {
                    if (FinishBad != null) FinishBad();
                }
                else
                {
                    if (FinishGood != null) FinishGood();
                }
            };
            Data.ActionAfterReconnect = () =>
            {
                //повторить, если во время сохранения произошла ошибка, если не выйдет, то ошибка
                Data.ActionAfterReconnect = FinishBad;
                SaveGameNow(single, act);
            };
            SaveGameNow(single, act);
        }

        private static byte[] SaveGameCore()
        {
            byte[] content;
            try
            {
                ScribeSaver_InitSaving_Patch.Enable = true;
                GameDataSaveLoader.SaveGame(SaveName);
                //content = File.ReadAllBytes(SaveFullName.Replace("\\", "" + Path.DirectorySeparatorChar));
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

        private static void UpdateGlobalTooltip()
        {
            try
            {
                GlobalControlsUtility_DoDate_Patch.Update = DateTime.UtcNow;
                GlobalControlsUtility_DoDate_Patch.OutText =
                    new List<string>() {
                        (SessionClient.IsRelogin || Data.LastServerConnectFail
                            ? (string)("(!) " + "OCity_Dialog_Connecting".TranslateCache() + " ")
                            : (SessionClient.Get?.IsLogined ?? false)
                            ? (string)($"✔ " + (int)Data.Ping.TotalMilliseconds + new TaggedString("ms")) : "X ")
                        + " " + SessionClientController.My.Login
                        };
                GlobalControlsUtility_DoDate_Patch.TooltipText = string.Format(
                    "OC_SessionCC_Info".Translate() +"." + "OC_SessionCC_Balance".Translate()
                    , Data.ServerName + " (" + (ModBaseData.GlobalData?.LastIP?.Value ?? "") + ")", My.Login);
                if (!SessionClient.IsRelogin && !Data.LastServerConnectFail && Data.CashlessBalance != 0)
                {
                    var mon = Data.CashlessBalance.ToString(); //ToStringMoney();
                    GlobalControlsUtility_DoDate_Patch.OutText.Add(mon + " ");
                    GlobalControlsUtility_DoDate_Patch.OutInLastLine = GameUtils.TextureCashlessBalance;
                    GlobalControlsUtility_DoDate_Patch.TooltipText += mon;
                }
                else
                {
                    GlobalControlsUtility_DoDate_Patch.OutInLastLine = null;
                    GlobalControlsUtility_DoDate_Patch.TooltipText += "-";
                }

                if ((DateTime.UtcNow - SessionClientController.My.LastSaveTime).TotalMinutes < 60)
                {
                    GlobalControlsUtility_DoDate_Patch.TooltipText += " " + Environment.NewLine
                        + "OC_SessionCC_MinSave".Translate() + " "
                        + (int)(DateTime.UtcNow - SessionClientController.My.LastSaveTime).TotalMinutes;
                }
            }
            catch (Exception ex)
            {
                GlobalControlsUtility_DoDate_Patch.Update = DateTime.MinValue;
                Loger.Log("Exception UpdateGlobalTooltip " + ex.ToString());
            }
            try
            {
                SetPauseWithRelogin();
            }
            catch { }
        }
        private static bool ReloginPauseActived = false;
        private static bool ReloginPauseShowDialog = false;
        private static void SetPauseWithRelogin()
        {
            if (SessionClient.IsRelogin
                || Data.LastServerConnectFail
                || Data.Ping.TotalMilliseconds == 0
                || Data.Ping.TotalMilliseconds > 9000)
            {
                //при проблемах подклюения
                if (!Find.TickManager.Paused)
                {
                    Find.TickManager.Pause();
                    if (!ReloginPauseActived)
                    {
                        //первый раз при обнаружении идущей игры ставим паузу молча
                        ReloginPauseActived = true;
                    }
                    else
                    {
                        //второй раз выдаем предупреждение
                        if (!ReloginPauseShowDialog)
                        {
                            ReloginPauseShowDialog = true;
                            var form = new Dialog_Input("OCity_SessionCC_Synchronization".TranslateCache(), "OCity_SessionCC_SynchronizationText".TranslateCache(), true);
                            form.PostCloseAction = () =>
                            {
                                ReloginPauseShowDialog = false;
                            };
                            Find.WindowStack.Add(form);
                        }
                    }
                }
            }
            else
            {
                ReloginPauseActived = false;
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

                        //обновляем чат
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

                        //в этой же процедуре делаем фоновую загрузку изображений с сервера
                        GeneralTexture.Get.Update(connect);
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
                    //to do Сделать сброс крутяшки после обновления чата (см. Dialog_MainOnlineCity)
                    UpdateColonyScreen();
                }
                catch (Exception ex)
                {
                    Loger.Log(ex.ToString());
                }
                finally
                {
                    ChatIsUpdating = false;
                    UpdateGlobalTooltip();
                }
            });
        }

        public static async Task<SessionClient> Connect(string hostname)
        {
            await ModBaseData.Scheduler.Schedule(() => TimersStop());

            var uri = new Uri("tcp://" + hostname.Trim());
            int port = SessionClient.DefaultPort;
            if (!uri.IsDefaultPort)
                port = uri.Port;

            var dnsName = uri.Host;

            var logMsg = "Connecting to server. Addr: " + dnsName + ". Port: " + port.ToString();
            Loger.Log("Client " + logMsg);
            Log.Warning(logMsg);

            var connect = SessionClient.Get;
            connect.Connect(dnsName, port);
            logMsg = "Connection OK";
            Loger.Log("Client " + logMsg);
            Log.Warning(logMsg);

            return connect;
        }

        private static string GetSaffix()
        {
            return "@@@"
                + "11" + FileChecker.GetCheckSum("y39¤`"
                    + Environment.UserName + "*" + Environment.MachineName
                    ).Replace("==", "").Substring(4, 19);
        }

        public static async Task DoUserRegistration()
        {
            try
            {
                // Get login credentials from user
                var lastIp = ModBaseData.GlobalData?.LastIP?.Value ?? "";
                var lastUsername = ModBaseData.GlobalData?.LastLoginName?.Value ?? "";
                if (string.IsNullOrEmpty(lastIp))
                {
                    lastIp = MainHelper.DefaultIP ?? "";
                }

                var registrationData = await Dialog_Registration.GetAsync();
                var status = await StatusWindow.Create("Connecting", "Connecting to " + registrationData.addr, "");
                var client = await Registration(registrationData.addr, registrationData.login, registrationData.password, registrationData.email);
                status.Heading = "Creating account...";
                await SetupAccount(client, registrationData.addr, registrationData.login);
                await status.Close();
            }
            catch (OperationCanceledException)
            {
                // User canceled this
            }
            catch (Exception e)
            {
                Log.Message("An error occurred during registration. Please try again.\n" + e.Message + "\n\n" + e.StackTrace);
                Loger.Log("An error occurred during registration. Please try again.\n" + e.Message + "\n\n" + e.StackTrace);
                await ModBaseData.Scheduler.Schedule(() => Disconnected("An error occurred during registration. Please try again.\n" + e.Message + "\n\n" + e.StackTrace));
            }
        }

        public static async Task DoUserLogin()
        {
            try
            {
                // Get login credentials from user
                var lastIp = ModBaseData.GlobalData?.LastIP?.Value ?? "";
                var lastUsername = ModBaseData.GlobalData?.LastLoginName?.Value ?? "";
                if (string.IsNullOrEmpty(lastIp))
                {
                    lastIp = MainHelper.DefaultIP ?? "";
                }

                LoginFormResult credentials;
                credentials.hostname = lastIp;
                credentials.username = lastUsername;
                credentials.password = "";
                SessionClient client = null;
                while (true)
                {
                    try
                    {
                        credentials = await Dialog_LoginForm.GetAsync(credentials.hostname, credentials.username, credentials.password);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    var status = await StatusWindow.Create("Connecting", "Connecting to " + credentials.hostname, "");
                    try
                    {
                        client = await Login(credentials.hostname, credentials.username, credentials.password);
                        await status.Close();
                        break;
                    }
                    catch (ApplicationException e)
                    {
                        await status.Close();
                        await AsyncDialog_Input.GetAsync("OCity_SessionCC_LoginFailTitle".Translate(), e.Message, true);
                    }
                }
                await SetupAccount(client, credentials.hostname, credentials.username);
            } catch (OperationCanceledException)
            {
                // User canceled this
            } catch (Exception e)
            {
                Log.Message("An error occurred during login. Please try again.\n" + e.Message + "\n\n" + e.StackTrace);
                Loger.Log("An error occurred during login. Please try again.\n" + e.Message + "\n\n" + e.StackTrace);
                await ModBaseData.Scheduler.Schedule(() => Disconnected("An error occurred during login. Please try again.\n" + e.Message + "\n\n" + e.StackTrace));
            }
        }

        private static async Task SetupAccount(SessionClient client, string hostname, string username)
        {
            // Store login credentials for next time
            bool isNewServer = ModBaseData.GlobalData?.LastIP?.Value != hostname;
            if (ModBaseData.GlobalData?.LastIP != null)
            {
                ModBaseData.GlobalData.LastIP.Value = hostname;
                ModBaseData.GlobalData.LastLoginName.Value = username;
                HugsLibController.SettingsManager.SaveChanges();
            }

            if (isNewServer)
            {
                //Текстовое оповещение о возможном обновлении с сервера
                try
                {
                    await AsyncDialog_Input.GetAsync("OCity_SessionCC_InitConnectedIntro_Title".Translate()
                    , "OCity_SessionCC_InitConnectedIntro_Text".Translate());
                }
                catch (OperationCanceledException)
                {
                    await ModBaseData.Scheduler.Schedule(() => Disconnected("OCity_DialogInput_Cancele".Translate()));
                    return;
                }
            }

            // Model setup
            ModelInfo serverInfo = null;
            await ModBaseData.Scheduler.Schedule(() =>
            {
                Loger.Log("Client InitConnected()");
                Data = new ClientData();
                TimersStop();
                Timers = new WorkTimer();
                TimerReconnect = new WorkTimer();

                var packet = new ModelInt() { Value = (int)ServerInfoType.Full };
                serverInfo = client.TransObject2<ModelInfo>(packet, PackageType.Request5UserInfo, PackageType.Response6UserInfo);

                ServerTimeDelta = serverInfo.ServerTime - DateTime.UtcNow;
                SetFullInfo(serverInfo);

                Loger.Log($"Server time difference {ServerTimeDelta:hh\\:mm\\:ss\\.ffff}. Server UTC time: {serverInfo.ServerTime:yyyy-MM-dd HH:mm:ss.ffff}");
                Loger.Log("Client ServerName=" + serverInfo.ServerName);
                Loger.Log("Client ServerVersion=" + serverInfo.VersionInfo + " (" + serverInfo.VersionNum + ")");
                Loger.Log("Client IsAdmin=" + serverInfo.IsAdmin
                    + " Seed=" + serverInfo.Seed
                    + " Scenario=" + serverInfo.ScenarioName
                    + " NeedCreateWorld=" + serverInfo.NeedCreateWorld
                    + " DelaySaveGame=" + Data.DelaySaveGame
                    + " DisableDevMode=" + Data.DisableDevMode);
                Loger.Log("Client Grants=" + serverInfo.My.Grants.ToString());

                if (SessionClientController.Data.DisableDevMode)
                {
                    if (Prefs.DevMode) Prefs.DevMode = false;
                    if (IdeoUIUtility.devEditMode) IdeoUIUtility.devEditMode = false;
                    DebugSettingsDefault.SetDefault();
                    // также ещё можно подписаться в метод PrefsData.Apply() и следить за изменениями оттуда
                }
            });

            if (serverInfo.IsModsWhitelisted)
            {
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

                    try
                    {
                        await AsyncDialog_Input.GetAsync((string)serverInfo.ServerName + " (" + hostname + ")", entranceWarning, false);
                    }
                    catch (OperationCanceledException)
                    {
                        await ModBaseData.Scheduler.Schedule(() => Disconnected("OCity_SessionCC_MsgCanceledCreateW".Translate(), () => ModsConfig.RestartFromChangedMods()));
                        return;
                    }
                }

                var status = await StatusWindow.Create("Hashing files", "initializing", "");

                var checker = await ClientFileChecker.FromServer(SessionClient.Get);
                await checker.Initialize((s, i) =>
                {
                    status.Heading = s;
                    status.Status = i.ToString();
                });

                // Confirm synchronization and sync
                if (checker.ChangeSets.Any())
                {
                    var confirmation = await AsyncDialog_Input.GetAsync("Files differ between server and client. Do you want to sync?", string.Join("\n", checker.ChangeSets.Select(s => s.ToString())));

                    status.Title = "Downloading files";
                    status.Heading = "initializing";
                    status.Status = "";

                    int changeSetCounter = 0;
                    foreach (var changeSet in checker.ChangeSets)
                    {
                        changeSetCounter += 1;
                        status.Heading = string.Format("{0}, {1} / {2}", changeSet.SourceDescription, changeSetCounter, checker.ChangeSets.Count());
                        int counter = 0;
                        foreach (var deletion in changeSet.DeletePaths)
                        {
                            status.Status = string.Format("Removing {0} / {1}", counter, changeSet.DeletePaths.Count);
                            Loger.Log("rm " + changeSet.SourceDescription + ": " + Path.Combine(changeSet.RemoveRoot, deletion));
                            var path = Path.Combine(changeSet.RemoveRoot, deletion);
                            if (File.Exists(path))
                                File.Delete(path);
                        }
                        var pathsToDownload = changeSet.DownloadPaths.ToDictionary(t => t.Item1, t => t.Item2);
                        while (pathsToDownload.Count > 0)
                        {
                            status.Status = string.Format("Downloading {0} / {1}", changeSet.DownloadPaths.Count - pathsToDownload.Count, changeSet.DownloadPaths.Count);
                            int maxDownloads = 1024;

                            var request = new ModelModsFilesRequest()
                            {
                                Type = ModelModsFilesRequest.RequestType.FileData,
                                FileQueries = pathsToDownload.Keys.Take(maxDownloads).Select(p => new ModelModsFilesRequest.FileQuery() { ModId = changeSet.ModId, SourceDirectory = changeSet.FolderType, RelativePath = p }).ToList(),
                            };
                            var response = await SessionClient.Get.TransAsync<ModelModsFilesRequest, ModelModsFilesResponse>(request);
                            if (response.Contents?.Entries == null)
                            {
                                status.Status = "Server did not send a valid response.";
                                throw new Exception("Server did not send a valid response.");
                            }
                            List<string> pathsToRemove = new List<string>();
                            foreach (var file in response.Contents.Entries)
                            {
                                if (file.ModId != changeSet.ModId || file.SourceDirectory != changeSet.FolderType || !pathsToDownload.ContainsKey(file.RelativePath))
                                {
                                    status.Status = "Server did not send a valid response.";
                                    throw new Exception("Server did not send a valid response.");
                                }
                                pathsToRemove.Add(file.RelativePath);
                            }

                            status.Status = string.Format("Writing {0} / {1}", changeSet.DownloadPaths.Count - pathsToDownload.Count, changeSet.DownloadPaths.Count);
                            var writes = response.Contents.Entries.Select((e) =>
                            {
                                var localPath = pathsToDownload[e.RelativePath];
                                return Task.Factory.StartNew(() =>
                                {
                                    var stream = new MemoryStream(e.GZippedData);
                                    var gzip = new GZipStream(stream, CompressionMode.Decompress);

                                    var path = Path.Combine(changeSet.DownloadRoot, localPath);
                                    var parentDirectory = Path.GetDirectoryName(path);
                                    if (!Directory.Exists(parentDirectory))
                                        Directory.CreateDirectory(parentDirectory);

                                    using (var f = File.OpenWrite(path))
                                    {
                                        f.SetLength(0);
                                        gzip.CopyTo(f);
                                    }
                                });
                            });
                            await Task.WhenAll(writes);
                            foreach (var path in pathsToRemove)
                                pathsToDownload.Remove(path);
                        }
                    }

                    // Now restart
                    await ModBaseData.Scheduler.Schedule(() => Disconnected("Mod files changed.", () => ModsConfig.RestartFromChangedMods()));
                    return;
                }
                await status.Close();
            }


            if (MainHelper.VersionNum < serverInfo.VersionNum)
            {
                await ModBaseData.Scheduler.Schedule(() => Disconnected("OCity_SessionCC_Client_UpdateNeeded".Translate() + serverInfo.VersionInfo));
                return;
            }

            //создаем мир, если мы админ
            if (serverInfo.IsAdmin && serverInfo.Seed == "")
            {
                Loger.Log("Client InitConnected() IsAdmin");
                await ModBaseData.Scheduler.Schedule(() =>
                {
                    var form = new Dialog_CreateWorld();
                    form.PostCloseAction = () =>
                    {
                        if (!form.ResultOK)
                        {
                            GetScenarioByName(form.InputScenario);
                            Disconnected("OCity_SessionCC_MsgCanceledCreateW".Translate());
                            return;
                        }

                        GameStarter.SetMapSize = int.Parse(form.InputMapSize);
                        GameStarter.SetPlanetCoverage = form.InputPlanetCoverage / 100f;
                        GameStarter.SetSeed = form.InputSeed;
                        GameStarter.SetDifficulty = form.InputDifficultyDefName;
                        GameStarter.SetScenario = GetScenarioByName(form.InputScenario);
                        GameStarter.SetScenarioName = form.InputScenarioKey;
                        ChoosedTeller = form.InputStorytellerDef;

                        GameStarter.AfterStart = CreatingServerWorld;
                        GameStarter.GameGeneration();
                    };

                    Find.WindowStack.Add(form);
                });
                return;
            }

            if (serverInfo.NeedCreateWorld)
            {
                await CreatePlayerWorld(serverInfo);
                return;
            }

            await ModBaseData.Scheduler.Schedule(() => LoadPlayerWorld(serverInfo));
        }

        public static async Task<SessionClient> Login(string hostname, string username, string password)
        {
            var client = await Connect(hostname);
            ConnectAddr = hostname;

            var saffix = GetSaffix();
            var logMsg = "Registration. Login: " + username + ", Saffix: " + saffix;
            Loger.Log("Client " + logMsg + ", Saffix: " + saffix);
            Log.Warning(logMsg);
            My = null;
            var pass = new CryptoProvider().GetHash(password);

            await client.Login(username, pass, saffix);

            logMsg = "Login OK";
            Loger.Log("Client " + logMsg);
            Log.Warning(logMsg);
            
            return client;
        }


        /// <summary>
        /// Регистрация
        /// </summary>
        /// <returns>null, или текст произошедшей ошибки</returns>
        public static async Task<SessionClient> Registration(string addr, string login, string password, string email)
        {
            var client = await Connect(addr);
            ConnectAddr = addr;

            var saffix = GetSaffix();
            var logMsg = "Registration. Login: " + login + ", Saffix: " + saffix + ", Email: " + email;
            Loger.Log("Client " + logMsg + ", Saffix: " + saffix);
            Log.Warning(logMsg);
            My = null;
            var pass = new CryptoProvider().GetHash(password);

            await client.Registration(login, pass, email + saffix);

            logMsg = "Registration OK";
            Loger.Log("Client " + logMsg);
            Log.Warning(logMsg);

            return client;
        }

        /// <summary>
        /// Отбрасывание старого подключения, создание нового и аутэнтификация. Перед вызовом убедиться, что поток таймера остановлен
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> Reconnect()
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

            var logMsg = "Reconnect to server. Addr: " + ConnectAddr;
            Loger.Log("Client " + logMsg);
            Log.Warning(logMsg);
            var client = await Connect(ConnectAddr);

            SessionClient.Recreate(client);

            logMsg = "Reconnect login: " + My.Login;
            Loger.Log("Client " + logMsg);
            Log.Warning(logMsg);
            await client.Reconnect(My.Login, Data.KeyReconnect, GetSaffix());

            logMsg = "Reconnect OK";
            Loger.Log("Client " + logMsg);
            Log.Warning(logMsg);
            return true;
        }

        /// <summary>
        /// Проверяет подключение и немедленно вызывает netAct
        /// </summary>
        public static void Command(Action<SessionClient> netAct)
        {
            int time = 0;
            while (SessionClient.IsRelogin && time < 40000)
            {
                UpdateGlobalTooltip();
                Thread.Sleep(500);
                time += 500;
            }
            if (SessionClient.IsRelogin)
            {
                Loger.Log("Client Command: fail wait IsRelogin. Try to continue", Loger.LogLevel.WARNING);
            }
            var connect = SessionClient.Get;
            netAct(connect);
        }

        private static Thread SingleCommandThread = null;

        public static bool SingleCommandIsBusy => SingleCommandThread != null;
        /// <summary>
        /// Проверяет подключение и вызывает netAct в потоке, если оне не занят, иначе возвращает false
        /// </summary>
        public static bool SingleCommand(Action<SessionClient> netAct)
        {
            if (SingleCommandIsBusy) return false;
            SingleCommandThread = new Thread(() =>
            {
                try
                {
                    Command(netAct);
                }
                catch (Exception ext)
                {
                    Loger.Log("Exception SingleCommand: " + ext.ToString());
                }
                finally
                { 
                    SingleCommandThread = null; 
                }
            });
            SingleCommandThread.IsBackground = true;
            SingleCommandThread.Start();
            return true;
        }

        private static void TimersStop()
        {
            ReconnectSupportRuning = false;
            Loger.Log("Client TimersStop b");
            if (TimerReconnect != null) TimerReconnect.Stop();
            TimerReconnect = null;

            if (Timers != null) Timers.Stop();
            Timers = null;
            Loger.Log("Client TimersStop e");
        }

        public static Scenario GetScenarioByName(string scenarioName)
        {
            var list = GameUtils.AllowedScenarios();

            var scenario = list.FirstOrDefault(s => s.Value.name == scenarioName).Value;
            if (scenario == null)
            {
                Loger.Log("error: scenario not found: " + scenarioName);
            }
            return scenario;
        }

        public static Storyteller GetStoryteller(string difficultyName, StorytellerDef teller = null)
        {
            var list = DefDatabase<DifficultyDef>.AllDefs.ToList();
            var difficulty = list.FirstOrDefault(d => d.defName == difficultyName);
            if (difficulty == null)
            {
                difficulty = DifficultyDefOf.Easy;
                //throw new ApplicationException($"Dont find difficulty: {difficultyName}");
                //difficulty = list[list.Count - 1];
            }

            if (teller == null) teller = StorytellerDefOf.Cassandra;
            return new Storyteller(teller, difficulty);
        }

        public static void SetFullInfo(ModelInfo serverInfo)
        {
            My = serverInfo.My;
            Data.ServerName = serverInfo.ServerName;
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

        private static void LoadPlayerWorld(ModelInfo serverInfo)
        {
            Loger.Log("Client LoadPlayerWorld");

            var connect = SessionClient.Get;
            var worldData = connect.WorldLoad();

            Action loadAction = () =>
            {
                LongEventHandler.QueueLongEvent(delegate
                {
                    Current.Game = new Game { InitData = new GameInitData { gameToLoad = SaveName } };
                    
                    Current.Game.storyteller = GetStoryteller(Data.GeneralSettings.Difficulty, GameUtils.GetStorytallerByName(Data.GeneralSettings.StorytellerDef));
                    Loger.Log($"storyteller: {Current.Game.storyteller.def.defName}      difficulty: {Current.Game.storyteller.difficultyDef.defName}");
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


        //Создание мира для обычного игрока

        private static async Task CreatePlayerWorld(ModelInfo serverInfo)
        {
            Loger.Log("Client InitConnected() ExistMap0");
            string scenario = null;
            if (SessionClientController.Data.GeneralSettings.ScenarioAviable)
            {
                scenario = await Dialog_Scenario.GetAsync();
            } else
            {
                scenario = serverInfo.ScenarioName;
            }

            await ModBaseData.Scheduler.Schedule(() => CreatePlayerWorldPart1(serverInfo, scenario));
        }
        private static void CreatePlayerWorldPart1(ModelInfo serverInfo, string choosed_scenario)
        {
            //создать поселение
            GameStarter.SetMapSize = serverInfo.MapSize;
            GameStarter.SetPlanetCoverage = serverInfo.PlanetCoverage;
            GameStarter.SetSeed = serverInfo.Seed;
            if (choosed_scenario == null)
            {
                GameStarter.SetScenario = GetScenarioByName(serverInfo.ScenarioName);
            }
            else
            {
                GameStarter.SetScenario = GetScenarioByName(choosed_scenario);
                GameStarter.SetScenarioName = choosed_scenario;
            }
            GameStarter.SetDifficulty = serverInfo.Difficulty;
            GameStarter.AfterStart = CreatePlayerMap;
            ChoosedTeller = GameUtils.GetStorytallerByName(serverInfo.Storyteller);

            GameStarter.GameGeneration(false);

            //выбор места на планете. Код из события завершения выбора параметров планеты Page_CreateWorldParams
            Loger.Log($"Client InitConnected() ExistMap1 Scenario={choosed_scenario ?? serverInfo.ScenarioName}({GameStarter.SetScenario.name}/{GameStarter.SetScenario.fileName})" +
                $" Difficulty={GameStarter.SetDifficulty}" + $" Storyteller={serverInfo.Storyteller}");

            Current.Game = new Game();
            Current.Game.InitData = new GameInitData();
            //Current.Game.storyteller
            Current.Game.Scenario = GameStarter.SetScenario;
            Current.Game.Scenario.PreConfigure();
            /* Current.Game.storyteller = new Storyteller(StorytellerDefOf.Cassandra
                , GameStarter.SetDifficulty == 0 ? DifficultyDefOf.Easy
                    : DifficultyDefOf.Rough); */
            Current.Game.storyteller = GetStoryteller(GameStarter.SetDifficulty, ChoosedTeller);

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

        public static Page GetFirstConfigPage()
        {
            //скопированно из Scenario.GetFirstConfigPage()
            List<Page> list = new List<Page>();
            //list.Add(new Page_SelectStoryteller());
            //list.Add(new Page_CreateWorldParams());
            list.Add(new Page_SelectStartingSite());
            if (ModsConfig.IdeologyActive)
            {
                list.Add(new Page_ChooseIdeoPreset());
            }
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
            toServ.Storyteller = ChoosedTeller.defName;
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
                    + Environment.NewLine + connect.ErrorMessage?.ServerTranslate();
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
                actionOnDisctonnect();
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
                if (IdeoUIUtility.devEditMode) IdeoUIUtility.devEditMode = false;
                // также ещё можно подписаться в метод PrefsData.Apply() и следить за изменениями оттуда
            }

            if (UpdateWorldSafelyRun != null)
            {
                Loger.Log("Client UpdateWorldSafelyRun() b");
                var fun = UpdateWorldSafelyRun;
                UpdateWorldSafelyRun = null;
                UpdateWorld(false);
                fun();
                Loger.Log("Client UpdateWorldSafelyRun() e");
            }
        }
        #region На время реконнекта запускаем поток, который будет обновлять статус в интерфейсе запуская UpdateGlobalTooltip();
        private static Thread ReconnectSupportThread = null;
        private static bool ReconnectSupportRuning = false;
        private static void ReconnectSupportInit()
        {
            if (ReconnectSupportThread == null)
            {
                ReconnectSupportThread = new Thread(() =>
                {
                    while (true)
                    {
                        Thread.Sleep(900);
                        if (!SessionClient.Get.IsLogined)
                        {
                            ReconnectSupportRuning = false;
                            ReconnectSupportThread = null;
                            return;
                        }
                        if (!ReconnectSupportRuning) continue;

                        UpdateGlobalTooltip();
                    }
                });
                ReconnectSupportThread.IsBackground = true;
                ReconnectSupportThread.Start();
            }
        }
        #endregion
        public static bool ReconnectWithTimers()
        {
            Timers.Pause = true;
            SessionClient.IsRelogin = true;
            ReconnectSupportInit();
            ReconnectSupportRuning = true;
            try
            {
                var repeat = 3;
                while (repeat-- > 0)
                {
                    Loger.Log("Client CheckReconnectTimer() " + (3 - repeat).ToString());
                    try
                    {
                        var reconnectTask = Reconnect();
                        reconnectTask.Wait();
                        if (reconnectTask.Result)
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
                    var sleep = 7000;
                    while (sleep > 0)
                    { 
                        Data.Ping = DateTime.UtcNow - Data.LastServerConnect;
                        UpdateGlobalTooltip();
                        Thread.Sleep(500);
                        sleep -= 500;
                    }
                }
                return false;
            }
            finally
            {
                Timers.Pause = false;
                SessionClient.IsRelogin = false;
                ReconnectSupportRuning = false;
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

            try
            {
                //обновление стауса
                if ((DateTime.UtcNow - Data.LastServerConnect).TotalMilliseconds > 1000)
                {
                    Data.Ping = DateTime.UtcNow - Data.LastServerConnect;
                    UpdateGlobalTooltip();
                }

                //Loger.Log("Client TestBagSD CRTb");
                var connect = SessionClient.Get;
                var needReconnect = false;
                //проверка коннекта
                if (connect.Client.CurrentRequestStart != DateTime.MinValue)
                {
                    var sec = (long)(DateTime.UtcNow - connect.Client.CurrentRequestStart).TotalSeconds;
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
                    var sec = (long)(DateTime.UtcNow - Timers.LastLoop).TotalSeconds;
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
            }
            catch (Exception e)
            {
                //Никогда не должен был сюда заходить, но как то раз зашел, почему - так и не разобрались. Но теперь этот код тут :)
                Loger.Log("Client CheckReconnectTimer exception: " + e.ToString(), Loger.LogLevel.ERROR);
                try
                {
                    Disconnected("OCity_SessionCC_Disconnected".Translate());
                }
                catch
                {
                    Environment.Exit(0);
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

                //Делаем запрос на главный поток, чтобы дождаться когда игра прогрузится и начнуться регулярный обновления интерфейся
                ModBaseData.RunMainThreadSync(() => Loger.Log("Client InitGame MainThread check OK"), 60 * 5);
                Loger.Log("Client InitGame MainThread check end");

                GeneralTexture.Init();

                UpdateColonyScreenLastTickBySettlementID = new Dictionary<long, long>();

                //сбрасываем с мышки выбранный инструмент DevMode
                DebugTools.curTool = null;

                MainButtonWorker_OC.ShowOnStart();
                UpdateWorldController.ClearWorld();
                UpdateWorldController.InitGame();
                ChatController.Init(true);
                Data.UpdateTime = DateTime.MinValue;
                //UpdateWorld Синхронизация мира
                UpdateWorld(true);
                Data.LastServerConnect = DateTime.MinValue;

                Timers.Add(100, UpdateFastTimer);

                Timers.Add(500, UpdateChats);
                //Обновление мира
                Timers.Add(5000, () => UpdateWorld(false));
                //Пинг раз в 10 сек для поддержания соединения
                Timers.Add(10000, PingServer);
                //Сохранение игры
                Timers.Add(60000 * Data.DelaySaveGame, BackgroundSaveGame);
                //Следит за 1 таймером, создаёт новое соединение
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
                        Loger.Log("Client BeforeExit Exception: " + e.ToString(), Loger.LogLevel.ERROR);
                        throw;
                    }
                };



                /*
                // <<<<<<<<<<<<<<<<<<<<<<<<<<
                //отладка!!!
                Find.WindowStack.Add(new Dialog_InputImage()
                {
                    SelectImageAction = (img, data) =>
                    {
                        Command((connect) =>
                        {
                            connect.FileSharingUpload(FileSharingCategory.PlayerIcon, My.Login, data);
                            var p = connect.FileSharingDownload(FileSharingCategory.PlayerIcon, My.Login);


                            connect.FileSharingUpload(FileSharingCategory.ColonyScreen, My.Login + "@6", data); // Login_serverId
                            var p2 = connect.FileSharingDownload(FileSharingCategory.ColonyScreen, My.Login + "@6");

                            Disconnected("test " + data.Length + " " + p.Data.Length + " " + (p2.Data?.Length ?? 0));
                        });
                    }
                });
                // <<<<<<<<<<<<<<<<<<<<<<<<<<
                */


            }
            catch (Exception e)
            {
                ExceptionUtil.ExceptionLog(e, "Client InitGame Error");
                Disconnected(null);
                //GameExit.BeforeExit = null;
                //TimersStop();
                //if (Current.Game == null) return;
                //SessionClient.Get.Disconnect();
            }
        }

    }
}