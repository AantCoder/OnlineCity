using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model;
using OCUnion;
using OCUnion.Transfer.Model;
using Transfer;

namespace ServerOnlineCity.Model
{
    public class AttackServer
    {
        /*
         *      
         * А0: 0 в 1 Атакующий пересылает айди объектов
         * А1: Атакующий посылает запрос пока статус не придет 2
         * Н2: 1 в 2 Хосту приходит сигнал об атаки и он запрашивает айди 
         * А2: Посылает атакующих пешек
         * А3: Посылает запрос пока статус не придет больше 3. Ответ со статусом 4 содержит данные для создания карты. Создает карту вообще без пешек и запускает таймер
         * Н4: 2 в 4 Посылает данные для создания карты
         * Н5: 4 в 5 Считывает атакующих пешек, повторяет пока не придут данные пешек. После создает пешек и запускает таймер
        */
        public int State { get; set; }

        public bool TestMode { get; set; }

        public PlayerServer Attacker { get; set; }

        public PlayerServer Host { get; set; }

        /// <summary>
        /// Признак победы. Вычисляется и принимается от Хоста. После передачи этого значение Атакующему вызывается завершение.
        /// </summary>
        public bool? VictoryAttacker { get; set; }

        public long HostPlaceServerId { get; set; }

        public long InitiatorPlaceServerId { get; set; }
        public int InitiatorPlaceTile { get; set; }

        public IntVec3S MapSize { get; set; }
        public List<ThingEntry> Pawns { get; set; }
        public List<IntVec3S> TerrainDefNameCell { get; set; }
        public List<string> TerrainDefName { get; set; }
        public List<ThingTrade> Thing { get; set; }
        public List<IntVec3S> ThingCell { get; set; }

        public List<ThingEntry> NewPawns { get; set; }
        public List<int> NewPawnsId { get; set; }
        public List<ThingTrade> NewThings { get; set; }
        public List<int> NewThingsId { get; set; }
        public List<AttackCorpse> NewCorpses { get; set; }
        public List<int> Delete { get; set; }
        public Dictionary<int, AttackThingState> UpdateState { get; set; }
        public Dictionary<int, AttackPawnCommand> UpdateCommand { get; set; }
        public HashSet<int> NeedNewThingIDs { get; set; }

