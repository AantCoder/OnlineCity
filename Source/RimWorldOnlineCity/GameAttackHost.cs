using Model;
using OCUnion;
using OCUnion.Transfer.Model;
using RimWorld;
using RimWorld.Planet;
using RimWorldOnlineCity.GameClasses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Transfer;
using Verse;
using Verse.AI;

namespace RimWorldOnlineCity
{
    public class GameAttackHost
    {
        /// <summary>
        /// Время в ms между синхронизациями с сервером
        /// </summary>
        public int AttackUpdateDelay { get; } = 50;

        /// <summary>
        /// Для быстрой передачи не передавать растения (Кроме деревьев они всегда передаются)
        /// </summary>
        public bool PlantNotSend { get; } = false;

        /// <summary>
        /// Время в сек между полной синхронизацией пешек, например, чтобы после получаения урона увидеть точное здоровье
        /// </summary>
        public int SendDelayedFillPawnsSeconds { get; } = 30;


        /// <summary>
        /// Сколько секунд ждать после подения последней пешки прежде чем засчитывать победу
        /// </summary>
        public int CheckVictoryDelay { get; } = 20;

        /// <summary>
        /// Расстояние от краев карты за которым пешка мжет сбежать
        /// </summary>
        public int MapBorder { get; } = 10;

        /// <summary>
        /// Установить заданную скорость вместо нормальной 1
        /// </summary>
        public float TickTimeSpeed { get; } = 0.5f;

        /// <summary>
        /// Насколько изменяется скорость всех пешек кроме атакующих (для баланса)
        /// </summary>
        public float HostPawnMoveSpeed { get; } = 0.5f;

        public bool TestMode { get; set; }

        public string AttackerLogin { get; set; }

        public long HostPlaceServerId { get; set; }

        public long InitiatorPlaceServerId { get; set; }

        /// <summary>
        /// Список уже переданных пешек, сверемся с ним и передаем тех которых тут нет. И наборот, если тут есть, а пешки уже нет, то в список к удалению
        /// </summary>
        public HashSet<int> SendedPawnsId { get; set; }

        /// <summary>
        /// Переданные последний раз данные (их хеш) по ID
        /// </summary>
        public Dictionary<int, int> SendedState { get; set; }
        /// <summary>
        /// Переданные последний раз данные по здоровью по ID
        /// </summary>
        public Dictionary<int, int> SendedXP { get; set; }


        /// <summary>
        /// Вещи, информацию по которым нужно обновить
        /// </summary>
        public List<int> ToUpdateStateId { get; set; }
        public List<Thing> ToUpdateState { get; set; }

        /// <summary>
        /// К передаче на удаление
        /// </summary>
        public HashSet<int> ToSendDeleteId { get; set; }

        /// <summary>
        /// К передаче на создание/обновление пешек
        /// </summary>
        public HashSet<int> ToSendAddId { get; set; }

        /// <summary>
        /// К передаче на создание не пешек и трупы 
        /// </summary>
        public HashSet<Thing> ToSendThingAdd { get; set; }

        /// <summary>
        /// Вещи которые когда-либо передавались атакующему, актуальные (те, что к удалюению удаляються и тут)
        /// </summary>
        public Dictionary<int, Thing> SendedActual { get; set; }

        /// <summary>
        /// Список огня на карте, который нужно обновлять регулярно раз в SendDelayedFillPawnsSeconds
        /// </summary>
        public HashSet<Thing> FireList { get; set; }

        /// <summary>
        /// Пешки из этого массива должны быть переданны для полного обновления раз в SendDelayedFillPawnsSeconds сек
        /// </summary>
        public HashSet<int> ToSendDelayedFillPawnsId { get; set; }

        public DateTime SendDelayedFillPawnsLastTime;

        /// <summary>
        /// Пешки которые атакуют хост.
        /// </summary>
        public HashSet<Pawn> AttackingPawns { get; set; }

        /// <summary>
        /// Пешки которые атакуют хост. (ID пешки у хоста, ID пешки у атакующего (пришло в OriginalID, а ему отправляется в TransportID))
        /// </summary>
        public Dictionary<int, int> AttackingPawnDic { get; set; }

        /// <summary>
        /// Предыдущее местоположение атакующих пешек, нужно только для механизма заплатки бага. См. Thing_Position_Patch
        /// </summary>
        public Dictionary<Pawn, IntVec3> AttackingPawnsLastPos { get; set; }

        /// <summary>
        /// Команды у пешек, которые атакуют
        /// </summary>
        public Dictionary<int, AttackPawnCommand> AttackingPawnJobDic { get; set; }

        private Object ToSendListsSync = new Object();

        public long AttackUpdateTick { get; set; }

        private Map GameMap { get; set; }

        /// <summary>
        /// Пешка, которая начала Job связанный с вещью. Пешка - Вещь
        /// </summary>
        private Dictionary<int, Thing> ThingPrepareChange2 = new Dictionary<int, Thing>();

        /// <summary>
        /// Job связанный с вещью был завершен, она готова к следующей отправке
        /// </summary>
        private HashSet<Thing> ThingPrepareChange1 = new HashSet<Thing>();

        /// <summary>
        /// Job связанный с вещью был завершен, она готова к отправке
        /// </summary>
        private HashSet<Thing> ThingPrepareChange0 = new HashSet<Thing>();

        /// <summary>
        /// Должна быть установлена пауза до достижения этого времени
        /// </summary>
        public DateTime CurrentPauseToTime;

        /// <summary>
        /// Должна ли быть установлена пауза прямо сейчас
        /// </summary>
        public bool IsPause => CurrentPauseToTime > DateTime.UtcNow;

        /// <summary>
        /// Замедлять защищающихся после минуты игры
        /// </summary>
        public bool HostPawnMoveSpeedActive =>
            TimeStartGameAttack != DateTime.MinValue
            && TimeStartGameAttack.AddMinutes(1) < DateTime.UtcNow;

        /// <summary>
        /// Начало нападения, снятия с паузы
        /// </summary>
        public DateTime TimeStartGameAttack = DateTime.MinValue;

        /// <summary>
        /// Когда было обнаружено условие победы (все пешки одной из сторон недееспособны).
        /// Равно DateTime.MaxValue, если в текущий момент такого нет.
        /// </summary>
        private DateTime CheckVictoryTime = DateTime.MaxValue;

        /// <summary>
        /// Если не null, значит атака завершена. True если победил атакующий
        /// </summary>
        public bool? ConfirmedVictoryAttacker { get; set; }
        /// <summary>
        /// Произошла ужасная ошибка, и всё нужно отменить. Когда истина, перестаём что-либо делать и ждем от сервера команды на перезапуск
        /// </summary>
        public bool TerribleFatalError { get; set; }

