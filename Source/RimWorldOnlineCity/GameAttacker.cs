using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using OCUnion;
using Model;
using System.Threading;
using RimWorldOnlineCity.GameClasses;
using HugsLib.Utils;
using OCUnion.Transfer.Model;
using Verse.AI;

namespace RimWorldOnlineCity
{
    public class GameAttacker
    {

        /// <summary>
        /// Время в ms между синхронизациями с сервером
        /// </summary>
        public int AttackUpdateDelay { get; } = 50;

        /// <summary>
        /// Время в секундах после всех загрузок, чтобы дать оглядеться атакующиму, по умолчанию минута
        /// </summary>
        public int TimeStopBeforeAttack { get; } = 60;

        /// <summary>
        /// Расстояние от краев карты за которым пешка мжет сбежать
        /// </summary>
        public int MapBorder { get; } = 10;

        public static bool CanStart
        {
            get { return SessionClientController.Data.AttackModule == null; }
        }

        public static GameAttacker Get
        {
            get { return SessionClientController.Data.AttackModule; }
        }

        public bool TestMode { get; set; }

        public int AttackUpdateTick { get; set; }
        private object TimerObj;
        private bool InTimer { get; set; }
        private Map GameMap { get; set; }
        /// <summary>
        /// Словарь сопоставления ID с хоста и сейчас созданных (Key с хоста из OriginalID, Value наш ID)
        /// </summary>
        private Dictionary<int, int> ThingsIDDicRev { get; set; }
        /// <summary>
        /// Словарь сопоставления ID с хоста и сейчас созданных (Key наш ID, Value с хоста из OriginalID)
        /// </summary>
        private Dictionary<int, int> ThingsIDDic { get; set; }
        /// <summary>
        /// Словарь всех вещей на нашей карте по нашим ID
        /// </summary>
        private Dictionary<int, Thing> ThingsObjDic { get; set; }
        /// <summary>
        /// Атакующие пешки и ID с хоста
        /// </summary>
        private Dictionary<Pawn, int> AttackerPawns { get; set; }
        /// <summary>
        /// Команды отданые игроком для отправки
        /// </summary>
        private Dictionary<int, AttackPawnCommand> ToSendCommand { get; set; }
        /// <summary>
        /// Для пешек которым прикла команда на удаление идет задержка перед удалением на 1 цикл
        /// Если в этом же или следующем пакете придет кодманда создать труп из пешки, то команда станет не актуальной
        /// </summary>
        private List<Thing> DelayDestroyPawn { get; set; }

        /// <summary>
        /// Истина, когда атакующий сдается. Флаг для передачи хосту и начала завершения
        /// </summary>
        public bool VictoryHostToHost { get; set; }

        public static bool Create()
        {
            if (!CanStart) return false;
            SessionClientController.Data.AttackModule = new GameAttacker();
            SessionClientController.Data.BackgroundSaveGameOff = true;
            return true;
        }

        private GameAttacker()
        {
        }

