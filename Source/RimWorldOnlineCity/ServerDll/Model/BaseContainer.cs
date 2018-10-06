using Model;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Transfer;

namespace OCServer.Model
{
    [Serializable]
    public class BaseContainer
    {
        public List<PlayerServer> PlayersAll { get; set; }
        
        //public List<Playser> PlayersOnline;

        public string WorldSeed { get; set; }
        public int WorldDifficulty { get; set; }
        public int WorldMapSize { get; set; }
        public float WorldPlanetCoverage { get; set; }
        public long MaxServerIdWorldObjectEntry { get; set; }
        public long MaxIdChat { get; set; }
        public List<WorldObjectEntry> WorldObjects { get; set; }
        public List<WorldObjectEntry> WorldObjectsDeleted { get; set; }
        public List<OrderTrade> Orders { get; set; }

        public BaseContainer()
        {
            PlayersAll = new List<PlayerServer>()
            {
                new PlayerServer("system")
            };
            WorldObjects = new List<WorldObjectEntry>();
            WorldObjectsDeleted = new List<WorldObjectEntry>();
            Orders = new List<OrderTrade>();
            MaxIdChat = 1; //Id = 1 Занят на общий чат
        }

        public long GetChatId()
        {
            return ++MaxIdChat;
        }

        public long GetWorldObjectEntryId()
        {
            return ++MaxServerIdWorldObjectEntry;
        }
    }
}