        /// <summary>
        /// Произошла ошибка на сервере. Мы ждем 60 секунд пока сервер не пришлет по системе mail команду. 
        /// Если этого не происходит, то вызывает отключение по неизвестной ошибке. 
        /// Также ожидание нужно, чтобы показать серверу, что ошибка произошла не из-за того что атакуемый отключился
        /// </summary>
        public bool WaitOrErrorExit { get; set; }
        private DateTime WaitOrErrorExitStart { get; set; }

        /// <summary>
        /// Пешки с текущей карты
        /// </summary>
        private Dictionary<int, Pawn> AllPawns = new Dictionary<int, Pawn>();

        public static GameAttackHost Get
        {
            get { return SessionClientController.Data.AttackUsModule; }
        }

        private object TimerObj;
        private bool InTimer;

        public static bool AttackMessage()
        {
            if (SessionClientController.Data.AttackModule == null
                && SessionClientController.Data.AttackUsModule == null)
            {
                SessionClientController.Data.AttackUsModule = new GameAttackHost();
                SessionClientController.Data.BackgroundSaveGameOff = true;
                return true;
            }
            return false;
        }

        private void PauseMessage()
        {
            GameUtils.ShowDialodOKCancel(
                TestMode
                    ? "OCity_GameAttack_Host_Test_Attack".Translate(AttackerLogin)
                    : "OCity_GameAttack_Host_Settlement_Attacking".Translate(AttackerLogin)
                , TestMode
                    ? "OCity_GameAttack_Host_GameSpeed_Lock_Dialog".Translate() + Environment.NewLine
                        + "OCity_GameAttack_Host_Cancel_Action".Translate() + Environment.NewLine
                        + "OCity_GameAttack_Host_Surrender".Translate()
                    : "OCity_GameAttack_Host_GameSpeed_Lock_Dialog".Translate() + Environment.NewLine
                        + "OCity_GameAttack_Host_GameSpeed_Lock_Dialog2".Translate() + Environment.NewLine
                        + "OCity_GameAttack_Host_Surrender".Translate()
                , () => { }
                , null
            );
        }