        public void Start(Caravan caravan, BaseOnline attackedBase, bool testMode)
        {
            //Ожидается что Start будет запукаться с UI
            SessionClientController.SaveGameNowInEvent(false);

            SessionClientController.Command((connect) =>
            {
                connect.ErrorMessage = null;
                Find.TickManager.Pause();

                Loger.Log("Client GameAttack State 0");
                var res = connect.AttackOnlineInitiator(new AttackInitiatorToSrv()
                {
                    State = 0,
                    StartHostPlayer = attackedBase.Player.Public.Login,
                    HostPlaceServerId = attackedBase.OnlineWObject.ServerId,
                    InitiatorPlaceServerId = UpdateWorldController.GetServerInfo(caravan).ServerId,
                    TestMode = testMode,
                });
                if (!string.IsNullOrEmpty(connect.ErrorMessage))
                {
                    ErrorBreak(connect.ErrorMessage);
                    return;
                }

                // ждем положительного ответа с статусом больше 2 и отправляем своих колонистов
                Loger.Log("Client GameAttack State 1");
                var s1Time = DateTime.UtcNow;
                while (true)
                {
                    var res1 = connect.AttackOnlineInitiator(new AttackInitiatorToSrv()
                    {
                        State = 1
                    });
                    if (!string.IsNullOrEmpty(connect.ErrorMessage))
                    {
                        ErrorBreak(connect.ErrorMessage);
                        return;
                    }
                    if (res1.State == 2) break;
                    if ((DateTime.UtcNow - s1Time).TotalSeconds > 20)
                    {
                        ErrorBreak("Timeout");
                        return;
                    }
                }
                var pawnsToSend = GetPawnsAndDeleteCaravan(caravan);
                var response = connect.AttackOnlineInitiator(new AttackInitiatorToSrv()
                {
                    State = 2,
                    Pawns = pawnsToSend,
                });
                if (!string.IsNullOrEmpty(connect.ErrorMessage))
                {
                    ErrorBreak(connect.ErrorMessage);
                    return;
                }
                TestMode = response.TestMode;

                // принимаем карту и создаем
                Loger.Log("Client GameAttack CreateMap State=" + response.State + " TestMode=" + TestMode);

                Loger.Log("Client GameAttack WaitTo3");
                s1Time = DateTime.UtcNow;
                while (true)
                {
                    response = connect.AttackOnlineInitiator(new AttackInitiatorToSrv()
                    {
                        State = 3
                    });
                    if (!string.IsNullOrEmpty(connect.ErrorMessage))
                    {
                        ErrorBreak(connect.ErrorMessage);
                        return;
                    }
                    if (response.State >= 4) break;
                    if ((DateTime.UtcNow - s1Time).TotalSeconds > 60)
                    {
                        ErrorBreak("Timeout");
                        return;
                    }
                }
                Find.TickManager.Pause();
                GameAttackTrigger_Patch.ForceSpeed = 0f;
                CreateClearMap(attackedBase.Tile, response.MapSize.Get(), (map, mapParent) =>
                {
                    GameMap = map;
                    Loger.Log("Client StartCreateClearMap 1");
                    CellRect cellRect = CellRect.WholeMap(map);
                    cellRect.ClipInsideMap(map);

                    Loger.Log("Client StartCreateClearMap 2");
                    //почва
                    for (int i = 0; i < response.TerrainDefNameCell.Count; i++)
                    {
                        var current = response.TerrainDefNameCell[i].Get();
                        var terrName = response.TerrainDefName[i];
                        //Log.Message(terr.defName);
                        map.terrainGrid.SetTerrain(current, DefDatabase<TerrainDef>.GetNamed(terrName));
                    }

                    Loger.Log("Client StartCreateClearMap 3");
                    //скалы, преграды и строения без пешек и без растений не деревьев
                    ThingsIDDicRev = new Dictionary<int, int>();
                    ThingsIDDic = new Dictionary<int, int>();
                    ThingsObjDic = new Dictionary<int, Thing>();
                    AttackerPawns = new Dictionary<Pawn, int>();
                    ToSendCommand = new Dictionary<int, AttackPawnCommand>();
                    DelayDestroyPawn = new List<Thing>();
                    for (int i = 0; i < response.ThingCell.Count; i++)
                    {
                        var current = response.ThingCell[i].Get();
                        var tt = response.Thing[i];
                        var th = tt.CreateThing();
                        GenSpawn.Spawn(th, current, map, new Rot4(tt.Rotation), WipeMode.Vanish);
                        //словарь сопоставления ID их и сейчас созданный (их из OriginalID, наш ID)
                        if (tt.OriginalID != 0 && th.thingIDNumber != 0)
                        {
                            ThingsIDDicRev[tt.OriginalID] = th.thingIDNumber;
                            ThingsIDDic[th.thingIDNumber] = tt.OriginalID;
                            ThingsObjDic[th.thingIDNumber] = th;
                        }
                    }

                    Loger.Log("Client StartCreateClearMap 4 CountThings=" + ThingsIDDic.Count.ToString()
                        + " CountThingsRev=" + ThingsIDDicRev.Count.ToString()
                        + " CountThingsObj=" + ThingsObjDic.Count.ToString());

                    Loger.Log("Client StartCreateClearMap 5");
                    AttackUpdateTick = 0;
                    AttackUpdate();

                    TimerObj = SessionClientController.Timers.Add(AttackUpdateDelay, AttackUpdate);

                    //включаем обработку событий выдачи команд
                    Loger.Log("Client StartCreateClearMap 6");
                });

            });
        }

