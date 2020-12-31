using Model;
using OCUnion;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Transfer;
using System.Linq;
using ServerOnlineCity.Services;

namespace ServerOnlineCity.Model
{
    [Serializable]
    public class BaseContainer
    {
        public string Version { get; set; }
        public long VersionNum { get; set; }

        //public long VersionNum => long.Parse((Version ?? "0").Where(c => Char.IsDigit(c)).Aggregate("0", (r, i) => r + i));

        public List<PlayerServer> PlayersAll { get; set; }

        [NonSerialized]
        public ConcurrentDictionary<string, PlayerServer> PlayersAllDic;

        public void UpdatePlayersAllDic()
        {
            PlayersAllDic = new ConcurrentDictionary<string, PlayerServer>(PlayersAll.ToDictionary(p => p.Public.Login));
        }

        public string WorldSeed { get; set; }
        public string WorldScenarioName { get; set; }
        public string WorldDifficulty { get; set; }
        public int WorldMapSize { get; set; }
        public float WorldPlanetCoverage { get; set; }
        public long MaxServerIdWorldObjectEntry { get; set; }
        public int MaxIdChat { get; set; }

        public int MaxPlayerId { get; set; }

        public List<WorldObjectEntry> WorldObjects { get; set; }
        public List<WorldObjectEntry> WorldObjectsDeleted { get; set; }
        public List<OrderTrade> Orders { get; set; }

        [NonSerialized]
        public bool EverybodyLogoff;

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
           
            WorldObjects = new List<WorldObjectEntry>();
            WorldObjectsDeleted = new List<WorldObjectEntry>();
            Orders = new List<OrderTrade>();
            VersionNum = MainHelper.VersionNum;
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
