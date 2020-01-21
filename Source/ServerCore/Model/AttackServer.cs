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
        public List<ThingEntry> NewThings { get; set; }
        public List<int> NewThingsId { get; set; }
        public List<int> Delete { get; set; }
        public Dictionary<int, AttackThingState> UpdateState { get; set; }
        public Dictionary<int, AttackPawnCommand> UpdateCommand { get; set; }

        public DateTime? SetPauseOnTimeToHost { get; set; }
        public DateTime StartTime { get; set; }
        public bool VictoryHostToHost { get; set; }

        private object SyncObj = new Object();

        public string New(PlayerServer player, PlayerServer hostPlayer, AttackInitiatorToSrv fromClient, bool testMode)
        {
            if (!player.Online || !hostPlayer.Online) return "Attack not possible: player offline";
            var err = AttackUtils.CheckPossibilityAttack(player, hostPlayer, fromClient.InitiatorPlaceServerId, fromClient.HostPlaceServerId);
            if (err != null) return err;

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
            NewThings = new List<ThingEntry>();
            NewThingsId = new List<int>();
            Delete = new List<int>();
            UpdateState = new Dictionary<int, AttackThingState>();
            UpdateCommand = new Dictionary<int, AttackPawnCommand>();

            Loger.Log($"Server AttackServer {Attacker.Public.Login} -> {Host.Public.Login} New");
            return null;
        }
        
        public AttackHostFromSrv RequestHost(AttackHostToSrv fromClient)
        {
            //Loger.Log($"Server AttackOnlineHost RequestHost State: {State} -> {fromClient.State}");
            lock (SyncObj)
            {
                if (CheckConnect(false))
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
                    State = 4;
                    MapSize = fromClient.MapSize;
                    TerrainDefNameCell = fromClient.TerrainDefNameCell;
                    TerrainDefName = fromClient.TerrainDefName;
                    Thing = fromClient.Thing;
                    ThingCell = fromClient.ThingCell;
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
                        || fromClient.Delete.Count > 0)
                    {
                        //удаляем из Delete если сейчас команда добавитьс таким id
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
                        for (int i = 0; i < fromClient.Delete.Count; i++)
                        {
                            if (Delete.Contains(fromClient.Delete[i])) continue;
                            Delete.Add(fromClient.Delete[i]);
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
                        SetPauseOnTime = SetPauseOnTimeToHost == null ? DateTime.MinValue : SetPauseOnTimeToHost.Value,
                        VictoryHost = VictoryHostToHost
                    };

                    UpdateCommand = new Dictionary<int, AttackPawnCommand>();

                    if (SetPauseOnTimeToHost != null) Loger.Log("Server Send SetPauseOnTimeToHost=" + SetPauseOnTimeToHost.Value.ToGoodUtcString());

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
                if (CheckConnect(true))
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
                    SetPauseOnTimeToHost = DateTime.UtcNow.AddMinutes(5);
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
                    if (StartTime == DateTime.MinValue)
                    {
                        Loger.Log($"Server AttackServer {Attacker.Public.Login} -> {Host.Public.Login} Start");
                        StartTime = DateTime.UtcNow;
                    }

                    if (fromClient.VictoryHostToHost)
                    {
                        VictoryHostToHost = fromClient.VictoryHostToHost;
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

                    var res = new AttackInitiatorFromSrv()
                    {
                        State = State,
                        NewPawns = NewPawns,
                        NewPawnsId = NewPawnsId,
                        NewThings = NewThings,
                        NewThingsId = NewThingsId,
                        Delete = Delete,
                        UpdateState = UpdateState.Values.ToList(),
                        Finishing = VictoryAttacker != null,
                        VictoryAttacker = VictoryAttacker != null ? VictoryAttacker.Value : false,
                    };
                    NewPawns = new List<ThingEntry>();
                    NewPawnsId = new List<int>();
                    NewThings = new List<ThingEntry>();
                    NewThingsId = new List<int>();
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
            bool fail = false;
            string logDet;
            if (attacker)
            {
                fail = !Host.Online;
                logDet = " (is attacker)";
            }
            else
            {
                //атакующий может подвиснуть при создании карты, но когда максимум ожидания (=пауза у хоста) кончилось, то всё равно проверяем
                if (StartTime != DateTime.MinValue
                    || SetPauseOnTimeToHost != DateTime.MinValue && SetPauseOnTimeToHost < DateTime.UtcNow)
                {
                    fail = !Attacker.Online;
                }
                logDet = " (is host)";
            }

            if (fail)
            {
                var data = Repository.GetData;

                if (TestMode)
                {   //При тестовом режиме всех участников отключаем
                    Loger.Log("Server AttackServer Fail TestMode" + logDet);

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
                else if (!attacker)
                {   //Если отключился атакующий
                    //до State == 10 или меньше 1 мин, то отмена 
                    if (StartTime == DateTime.MinValue || (DateTime.UtcNow - StartTime).TotalSeconds < 60) // State == 10 проверяется косвенно: StartTime устанавливается только при State == 10
                    {
                        Loger.Log("Server AttackServer Fail attacker off in start" + logDet); 

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

        private void Finish()
        {
            Loger.Log($"Server AttackServer {Attacker.Public.Login} -> {Host.Public.Login} Finish StartTime sec = " 
                + (StartTime == DateTime.MinValue ? "-" : (DateTime.UtcNow - StartTime).TotalSeconds.ToString())
                + (VictoryAttacker == null ? "" : VictoryAttacker.Value ? "VictoryAttacker" : "VictoryHost"));
            Attacker.AttackData = null;
            Host.AttackData = null;
        }
    }
}
