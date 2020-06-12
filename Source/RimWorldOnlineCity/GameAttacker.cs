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
        /// Имена атакующих пешек, до того как они были отправлены, для проверки, что все они были созданы вновь
        /// </summary>
        private List<string> AttackerOriginalPawnLabels { get; set; }
        /// <summary>
        /// Команды отданые игроком для отправки
        /// </summary>
        private Dictionary<int, AttackPawnCommand> ToSendCommand { get; set; }

        /* NewCorpses
        /// <summary>
        /// Для пешек которым прикла команда на удаление идет задержка перед удалением на 1 цикл
        /// Если в этом же или следующем пакете придет кодманда создать труп из пешки, то команда станет не актуальной
        /// </summary>
        private List<Thing> DelayDestroyPawn { get; set; }
        */
        /// <summary>
        /// Проверить действительно ли объект должен быть удален, если нет, то запросить его снова.
        /// </summary>
        private Dictionary<int, Thing> CheckDestroy { get; set; }

        /// <summary>
        /// Проверить действительно ли объект должен быть удален, если нет, то запросить его снова.
        /// Для трупов не проверять
        /// </summary>
        private Dictionary<int, Thing> CheckSpawn { get; set; }

        /// <summary>
        /// Не пересылать дроп на сервер, напрмиер, когда происходит удаление
        /// </summary>
        private bool ThingDropIgnore { get; set; }

        private bool CheckSpawnDestroyDisable { get; set; }
        private Object CheckSpawnDestroySunc { get; set; } = new Object();

        /// <summary>
        /// Истина, когда атакующий сдается. Флаг для передачи хосту и начала завершения
        /// </summary>
        public bool VictoryHostToHost { get; set; }
        /// <summary>
        /// Произошла ужасная ошибка, и всё нужно отменить. Когда истина, перестаём что-либо делать и ждем от сервера команды на перезапуск
        /// </summary>
        public bool TerribleFatalError { get; set; }

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
            Find.TickManager.Pause();
            SessionClientController.Data.DontChactTimerFail = true;
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
                if (!string.IsNullOrEmpty(res.ErrorText))
                {
                    ErrorBreak(res.ErrorText);
                    return;
                }
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
                AttackerOriginalPawnLabels = pawnsToSend.Select(p => p.Name).ToList();
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
                Loger.Log($"Client StartCreateClearMap 0 TerrainCell={response.TerrainDefNameCell.Count} Thing={response.ThingCell.Count}");
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
                    //DelayDestroyPawn = new List<Thing>();   NewCorpses
                    CheckDestroy = new Dictionary<int, Thing>();
                    CheckSpawn = new Dictionary<int, Thing>();
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

        private Thing GetThingByHostId(int hostid)
        {
            Thing thing = null;
            int id;
            if (!ThingsIDDicRev.TryGetValue(hostid, out id))
            {
                Loger.Log("Client GetThingByHostId Err1 " + hostid.ToString());
                return null;
            }

            if (!ThingsObjDic.TryGetValue(id, out thing))
            {
                Loger.Log("Client GetThingByHostId Err2 " + hostid.ToString() + " id=" + id.ToString());
                return null;
            }
            if (thing == null)
            {
                Loger.Log("Client GetThingByHostId Err3 " + hostid.ToString() + " id=" + id.ToString());
            }
            return thing;
        }

        private void DestroyThing(Thing thing, int hostId = 0)
        {
            var id = thing.thingIDNumber;
            if (hostId != 0 || ThingsIDDic.TryGetValue(id, out hostId))
                ThingsIDDicRev.Remove(hostId);
            ThingsIDDic.Remove(id);
            ThingsObjDic.Remove(id);
            CheckDestroy.Remove(id);

            ModBaseData.RunMainThreadSync(() =>
            {
                try
                {
                    ThingDropIgnore = true;
                    thing.Destroy();
                }
                finally
                {
                    ThingDropIgnore = false;
                }
            });
        }

        /// <summary>
        /// Ошибка при создании карты
        /// </summary>
        /// <param name="msg"></param>
        private void ErrorBreak(string msg)
        {
            Loger.Log("Client GameAttack error " + msg);

            Clear();

            SessionClientController.Disconnected("OCity_GameAttacker_Dialog_ErrorMessage".Translate());
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
            try
            {
                ThingDropIgnore = true;
                caravan.RemoveAllPawns();
                if (caravan.Spawned)
                {
                    Find.WorldObjects.Remove(caravan);
                }
                foreach (var pair in select)
                {
                    GameUtils.PawnDestroy(pair);
                }
            }
            finally
            {
                ThingDropIgnore = false;
            }
            return sendThings;
        }

        private void AttackUpdate()
        {
            bool inTimerEvent = false;
            try
            {
                if (TerribleFatalError)
                {
                    Find.TickManager.Pause();
                    return;
                }

                //Scribe.ForceStop();
                if (!Find.TickManager.Paused)
                {
                    Find.TickManager.Pause();
                    GameUtils.ShowDialodOKCancel("OCity_GameAttacker_Dialog_Settlement_Attack".Translate()
                        , "OCity_GameAttacker_Main_Dialog".Translate() + Environment.NewLine
                            + "OCity_GameAttacker_Withdraw".Translate()
                        , () => { }
                        , null
                    );
                }

                if (InTimer) return;
                InTimer = true;
                AttackUpdateTick++;
                Loger.Log("Client AttackUpdate #" + AttackUpdateTick.ToString() + ".");

                if (AttackUpdateTick == 2)
                {
                    SessionClientController.Data.DontChactTimerFail = false;

                    //Убираем сообщения Открыта область
                    var findLabel = "LetterLabelAreaRevealed".Translate();
                    for (int i = 0; i < Find.LetterStack.LettersListForReading.Count; i++)
                    {
                        if (Find.LetterStack.LettersListForReading[i].label == findLabel)
                        {
                            Find.LetterStack.RemoveLetter(Find.LetterStack.LettersListForReading[i--]);
                        }
                    }
                    //проверяем, что все наши пешки на месте, если нет, то считаем, что загрузка прошла с ошибкой и вызываем отмену
                    var terribleFatalError = AttackerOriginalPawnLabels.Count != AttackerPawns.Count;
                    if (terribleFatalError)
                    {
                        Loger.Log($"Client AttackUpdate TerribleFatalError count: {AttackerOriginalPawnLabels.Count} != {AttackerPawns.Count}");
                    }
                    else
                    {
                        foreach (var pawnLabel in AttackerOriginalPawnLabels)
                        {
                            if (!AttackerPawns.Keys.Any(ap => ap.LabelCapNoCount == pawnLabel))
                            {
                                Loger.Log("Client AttackUpdate TerribleFatalError: " + pawnLabel);
                                terribleFatalError = true;
                                break;
                            }
                        }
                    }
                    if (terribleFatalError)
                    {
                        Loger.Log("Client AttackUpdate TerribleFatalError");
                        SessionClientController.Command((connect) =>
                        {
                            var toClient = connect.AttackOnlineInitiator(new AttackInitiatorToSrv()
                            {
                                State = 10,
                                TerribleFatalError = true,
                            });
                            TerribleFatalError = true;
                            Loger.Log("Client AttackUpdate TerribleFatalError end");
                        });
                        return;
                    }
                }

                if (AttackUpdateTick == 3)
                {
                    GameUtils.ShowDialodOKCancel("OCity_GameAttacker_Dialog_Settlement_Attack".Translate()
                        , "OCity_GameAttacker_Main_Preparation_Dialog".Translate() + Environment.NewLine
                            + "OCity_GameAttacker_Main_Dialog".Translate() + Environment.NewLine
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
                        //Loger.Log($"Client AttackUpdate 1");
                        //удаляем объекты которые созданы игрой а не нами
                        List<int> needNewThings = new List<int>();
                        CheckSpawnDestroyDisable = true;
                        lock (CheckSpawnDestroySunc)
                        {
                            foreach (var checkSpawn in CheckSpawn)
                            {
                                if (!ThingsIDDic.ContainsKey(checkSpawn.Key))
                                {
                                    Loger.Log("Client AttackUpdate 1 Destroy thing=" + checkSpawn.Value.Label + " ID=" + checkSpawn.Key);
                                    try
                                    {
                                        checkSpawn.Value.Destroy();
                                    }
                                    catch (Exception ext2)
                                    {
                                        Loger.Log("Client AttackUpdate Destroy Exception " + ext2.ToString());
                                    }
                                }
                            }
                            CheckSpawn.Clear();
                            //запрашиваем повторно объекты, которые были удалены игрой
                            foreach (var checkDestroy in CheckDestroy)
                            {
                                if (ThingsIDDic.TryGetValue(checkDestroy.Key, out int cdOutID))
                                {
                                    Loger.Log("Client AttackUpdate 1 Lost thing=" + checkDestroy.Value.Label + " ID=" + checkDestroy.Key);
                                    needNewThings.Add(cdOutID);
                                }
                            }
                            CheckDestroy.Clear();
                        }
                        CheckSpawnDestroyDisable = false;

                        //Loger.Log($"Client AttackUpdate 2 ({AttackUpdateTick})");
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
                            NeedNewThingIDs = needNewThings,
                        });

                        errNums += "3 ";
                        Action actUpdateState = () =>
                        {
                            if (toClient.UpdateState.Count > 0) Loger.Log("Client AttackUpdate 4. UpdateState=" + toClient.UpdateState.Count);

                            //Применение изменения местоположения и пр. по ID хоста
                            for (int i = 0; i < toClient.UpdateState.Count; i++)
                            {
                                Thing thing = GetThingByHostId(toClient.UpdateState[i].HostThingID);
                                if (thing == null) continue;

                                if (!(thing is Pawn)) Loger.Log("Client AttackUpdate 4 Apply " + toClient.UpdateState[i].ToString() + " thing=" + thing.Label + " ID=" + thing.thingIDNumber);
                                /*  NewCorpses
                                if (thing is Pawn && toClient.UpdateState[i].DownState == AttackThingState.PawnHealthState.Dead)
                                {
                                    DelayDestroyPawn.Remove(thing);
                                }
                                */
                                try
                                {
                                    ThingDropIgnore = true;
                                    GameUtils.ApplyState(thing, toClient.UpdateState[i]);
                                }
                                finally
                                {
                                    ThingDropIgnore = false;
                                }
                            }

                            /* NewCorpses
                            for (int i = 0; i < DelayDestroyPawn.Count; i++)
                            {
                                Thing thing = DelayDestroyPawn[i];
                                Loger.Log("Client AttackUpdate 4 DelayDestroyPawn thing=" + thing.Label + " ID=" + thing.thingIDNumber);
                                DestroyThing(thing);
                            }
                            DelayDestroyPawn.Clear();
                            */

                            for (int i = 0; i < toClient.Delete.Count; i++)
                            {
                                Thing thing = GetThingByHostId(toClient.Delete[i]);
                                if (thing == null) continue;
                                /* NewCorpses
                                if (thing is Pawn)
                                {
                                    Loger.Log("Client AttackUpdate 4 ToDelayDestroy " + toClient.Delete[i].ToString() + " thing=" + thing.Label + " ID=" + thing.thingIDNumber);
                                    DelayDestroyPawn.Add(thing);
                                }
                                else
                                */
                                {
                                    Loger.Log("Client AttackUpdate 4 Destroy " + toClient.Delete[i].ToString() + " thing=" + thing.Label + " ID=" + thing.thingIDNumber);
                                    DestroyThing(thing, toClient.Delete[i]);
                                }
                            }

                        };

                        errNums += "4 ";
                        if (toClient.NewPawns != null && toClient.NewCorpses != null && toClient.NewThings != null 
                            && (toClient.NewPawns.Count > 0 || toClient.NewCorpses.Count > 0 || toClient.NewThings.Count > 0))
                        {
                            //LongEventHandler.QueueLongEvent(delegate
                            {
                                try
                                {

                                    if (toClient.NewPawns.Count > 0)
                                    {
                                        Loger.Log("Client AttackUpdate 3. NewPawn=" + toClient.NewPawns.Count);
                                        errNums += "5 ";
                                        //удаляем пешки NewPawnsId (здесь список thingIDNumber от хоста), которые сейчас обновим
                                        for (int i = 0; i < toClient.NewPawnsId.Count; i++)
                                        {
                                            var hostid = toClient.NewPawnsId[i];
                                            Thing thing = GetThingByHostId(hostid);
                                            if (thing == null) continue;
                                            Loger.Log("Client AttackUpdate 3 DestroyPawnForUpdate " + hostid.ToString() + " pawn=" + thing.Label + " ID=" + thing.thingIDNumber);
                                            DestroyThing(thing, hostid);
                                        }

                                        errNums += "6 ";
                                        //создаем список пешек toClient.NewPawns
                                        GameUtils.SpawnList(GameMap, toClient.NewPawns, false
                                            , (p) => p.TransportID == 0 //если без нашего ID, то у нас как пират
                                            , (th, te) =>
                                            {
                                                if (CheckDestroy.ContainsKey(th.thingIDNumber)) CheckDestroy.Remove(th.thingIDNumber);
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

                                                        if (p.playerSettings != null) p.playerSettings.hostilityResponse = HostilityResponseMode.Ignore;
                                                        if (p.drafter != null) p.drafter.Drafted = true;
                                                        p.jobs.StartJob(new Job(JobDefOf.Wait_Combat)
                                                        {
                                                            playerForced = true,
                                                            expiryInterval = int.MaxValue,
                                                            checkOverrideOnExpire = false,
                                                        }
                                                            , JobCondition.InterruptForced);
                                                    }
                                                    else
                                                    {
                                                        //для нейтральных животных
                                                        //все созданые пешки или враги или игрока, но если у них нет признака TransportID, то делаем их нейтральными
                                                        //Loger.Log($"Client NewPawnsSpawnList {p.Label} CanHaveFaction {p.def.CanHaveFaction}, Humanlike {p.RaceProps.Humanlike}, Faction " + (p.Faction == null ? "null" : p.Faction.Name + " " + p.Faction.IsPlayer));
                                                        if (p.def.CanHaveFaction /*&& !p.RaceProps.Humanlike*/ && p.Faction != null && p.Faction.IsPlayer)
                                                        {
                                                            p.SetFaction(null);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    Loger.Log("Client AttackUpdate SpawnListPawn NotOrigID! " + " thing=" + th.Label + " ID=" + th.thingIDNumber);
                                                }
                                            });
                                    }

                                    if (toClient.NewCorpses.Count > 0)
                                    {
                                        Loger.Log("Client AttackUpdate 3. NewCorpses=" + toClient.NewCorpses.Count);
                                        errNums += "7 ";
                                        //удаляем пешку и труп NewCorpses (здесь список thingIDNumber от хоста и для того и для того), которые сейчас обновим
                                        for (int i = 0; i < toClient.NewCorpses.Count; i++)
                                        {
                                            var hostid = toClient.NewCorpses[i].PawnId;
                                            Thing thing = GetThingByHostId(hostid);
                                            if (thing == null) continue;

                                            Loger.Log("Client AttackUpdate 3 DestroyPawnCorpsForUpdate " + hostid.ToString() + " pawn=" + thing.Label + " ID=" + thing.thingIDNumber);
                                            DestroyThing(thing, hostid);
                                        }

                                        errNums += "7* ";
                                        for (int i = 0; i < toClient.NewCorpses.Count; i++)
                                        {
                                            var hostid = toClient.NewCorpses[i].CorpseId;
                                            Thing thing = GetThingByHostId(hostid);
                                            if (thing == null) continue;

                                            Loger.Log("Client AttackUpdate 3 Destroy " + hostid.ToString() + " thing=" + thing.Label + " ID=" + thing.thingIDNumber);
                                            DestroyThing(thing, hostid);                                            
                                        }

                                        errNums += "8 ";
                                        //создаем список трупов пешек toClient.NewCorpses
                                        GameUtils.SpawnList(GameMap, toClient.NewCorpses.Select(c => c.CorpseWithPawn).ToList(), false
                                            , (p) => p.TransportID == 0 //если без нашего ID, то у нас как пират
                                            , (th, te) =>
                                            {
                                                //мы спавним пешку у которой в здоровье "труп", поэтому фактически спавница труп, внутри которого пешка
                                                //ищим этот труп в CheckSpawn
                                                int corpsId;
                                                lock (CheckSpawnDestroySunc)
                                                {
                                                    corpsId = CheckSpawn.FirstOrDefault(cs => cs.Value is Corpse && (cs.Value as Corpse).InnerPawn.thingIDNumber == th.thingIDNumber).Key;
                                                    if (CheckDestroy.ContainsKey(th.thingIDNumber)) CheckDestroy.Remove(th.thingIDNumber);
                                                    if (CheckDestroy.ContainsKey(corpsId)) CheckDestroy.Remove(corpsId);
                                                }
                                                Loger.Log("Client AttackUpdate 3. NewCorpses " + th.Label + " ID=" + th.thingIDNumber + " corpsID=" + corpsId);
                                                //Дополнить словарь сопоставления ID (их из OriginalID, наш ID, а TransportID уже никому не нужен, т.к. пешки дропнуты и переозданы)
                                                if (te.OriginalID != 0 && corpsId != 0)
                                                {
                                                    ThingsIDDicRev[te.OriginalID] = corpsId;
                                                    ThingsIDDic[corpsId] = te.OriginalID;
                                                    ThingsObjDic[corpsId] = th;
                                                }
                                                else
                                                {
                                                    Loger.Log("Client AttackUpdate SpawnListPawnCorps NotOrigID! " + " thing=" + th.Label + " ID=" + th.thingIDNumber + " corpsID=" + corpsId);
                                                }
                                            });
                                    }

                                    errNums += "9 ";

                                    if (toClient.NewThings.Count > 0)
                                    {
                                        Loger.Log("Client AttackUpdate 3. NewThings=" + toClient.NewThings.Count);
                                        //удаляем вешь
                                        for (int i = 0; i < toClient.NewThingsId.Count; i++)
                                        {
                                            var hostid = toClient.NewThingsId[i];
                                            Thing thing = GetThingByHostId(hostid);
                                            if (thing == null) continue;

                                            Loger.Log("Client AttackUpdate 3 Destroy " + hostid.ToString() + " thing=" + thing.Label + " ID=" + thing.thingIDNumber);
                                            DestroyThing(thing, hostid);
                                        }

                                        //создаем вешь
                                        GameUtils.SpawnList(GameMap, toClient.NewThings, false, (p) => false
                                            , (th, te) =>
                                            {
                                                if (CheckDestroy.ContainsKey(th.thingIDNumber)) CheckDestroy.Remove(th.thingIDNumber);
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

                                    errNums += "10 ";
                                    actUpdateState();

                                    errNums += "11 ";
                                    Loger.Log("Client AttackUpdate 5");

                                    //после первого массового спавна всех пешек
                                    if (AttackUpdateTick == 1)
                                    {
                                        errNums += "12 ";
                                        ModBaseData.RunMainThreadSync(() =>
                                        {
                                            errNums += "13 ";
                                            //проверка обзора и переключение на карту
                                            FloodFillerFog.DebugRefogMap(GameMap);
                                            CameraJumper.TryJump(GameMap.Center, GameMap);
                                            errNums += "14 ";
                                        });
                                        errNums += "15 ";
                                        GameAttackTrigger_Patch.ActiveAttacker.Add(GameMap, this);
                                        errNums += "16 ";
                                    }
                                }
                                catch (Exception ext)
                                {
                                    Loger.Log("Client AttackUpdate SpawnListEvent Exception " + ext.ToString());
                                }
                                InTimer = false;
                            }//, "", false, null); //".."
                            errNums += "17 ";
                        }
                        else
                        {
                            errNums += "18 ";
                            actUpdateState();

                            InTimer = false;
                        }

                        errNums += "19 ";
                        //заканчиваем
                        if (toClient.Finishing) Finish(toClient.VictoryAttacker);
                        errNums += "20 ";

                    }
                    catch (Exception ext)
                    {
                        InTimer = false;
                        Loger.Log("Client AttackUpdate Command Exception " + ext.ToString() + " ErrNums:" + errNums);
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
                            //если не нашли в словаре, то предполагаем, что ссылка на вешь на нас, для этого запоминаем defName
                            comm.TargetDefName = job.targetA.Thing.def.defName;
                        }
                        else
                        {
                            comm.TargetID = tid;
                            Loger.Log($"AttackUpdate UIEventNewJob {job.targetA.Thing.Label} ThingsIDDic({job.targetA.Thing.thingIDNumber}, {tid}) " +
                                $"ThingsObjDic({job.targetA.Thing.thingIDNumber}, {ThingsObjDic[job.targetA.Thing.thingIDNumber]})");
                        }
                    }
                    else
                        comm.TargetPos = new IntVec3S(job.targetA.Cell);

                    Loger.Log("AttackUpdate UIEventNewJob Command:" + job.def.defName
                        + " targetA:" + (job.targetA == null ? "null" : job.targetA.ToString() + (job.targetA.HasThing ? "-" + job.targetA.Thing.def.defName + "-" + job.targetA.Thing.Label : ""))
                        + " targetB:" + (job.targetB == null ? "null" : job.targetB.ToString() + (job.targetB.HasThing ? "-" + job.targetB.Thing.def.defName + "-" + job.targetB.Thing.Label : ""))
                        + " targetC:" + (job.targetC == null ? "null" : job.targetC.ToString() + (job.targetC.HasThing ? "-" + job.targetC.Thing.def.defName + "-" + job.targetC.Thing.Label : ""))
                        + " TargetID:" + comm.TargetID
                        + " TargetDefName:" + comm.TargetDefName
                        + " pawn:" + pawn.Label);

                    if (job.def == JobDefOf.Goto) comm.Command = AttackPawnCommand.PawnCommand.Goto;
                    else if (job.def == JobDefOf.AttackStatic) comm.Command = AttackPawnCommand.PawnCommand.Attack;
                    else if (job.def == JobDefOf.AttackMelee) comm.Command = AttackPawnCommand.PawnCommand.AttackMelee;
                    else if (job.def == JobDefOf.Equip) comm.Command = AttackPawnCommand.PawnCommand.Equip;
                    else if (job.def == JobDefOf.TakeInventory) comm.Command = AttackPawnCommand.PawnCommand.TakeInventory;
                    else if (job.def == JobDefOf.Wear) comm.Command = AttackPawnCommand.PawnCommand.Wear;
                    else if (job.def == JobDefOf.DropEquipment) comm.Command = AttackPawnCommand.PawnCommand.DropEquipment;
                    else if (job.def == JobDefOf.RemoveApparel) comm.Command = AttackPawnCommand.PawnCommand.RemoveApparel;
                    else if (job.def == JobDefOf.Ingest) comm.Command = AttackPawnCommand.PawnCommand.Ingest;
                    else if (job.def == JobDefOf.Strip) comm.Command = AttackPawnCommand.PawnCommand.Strip;
                    else if (job.def == JobDefOf.TendPatient) comm.Command = AttackPawnCommand.PawnCommand.TendPatient;
                    else
                    {
                        return; //левые команды игнорятся, но перед ними идет отмена предыдущего с job == null
                        //comm.Command = AttackPawnCommand.PawnCommand.Wait_Combat;
                    }
                }
                else
                {
                    return;
                    //comm.Command = AttackPawnCommand.PawnCommand.Wait_Combat;
                }

                ToSendCommand[id] = comm;
            }
            catch (Exception exp)
            {
                Loger.Log("AttackUpdate UIEventNewJob " + exp.ToString());
            }
        }

        /// <summary>
        /// Команда выложить из инвентаря. При false отменить действие
        /// </summary>
        public bool UIEventInventoryDrop(Thing thing)
        {
            Loger.Log("AttackerUpdate UIEventInventoryDrop "
                + thing.GetType().ToString() + " " + thing.Label + " id=" + thing.thingIDNumber
                + (thing is Corpse ? " Corpse " + (thing as Corpse).InnerPawn.thingIDNumber.ToString() : ""));

            try
            {
                var pawn = (thing.ParentHolder as Pawn_InventoryTracker)?.pawn;
                if (pawn == null) return true;

                int id;
                if (!AttackerPawns.TryGetValue(pawn, out id))
                {
                    return true;
                }

                if (ToSendCommand.TryGetValue(id, out var comm0) && comm0.Command == AttackPawnCommand.PawnCommand.OC_InventoryDrop)
                {
                    Loger.Log("AttackerUpdate UIEventInventoryDrop last InventoryDrop not send");
                    return false;
                }

                var comm = new AttackPawnCommand()
                {
                    HostPawnID = id
                };
                comm.TargetDefName = thing.def.defName;
                comm.Command = AttackPawnCommand.PawnCommand.OC_InventoryDrop;

                ToSendCommand[id] = comm;
            }
            catch (Exception exp)
            {
                Loger.Log("AttackUpdate UIEventInventoryDrop " + exp.ToString());
            }
            return true;
        }

        /// <summary>
        /// Перехват события когда что-то было уничтожено, получило повреждения или только что создано
        /// </summary>
        public void UIEventChange(Thing thing, bool distroy = false, bool newSpawn = false)
        {
            //if (InTimer) return;
            if (/*PlantNotSend &&*/
                                thing is Plant && !thing.def.plant.IsTree) return;

            Loger.Log("AttackerUpdate UIEventChange " + (CheckSpawnDestroyDisable ? "OFF " : "")
                + thing.GetType().ToString() + " " + thing.Label + " id=" + thing.thingIDNumber
                + (distroy ? " distroy!" : "")
                + (newSpawn ? " newSpawn!" : "")
                + (thing is Corpse ? " Corpse " + (thing as Corpse).InnerPawn.thingIDNumber.ToString() : ""));

            if (CheckSpawnDestroyDisable) return;

            lock (CheckSpawnDestroySunc)
            {
                if (distroy)
                {
                    CheckDestroy[thing.thingIDNumber] = thing;
                    CheckSpawn.Remove(thing.thingIDNumber);
                }
                else if (newSpawn)
                {
                    CheckSpawn[thing.thingIDNumber] = thing;
                    CheckDestroy.Remove(thing.thingIDNumber);
                }
            }
        }

        private void CreateClearMap(int tile, IntVec3 mapSize, Action<Map, MapParent> ready)
        {
            LongEventHandler.QueueLongEvent(delegate
            {
                Loger.Log("Client CreateClearMap 1");
                try
                {
                    Rand.PushState();
                    try
                    {
                        TileFinder_IsValidTileForNewSettlement_Patch.Off = true;
                        int seed = Gen.HashCombineInt(Find.World.info.Seed, tile); //todo по идеи не нужен
                        Rand.Seed = seed;

                        Loger.Log("Client CreateClearMap 2");
                        var mapParent = (MapParent)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Ambush); //WorldObjectDefOf.Ambush  WorldObjectDefOf.AttackedNonPlayerCaravan
                        mapParent.Tile = tile;
                        Loger.Log("Client CreateClearMap 3");
                        Find.WorldObjects.Add(mapParent);
                        mapParent.SetFaction(Find.FactionManager.OfPlayer);
                        Loger.Log("Client CreateClearMap 4");
                        Map map;
                        try
                        {
                            Loger.Log("Client CreateClearMap 4 " + mapParent.MapGeneratorDef.genSteps.Count
                                + mapParent.MapGeneratorDef.genSteps.Aggregate("", (r, i) => r + Environment.NewLine + " " + i.defName + " " + i.ToString()));
                            //отключаем заполнение содержимым карты
                            //MapGenerator_GenerateContentsIntoMap_Patch.Disable = true;
                            var mapSet = new MapGeneratorDef()
                            {
                                genSteps = mapParent.MapGeneratorDef.genSteps
                                    .Where(gs => gs.defName == "ElevationFertility"
                                        || gs.defName == "Caves"
                                        || gs.defName == "Terrain"
                                        || gs.defName == "CavesTerrain"
                                        || gs.defName == "FindPlayerStartSpot"
                                        || gs.defName == "ScenParts"
                                        || gs.defName == "Fog") //Animals
                                    .ToList()
                            };
                            //создаем карту
                            map = MapGenerator.GenerateMap(mapSize, mapParent, mapSet, null, null);
                        }
                        finally
                        {
                            //MapGenerator_GenerateContentsIntoMap_Patch.Disable = false;
                        }
                        Loger.Log("Client CreateClearMap 5");

                        LongEventHandler.QueueLongEvent(delegate
                        {
                            try
                            { 
                                Loger.Log("Client CreateClearMap 6");
                                CellRect cellRect = CellRect.WholeMap(map);
                                cellRect.ClipInsideMap(map);

                                Loger.Log("Client CreateClearMap 7");
                                GenDebug.ClearArea(cellRect, map);

                                Loger.Log("Client CreateClearMap 8");
                                ready(map, mapParent);

                                TileFinder_IsValidTileForNewSettlement_Patch.Off = false;
                            }
                            catch (Exception exp)
                            {
                                Loger.Log("Client CreateClearMap Event Exception: " + exp.ToString());
                                ErrorBreak("Error CreateClearMap2");
                            }
                        }, "GeneratingMapForNewEncounter", false, null);
                    }
                    finally
                    {
                        Rand.PopState();
                    }
                }
                catch (Exception exp)
                {
                    Loger.Log("Client CreateClearMap Exception: " + exp.ToString());
                    ErrorBreak("Error CreateClearMap1");
                }
            }, "GeneratingMapForNewEncounter", false, null);
        }

        /// <summary>B
        /// Принудительная остановка режима pvp
        /// </summary>
        public void Clear()
        {
            //отключить таймер и признаки, что нас атакуют
            if (TimerObj != null) SessionClientController.Timers.Remove(TimerObj);
            if (GameMap != null) GameAttackTrigger_Patch.ActiveAttacker.Remove(GameMap);
            SessionClientController.Data.AttackModule = null;
            SessionClientController.Data.BackgroundSaveGameOff = false;
            SessionClientController.Data.DontChactTimerFail = false;
            GameAttackTrigger_Patch.ForceSpeed = -1f;
        }

        public void Finish(bool victoryAttacker)
        {
            Find.TickManager.Pause();
            Loger.Log("Client AttackerFinish");

            Clear();

            if (TestMode)
            {
                GameUtils.ShowDialodOKCancel("OCity_GameAttacker_Dialog_Settlement_Attack".Translate()
                    , victoryAttacker
                        ? "Ocity_GameAttacker_TrainingFight_Won".Translate() + Environment.NewLine +
                            "Ocity_GameAttacker_TrainingFight_Caravan_Restore".Translate()
                        : "Ocity_GameAttacker_TrainingFight_Lost".Translate() + Environment.NewLine +
                            "Ocity_GameAttacker_TrainingFight_Caravan_Restore".Translate()
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
                    //переделать карту в постоянную

                    //переводим всех врагов в нейтральные, кроме людй (Humanlike)
                    var mapPawnsA = new Pawn[GameMap.mapPawns.AllPawnsSpawned.Count];
                    GameMap.mapPawns.AllPawnsSpawned.CopyTo(mapPawnsA);
                    foreach (var pawn in mapPawnsA)
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
                        && (pawn.Position.x < MapBorder || pawn.Position.x > GameMap.Size.x - 1 - MapBorder
                            || pawn.Position.z < MapBorder || pawn.Position.z > GameMap.Size.z - 1 - MapBorder))
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
                        : "OCity_GameAttacker_Defeated".Translate() + Environment.NewLine +
                            "OCity_GameAttacker_Colonist_Return".Translate()
                    , () => { }
                    , null
                );
            });

            Loger.Log("Client AttackerFinish end");
        }
    }
}