        private void DestroyThing(Thing thing, int hostId = 0)
        {
            var id = thing.thingIDNumber;
            if (hostId != 0 || ThingsIDDic.TryGetValue(id, out hostId))
                ThingsIDDicRev.Remove(hostId);
            ThingsIDDic.Remove(id);
            ThingsObjDic.Remove(id);
            thing.Destroy();
        }

        /// <summary>
        /// Ошибка при создании карты
        /// </summary>
        /// <param name="msg"></param>
        private void ErrorBreak(string msg)
        {
            Loger.Log("Client GameAttack error" + msg);

            SessionClientController.Disconnected("OCity_GameAttacker_Dialog_ErrorMessage".Translate());
            /*
            if (AttackerPawns != null && AttackerPawns.Count > 0) Finish(false);
            Find.WindowStack.Add(new Dialog_Message("OCity_GameAttacker_Dialog_ErrorMessage".Translate(), msg, null, () => { }));
            */
        }

        private List<ThingEntry> GetPawnsAndDeleteCaravan(Caravan caravan)
        {

            var select = caravan.PawnsListForReading.ToList();
            //передаем выбранные товары из caravan к другому игроку в сaravanOnline
            var sendThings = new List<ThingEntry>();
            foreach (var pair in select)
            {
                var thing = pair;
                sendThings.Add(ThingEntry.CreateEntry(thing, 1));
                /*
                if (thing is Pawn)
                {
                    var pawn = thing as Pawn;
                    GameUtils.DeSpawnSetupOnCaravan(caravan, pawn);
                }*/
            }
            //пешки получины, остальное уничтожаем
            caravan.RemoveAllPawns();
            if (caravan.Spawned)
            {
                Find.WorldObjects.Remove(caravan);
            }
            foreach (var pair in select)
            {
                GameUtils.PawnDestroy(pair);
            }
            return sendThings;
        }

