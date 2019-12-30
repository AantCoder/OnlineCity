using Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Transfer;

namespace ServerOnlineCity.Model
{
    [Serializable]
    public class BaseContainer
    {
        public string Version { get; set; }

        //public long VersionNum => long.Parse((Version ?? "0").Where(c => Char.IsDigit(c)).Aggregate("0", (r, i) => r + i));

        //[Obsolete("Перебирать 1000+ пользователей, для поиска одного единственного и не повторимого, не очень хорошая идея, предлагаю подумать")]
        public List<PlayerServer> PlayersAll { get; set; }

        public List<ChatPost> ChatPosts { get; set; }

        public string WorldSeed { get; set; }
        public int WorldDifficulty { get; set; }
        public int WorldMapSize { get; set; }
        public float WorldPlanetCoverage { get; set; }
        public long MaxServerIdWorldObjectEntry { get; set; }
        public long MaxIdChat { get; set; }

        public int MaxPlayerId { get; set; }

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
            MaxIdChat = 1; //Id = 1 Занят на общий чат, 0 - системный приватный чат

            ChatPosts = new List<ChatPost>();
        }

        public long GetChatId()
        {
            return ++MaxIdChat;
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
