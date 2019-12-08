using HugsLib.Utils;
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
        /// К передаче на создание не пешек
        /// </summary>
        public HashSet<Thing> ToSendThingAdd { get; set; }

        /// <summary>
        /// К передаче на создание/обновление
        /// </summary>
        public HashSet<ThingEntry> ToSendAdd { get; set; }

        /// <summary>
        /// К передаче на корретировку состояния, положения и пр.
        /// </summary>
        public HashSet<int> ToSendStateId { get; set; }

        /// <summary>
        /// Пешки которые атакуют хост.
        /// </summary>
        public List<Pawn> AttackingPawns { get; set; }

        /// <summary>
        /// Пешки которые атакуют хост. (ID пешки у хоста, ID пешки у атакующего (пришло в OriginalID, а ему отправляется в TransportID))
        /// </summary>
        public Dictionary<int, int> AttackingPawnDic { get; set; }

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
                return true;
            }
            return false;
        }

        /// <summary>
        /// Начинаем процесс, первые запросы информации
        /// </summary>
        /// <param name="connect"></param>
        public void Start(SessionClient connect)
        {
            Loger.Log("Client GameAttackHost Start 1");
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

            Loger.Log("Client GameAttackHost Start 2 " + tolient.HostPlaceServerId);

            LongEventHandler.QueueLongEvent(delegate
            {
                try
                {
                    Loger.Log("Client GameAttackHost Start 3");
                    var hostPlace = UpdateWorldController.GetWOByServerId(HostPlaceServerId) as MapParent;
                    var cloneMap = hostPlace.Map;

                    var toSrvMap = new AttackHostToSrv()
                    {
                        State = 4,
                        TerrainDefNameCell = new List<IntVec3S>(),
                        TerrainDefName = new List<string>(),
                        Thing = new List<ThingTrade>(),
                        ThingCell = new List<IntVec3S>()
                    };

                    toSrvMap.MapSize = new IntVec3S(cloneMap.Size);

                    CellRect cellRect = CellRect.WholeMap(cloneMap);
                    cellRect.ClipInsideMap(cloneMap);

                    //почва
                    Loger.Log("Client GameAttackHost Start 4");
                    foreach (IntVec3 current in cellRect)
                    {
                        var terr = cloneMap.terrainGrid.TerrainAt(current);
                        toSrvMap.TerrainDefNameCell.Add(new IntVec3S(current));
                        toSrvMap.TerrainDefName.Add(terr.defName);
                    }

                    //скалы, преграды и строения без пешек и без растений не деревьев, вывести кол-во
                    Loger.Log("Client GameAttackHost Start 5");
                    foreach (IntVec3 current in cellRect)
                    {
                        foreach (Thing thc in Find.CurrentMap.thingGrid.ThingsAt(current).ToList<Thing>())
                        {
                            if (thc is Pawn) continue;

                            //из растений оставляем только деревья
                            if (thc.def.category == ThingCategory.Plant && !thc.def.plant.IsTree) continue;

                            if (thc.Position != current) continue;

                            var tt = ThingTrade.CreateTrade(thc, thc.stackCount, false);

                            toSrvMap.ThingCell.Add(new IntVec3S(current));
                            toSrvMap.Thing.Add(tt);
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
                        ToUpdateStateId = new List<int>();
                        ToUpdateState = new List<Thing>();
                        ToSendDeleteId = new HashSet<int>();
                        ToSendAddId = new HashSet<int>();
                        ToSendThingAdd = new HashSet<Thing>();
                        AttackingPawns = new List<Pawn>();
                        AttackingPawnDic = new Dictionary<int, int>();
                        AttackingPawnJobDic = new Dictionary<int, AttackPawnCommand>();
                        AttackUpdateTick = 0;
                        GameMap = cloneMap;

                        UIEventNewJobDisable = true;
                        var cellPawns = GameUtils.SpawnCaravanPirate(cloneMap, pawnsA,
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

                        CameraJumper.TryJump(cellPawns, cloneMap);

                        TimerObj = SessionClientController.Timers.Add(200, AttackUpdate);

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
            //todo
            Find.WindowStack.Add(new Dialog_Message("Error attak".NeedTranslate(), msg, null, () => { }));
        }

        private void AttackUpdate()
        {
            bool inTimerEvent = false;
            try
            {
                if (InTimer) return;
                InTimer = true;
                AttackUpdateTick++;
                //                Loger.Log("Client HostAttackUpdate #" + AttackUpdateTick.ToString());

                SessionClientController.Command((connect) =>
                {
                    try
                    {
                        List<Pawn> mapPawns;
                        AttackHostFromSrv toClient;
                        lock (ToSendListsSync)
                        {
                            //                      Loger.Log("Client HostAttackUpdate 1");
                            //обновляем списки
                            mapPawns = GameMap.mapPawns.AllPawnsSpawned;
                            var mapPawnsId = new HashSet<int>(mapPawns.Select(p => p.thingIDNumber));
                            mapPawnsId.SymmetricExceptWith(SendedPawnsId); //новые пешки + те что на сервере, но их уже нет на карте

                            if (mapPawnsId.Count > 0)
                            {
                                var toSendAddId = new HashSet<int>(mapPawnsId);
                                toSendAddId.ExceptWith(SendedPawnsId); //только новые пешки
                                if (toSendAddId.Count > 0)
                                {
                                    toSendAddId.ExceptWith(ToSendAddId); //исключаем те, которые уже есть в списке
                                    ToSendAddId.AddRange(toSendAddId);
                                }

                                var toSendDeleteId = new HashSet<int>(mapPawnsId);
                                toSendDeleteId.IntersectWith(SendedPawnsId); //только те, что на сервере но их уже нет на карте
                                if (toSendDeleteId.Count > 0)
                                {
                                    toSendDeleteId.ExceptWith(ToSendDeleteId); //исключаем те, которые уже есть в списке 
                                    ToSendDeleteId.AddRange(toSendDeleteId);
                                }
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

                                var thing = mapPawns.Where(p => p.thingIDNumber == id).FirstOrDefault();
                                var tt = ThingEntry.CreateEntry(thing, 1);
                                //передаем те, что были исходные у атакуемого (хотя там используется только как признак TransportID != 0 - значит те кто атакует)
                                if (AttackingPawnDic.ContainsKey(tt.OriginalID)) tt.TransportID = AttackingPawnDic[tt.OriginalID];
                                newPawns.Add(tt);
                                newPawnsId.Add(id);
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

                            //вещи
                            var newThings = ToSendThingAdd
                                .Where(thing => !ToSendDeleteId.Any(d => thing.thingIDNumber == d))
                                .Select(thing => ThingEntry.CreateEntry(thing, thing.stackCount))
                                .ToList();

                            //передаем изменение местоположения и пр. по ID хоста
                            var toSendState = new List<AttackThingState>();
                            var toSendStateId = new List<int>();
                            for (int imp = 0; imp < mapPawns.Count; imp++)
                            {
                                var mp = mapPawns[imp];
                                var mpID = mp.thingIDNumber;
                                if (ToUpdateStateId.Contains(mpID)) continue;

                                var mpHash = AttackThingState.GetHash(mp);
                                int mpHS;
                                if (!SendedState.TryGetValue(mpID, out mpHS) || mpHS != mpHash)
                                {
                                    SendedState[mpID] = mpHash;
                                    toSendState.Add(new AttackThingState(mp));
                                }
                            }
                            for (int imp = 0; imp < ToUpdateState.Count; imp++)
                            {
                                var mp = ToUpdateState[imp];
                                var mpID = mp.thingIDNumber;
                                if (ToSendDeleteId.Contains(mpID)) continue;

                                if (mp is Pawn)
                                {
                                    var mpHash = AttackThingState.GetHash(mp);
                                    SendedState[mpID] = mpHash; //заносим только для проверки выше
                                }
                                toSendState.Add(new AttackThingState(mp));
                            }

                            //обновляем поколения вещей учавствующих в Job и отправляем нужные
                            foreach (var mp in ThingPrepareChange0)
                            {
                                var mpID = mp.thingIDNumber;
                                if (ToSendDeleteId.Contains(mpID)) continue;

                                toSendState.Add(new AttackThingState(mp));
                            }
                            Loger.Log("HostAttackUpdate UpdateCommand FromJob Count=" + ThingPrepareChange0.Count.ToString());
                            ThingPrepareChange0 = ThingPrepareChange1;
                            ThingPrepareChange1 = new HashSet<Thing>();

                            //Loger.Log("Client HostAttackUpdate 3");
                            toClient = connect.AttackOnlineHost(new AttackHostToSrv()
                            {
                                State = 10,
                                NewPawns = newPawns,
                                NewPawnsId = newPawnsId,
                                NewThings = newThings,
                                NewThingsId = newThings.Select(th => th.OriginalID).ToList(),
                                Delete = ToSendDeleteId.ToList(),
                                UpdateState = toSendState
                            });
                            ToSendThingAdd.Clear();
                            ToSendDeleteId.Clear();
                            ToUpdateStateId.Clear();
                            ToUpdateState.Clear();
                        }

                        //принимаем обновление команд атакующих
                        if (toClient.UpdateCommand.Count > 0)
                        {
                            Loger.Log("HostAttackUpdate UpdateCommand Count=" + toClient.UpdateCommand.Count.ToString());
                            UIEventNewJobDisable = true;
                            for (int ii = 0; ii < toClient.UpdateCommand.Count; ii++)
                            {
                                var comm = toClient.UpdateCommand[ii];
                                var pawn = mapPawns.Where(p => p.thingIDNumber == comm.HostPawnID).FirstOrDefault() as Pawn;
                                if (pawn == null)
                                {
                                    Loger.Log("HostAttackUpdate UpdateCommand pawn == null " + comm.HostPawnID.ToString());
                                    continue;
                                }

                                AttackingPawnJobDic[comm.HostPawnID] = comm;
                                ApplyAttackingPawnJob(pawn);

                            }
                            UIEventNewJobDisable = false;
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
        private void ApplyAttackingPawnJob(Pawn pawn)
        {
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
                        if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("HostAttackUpdate ApplyAttackingPawnJob stopJob(repeat) " + comm.TargetPos.Get().ToString());
                        stopJob = true;
                    }
                }

                //UIEventNewJobDisable = true;
                Thing target = null;
                if (!stopJob
                    && (comm.Command == AttackPawnCommand.PawnCommand.Attack
                        || comm.Command == AttackPawnCommand.PawnCommand.AttackMelee))
                {
                    var mapPawns = GameMap.mapPawns.AllPawnsSpawned;
                    target = mapPawns.Where(p => p.thingIDNumber == comm.TargetID).FirstOrDefault();
                    if (target == null) target = GameMap.listerThings.AllThings.Where(p => p.thingIDNumber == comm.TargetID).FirstOrDefault();
                    if (target == null)
                    {
                        if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("HostAttackUpdate ApplyAttackingPawnJob TargetThing == null " + comm.HostPawnID.ToString());
                        stopJob = true;
                    }
                }

                if (!stopJob)
                {
                    if (comm.Command == AttackPawnCommand.PawnCommand.Attack)
                    {
                        //задаем команду атаковать
                        if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("HostAttackUpdate ApplyAttackingPawnJob StartJob Attack " + comm.TargetID.ToString());
                        pawn.jobs.StartJob(new Job(JobDefOf.AttackStatic, target)
                        {
                            playerForced = true,
                            expiryInterval = int.MaxValue,
                            checkOverrideOnExpire = false,
                        }
                            , JobCondition.InterruptForced);
                    }
                    else if (comm.Command == AttackPawnCommand.PawnCommand.AttackMelee)
                    {
                        if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("HostAttackUpdate ApplyAttackingPawnJob StartJob AttackMelee " + comm.TargetID.ToString());
                        pawn.jobs.StartJob(new Job(JobDefOf.AttackMelee, target)
                        {
                            playerForced = true,
                            expiryInterval = int.MaxValue,
                            checkOverrideOnExpire = false,
                        }
                            , JobCondition.InterruptForced);
                    }
                    else if (comm.Command == AttackPawnCommand.PawnCommand.Goto)
                    {
                        //задаем команду идти
                        if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("HostAttackUpdate ApplyAttackingPawnJob StartJob Goto " + comm.TargetPos.Get().ToString());
                        pawn.jobs.StartJob(new Job(JobDefOf.Goto, comm.TargetPos.Get())
                        {
                            playerForced = true,
                            expiryInterval = int.MaxValue,
                            checkOverrideOnExpire = false,
                        }
                            , JobCondition.InterruptForced);
                    }
                    else stopJob = true;
                }
                if (stopJob)
                {
                    if (AttackingPawnJobDic.ContainsKey(pId))
                    {
                        if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("HostAttackUpdate ApplyAttackingPawnJob Remove Job " + comm.TargetPos.Get().ToString());
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
                if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("HostAttackUpdate ApplyAttackingPawnJob " + exp.ToString());
                if (AttackingPawnJobDic.ContainsKey(pId)) AttackingPawnJobDic.Remove(pId);
            }
            //UIEventNewJobDisable = false;
        }

        private bool UIEventNewJobDisable = false;
        public void UIEventNewJob(Pawn pawn, Job job) //если job == null значит команда стоять и не двигаться Wait_Combat
        {
            try
            {
                if (UIEventNewJobDisable) return;
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

                //у атакующих отменяем все команды и повторяем те, которые были переданы нам последний раз
                if (!AttackingPawnDic.ContainsKey(pawnId))
                {
                    if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("HostAttackUpdate UIEventNewJob StartJob " + pawn.Label + " job=" + (job == null ? "null" : job.def.defName.ToString()) + " -> ignore");
                    return;
                }
                var stack = "";
                /*
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
                if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("HostAttackUpdate UIEventNewJob StartJob " + pawn.Label + " job=" + (job == null ? "null" : job.def.defName.ToString()) + " -> <...> " + stack);
                ApplyAttackingPawnJob(pawn);

            }
            catch (Exception exp)
            {
                if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("HostAttackUpdate UIEventNewJob " + exp.ToString());
            }
            UIEventNewJobDisable = false;
        }

        /// <summary>
        /// Перехват события когда что-то было уничтожено, получило повреждения или только что создано
        /// </summary>
        public void UIEventChange(Thing thing, bool distroy = false, bool newSpawn = false)
        {
            Loger.Log("HostAttackUpdate UIEventChange " + thing.GetType().ToString() + " " + thing.Label + " id=" + thing.thingIDNumber
                + (distroy ? " distroy!" : "")
                + (newSpawn ? " newSpawn!" : "")
                + (thing is Corpse ? " Corpse" : ""));

            var tId = thing.thingIDNumber;
            lock (ToSendListsSync)
            {
                if (distroy)
                {
                    if (!ToSendDeleteId.Contains(tId)) ToSendDeleteId.Add(tId);
                }
                else if (newSpawn)
                {
                    if (thing is Corpse)
                    {
                        //трупы обрабатываем отдельно: передаем труп не как объект, а как редактирование состояния пешки - она сама станет трупом + после изменеия состояния удалить из словарей, чтобы её не удалили (да ID созданного трупа будет не синхронизированно, но считаем что с ними ничего не будут делать)
                        //todo здесь, а также изменить у атакующего: когда пришла команда удалить пешку, то задержать команду на 1 цикл (новый массив)
                    }
                    else ToSendThingAdd.Add(thing);
                }
                else
                {
                    if (!ToUpdateStateId.Contains(tId))
                    {
                        ToUpdateStateId.Add(tId);
                        ToUpdateState.Add(thing);
                    }
                }
            }
        }

    }
}