        private void AttackUpdate()
        {
            bool inTimerEvent = false;
            try
            {
                //Scribe.ForceStop();
                if (!Find.TickManager.Paused)
                {
                    Find.TickManager.Pause();
                    GameUtils.ShowDialodOKCancel("OCity_GameAttacker_Dialog_Settlement_Attack".Translate()
                        , ("OCity_GameAttacker_Main_Dialog").Translate()
                            + "OCity_GameAttacker_Withdraw".Translate()
                        , () => { }
                        , null
                    );

                }

                if (InTimer) return;
                InTimer = true;
                AttackUpdateTick++;
                Loger.Log("Client AttackUpdate #" + AttackUpdateTick.ToString() + ".");

                if (AttackUpdateTick == 3)
                {
                    GameUtils.ShowDialodOKCancel("OCity_GameAttacker_Dialog_Settlement_Attack".Translate()
                        , "OCity_GameAttacker_Main_Preparation_Dialog".Translate()
                            + Environment.NewLine
                            + ("OCity_GameAttacker_Main_Dialog").Translate()
                            + "OCity_GameAttacker_Withdraw".Translate()
                        , () => { }
                        , null
                    );
                }

                inTimerEvent = true;
                SessionClientController.Command((connect) =>
                {
                    var errNums = "1 ";
                    try
                    {
                        Loger.Log($"Client AttackUpdate 2 ({AttackUpdateTick})");
                        var toSendCommand = ToSendCommand;
                        ToSendCommand = new Dictionary<int, AttackPawnCommand>();
                        errNums += "2 ";
                        var toClient = connect.AttackOnlineInitiator(new AttackInitiatorToSrv()
                        {
                            State = 10,
                            UpdateCommand = toSendCommand.Values.ToList(),
                            //снимаем паузу на загрузку у хоста и устанавливаем после загрузки, чтобы оглядеться атакующиму
                            SetPauseOnTimeToHost = AttackUpdateTick == 2 ? new TimeSpan(0, 0, TimeStopBeforeAttack) : TimeSpan.MinValue,
                            VictoryHostToHost = VictoryHostToHost,
                        });

                        errNums += "3 ";
                        Action actUpdateState = () =>
                        {
                            Loger.Log("Client AttackUpdate 4. UpdateState=" + toClient.UpdateState.Count);

                            //Применение изменения местоположения и пр. по ID хоста
                            for (int i = 0; i < toClient.UpdateState.Count; i++)
                            {
                                int id;
                                if (!ThingsIDDicRev.TryGetValue(toClient.UpdateState[i].HostThingID, out id))
                                {
                                    Loger.Log("Client AttackUpdate 4 Err1 " + toClient.UpdateState[i].ToString());
                                    continue;
                                }

                                Thing thing;
                                if (!ThingsObjDic.TryGetValue(id, out thing))
                                {
                                    Loger.Log("Client AttackUpdate 4 Err2 " + toClient.UpdateState[i].ToString() + " id=" + id.ToString());
                                    continue;
                                }
                                if (thing == null)
                                {
                                    Loger.Log("Client AttackUpdate 4 Err3 " + toClient.UpdateState[i].ToString() + " id=" + id.ToString());
                                    continue;
                                }
                                if (!(thing is Pawn)) Loger.Log("Client AttackUpdate 4 Apply " + toClient.UpdateState[i].ToString() + " thing=" + thing.Label + " ID=" + thing.thingIDNumber);
                                if (thing is Pawn && toClient.UpdateState[i].DownState == AttackThingState.PawnHealthState.Dead)
                                {
                                    DelayDestroyPawn.Remove(thing);
                                }
                                GameUtils.ApplyState(thing, toClient.UpdateState[i]);
                            }

                            for (int i = 0; i < DelayDestroyPawn.Count; i++)
                            {
                                Thing thing = DelayDestroyPawn[i];
                                Loger.Log("Client AttackUpdate 4 DelayDestroyPawn thing=" + thing.Label + " ID=" + thing.thingIDNumber);
                                DestroyThing(thing);
                            }
                            DelayDestroyPawn.Clear();

                            for (int i = 0; i < toClient.Delete.Count; i++)
                            {
                                int id;
                                if (!ThingsIDDicRev.TryGetValue(toClient.Delete[i], out id))
                                {
                                    Loger.Log("Client AttackUpdate 4 Err4 " + toClient.Delete[i].ToString());
                                    continue;
                                }

                                Thing thing;
                                if (!ThingsObjDic.TryGetValue(id, out thing))
                                {
                                    Loger.Log("Client AttackUpdate 4 Err5 " + toClient.Delete[i].ToString() + " id=" + id.ToString());
                                    continue;
                                }
                                if (thing == null)
                                {
                                    Loger.Log("Client AttackUpdate 4 Err6 " + toClient.Delete[i].ToString() + " id=" + id.ToString());
                                    continue;
                                }
                                if (thing is Pawn)
                                {
                                    Loger.Log("Client AttackUpdate 4 ToDelayDestroy " + toClient.Delete[i].ToString() + " thing=" + thing.Label + " ID=" + thing.thingIDNumber);
                                    DelayDestroyPawn.Add(thing);
                                }
                                else
                                {
                                    Loger.Log("Client AttackUpdate 4 Destroy " + toClient.Delete[i].ToString() + " thing=" + thing.Label + " ID=" + thing.thingIDNumber);
                                    DestroyThing(thing, toClient.Delete[i]);
                                }
                            }

                        };

                        errNums += "4 ";
                        if (toClient.NewPawns.Count > 0 || toClient.NewThings.Count > 0)
                        {
                            //LongEventHandler.QueueLongEvent(delegate
                            {
                                try
                                {
                                    Loger.Log("Client AttackUpdate 3. NewPawn=" + toClient.NewPawns.Count);

                                    if (toClient.NewPawns.Count > 0)
                                    {
                                        errNums += "5 ";
                                        //удаляем пешки NewPawnsId (здесь список thingIDNumber от хоста), которые сейчас обновим
                                        for (int i = 0; i < toClient.NewPawnsId.Count; i++)
                                        {
                                            var hostid = toClient.NewPawnsId[i];
                                            int id;
                                            if (!ThingsIDDicRev.TryGetValue(hostid, out id))
                                            {
                                                continue;
                                            }

                                            Thing thing;
                                            if (!ThingsObjDic.TryGetValue(id, out thing))
                                            {
                                                continue;
                                            }
                                            if (thing == null)
                                            {
                                                continue;
                                            }
                                            Loger.Log("Client AttackUpdate 3 DestroyPawnForUpdate " + hostid.ToString() + " pawn=" + thing.Label + " ID=" + thing.thingIDNumber);
                                            DestroyThing(thing, hostid);
                                        }

                                        errNums += "6 ";
                                        //создаем список пешек toClient.NewPawns
                                        GameUtils.SpawnList(GameMap, toClient.NewPawns, false
                                            , (p) => p.TransportID == 0 //если без нашего ID, то у нас как пират
                                            , (th, te) =>
                                            {
                                                var p = th as Pawn;
                                                //Loger.Log("Client AttackUpdate 3. NewPawn " + (p.IsColonist ? "IsColonist" : "NotColonist"));
                                                //Дополнить словарь сопоставления ID (их из OriginalID, наш ID, а TransportID уже никому не нужен, т.к. пешки дропнуты и переозданы)
                                                if (te.OriginalID != 0 && th.thingIDNumber != 0)
                                                {
                                                    ThingsIDDicRev[te.OriginalID] = th.thingIDNumber;
                                                    ThingsIDDic[th.thingIDNumber] = te.OriginalID;
                                                    ThingsObjDic[th.thingIDNumber] = th;
                                                    //Наши пешки сохраняем отдельно
                                                    if (te.TransportID != 0)
                                                    {
                                                        AttackerPawns[p] = te.OriginalID;

                                                        p.playerSettings.hostilityResponse = HostilityResponseMode.Ignore;
                                                        p.jobs.StartJob(new Job(JobDefOf.Wait_Combat)
                                                        {
                                                            playerForced = true,
                                                            expiryInterval = int.MaxValue,
                                                            checkOverrideOnExpire = false,
                                                        }
                                                            , JobCondition.InterruptForced);
                                                    }
                                                }
                                                else
                                                {
                                                    Loger.Log("Client AttackUpdate SpawnListPawn NotOrigID! " + " thing=" + th.Label + " ID=" + th.thingIDNumber);
                                                }
                                            });
                                    }

                                    errNums += "7 ";
                                    Loger.Log("Client AttackUpdate 3. NewThings=" + toClient.NewThings.Count);

                                    if (toClient.NewThings.Count > 0)
                                    {
                                        GameUtils.SpawnList(GameMap, toClient.NewThings, false, (p) => false
                                            , (th, te) =>
                                            {
                                                var p = th as Pawn;
                                                //Loger.Log("Client AttackUpdate 3. NewPawn " + (p.IsColonist ? "IsColonist" : "NotColonist"));
                                                //Дополнить словарь сопоставления ID (их из OriginalID, наш ID, а TransportID уже никому не нужен, т.к. пешки дропнуты и переозданы)
                                                if (te.OriginalID != 0 && th.thingIDNumber != 0)
                                                {
                                                    ThingsIDDicRev[te.OriginalID] = th.thingIDNumber;
                                                    ThingsIDDic[th.thingIDNumber] = te.OriginalID;
                                                    ThingsObjDic[th.thingIDNumber] = th;
                                                }
                                                else
                                                {
                                                    Loger.Log("Client AttackUpdate SpawnListThings NotOrigID! " + " thing=" + th.Label + " ID=" + th.thingIDNumber);
                                                }
                                            });
                                    }

                                    errNums += "8 ";
                                    actUpdateState();

                                    errNums += "9 ";
                                    Loger.Log("Client AttackUpdate 5");

                                    //после первого массового спавна всех пешек
                                    if (AttackUpdateTick == 1)
                                    {
                                        errNums += "10 ";
                                        ModBaseData.RunMainThreadSync(() =>
                                        {
                                            errNums += "11 ";
                                            //проверка обзора и переключение на карту
                                            FloodFillerFog.DebugRefogMap(GameMap);
                                            CameraJumper.TryJump(GameMap.Center, GameMap);
                                            errNums += "12 ";
                                        });
                                        errNums += "13 ";
                                        GameAttackTrigger_Patch.ActiveAttacker.Add(GameMap, this);
                                        errNums += "14 ";
                                    }
                                }
                                catch (Exception ext)
                                {
                                    Loger.Log("Client AttackUpdate SpawnListEvent Exception " + ext.ToString());
                                }
                                InTimer = false;
                            }//, "", false, null); //".."
                            errNums += "15 ";
                        }
                        else
                        {
                            errNums += "16 ";
                            actUpdateState();

                            InTimer = false;
                        }

                        errNums += "17 ";
                        //заканчиваем
                        if (toClient.Finishing) Finish(toClient.VictoryAttacker);
                        errNums += "18 ";

                    }
                    catch (Exception ext)
                    {
                        InTimer = false;
                        Loger.Log("Client AttackUpdate SpawnList Exception " + ext.ToString() + " ErrNums:" + errNums);
                    }
                });
            }
            catch (Exception ext)
            {
                Loger.Log("AttackUpdate Exception " + ext.ToString());
            }
            if (!inTimerEvent) InTimer = false;
        }