        public DateTime? SetPauseOnTimeToHost { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime CreateTime { get; set; }
        public bool VictoryHostToHost { get; set; }
        public bool TerribleFatalError { get; set; }
        public long AttackUpdateTick { get; set; }

        private object SyncObj = new Object();

        public string New(PlayerServer player, PlayerServer hostPlayer, AttackInitiatorToSrv fromClient, bool testMode)
        {
            if (!player.Online || !hostPlayer.Online)
            {
                Loger.Log($"Server AttackServer {Attacker.Public.Login} -> {Host.Public.Login} canceled: Attack not possible: player offline");
                return "Attack not possible: player offline";
            }
            var err = AttackUtils.CheckPossibilityAttack(player, hostPlayer, fromClient.InitiatorPlaceServerId, fromClient.HostPlaceServerId);
            if (err != null)
            {
                Loger.Log($"Server AttackServer {Attacker.Public.Login} -> {Host.Public.Login} canceled: {err}");
                return err;
            }

            TestMode = testMode;
            Attacker = player;
            Host = hostPlayer;
            Attacker.AttackData = this;
            Host.AttackData = this;
            HostPlaceServerId = fromClient.HostPlaceServerId;
            InitiatorPlaceServerId = fromClient.InitiatorPlaceServerId;
            var data = Repository.GetData;
            var woip = data.WorldObjects.FirstOrDefault(wo => wo.ServerId == InitiatorPlaceServerId);
            if (woip != null) InitiatorPlaceTile = woip.Tile;

            NewPawns = new List<ThingEntry>();
            NewPawnsId = new List<int>();
            NewThings = new List<ThingTrade>();
            NewThingsId = new List<int>();
            NewCorpses = new List<AttackCorpse>();
            Delete = new List<int>();
            UpdateState = new Dictionary<int, AttackThingState>();
            UpdateCommand = new Dictionary<int, AttackPawnCommand>();
            NeedNewThingIDs = new HashSet<int>();

            CreateTime = DateTime.UtcNow;
            AttackUpdateTick = 0;

            if (!TestMode)
            {
                Host.Public.LastPVPTime = DateTime.UtcNow;
            }

            Loger.Log($"Server AttackServer {Attacker.Public.Login} -> {Host.Public.Login} New");
            return null;
        }
        
        public AttackHostFromSrv RequestHost(AttackHostToSrv fromClient)
        {
            //Loger.Log($"Server AttackOnlineHost RequestHost State: {State} -> {fromClient.State}");
            lock (SyncObj)
            {
                //первые 5 минут не проверяем на отключения, т.к. загрузка может быть долгой (а дисконектит уже после 10 сек)
                if ((fromClient.State == 10 || (DateTime.UtcNow - CreateTime).TotalSeconds > 8*60)
                    && CheckConnect(false))
                {
                    return new AttackHostFromSrv()
                    {
                        State = State
                    };
                }
                if (fromClient.State < State)
                {
                    return new AttackHostFromSrv()
                    {
                        ErrorText = "Unexpected request " + fromClient.State.ToString() + ". Was expected" + State.ToString()
                    };
                }

                if (fromClient.State == 2)
                {
                    State = 2;
                    return new AttackHostFromSrv()
                    {
                        State = State,
                        HostPlaceServerId = HostPlaceServerId,
                        InitiatorPlaceServerId = InitiatorPlaceServerId,
                        StartInitiatorPlayer = Attacker.Public.Login,
                        TestMode = TestMode,
                    };
                }

                if (fromClient.State == 4)
                {
                    MapSize = fromClient.MapSize;
                    TerrainDefNameCell = fromClient.TerrainDefNameCell;
                    TerrainDefName = fromClient.TerrainDefName;
                    Thing = fromClient.Thing;
                    ThingCell = fromClient.ThingCell;
                    State = 4;
                    return new AttackHostFromSrv()
                    {
                        State = State,
                    };
                }

                if (fromClient.State == 5)
                {
                    State = 5;
                    return new AttackHostFromSrv()
                    {
                        State = State,
                        Pawns = Pawns
                    };
                }

                if (fromClient.State == 10)
                {
                    State = 10;

                    if (VictoryAttacker == null) VictoryAttacker = fromClient.VictoryAttacker;

                    if (fromClient.NewPawnsId.Count > 0
                        || fromClient.NewThingsId.Count > 0
                        || fromClient.NewCorpses.Count > 0
                        || fromClient.Delete.Count > 0)
                    {
                        //удаляем из Delete если сейчас команда добавить с таким id
                        foreach (var n in fromClient.NewPawnsId)
                        {
                            var index = Delete.IndexOf(n);
                            if (index >= 0) Delete.RemoveAt(index);
                        }
                        foreach (var n in fromClient.NewThingsId)
                        {
                            var index = Delete.IndexOf(n);
                            if (index >= 0) Delete.RemoveAt(index);
                        }
                        foreach (var corps in fromClient.NewCorpses)
                        {
                            var index = Delete.IndexOf(corps.CorpseId);
                            if (index >= 0) Delete.RemoveAt(index);
                        }

                        //объединяем
                        for (int i = 0; i < fromClient.NewPawnsId.Count; i++)
                        {
                            if (NewPawnsId.Contains(fromClient.NewPawnsId[i])) continue;
                            NewPawnsId.Add(fromClient.NewPawnsId[i]);
                            NewPawns.Add(fromClient.NewPawns[i]);
                        }
                        for (int i = 0; i < fromClient.NewThingsId.Count; i++)
                        {
                            if (NewThingsId.Contains(fromClient.NewThingsId[i])) continue;
                            NewThingsId.Add(fromClient.NewThingsId[i]);
                            NewThings.Add(fromClient.NewThings[i]);
                        }
                        for (int i = 0; i < NewCorpses.Count; i++)
                        {
                            if (fromClient.NewCorpses.Any(c => c.PawnId == NewCorpses[i].PawnId || c.CorpseId == NewCorpses[i].CorpseId))
                            {
                                NewCorpses.RemoveAt(i--);
                            }
                        }
                        for (int i = 0; i < fromClient.NewCorpses.Count; i++)
                        {
                            NewCorpses.Add(fromClient.NewCorpses[i]);
                        }
                        for (int i = 0; i < fromClient.Delete.Count; i++)
                        {
                            if (Delete.Contains(fromClient.Delete[i])) continue;
                            Delete.Add(fromClient.Delete[i]);
                        }
                        for (int i = 0; i < fromClient.NewCorpses.Count; i++)
                        {
                            if (Delete.Contains(fromClient.NewCorpses[i].PawnId)) continue;
                            Delete.Add(fromClient.NewCorpses[i].PawnId);
                        }

                        //на всякий случай корректируем: удаляем из добавляемых те, что на удаление
                        foreach (var n in Delete)
                        {
                            var index = NewPawnsId.IndexOf(n);
                            if (index >= 0)
                            {
                                NewPawnsId.RemoveAt(index);
                                NewPawns.RemoveAt(index);
                            }
                            index = NewThingsId.IndexOf(n);
                            if (index >= 0)
                            {
                                NewThingsId.RemoveAt(index);
                                NewThings.RemoveAt(index);
                            }

                            for (int i = 0; i < NewCorpses.Count; i++)
                            {
                                if (n == NewCorpses[i].CorpseId)
                                {
                                    NewCorpses.RemoveAt(i--);
                                }
                            }
                        }
                    }


                    if (fromClient.UpdateState.Count > 0)
                    {
                        //объединяем
                        for (int i = 0; i < fromClient.UpdateState.Count; i++)
                        {
                            var id = fromClient.UpdateState[i].HostThingID;
                            UpdateState[id] = fromClient.UpdateState[i];
                        }
                    }

                    var res = new AttackHostFromSrv()
                    {
                        State = State,
                        UpdateCommand = UpdateCommand.Values.ToList(),
                        NeedNewThingIDs = NeedNewThingIDs.ToList(),
                        SetPauseOnTime = SetPauseOnTimeToHost == null ? DateTime.MinValue : SetPauseOnTimeToHost.Value,
                        VictoryHost = VictoryHostToHost,
                        TerribleFatalError = TerribleFatalError,
                    };

                    UpdateCommand = new Dictionary<int, AttackPawnCommand>();
                    NeedNewThingIDs = new HashSet<int>();

                    if (SetPauseOnTimeToHost != null) Loger.Log("Server Send SetPauseOnTimeToHost=" + SetPauseOnTimeToHost.Value.ToGoodUtcString());

                    Host.PVPHostLastTime = DateTime.UtcNow;
                    SetPauseOnTimeToHost = null;
                    VictoryHostToHost = false;
                    return res;
                }

                return new AttackHostFromSrv()
                {
                    ErrorText = "Unexpected request " + fromClient.State.ToString() + "! Was expected" + State.ToString()
                };
            }
        }

        public AttackInitiatorFromSrv RequestInitiator(AttackInitiatorToSrv fromClient)
        {
            lock (SyncObj)
            {
                //первые 5 минут не проверяем на отключения, т.к. загрузка может быть долгой (а дисконектит уже после 10 сек)
                if ((fromClient.State == 10 || (DateTime.UtcNow - CreateTime).TotalSeconds > 8 * 60)
                    && CheckConnect(true))
                { 
                    return new AttackInitiatorFromSrv()
                    {
                        State = State
                    };
                }
                if (fromClient.State == 0 && fromClient.StartHostPlayer != null)
                {
                    State = 1;
                    TestMode = fromClient.TestMode;
                    return new AttackInitiatorFromSrv()
                    {
                        State = State
                    };
                }
                if (fromClient.State == 1)
                {
                    return new AttackInitiatorFromSrv()
                    {
                        State = State
                    };
                }
                if (fromClient.State == 2 && State >= 2 && fromClient.Pawns != null && fromClient.Pawns.Count > 0)
                {
                    Pawns = fromClient.Pawns;
                    //После передачи своих пешек, в ответ передаются данные карты и начинается её длительное создание
                    //в это время включаем на хосте обязательную паузу
                    //После загрузки пауза обновлется на 1 минуту, чтобы атакующий огляделся (ищи SetPauseOnTimeToHost в GameAttacker)
                    SetPauseOnTimeToHost = DateTime.UtcNow.AddMinutes(8);
                    Loger.Log("Server Set 1 SetPauseOnTimeToHost=" + SetPauseOnTimeToHost.Value.ToGoodUtcString());

                    return new AttackInitiatorFromSrv()
                    {
                        State = State,
                        TestMode = TestMode,
                    };
                }
                if (fromClient.State == 3 && State >= 3)
                {
                    return new AttackInitiatorFromSrv()
                    {
                        State = State,
                        MapSize = MapSize,
                        TerrainDefNameCell = TerrainDefNameCell,
                        TerrainDefName = TerrainDefName,
                        Thing = Thing,
                        ThingCell = ThingCell,
                    };
                }

                if (fromClient.State == 10 && State < 10)
                {
                    return new AttackInitiatorFromSrv()
                    {
                        State = State,
                    };
                }
                if (fromClient.State == 10)
                {
                    AttackUpdateTick++;

                    if (StartTime == DateTime.MinValue)
                    {
                        Loger.Log($"Server AttackServer {Attacker.Public.Login} -> {Host.Public.Login} Start");
                        StartTime = DateTime.UtcNow;
                    }

                    if (fromClient.VictoryHostToHost)
                    {
                        VictoryHostToHost = fromClient.VictoryHostToHost;
                    }

                    if (fromClient.TerribleFatalError)
                    {
                        Loger.Log($"Server AttackServer TerribleFatalError {AttackUpdateTick}");
                        if (AttackUpdateTick != 2)
                        {
                            //такая ошибка с отменой атаки возможна только на 2 обновлении (на 1 создаются все пешки, на 2 проверка, что все переданы)
                            //если это не 2, то считаем, что атакующий жульничает
                            VictoryHostToHost = true;
                        }
                        else
                        {
                            TerribleFatalError = fromClient.TerribleFatalError;

                            SendAttackCancel();
                            Finish();
                        }
                    }

                    if (fromClient.SetPauseOnTimeToHost != TimeSpan.MinValue)
                    {
                        SetPauseOnTimeToHost = DateTime.UtcNow + fromClient.SetPauseOnTimeToHost;
                        Loger.Log("Server Set 2 SetPauseOnTimeToHost=" + SetPauseOnTimeToHost.Value.ToGoodUtcString());
                    }

                    if (fromClient.UpdateCommand != null && fromClient.UpdateCommand.Count > 0)
                    {
                        //объединяем
                        for (int i = 0; i < fromClient.UpdateCommand.Count; i++)
                        {
                            var id = fromClient.UpdateCommand[i].HostPawnID;
                            UpdateCommand[id] = fromClient.UpdateCommand[i];
                        }
                    }

                    if (fromClient.NeedNewThingIDs != null && fromClient.NeedNewThingIDs.Count > 0)
                    {
                        //объединяем
                        for (int i = 0; i < fromClient.NeedNewThingIDs.Count; i++)
                        {
                            NeedNewThingIDs.Add(fromClient.NeedNewThingIDs[i]);
                        }
                    }

                    var res = new AttackInitiatorFromSrv()
                    {
                        State = State,
                        NewPawns = NewPawns,
                        NewPawnsId = NewPawnsId,
                        NewThings = NewThings,
                        NewThingsId = NewThingsId,
                        NewCorpses = NewCorpses,
                        Delete = Delete,
                        UpdateState = UpdateState.Values.ToList(),
                        Finishing = VictoryAttacker != null,
                        VictoryAttacker = VictoryAttacker != null ? VictoryAttacker.Value : false,
                    };
                    NewPawns = new List<ThingEntry>();
                    NewPawnsId = new List<int>();
                    NewThings = new List<ThingTrade>();
                    NewThingsId = new List<int>();
                    NewCorpses = new List<AttackCorpse>();
                    Delete = new List<int>();
                    UpdateState = new Dictionary<int, AttackThingState>();

                    if (VictoryAttacker != null) Finish();

                    return res;
                }

                return new AttackInitiatorFromSrv()
                {
                    ErrorText = "Unexpected request " + fromClient.State.ToString() + "! Was expected" + State.ToString()
                };
            }
        }

        private void SendAttackCancel()
        {
            var data = Repository.GetData;
            //команда хосту
            var packet = new ModelMailTrade()
            {
                Type = ModelMailTradeType.AttackCancel,
                From = data.PlayersAll[0].Public,
                To = Host.Public,
            };
            lock (Host)
            {
                Host.Mails.Add(packet);
            }
            //команда атакующему
            packet = new ModelMailTrade()
            {
                Type = ModelMailTradeType.AttackCancel,
                From = data.PlayersAll[0].Public,
                To = Attacker.Public,
            };
            lock (Attacker)
            {
                Attacker.Mails.Add(packet);
            }
        }

        /// <summary>
        /// Проверка и действия при сбоях.
        /// Если отключился атакующий до State == 10 или меньше 1 мин, то отмена.
        /// Если отключился атакующий при State == 10 и больше 1 мин, то уничтожение каравана, поселение остается как есть.
        /// Если отключился хост до State == 10, то уничтожение поселения хоста и отмена у атакующего.
        /// Если отключился хост после State == 10, то уничтожение поселения хоста и поселение переходит к атакующему.
        /// </summary>
        /// <param name="attacker">Это запрос атакующего, иначе хоста</param>
        /// <returns></returns>
        private bool CheckConnect(bool attacker)
        {
            if (!TestMode)
            {
                Host.Public.LastPVPTime = DateTime.UtcNow;
            }

            bool fail = false;
            bool asTestMode = TestMode;
            string logDet;
            if (attacker)
            {
                logDet = " (is attacker)";
                fail = !Host.Online;
            }
            else
            {
                logDet = " (is host)";
                //атакующий может подвиснуть при создании карты, но когда максимум ожидания (=пауза у хоста) кончилось, то всё равно проверяем
                if (StartTime != DateTime.MinValue
                    || SetPauseOnTimeToHost != DateTime.MinValue && SetPauseOnTimeToHost < DateTime.UtcNow)
                {
                    fail = !Attacker.Online;
                }
            }
            if (!fail)
            {
                if (State < 10 && (DateTime.UtcNow - CreateTime).TotalSeconds > 8 * 60)
                {
                    fail = true;
                    asTestMode = true;
                    logDet = " Loading too long.";
                }
            }

            if (fail)
            {
                var data = Repository.GetData;

                if (asTestMode)
                {   //При тестовом режиме всех участников отключаем
                    if (TestMode) Loger.Log("Server AttackServer Fail and back. TestMode" + logDet);
                    else Loger.Log("Server AttackServer Fail and back. " + logDet);

                    SendAttackCancel();
                }
                else if (!attacker)
                {   //Если отключился атакующий
                    //до State == 10 или меньше 1 мин, то отмена 
                    if (StartTime == DateTime.MinValue || (DateTime.UtcNow - StartTime).TotalSeconds < 60) // State == 10 проверяется косвенно: StartTime устанавливается только при State == 10
                    {
                        Loger.Log("Server AttackServer Fail attacker off in start" + logDet);

                        SendAttackCancel();
                    }
                    //при State == 10 и больше 1 мин, то уничтожение каравана, поселение остается как есть.
                    else
                    {
                        Loger.Log("Server AttackServer Fail attacker off in progress" + logDet);

                        //команда хосту
                        var packet = new ModelMailTrade()
                        {
                            Type = ModelMailTradeType.AttackTechnicalVictory,
                            From = data.PlayersAll[0].Public,
                            To = Host.Public,
                        };
                        lock (Host)
                        {
                            Host.Mails.Add(packet);
                        }
                        //команда атакующему
                        packet = new ModelMailTrade()
                        {
                            Type = ModelMailTradeType.AttackCancel,
                            From = data.PlayersAll[0].Public,
                            To = Attacker.Public,
                        };
                        var packet2 = new ModelMailTrade()
                        {
                            Type = ModelMailTradeType.DeleteByServerId,
                            From = data.PlayersAll[0].Public,
                            To = Attacker.Public,
                            PlaceServerId = InitiatorPlaceServerId,
                            Tile = InitiatorPlaceTile,
                        };
                        lock (Attacker)
                        {
                            Attacker.Mails.Add(packet);
                            Attacker.Mails.Add(packet2);
                        }
                    }
                }
                else
                {   //Если отключился хост
                    //то уничтожение поселения хоста
                    var packet1 = new ModelMailTrade()
                    {
                        Type = ModelMailTradeType.AttackCancel,
                        From = data.PlayersAll[0].Public,
                        To = Host.Public,
                    };
                    var packet2 = new ModelMailTrade()
                    {
                        Type = ModelMailTradeType.DeleteByServerId,
                        From = data.PlayersAll[0].Public,
                        To = Host.Public,
                        PlaceServerId = HostPlaceServerId,
                    };
                    lock (Host)
                    {
                        Host.Mails.Add(packet1);
                        Host.Mails.Add(packet2);
                    }

                    //до State == 10, отмена у атакующего
                    if (StartTime == DateTime.MinValue) // State == 10 проверяется косвенно: StartTime устанавливается только при State == 10
                    {
                        Loger.Log("Server AttackServer Fail host off in start" + logDet);

                        //команда атакующему
                        var packet = new ModelMailTrade()
                        {
                            Type = ModelMailTradeType.AttackCancel,
                            From = data.PlayersAll[0].Public,
                            To = Attacker.Public,
                        };
                        lock (Attacker)
                        {
                            Attacker.Mails.Add(packet);
                        }
                    }
                    //после State == 10, поселение переходит к атакующему
                    else
                    {
                        Loger.Log("Server AttackServer Fail host off in progress" + logDet);

                        //команда атакующему
                        var packet = new ModelMailTrade()
                        {
                            Type = ModelMailTradeType.AttackTechnicalVictory,
                            From = data.PlayersAll[0].Public,
                            To = Attacker.Public,
                        };
                        lock (Attacker)
                        {
                            Attacker.Mails.Add(packet);
                        }
                    }

                }
                Finish();
            }

            return fail;
        }

        public void Finish()
        {
            Loger.Log($"Server AttackServer {Attacker.Public.Login} -> {Host.Public.Login} Finish StartTime sec = " 
                + (StartTime == DateTime.MinValue ? "-" : (DateTime.UtcNow - StartTime).TotalSeconds.ToString())
                + (VictoryAttacker == null ? "" : VictoryAttacker.Value ? " VictoryAttacker" : " VictoryHost")
                + (TestMode ? " TestMode" : "")
                + (TerribleFatalError ? " TerribleFatalError" : ""));
            Attacker.AttackData = null;
            Host.AttackData = null;
        }
    }
}
