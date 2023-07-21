﻿using Model;
using OCUnion;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Transfer;
using System.Linq;
using ServerOnlineCity.Services;
using ServerOnlineCity.Mechanics;
using OCUnion.Transfer.Model;
using Server.Mechanics;

namespace ServerOnlineCity.Model
{
    [Serializable]
    public class BaseContainer
    {
        public string Version { get; set; }
        public long VersionNum { get; set; }

        //public long VersionNum => long.Parse((Version ?? "0").Where(c => Char.IsDigit(c)).Aggregate("0", (r, i) => r + i));

        public List<PlayerServer> PlayersAll { get; set; }
        public PlayerServer PlayerSystem { get { return PlayersAll[0]; } }
        [NonSerialized]
        public ICollection<string> GetPlayerLoginsAll;

        [NonSerialized]
        public ICollection<PlayerServer> GetPlayersAll;
        [NonSerialized]
        public ConcurrentDictionary<string, PlayerServer> PlayersAllDic;
        [NonSerialized]
        public ConcurrentDictionary<string, PlayerServer> PlayersAllDicWithNotApprove;

        public DateTime RankingUpdate { get; set; }
        public List<string> PlayersRanking { get; set; }
        public List<string> StatesRanking { get; set; }
        public List<string> PlayersRankingLast { get; set; }
        public List<string> StatesRankingLast { get; set; }

        public void UpdatePlayersAllDic()
        {
            PlayersAllDic = new ConcurrentDictionary<string, PlayerServer>(PlayersAll.Where(p => p.Approve).ToDictionary(p => p.Public.Login));
            PlayersAllDicWithNotApprove = new ConcurrentDictionary<string, PlayerServer>(PlayersAll.ToDictionary(p => p.Public.Login));
            GetPlayersAll = PlayersAllDic.Values.ToHashSet();
            GetPlayerLoginsAll = GetPlayersAll.Select(p => p.Public.Login).ToHashSet();
        }

        public string WorldSeed { get; set; }
        public string WorldScenarioName { get; set; }
        public string WorldStoryteller { get; set; }
        public string WorldDifficulty { get; set; }
        public int WorldMapSize { get; set; }
        public float WorldPlanetCoverage { get; set; }
        public long MaxServerIdWorldObjectEntry { get; set; }
        public int MaxIdChat { get; set; }

        public int MaxPlayerId { get; set; }

        public List<WorldObjectEntry> WorldObjects { get; set; }
        [NonSerialized]
        public List<WorldObjectEntry> WorldObjectsDeleted = new List<WorldObjectEntry>();

        public List<TradeOrder> Orders { get; set; }

        public Dictionary<int, long> OrdersPlaceServerIdByTile { get; set; }

        public ExchengeOperator OrderOperator => _OrderOperator;

        [NonSerialized]
        private ExchengeOperator _OrderOperator;

        [NonSerialized]
        public Dictionary<long, string> UploadService; //не реализовано удаление (только перезагрузкой), если будет жрать память сделать отслеживания по кол-во добавлений

        internal long SetInUploadService(string data)
        {
            long hash = data.GetHashCode(); //to do?
            UploadService[hash] = data;
            return hash;
        }

        // WorldObject Online
        public List<WorldObjectOnline> WorldObjectOnlineList { get; set; }
        public List<FactionOnline> FactionOnlineList { get; set; }

        [NonSerialized]
        public bool EverybodyLogoff;


        public List<State> States { get; set; }

        public List<StatePosition> StatePositions { get; set; }

        public StateOperator StateOperator => _StateOperator;

        [NonSerialized]
        private StateOperator _StateOperator;

        [NonSerialized]
        public ICollection<State> GetStates;
        [NonSerialized]
        public ConcurrentDictionary<string, State> StatesDic;
        [NonSerialized]
        public ConcurrentDictionary<string, ConcurrentDictionary<string, StatePosition>> StatePositionsDic;
        [NonSerialized]
        private ConcurrentDictionary<string, HashSet<PlayerServer>> StatePlayersDic;
        [NonSerialized]
        public DateTime StateUpdateTime;