        public void UIEventNewJob(Pawn pawn, Job job) //если job == null значит команда стоять и не двигаться Wait_Combat
        {
            try
            {
                int id;
                if (!AttackerPawns.TryGetValue(pawn, out id))
                {
                    //if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("AttackUpdate UIEventNewJob Out of event " + pawn.Label + " " + pawn.thingIDNumber.ToString());
                    return;
                }
                var comm = new AttackPawnCommand()
                {
                    HostPawnID = id
                };

                if (job != null)
                {
                    if (job.targetA.HasThing)
                    {
                        int tid;
                        if (!ThingsIDDic.TryGetValue(job.targetA.Thing.thingIDNumber, out tid))
                        {
                            //if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("AttackUpdate UIEventNewJob Error " + pawn.Label + " ThingID " + job.targetA.Thing.Label + " " + job.targetA.Thing.thingIDNumber.ToString());
                            return;
                        }
                        comm.TargetID = tid;
                    }
                    else
                        comm.TargetPos = new IntVec3S(job.targetA.Cell);

                    if (job.def == JobDefOf.AttackStatic)
                    {
                        comm.Command = AttackPawnCommand.PawnCommand.Attack;
                    }
                    else if (job.def == JobDefOf.AttackMelee)
                    {
                        comm.Command = AttackPawnCommand.PawnCommand.AttackMelee;
                    }
                    else if (job.def == JobDefOf.Goto)
                    {
                        comm.Command = AttackPawnCommand.PawnCommand.Goto;
                    }
                    else
                    {
                        //if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("AttackUpdate UIEventNewJob " + pawn.Label + " job=" + (job == null ? "null" : job.def.defName.ToString()) + " -> ignore");
                        return; //левые команды игнорятся, но перед ними идет отмена предыдущего с job == null
                        //comm.Command = AttackPawnCommand.PawnCommand.Wait_Combat;
                    }
                }
                else
                {
                    //if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("AttackUpdate UIEventNewJob " + pawn.Label + " job=" + (job == null ? "null" : job.def.defName.ToString()) + " -> ignore2");
                    return;
                    //comm.Command = AttackPawnCommand.PawnCommand.Wait_Combat;
                }

                //if (MainHelper.DebugMode && pawn.Label == "Douglas, Клерк") Loger.Log("AttackUpdate UIEventNewJob " + pawn.Label + " job=" + (job == null ? "null" : job.def.defName.ToString()) + " -> " + comm.Command.ToString());

                ToSendCommand[id] = comm;
            }
            catch (Exception exp)
            {
                Loger.Log("AttackUpdate UIEventNewJob " + exp.ToString());
            }
        }