        /// <summary>
        /// Начинаем процесс, первые запросы информации
        /// </summary>
        /// <param name="connect"></param>
        public void Start(SessionClient connect)
        {
            Find.TickManager.Pause();
            SessionClientController.Data.DontChactTimerFail = true;

            Loger.Log("Client GameAttackHost Start 1");
            //Loger.PathLog = "E:\\RWT";
            var tolient = connect.AttackOnlineHost(new AttackHostToSrv()
            {
                State = 2
            });
            if (!string.IsNullOrEmpty(connect.ErrorMessage))
            {
                ErrorBreak(connect.ErrorMessage);
                return;
            }

            AttackerLogin = tolient.StartInitiatorPlayer;
            InitiatorPlaceServerId = tolient.InitiatorPlaceServerId;
            HostPlaceServerId = tolient.HostPlaceServerId;
            TestMode = tolient.TestMode;

            Loger.Log("Client GameAttackHost Start 2 " + tolient.HostPlaceServerId + " TestMode=" + TestMode);

            Find.TickManager.Pause();

            LongEventHandler.QueueLongEvent(delegate
            {
                try
                {
                    SessionClientController.SaveGameNowInEvent(false);

                    Find.TickManager.Pause();
                    GameAttackTrigger_Patch.ForceSpeed = 0f;
                    PauseMessage();

                    //Устанавливаем паузу сейчас первый раз,
                    //далее она будет обновлена на 5 минут перед началом создания карты у атакующего игрока (см AttackServer.RequestInitiator())
                    //и последний раз после создания карты на 1 минуту, чтобы дать оглядеться атакующему (ищи SetPauseOnTimeToHost в GameAttacker)
                    CurrentPauseToTime = DateTime.UtcNow.AddMinutes(5);
                    Loger.Log("HostAttackUpdate Set 1 CurrentPauseToTime=" + CurrentPauseToTime.ToGoodUtcString());

                    Loger.Log("Client GameAttackHost Start 3");
                    var hostPlace = UpdateWorldController.GetWOByServerId(HostPlaceServerId) as MapParent;
                    GameMap = hostPlace.Map;

                    var toSrvMap = new AttackHostToSrv()
                    {
                        State = 4,
                        TerrainDefNameCell = new List<IntVec3S>(),
                        TerrainDefName = new List<string>(),
                        Thing = new List<ThingTrade>(),
                        ThingCell = new List<IntVec3S>()
                    };

                    toSrvMap.MapSize = new IntVec3S(GameMap.Size);

                    CellRect cellRect = CellRect.WholeMap(GameMap);
                    cellRect.ClipInsideMap(GameMap);

                    //почва
                    Loger.Log("Client GameAttackHost Start 4");
                    foreach (IntVec3 current in cellRect)
                    {
                        var terr = GameMap.terrainGrid.TerrainAt(current);
                        toSrvMap.TerrainDefNameCell.Add(new IntVec3S(current));
                        toSrvMap.TerrainDefName.Add(terr.defName);
                    }

                    //скалы, преграды и строения без пешек и без растений не деревьев, вывести кол-во
                    SendedActual = new Dictionary<int, Thing>();
                    Loger.Log("Client GameAttackHost Start 5");
                    foreach (IntVec3 current in cellRect)
                    {
                        foreach (Thing thc in GameMap.thingGrid.ThingsAt(current).ToList<Thing>())
                        {
                            if (thc is Pawn) continue;

                            if (thc is Fire) FireList.Add(thc as Fire);

                            //из растений оставляем только деревья
                            var isPlant = thc.def.category == ThingCategory.Plant && !thc.def.plant.IsTree;
                            if (PlantNotSend)
                            {
                                if (isPlant) continue;
                            }

                            if (thc.Position != current) continue;

                            var tt = ThingTrade.CreateTrade(thc, thc.stackCount, false);

                            toSrvMap.ThingCell.Add(new IntVec3S(current));
                            toSrvMap.Thing.Add(tt);

                            if (!isPlant) SendedActual[thc.thingIDNumber] = thc;

                        }
                    }

                    Loger.Log("Client GameAttackHost Start 6");
                    SessionClientController.Command((connect0) =>
                    {
                        Loger.Log("Client GameAttackHost Start 7");
                        connect0.AttackOnlineHost(toSrvMap);
                        if (!string.IsNullOrEmpty(connect0.ErrorMessage))
                        {
                            ErrorBreak(connect0.ErrorMessage);
                            return;
                        }

                        //Ждем и получаем пешки атакующего и добавляем их
                        Loger.Log("Client GameAttackHost Start 8");
                        List<ThingEntry> pawnsA = null;
                        var s1Time = DateTime.UtcNow;
                        while (true)
                        {
                            var toClient5 = connect0.AttackOnlineHost(new AttackHostToSrv()
                            {
                                State = 5
                            });
                            if (!string.IsNullOrEmpty(connect0.ErrorMessage))
                            {
                                ErrorBreak(connect0.ErrorMessage);
                                return;
                            }
                            pawnsA = toClient5.Pawns;
                            if (pawnsA != null && pawnsA.Count > 0) break;
                            if ((DateTime.UtcNow - s1Time).TotalSeconds > 20)
                            {
                                ErrorBreak("Timeout");
                                return;
                            }
                        }
                        if (!string.IsNullOrEmpty(connect0.ErrorMessage))
                        {
                            ErrorBreak(connect0.ErrorMessage);
                            return;
                        }

                        Loger.Log("Client GameAttackHost Start 9");
                        //Потом добавляем список к отправке
                        SendedPawnsId = new HashSet<int>();
                        SendedState = new Dictionary<int, int>();
                        SendedXP = new Dictionary<int, int>();
                        ToUpdateStateId = new List<int>();
                        ToUpdateState = new List<Thing>();
                        ToSendDeleteId = new HashSet<int>();
                        ToSendAddId = new HashSet<int>();
                        ToSendThingAdd = new HashSet<Thing>();
                        FireList = new HashSet<Thing>();
                        ToSendDelayedFillPawnsId = new HashSet<int>();
                        AttackingPawns = new HashSet<Pawn>();
                        AttackingPawnDic = new Dictionary<int, int>();
                        AttackingPawnsLastPos = new Dictionary<Pawn, IntVec3>();
                        AttackingPawnJobDic = new Dictionary<int, AttackPawnCommand>();
                        AttackUpdateTick = 0;

                        //защита от сбоев: проверяем если ли такие пешки уже на карте по имени thing.LabelCapNoCount
                        Pawn[] mapPawns;
                        try
                        {
                            /*try
                            {*/
                                mapPawns = new Pawn[GameMap.mapPawns.AllPawnsSpawned.Count];
                                GameMap.mapPawns.AllPawnsSpawned.CopyTo(mapPawns);
                                /*
                            }
                            catch
                            {
                                Thread.Sleep(20);
                                mapPawns = GameMap.mapPawns.AllPawns.ToList();
                            }*/
                            for (int i = 0; i < pawnsA.Count; i++)
                            {
                                var label = pawnsA[i].Name;
                                var exist = mapPawns.FirstOrDefault(p => p != null && p.LabelCapNoCount == label);
                                if (exist != null)
                                {
                                    if (exist.IsColonist)
                                    {
                                        Loger.Log("Client GameAttackHost Start Name exists isColonist! Not add: " + label);
                                        pawnsA.RemoveAt(i--);
                                        continue;
                                    }
                                    else
                                    {
                                        Loger.Log("Client GameAttackHost Start Name exists! Drop pawn: " + label);
                                        try
                                        {
                                            exist.Destroy();
                                        }
                                        catch
                                        {
                                            Thread.Sleep(5);
                                            try
                                            {
                                                if (exist.Spawned) exist.Destroy();
                                            }
                                            catch
                                            { }
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        { }
                        
                        //создаем пешки
                        UIEventNewJobDisable = true;
                        var cellPawns = GameUtils.SpawnCaravanPirate(GameMap, pawnsA,
                            (th, te) =>
                            {
                                var p = th as Pawn;
                                if (p == null) return;

                                AttackingPawns.Add(p);
                                AttackingPawnDic.Add(p.thingIDNumber, te.OriginalID);

                                //задаем команду стоять и не двигаться (но стрелять если кто в радиусе)
                                Loger.Log("Client GameAttackHost Start 9 StartJob Wait_Combat ");
                                p.playerSettings.hostilityResponse = HostilityResponseMode.Ignore;
                                p.jobs.StartJob(new Job(JobDefOf.Wait_Combat)
                                {
                                    playerForced = true,
                                    expiryInterval = int.MaxValue,
                                    checkOverrideOnExpire = false,
                                }
                                    , JobCondition.InterruptForced);
                                /*
                                if (p.Label == "Douglas, Клерк")
                                {
                                    //todo для теста не забыть удалить!
                                    var pp = p;
                                    var th = new Thread(() =>
                                    {
                                        Thread.Sleep(5000);
                                        while (true)
                                        {
                                            Thread.Sleep(1000);
                                            try
                                            {
                                                var jj = pp.jobs.curJob;
                                                Loger.Log("Host ThreadTestJob " + pp.Label + " job=" + (jj == null ? "null" : jj.def.defName.ToString()));
                                            }
                                            catch
                                            { }
                                        }
                                    });
                                    th.IsBackground = true;
                                    th.Start();
                                }
                                */
                            });
                        UIEventNewJobDisable = false;

                        Loger.Log("Client GameAttackHost Start 10");

                        CameraJumper.TryJump(cellPawns, GameMap);

                        TimerObj = SessionClientController.Timers.Add(AttackUpdateDelay, AttackUpdate);

                        SessionClientController.Data.DontChactTimerFail = false;

                        //включаем обработку событий урона и уничтожения объектов
                        GameAttackTrigger_Patch.ActiveAttackHost.Add(GameMap, this);

                        Loger.Log("Client GameAttackHost Start 11");
                    });
                }
                catch (Exception ext)
                {
                    Loger.Log("GameAttackHost Start() Exception " + ext.ToString());
                }
            }, "...", false, null);
        }

        private void ErrorBreak(string msg)
        {
            Loger.Log("Client GameAttackHost error" + msg);

            Clear();

            SessionClientController.Disconnected("OCity_GameAttacker_Dialog_ErrorMessage".Translate());
            /*
            Find.WindowStack.Add(new Dialog_Message("OCity_GameAttacker_Dialog_ErrorMessage".Translate(), msg, null, () => { }));
            */
        }

        private void AttackUpdate()
        {
            bool inTimerEvent = false;
            try
            {
                if (WaitOrErrorExit)
                {
                    Find.TickManager.Pause();
                    if (WaitOrErrorExitStart == DateTime.MinValue)
                    {
                        WaitOrErrorExitStart = DateTime.UtcNow;
                    }
                    else
                    {
                        if ((DateTime.UtcNow - WaitOrErrorExitStart).TotalSeconds > 60)
                        {
                            ErrorBreak("Unknown error");
                        }
                    }
                    return;
                }
                if (TerribleFatalError)
                {
                    Find.TickManager.Pause();
                    return;
                }

                if (InTimer) return;
                InTimer = true;

                double AU1ms = 0;
                DateTime AUSTime = DateTime.UtcNow;

                AttackUpdateTick++;
                //                Loger.Log("Client HostAttackUpdate #" + AttackUpdateTick.ToString());

                if (IsPause)
                {
                    if (!Find.TickManager.Paused)
                    {
                        Find.TickManager.Pause();
                        GameAttackTrigger_Patch.ForceSpeed = 0f;
                        PauseMessage();
                    }
                }
                else
                {
                    if (Find.TickManager.Paused || Find.TickManager.CurTimeSpeed != TimeSpeed.Normal)
                    {
                        Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                        GameAttackTrigger_Patch.ForceSpeed = TickTimeSpeed;
                        if (TimeStartGameAttack == DateTime.MinValue) TimeStartGameAttack = DateTime.UtcNow;
                    }
                }

                SessionClientController.Command((connect) =>
                {
                    try
                    {
                        HashSet<int> mapPawnsId;
                        AttackHostFromSrv toClient;
                        lock (ToSendListsSync)
                        {
                            var mapPawnsA = new Pawn[GameMap.mapPawns.AllPawnsSpawned.Count];
                            GameMap.mapPawns.AllPawnsSpawned.CopyTo(mapPawnsA);
                            AllPawns = mapPawnsA.ToDictionary(p => p.thingIDNumber);

                            //проверяем условия победы (в паузе не проверяются, как проверка чтобы не проверялось до полной загрузки)
                            if (!IsPause && ConfirmedVictoryAttacker == null)
                            {
                                var vic = CheckAttackerVictory();
                                if (vic == null)
                                {
                                    if (CheckVictoryTime != DateTime.MaxValue) CheckVictoryTime = DateTime.MaxValue;
                                }
                                else
                                {
                                    Loger.Log($"Client CheckAttackerVictory {vic.Value}");
                                    if (CheckVictoryTime == DateTime.MaxValue)
                                    {
                                        CheckVictoryTime = DateTime.UtcNow;
                                    }
                                    else if ((DateTime.UtcNow - CheckVictoryTime).TotalSeconds > CheckVictoryDelay)
                                    {
                                        ConfirmedVictoryAttacker = vic;
                                    }
                                }
                            }
                            if (ConfirmedVictoryAttacker != null) Finish(ConfirmedVictoryAttacker.Value);

                            //                      Loger.Log("Client HostAttackUpdate 1");
                            //обновляем списки
                            var mapPawnsIdExt = new HashSet<int>(AllPawns.Keys);
                            mapPawnsIdExt.SymmetricExceptWith(SendedPawnsId); //новые пешки + те что на сервере, но их уже нет на карте
                            if (mapPawnsIdExt.Count > 0)
                            {
                                var toSendAddId = new HashSet<int>(mapPawnsIdExt);
                                toSendAddId.ExceptWith(SendedPawnsId); //только новые пешки
                                if (toSendAddId.Count > 0)
                                {
                                    toSendAddId.ExceptWith(ToSendAddId); //исключаем те, которые уже есть в списке
                                    ToSendAddId.AddRange(toSendAddId);
                                }

                                var toSendDeleteId = new HashSet<int>(mapPawnsIdExt);
                                toSendDeleteId.IntersectWith(SendedPawnsId); //только те, что на сервере но их уже нет на карте
                                if (toSendDeleteId.Count > 0)
                                {
                                    toSendDeleteId.ExceptWith(ToSendDeleteId); //исключаем те, которые уже есть в списке
                                    ToSendDeleteId.AddRange(toSendDeleteId);
                                }
                            }
                            //отложенная отправка полных данных по пешкам
                            if ((DateTime.UtcNow - SendDelayedFillPawnsLastTime).TotalSeconds >= SendDelayedFillPawnsSeconds)
                            {
                                //проверяется XP пешек и записывалось в новый словарь. Если отличается добавлять в ToSendDelayedFillPawnsId
                                foreach (var mpp in AllPawns)
                                {
                                    var mp = mpp.Value;
                                    var mpID = mpp.Key;
                                    if (ToSendDelayedFillPawnsId.Contains(mpID)) continue;

                                    var mpXPnew = (int)(mp.health.summaryHealth.SummaryHealthPercent * 10000f);
                                    int mpXP;
                                    if (!SendedXP.TryGetValue(mpID, out mpXP))
                                    {
                                        //при первом добавлении не обновляем, только при изменении
                                        SendedXP[mpID] = mpXPnew;
                                    }
                                    else if (mpXP != mpXPnew)
                                    {
                                        SendedXP[mpID] = mpXPnew;
                                        ToSendDelayedFillPawnsId.Add(mpID);
                                    }
                                }

                                //отправляем обновляться огонь
                                FireList.ExceptWith(ToUpdateState);
                                ToUpdateState.AddRange(FireList);

                                //отправляем те, которые к отложенной отправке
                                SendDelayedFillPawnsLastTime = DateTime.UtcNow;
                                ToSendDelayedFillPawnsId.IntersectWith(AllPawns.Keys); //только те, которые на карте
                                ToSendDelayedFillPawnsId.ExceptWith(ToSendAddId); //исключаем те, которые уже есть в списке
                                ToSendAddId.AddRange(ToSendDelayedFillPawnsId);

                                Loger.Log("HostAttackUpdate ToSendDelayedFillPawnsId Send Count=" + ToSendDelayedFillPawnsId.Count.ToString());

                                ToSendDelayedFillPawnsId.Clear();
                            }
                            //обновляем поколения пешек учавствующих в Job и отправляем нужные
                            foreach (var mp in ThingPrepareChange0)
                            {
                                if (!(mp is Pawn)) continue; //отправка вещей ниже
                                var mpID = mp.thingIDNumber;
                                if (ToSendAddId.Contains(mpID)) continue;

                                ToSendAddId.Add(mpID);
                            }

                            //посылаем пакеты с данными
                            //                      Loger.Log("Client HostAttackUpdate 2");

                            var newPawns = new List<ThingEntry>();
                            var newPawnsId = new List<int>();
                            int cnt = ToSendAddId.Count < 3 || ToSendAddId.Count > 6 || AttackUpdateTick == 0
                                ? ToSendAddId.Count
                                : 3;
                            int i = 0;
                            int[] added = new int[cnt];
                            foreach (int id in ToSendAddId)
                            {
                                if (i >= cnt) break;
                                added[i++] = id;

                                Pawn thing;
                                if (!AllPawns.TryGetValue(id, out thing)) continue;
                                var tt = ThingEntry.CreateEntry(thing, 1);
                                //передаем те, что были исходные у атакуемого (хотя там используется только как признак TransportID != 0 - значит те кто атакует)
                                if (AttackingPawnDic.ContainsKey(tt.OriginalID)) tt.TransportID = AttackingPawnDic[tt.OriginalID];
                                newPawns.Add(tt);
                                newPawnsId.Add(id);
                                SendedActual[thing.thingIDNumber] = thing;
                                //todo убрать
                                if (thing.thingIDNumber == 22121) Loger.Log($"HostAttackUpdate SendedActual Test2Add! {thing.Label} {thing.thingIDNumber}. {thing.Spawned}");
                            }
                            for (i = 0; i < cnt; i++)
                            {
                                ToSendAddId.Remove(added[i]);
                                SendedPawnsId.Add(added[i]);
                            }

                            foreach (int id in ToSendDeleteId)
                            {
                                SendedPawnsId.Remove(id);
                            }

                            //новые вещи
                            var newThings = ToSendThingAdd
                                .Where(thing => !ToSendDeleteId.Contains(thing.thingIDNumber) && !(thing is Corpse))
                                .Select(thing => ThingTrade.CreateTrade(thing, thing.stackCount, false))
                                .ToList();
                            //трупы
                            var newCorpses = ToSendThingAdd
                                .Where(thing => !ToSendDeleteId.Contains(thing.thingIDNumber) && thing is Corpse)
                                .Select(thing => new AttackCorpse()
                                {
                                    CorpseId = thing.thingIDNumber,
                                    PawnId = (thing as Corpse).InnerPawn.thingIDNumber,
                                    CorpseWithPawn = ThingEntry.CreateEntry((thing as Corpse).InnerPawn, 1)
                                })
                                .ToList();
                            foreach (var item in ToSendThingAdd)
                            {
                                SendedActual[item.thingIDNumber] = item;
                                //todo убрать
                                if (item.thingIDNumber == 22121) Loger.Log($"HostAttackUpdate SendedActual Test3Add! {item.Label} {item.thingIDNumber}. {item.Spawned}");
                            }

                            //передаем изменение местоположеня пешек (по ID хоста)
                            var toSendState = new List<AttackThingState>();
                            foreach (var mpp in AllPawns)
                            {
                                var mp = mpp.Value;
                                var mpID = mpp.Key;
                                if (ToUpdateStateId.Contains(mpID)) continue;

                                var mpHash = AttackThingState.GetHash(mp); //тут у пешек используется только позиция
                                int mpHS;
                                if (!SendedState.TryGetValue(mpID, out mpHS) || mpHS != mpHash)
                                {
                                    SendedState[mpID] = mpHash;
                                    toSendState.Add(new AttackThingState(mp));
                                }
                            }
                            //передаем обычные вещи, по которым прошел урон или другие события
                            for (int imp = 0; imp < ToUpdateState.Count; imp++)
                            {
                                var mp = ToUpdateState[imp];
                                var mpID = mp.thingIDNumber;
                                if (ToSendDeleteId.Contains(mpID)) continue;

                                if (mp is Pawn)
                                {
                                    var mpHash = AttackThingState.GetHash(mp); //тут у пешек используется только позиция
                                    SendedState[mpID] = mpHash; //заносим только для проверки выше
                                }
                                toSendState.Add(new AttackThingState(mp));
                            }

                            //обновляем поколения вещей учавствующих в Job и отправляем нужные
                            foreach (var mp in ThingPrepareChange0)
                            {
                                if (mp is Pawn) continue; //отправка пешек выше
                                var mpID = mp.thingIDNumber;
                                if (ToSendDeleteId.Contains(mpID)) continue;

                                toSendState.Add(new AttackThingState(mp));
                            }
                            if (ThingPrepareChange0.Count > 0) Loger.Log("HostAttackUpdate UpdateCommand FromJob Count=" + ThingPrepareChange0.Count.ToString());
                            ThingPrepareChange0 = ThingPrepareChange1;
                            ThingPrepareChange1 = new HashSet<Thing>();
                            
                            //Loger.Log("Client HostAttackUpdate 3");

                            foreach (var item in ToSendDeleteId)
                            {
                                SendedActual.Remove(item);
                            }

                            DateTime AUSTime1 = DateTime.UtcNow;

                            toClient = connect.AttackOnlineHost(new AttackHostToSrv()
                            {
                                State = 10,
                                NewPawns = newPawns,
                                NewPawnsId = newPawnsId,
                                NewThings = newThings,
                                NewThingsId = newThings.Select(th => th.OriginalID).ToList(),
                                NewCorpses = newCorpses,
                                Delete = ToSendDeleteId.ToList(),
                                UpdateState = toSendState,
                                VictoryAttacker = ConfirmedVictoryAttacker,
                            });

                            AU1ms = (DateTime.UtcNow - AUSTime1).TotalMilliseconds;

                            ToSendThingAdd.Clear();
                            ToSendDeleteId.Clear();
                            ToUpdateStateId.Clear();
                            ToUpdateState.Clear();
                        }

                        if (toClient == null || toClient.State == 0)
                        {
                            WaitOrErrorExit = true;
                            return;
                        }

                        //принимаем настройки паузы
                        if (toClient.SetPauseOnTime != DateTime.MinValue)
                        {
                            //текущее время сервера: var ntcNowServer = DateTime.UtcNow + SessionClientController.Data.ServetTimeDelta;
                            //а тут переводим в местное UTC время
                            CurrentPauseToTime = toClient.SetPauseOnTime - SessionClientController.Data.ServetTimeDelta;
                            Loger.Log("HostAttackUpdate Set 2 CurrentPauseToTime=" + CurrentPauseToTime.ToGoodUtcString());
                        }


                        if (toClient.VictoryHost)
                        {
                            ConfirmedVictoryAttacker = false;
                        }

                        if (toClient.TerribleFatalError)
                        {
                            Loger.Log("HostAttackUpdate TerribleFatalError");
                            //устанавливаем статус ожидания завершения, которое придет в виде команды от сервера
                            TerribleFatalError = true;
                        }

                        //принимаем обновление команд атакующих
                        if (toClient.UpdateCommand.Count > 0)
                        {
                            Loger.Log("HostAttackUpdate UpdateCommand Count=" + toClient.UpdateCommand.Count.ToString());
                            UIEventNewJobDisable = true;
                            for (int ii = 0; ii < toClient.UpdateCommand.Count; ii++)
                            {
                                var comm = toClient.UpdateCommand[ii];

                                Pawn pawn;
                                if (!AllPawns.TryGetValue(comm.HostPawnID, out pawn))
                                {
                                    Loger.Log("HostAttackUpdate UpdateCommand pawn == null " + comm.HostPawnID.ToString());
                                    continue;
                                }

                                AttackingPawnJobDic[comm.HostPawnID] = comm;
                                ApplyAttackingPawnJob(pawn);

                            }
                            UIEventNewJobDisable = false;
                        }

                        //принимаем объекты с нашим ID которые атакующий просит повторно отправть
                        if (toClient.NeedNewThingIDs.Count > 0)
                        {
                            Loger.Log("HostAttackUpdate NeedNewThingIDs Count=" + toClient.NeedNewThingIDs.Count.ToString());
                            lock (ToSendListsSync)
                            {
                                //объединяем
                                for (int i = 0; i < toClient.NeedNewThingIDs.Count; i++)
                                {
                                    var id = toClient.NeedNewThingIDs[i];

                                    Pawn pawn;
                                    if (AllPawns.TryGetValue(id, out pawn))
                                    {
                                        ToSendAddId.Add(id);
                                        Loger.Log("HostAttackUpdate NeedNewThingIDs AddPawn " + pawn.Label.ToString());
                                        continue;
                                    }

                                    if (SendedActual.TryGetValue(id, out Thing thing))
                                    {
                                        if (thing is Pawn) continue;
                                        ToSendThingAdd.Add(thing);
                                        Loger.Log("HostAttackUpdate NeedNewThingIDs AddThing " + pawn.Label.ToString());
                                        continue;
                                    }
                                }
                            }
                        }

                        //                      Loger.Log("Client HostAttackUpdate 4");
                    }
                    catch (Exception ext)
                    {
                        InTimer = false;
                        Loger.Log("HostAttackUpdate Event Exception " + ext.ToString());
                    }
                    finally
                    {
                        InTimer = false;
                    }
                });

                Loger.Log($"HostAttackUpdate TimeCPU={(DateTime.UtcNow - AUSTime).TotalMilliseconds - AU1ms} TimeNet={AU1ms}");

            }
            catch (Exception ext)
            {
                Loger.Log("HostAttackUpdate Exception " + ext.ToString());
            }
            if (!inTimerEvent) InTimer = false;
        }

        private class APJBT
        {
            public long Tick;
            public AttackPawnCommand Comm;
        }
        /// <summary>
        /// Id пешки и тик когда ей выдавалась задача
        /// </summary>
        private Dictionary<int, APJBT> ApplyPawnJobByTick = new Dictionary<int, APJBT>();

        private void SetPawnJob(Pawn pawn, JobDef def, LocalTargetInfo target, int count = -1)
        {
            pawn.jobs.StartJob(new Job(def, target)
            {
                playerForced = true,
                expiryInterval = int.MaxValue,
                checkOverrideOnExpire = false,
                count = count,
            }
            , JobCondition.InterruptForced);
        }
        private void ApplyAttackingPawnJob(Pawn pawn)
        {
            Loger.Log("HostAttackUpdate ApplyAttackingPawnJob " + pawn.Label.ToString() + " //"+ AttackingPawnDic.Count.ToString());
            var pId = pawn.thingIDNumber;
            AttackPawnCommand comm = null;
            bool stopJob = !AttackingPawnDic.ContainsKey(pId)
                || !AttackingPawnJobDic.TryGetValue(pId, out comm);
            try
            {
                var tick = (long)Find.TickManager.TicksGame;
                APJBT check;
                if (ApplyPawnJobByTick.TryGetValue(pId, out check))
                {
                    //Если в один тик мы второй раз пытаемся установить задачу, значит система её сразу отменяет
                    //В этом случае сбрасываем задачу, считаем что цель достигнута или недоступна
                    if (check.Comm == comm && check.Tick == tick)
                    {
                        Loger.Log("HostAttackUpdate ApplyAttackingPawnJob spam");
                        //if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("HostAttackUpdate ApplyAttackingPawnJob stopJob(repeat) " + comm.TargetPos.Get().ToString());
                        stopJob = true;
                    }
                }

                //UIEventNewJobDisable = true;
                //находим target
                Thing target = null;
                if (!stopJob && comm.TargetID != 0)
                {
                    var mapPawns = AllPawns.Values; //GameMap.mapPawns.AllPawnsSpawned.ToList();
                    target = mapPawns.FirstOrDefault(p => p.thingIDNumber == comm.TargetID);
                    //if (target == null) target = GameMap.listerThings.AllThings.FirstOrDefault(p => p.thingIDNumber == comm.TargetID);
                    if (target == null && (!SendedActual.TryGetValue(comm.TargetID, out target) || target == null))
                    {
                        Loger.Log("HostAttackUpdate ApplyAttackingPawnJob not in SendedActual by " + comm.TargetID + " // " + SendedActual.Count);

                        /*
                        //TODO Убрать этот тестовый фрагмент:
                        foreach(var item in SendedActual)
                        {
                            if (item.Key != item.Value.thingIDNumber)
                            {
                                Loger.Log($"HostAttackUpdate ApplyAttackingPawnJob Accident! {pawn.Label} {item.Key}->{item.Value.thingIDNumber}. {item.Value.Spawned}");
                            }
                        }
                        */

                        //if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("HostAttackUpdate ApplyAttackingPawnJob TargetThing == null " + comm.HostPawnID.ToString());
                        stopJob = true;
                    }
                }
                if (!stopJob && !string.IsNullOrEmpty(comm.TargetDefName))
                {
                    /*
                    target = pawn.inventory.innerContainer.FirstOrDefault(p => p.def.defName == comm.TargetDefName)
                        ?? pawn.carryTracker.innerContainer.FirstOrDefault(p => p.def.defName == comm.TargetDefName)
                        ?? pawn.equipment.AllEquipmentListForReading.FirstOrDefault(p => p.def.defName == comm.TargetDefName)
                        ?? pawn.apparel.WornApparel.FirstOrDefault(p => p.def.defName == comm.TargetDefName);
                    */
                    for (int i = 0; i < pawn.inventory.innerContainer.Count; i++)
                    {
                        var item = pawn.inventory.innerContainer[i];
                        if (item.def.defName == comm.TargetDefName)
                        {
                            target = item;
                            break;
                        }
                    }
                    if (target == null)
                    {
                        for (int i = 0; i < pawn.carryTracker.innerContainer.Count; i++)
                        {
                            var item = pawn.carryTracker.innerContainer[i];
                            if (item.def.defName == comm.TargetDefName)
                            {
                                target = item;
                                break;
                            }
                        }
                    }
                    if (target == null)
                    {
                        for (int i = 0; i < pawn.equipment.AllEquipmentListForReading.Count; i++)
                        {
                            var item = pawn.equipment.AllEquipmentListForReading[i];
                            if (item.def.defName == comm.TargetDefName)
                            {
                                target = item;
                                break;
                            }
                        }
                    }
                    if (target == null)
                    {
                        for (int i = 0; i < pawn.apparel.WornApparel.Count; i++)
                        {
                            var item = pawn.apparel.WornApparel[i];
                            if (item.def.defName == comm.TargetDefName)
                            {
                                target = item;
                                break;
                            }
                        }
                    }
                    if (target == null)
                    {
                        Loger.Log("HostAttackUpdate ApplyAttackingPawnJob not in pawn.inventory by " + comm.TargetDefName);
                        //if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("HostAttackUpdate ApplyAttackingPawnJob TargetThing == null " + comm.HostPawnID.ToString());
                        stopJob = true;
                    }
                }

                if (!stopJob)
                {
                    Loger.Log("HostAttackUpdate ApplyAttackingPawnJob Command:" + comm.Command.ToString() + " target:" 
                        + (target == null ? "null" : target.Label) 
                        + "(" + comm.TargetID.ToString() + " " + comm.TargetDefName + " " + (comm.TargetPos == null ? "" : comm.TargetPos.ToString()) + ")"
                        + " pawn:" + pawn.Label);

                    if (comm.Command == AttackPawnCommand.PawnCommand.Goto)
                    {
                        SetPawnJob(pawn, JobDefOf.Goto, comm.TargetPos.Get()); //ок
                    }
                    else if (comm.Command == AttackPawnCommand.PawnCommand.Attack)
                    {
                        if (target == null) stopJob = true;
                        else SetPawnJob(pawn, JobDefOf.AttackStatic, target); //ок
                    }
                    else if (comm.Command == AttackPawnCommand.PawnCommand.AttackMelee)
                    {
                        if (target == null) stopJob = true;
                        else SetPawnJob(pawn, JobDefOf.AttackMelee, target); //ок
                    }
                    else if (comm.Command == AttackPawnCommand.PawnCommand.Equip)
                    {
                        if (target == null) stopJob = true;
                        else SetPawnJob(pawn, JobDefOf.Equip, target); //ок
                    }
                    else if (comm.Command == AttackPawnCommand.PawnCommand.TakeInventory)
                    {
                        if (target == null) stopJob = true;
                        else SetPawnJob(pawn, JobDefOf.TakeInventory, target, target.stackCount); //ок
                    }
                    else if (comm.Command == AttackPawnCommand.PawnCommand.Wear)
                    {
                        if (target == null) stopJob = true;
                        else SetPawnJob(pawn, JobDefOf.Wear, target); //ок
                    }
                    else if (comm.Command == AttackPawnCommand.PawnCommand.DropEquipment)
                    {
                        if (target == null) stopJob = true;
                        else SetPawnJob(pawn, JobDefOf.DropEquipment, target); //ок
                    }
                    else if (comm.Command == AttackPawnCommand.PawnCommand.RemoveApparel)
                    {
                        if (target == null) stopJob = true;
                        else SetPawnJob(pawn, JobDefOf.RemoveApparel, target); //ок
                    }
                    else if (comm.Command == AttackPawnCommand.PawnCommand.Ingest)
                    {
                        if (target == null) stopJob = true;
                        else SetPawnJob(pawn, JobDefOf.Ingest, target, Math.Min(target.stackCount, target.def.ingestible.maxNumToIngestAtOnce)); //глючит см ошибку. Остальное не проверено
                    }
                    else if (comm.Command == AttackPawnCommand.PawnCommand.Strip)
                    {
                        if (target == null) stopJob = true;
                        else SetPawnJob(pawn, JobDefOf.Strip, target); //ок
                    }
                    else if (comm.Command == AttackPawnCommand.PawnCommand.TendPatient)
                    {
                        if (target == null) target = pawn;
                        SetPawnJob(pawn, JobDefOf.TendPatient, target, 1); //не проверено
                    }
                    else if (comm.Command == AttackPawnCommand.PawnCommand.OC_InventoryDrop)
                    {
                        stopJob = true;
                        //Это не job, а немедленная команда бросить из инвентаря
                        Loger.Log("HostAttackUpdate ApplyAttackingPawnJob InventoryDrop: " + target.Label);
                        GenDrop.TryDropSpawn(target, pawn.Position, GameMap, ThingPlaceMode.Near, out var nt);
                    }
                    else stopJob = true;
                }
                if (stopJob)
                {
                    Loger.Log("HostAttackUpdate ApplyAttackingPawnJob Wait_Combat pawn:" + pawn.Label);
                    if (AttackingPawnJobDic.ContainsKey(pId))
                    {
                        //if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("HostAttackUpdate ApplyAttackingPawnJob Remove Job " + comm.TargetPos.Get().ToString());
                        AttackingPawnJobDic.Remove(pId);
                    }
                    pawn.jobs.StartJob(new Job(JobDefOf.Wait_Combat)
                    {
                        playerForced = true,
                        expiryInterval = int.MaxValue,
                        checkOverrideOnExpire = false,
                    }
                    , JobCondition.InterruptForced);
                }
                else
                {
                    ApplyPawnJobByTick[pId] = new APJBT() { Comm = comm, Tick = tick };
                }
            }
            catch (Exception exp)
            {
                //if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("HostAttackUpdate ApplyAttackingPawnJob " + exp.ToString());
                if (AttackingPawnJobDic.ContainsKey(pId)) AttackingPawnJobDic.Remove(pId);
                Loger.Log("HostAttackUpdate ApplyAttackingPawnJob Exception " + exp.ToString());
            }
            //UIEventNewJobDisable = false;
        }

        public bool UIEventNewJobDisable = false;
        public void UIEventNewJob(Pawn pawn, Job job) //если job == null значит команда стоять и не двигаться Wait_Combat
        {
            try
            {
                var pawnId = pawn.thingIDNumber;

                //помимо главной обработки события изменения задания помечаем цель задачи для обновления её состояния позже, когда задача пешки завершиться
                //это всё для того, чтобы поймать, что кол-во какой-то вещи изменилось
                //добавляем тут намерение пешки взять вещь в новый словарь если Job не null,
                //а если null (когда джоб завершился) помещаем в предварительный массив к отправке
                //в момент отправки из предварительного массива данные переносятся в массив к отправке, а те что там были отправляются атакующиму с их текущим количеством стака
                Thing jobThing;
                if (ThingPrepareChange2.TryGetValue(pawnId, out jobThing) && !ThingPrepareChange1.Contains(jobThing))
                {
                    Loger.Log("HostAttackUpdate UIEventNewJob AddFromJob " + jobThing.Label);
                    ThingPrepareChange1.Add(jobThing);
                    ThingPrepareChange2.Remove(pawnId);
                }
                if (job != null && job.targetA.HasThing && !(job.targetA.Thing is Pawn))
                {
                    ThingPrepareChange2[pawnId] = job.targetA.Thing;
                }
                //аналогично добавляем к обновлению всю пешку, если это была задача, требующая обновления инвентаря или здоровья
                if (job != null 
                    && pawn.RaceProps.Humanlike
                    && (job.def == JobDefOf.Equip
                    || job.def == JobDefOf.TakeInventory
                    || job.def == JobDefOf.Wear
                    || job.def == JobDefOf.DropEquipment
                    || job.def == JobDefOf.RemoveApparel
                    || job.def == JobDefOf.Ingest
                    || job.def == JobDefOf.TendPatient
                    ))
                {
                    ThingPrepareChange2[pawnId] = pawn;
                }

                if (UIEventNewJobDisable) return;

                //у атакующих отменяем все команды и повторяем те, которые были переданы нам последний раз
                if (!AttackingPawnDic.ContainsKey(pawnId))
                {
                    //if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") 
                    //Loger.Log("HostAttackUpdate UIEventNewJob StartJob " + pawn.Label + " job=" + (job == null ? "null" : job.def.defName.ToString()) + " -> ignore");
                    //Log.Message("StartJob " + pawn.Label + " job=" + (job == null ? "null" : job.def.defName.ToString()));
                    return;
                }
                /*
                var stack = "";
                if (job == null || (job == null ? "null" : job.def.defName.ToString()) == "Wait_MaintainPosture")
                {
                    var stackTrace = new StackTrace();
                    var frames = stackTrace.GetFrames();
                    foreach (var frame in frames)
                    {
                        var methodDescription = frame.GetMethod();
                        stack += Environment.NewLine + methodDescription.Name;
                    }
                }
                */
                UIEventNewJobDisable = true;
                //if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("HostAttackUpdate UIEventNewJob StartJob " + pawn.Label + " job=" + (job == null ? "null" : job.def.defName.ToString()) + " -> <...> " + stack);
                ApplyAttackingPawnJob(pawn);

            }
            catch (Exception exp)
            {
                //if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("HostAttackUpdate UIEventNewJob " + exp.ToString());
            }
            UIEventNewJobDisable = false;
        }

        /// <summary>
        /// Перехват события когда что-то было уничтожено, получило повреждения или только что создано
        /// </summary>
        public void UIEventChange(Thing thing, bool distroy = false, bool newSpawn = false)
        {
            if (PlantNotSend && thing is Plant && !thing.def.plant.IsTree) return;

            Loger.Log("HostAttackUpdate UIEventChange " + thing.GetType().ToString() + " " + thing.Label + " id=" + thing.thingIDNumber
                + (distroy ? " distroy!" : "")
                + (newSpawn ? " newSpawn!" : "")
                + (thing is Corpse ? " Corpse" : ""));

            var tId = thing.thingIDNumber;
            lock (ToSendListsSync)
            {
                if (distroy)
                {
                    ToSendDeleteId.Add(tId);
                    ToSendThingAdd.Remove(thing);
                    var fi = thing as Fire;
                    if (fi != null) FireList.Remove(fi);
                }
                else if (newSpawn)
                {
                    ToSendThingAdd.Add(thing);
                    ToSendDeleteId.Remove(tId);
                    var fi = thing as Fire;
                    if (fi != null) FireList.Add(fi);
                }
                else //здесь остались события посл получения урона
                {
                    if (thing is Pawn)
                    {
                        //раз в SendDelayedFillPawnsSeconds сек передаем полное обновление пешек
                        if (!ToSendDelayedFillPawnsId.Contains(tId)) ToSendDelayedFillPawnsId.Add(tId);
                        Loger.Log("HostAttackUpdate UIEventChange Damage Pawn");
                    }
                    else
                    {
                        //обновляем общую информацию у вещей
                        if (!ToUpdateStateId.Contains(tId))
                        {
                            ToUpdateStateId.Add(tId);
                            ToUpdateState.Add(thing);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Изменяем скрость пешек от 1 до 450 (смотри Pawn.TicksPerMove)
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="speed"></param>
        public void ControlPawnMoveSpeed(Pawn pawn, ref int speed)
        {
            if (!HostPawnMoveSpeedActive) return;

            if (!AttackingPawnDic.ContainsKey(pawn.thingIDNumber))
            {
                speed = (int)(speed / HostPawnMoveSpeed);
                if (speed > 450) speed = 450;
            }
        }

        private bool? CheckAttackerVictory()
        {
            bool existHostPawn = false;
            bool existArrackerPawn = false;

            foreach (var pawn in AllPawns.Values)
            {
                if (!existArrackerPawn
                    && AttackingPawnDic.ContainsKey(pawn.thingIDNumber)
                    && pawn.RaceProps.Humanlike
                    && !pawn.Dead
                    && !pawn.Downed)
                {
                    existArrackerPawn = true;
                }
                if (!existHostPawn
                    && pawn.IsColonist
                    && !pawn.Dead
                    && !pawn.Downed)
                {
                    existHostPawn = true;
                }
            }
            return existHostPawn && existArrackerPawn
                ? (bool?)null
                : existArrackerPawn;
        }

        /// <summary>
        /// Принудительная остановка режима pvp
        /// </summary>
        public void Clear()
        {
            //отключить таймер и признаки, что нас атакуют
            if (TimerObj != null) SessionClientController.Timers.Remove(TimerObj);
            if (GameMap != null) GameAttackTrigger_Patch.ActiveAttackHost.Remove(GameMap);
            SessionClientController.Data.AttackUsModule = null;
            SessionClientController.Data.BackgroundSaveGameOff = false;
            SessionClientController.Data.DontChactTimerFail = false;
            GameAttackTrigger_Patch.ForceSpeed = -1f;
        }

        public void Finish(bool victoryAttacker)
        {
            Find.TickManager.Pause();

            Clear();

            if (TestMode)
            {
                GameUtils.ShowDialodOKCancel(
                    TestMode
                        ? "OCity_GameAttack_Host_Test_Attack".Translate(AttackerLogin)
                        : "OCity_GameAttack_Host_Settlement_Attacking".Translate(AttackerLogin)
                    , victoryAttacker
                        ? "Ocity_GameAttacker_TrainingFight_Lost".Translate() + Environment.NewLine +
                            "OCity_GameAttack_Host_Card_Restored".Translate()
                        : "OCity_GameAttack_Host_Training_Attack_Repulsed".Translate() + Environment.NewLine +
                            "OCity_GameAttack_Host_Card_Restored".Translate()
                    , () =>
                    {
                        SessionClientController.Disconnected("OCity_GameAttacker_Done".Translate());
                    }
                    , null
                );
                return;
            }
            if (victoryAttacker)
            {
                //удалить свою колонию
                Find.WorldObjects.Remove(GameMap.Parent);
            }
            else
            {
                //удалить всех чужих пешек с краев карты (они сбежали)
                foreach (var pawn in AttackingPawns)
                {
                    if (!pawn.Dead
                        && !pawn.Downed
                        && (pawn.Position.x < MapBorder || pawn.Position.x > GameMap.Size.x - 1 - MapBorder
                            || pawn.Position.z < MapBorder || pawn.Position.z > GameMap.Size.z - 1 - MapBorder))
                    {
                        GameUtils.PawnDestroy(pawn);
                    }
                }
            }

            //автосейв с единым сохранением
            SessionClientController.SaveGameNow(true, () =>
            {
                GameUtils.ShowDialodOKCancel(
                    TestMode
                        ? "OCity_GameAttack_Host_Test_Attack".Translate(AttackerLogin)
                        : "OCity_GameAttack_Host_Settlement_Attacking".Translate(AttackerLogin)
                    , victoryAttacker
                        ? "OCity_GameAttack_Host_Caravan_TransferToNewOwner".Translate()
                        : "OCity_GameAttack_Host_Atack_Repulsed".Translate() + Environment.NewLine +
                            "OCity_GameAttack_Host_Stranded_EnemiesLostCommander_Touch".Translate()
                    , () => { }
                    , null
                );
            });
        }
    }
}