        public void UpdateStatesDic()
        {
            StatesDic = new ConcurrentDictionary<string, State>(States.ToDictionary(p => p.Name));
            GetStates = StatesDic.Values.ToHashSet();
            var pd = States.ToDictionary(p => p.Name, p => new ConcurrentDictionary<string, StatePosition>(StatePositions.Where(sp => sp.StateName == p.Name).ToDictionary(sp => sp.Name)));
            StatePositionsDic = new ConcurrentDictionary<string, ConcurrentDictionary<string, StatePosition>>(pd);
            StatePlayersDic = new ConcurrentDictionary<string, HashSet<PlayerServer>>();
            StateUpdateTime = DateTime.UtcNow;
        }
        public HashSet<PlayerServer> GetStatePlayers(string StateName) => string.IsNullOrEmpty(StateName) ? new HashSet<PlayerServer>()
            : StatePlayersDic.GetOrAdd(StateName, GetPlayersAll.Where(p => p.Public.StateName == StateName).ToHashSet());

        public NameValidator NameValidator => _NameValidator;

        [NonSerialized]
        private NameValidator _NameValidator;


        public BaseContainer()
        {
            var publicChat = new Chat()
            {
                Id = 1,
                Name = "Public",
                OwnerLogin = "system",
                OwnerMaker = false,
                PartyLogin = new List<string>() { "system" },
                Posts = new List<ChatPost>(),
                LastChanged = DateTime.UtcNow,
            };

            MaxIdChat = 1; //Id = 1 Занят на общий чат, 0 - системный приватный чат
            ChatManager.Instance.NewChatManager(1, publicChat);

            PlayersAll = new List<PlayerServer>()
            {
                new PlayerServer("system")               
            };

            States = new List<State>();
            StatePositions = new List<StatePosition>();
           
            WorldObjects = new List<WorldObjectEntry>();
            WorldObjectsDeleted = new List<WorldObjectEntry>();
            Orders = new List<TradeOrder>();
            OrdersPlaceServerIdByTile = new Dictionary<int, long>();
            VersionNum = MainHelper.VersionNum;
            WorldObjectOnlineList = new List<WorldObjectOnline>();
            FactionOnlineList = new List<FactionOnline>();

            PostLoad();
        }

        public void PostLoad()
        {
            if (Orders == null) Orders = new List<TradeOrder>();
            if (OrdersPlaceServerIdByTile == null) OrdersPlaceServerIdByTile = new Dictionary<int, long>();
            if (UploadService == null) UploadService = new Dictionary<long, string>();
            if (States == null) States = new List<State>();
            if (StatePositions == null) StatePositions = new List<StatePosition>();
            if (PlayersRanking == null) PlayersRanking = new List<string>();
            if (StatesRanking == null) StatesRanking = new List<string>();
            if (PlayersRankingLast == null) PlayersRankingLast = new List<string>();
            if (StatesRankingLast == null) StatesRankingLast = new List<string>();

            // Преобразования при обновлениях {

            ////Переход на 0.4.75
            //foreach (var order in Orders)
            //{
            //    foreach (var item in order.BuyThings) if (item.PawnParam != null) item.PawnParam = item.PawnParam.Replace("Tribesperson", "Colonist");
            //    foreach (var item in order.SellThings) if (item.PawnParam != null) item.PawnParam = item.PawnParam.Replace("Tribesperson", "Colonist");
            //}
            //foreach (var player in PlayersAll)
            //{
            //    foreach(var tt in player.TradeThingStorages)
            //        foreach (var item in tt.Things)
            //            if (item.PawnParam != null) item.PawnParam = item.PawnParam.Replace("Tribesperson", "Colonist");
            //}

            // }

            if (!ServerManager.ServerSettings.PlayerNeedApprove)
            {
                foreach (var player in PlayersAll)
                {
                    player.Approve = true;
                }
            }

            _OrderOperator = new ExchengeOperator(this);
            _StateOperator = new StateOperator(this);
            _NameValidator = new NameValidator(this);
            if (WorldObjectsDeleted == null) WorldObjectsDeleted = new List<WorldObjectEntry>();
            UpdatePlayersAllDic();
            UpdateStatesDic();

            //Если PVP выключили, то выключаем его в настройках всех игроков
            if (!ServerManager.ServerSettings.GeneralSettings.EnablePVP)
            {
                foreach(var player in PlayersAll)
                {
                    player.Public.EnablePVP = false;
                }
            }

        }

        public long GetWorldObjectEntryId()
        {
            return ++MaxServerIdWorldObjectEntry;
        }

        public int GenerateMaxPlayerId()
        {
            return ++MaxPlayerId;
        }

    }
}