        private void CreateClearMap(int tile, IntVec3 mapSize, Action<Map, MapParent> ready)
        {
            LongEventHandler.QueueLongEvent(delegate
            {
                Loger.Log("Client CreateClearMap 1");
                Rand.PushState();
                try
                {
                    TileFinder_IsValidTileForNewSettlement_Patch.Off = true;
                    int seed = Gen.HashCombineInt(Find.World.info.Seed, tile); //todo по идеи не нужен
                    Rand.Seed = seed;

                    Loger.Log("Client CreateClearMap 2");
                    //todo проверить что временный родитель mapParent будет удален
                    var mapParent = (MapParent)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Ambush); //WorldObjectDefOf.Ambush  WorldObjectDefOf.AttackedNonPlayerCaravan
                    mapParent.Tile = tile;
                    Loger.Log("Client CreateClearMap 3");
                    Find.WorldObjects.Add(mapParent);
                    mapParent.SetFaction(Find.FactionManager.OfPlayer);
                    Loger.Log("Client CreateClearMap 4");
                    var map = MapGenerator.GenerateMap(mapSize, mapParent, mapParent.MapGeneratorDef, null, null);
                    Loger.Log("Client CreateClearMap 5");
                    /*

                    Map map = new Map();
                    map.uniqueID = Find.UniqueIDsManager.GetNextMapID();
                    map.info.Size = mapSize;
                    map.info.parent = mapParent;
                    map.ConstructComponents();
                    Current.Game.AddMap(map);
                    map.areaManager.AddStartingAreas();
                    map.weatherDecider.StartInitialWeather();


                    /*
                    var mapGenerator = MapGeneratorDefOf.Encounter;
                    IEnumerable<GenStepWithParams> enumerable = from x in mapGenerator.genSteps
                                                                select new GenStepWithParams(x, default(GenStepParams));
                    MapGenerator.GenerateContentsIntoMap(enumerable, map, seed);
                    * /
                    //RockNoises.Init(map); //это внутри MapGenerator.GenerateContentsIntoMap

                    Find.Scenario.PostMapGenerate(map);
                    map.FinalizeInit();
                    Verse.MapComponentUtility.MapGenerated(map);
                    if (mapParent != null)
                    {
                        mapParent.PostMapGenerate();
                    }
                    */

                    LongEventHandler.QueueLongEvent(delegate
                    {
                        Loger.Log("Client CreateClearMap 6");
                        CellRect cellRect = CellRect.WholeMap(map);
                        cellRect.ClipInsideMap(map);

                        GenDebug.ClearArea(cellRect, map);
                        /*
                        foreach (IntVec3 current in cellRect)
                        {
                            GenSpawn.Spawn(ThingDefOf.Granite, current, map, WipeMode.Vanish);
                        }

                        GenDebug.ClearArea(cellRect, map);
                        */
                        Loger.Log("Client CreateClearMap 7");
                        ready(map, mapParent);

                        TileFinder_IsValidTileForNewSettlement_Patch.Off = false;
                    }, "GeneratingMapForNewEncounter", false, null);
                }
                finally
                {
                    Rand.PopState();
                }
            }, "GeneratingMapForNewEncounter", false, null);
        }

        private void Finish(bool victoryAttacker)
        {
            Find.TickManager.Pause();
            Loger.Log("Client AttackerFinish");

            //отключить таймер и признаки, что нас атакуют
            if (TimerObj != null) SessionClientController.Timers.Remove(TimerObj);
            GameAttackTrigger_Patch.ActiveAttacker.Remove(GameMap);
            SessionClientController.Data.AttackModule = null;
            SessionClientController.Data.BackgroundSaveGameOff = false;
            GameAttackTrigger_Patch.ForceSpeed = -1f;

            if (TestMode)
            {
                GameUtils.ShowDialodOKCancel("OCity_GameAttacker_Dialog_Settlement_Attack".Translate()
                    , victoryAttacker
                        ? ("Ocity_GameAttacker_TrainingFight_Won" + Environment.NewLine +
                            "Ocity_GameAttacker_TrainingFight_Caravan_Restore").Translate()
                        : ("Ocity_GameAttacker_TrainingFight_Lost" + Environment.NewLine +
                            "Ocity_GameAttacker_TrainingFight_Caravan_Restore").Translate()
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
                ModBaseData.RunMainThreadSync(() =>
                {
                    //переделать карту в постоянную?
                    foreach (var pawn in GameMap.mapPawns.AllPawns.ToList())
                    {
                        //Loger.Log($"Client AttackerFinish {pawn.Label} CanHaveFaction {pawn.def.CanHaveFaction}, Humanlike {pawn.RaceProps.Humanlike}, Faction " + (pawn.Faction == null ? "null" : pawn.Faction.Name + " " + pawn.Faction.IsPlayer));
                        if (!pawn.def.CanHaveFaction || pawn.RaceProps.Humanlike || pawn.Faction == null || pawn.Faction.IsPlayer) continue;
                        //Loger.Log($"Client AttackerFinish SetFaction null {pawn.Label}");
                        pawn.SetFaction(null);
                    }
                });
            }
            else
            {
                //на этом месте у атакующего: из всех пешек что с краю создать караван, а карту удалить
                var listPawn = AttackerPawns.Keys
                    .Where(pawn =>
                        !pawn.Dead
                        && !pawn.Downed
                        && (pawn.Position.x < MapBorder || pawn.Position.x > GameMap.Size.x - MapBorder
                            || pawn.Position.z < MapBorder || pawn.Position.z > GameMap.Size.z - MapBorder))
                    .ToList();
                Caravan caravan = CaravanMaker.MakeCaravan(listPawn, Faction.OfPlayer, GameMap.Tile, false);
                Find.WorldObjects.Remove(GameMap.Parent);
                //добавляем пешки в мир, чтобы их не уничтожили
                foreach(var pawn in listPawn)
                {
                    if (!pawn.IsWorldPawn())
                    {
                        Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
                    }
                }
            }

            //автосейв с единым сохранением
            SessionClientController.SaveGameNow(true, () =>
            {
                GameUtils.ShowDialodOKCancel("OCity_GameAttacker_Dialog_Settlement_Attack".Translate()
                    , victoryAttacker
                        ? "OCity_GameAttacker_Settlement_TakenOver".Translate()
                        : ("OCity_GameAttacker_Defeated" + Environment.NewLine +
                            "OCity_GameAttacker_Colonist_Return").Translate()
                    , () => { }
                    , null
                );
            });

            Loger.Log("Client AttackerFinish end");
        }
    }
}
