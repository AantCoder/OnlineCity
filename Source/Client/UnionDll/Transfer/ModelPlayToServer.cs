using Model;
using OCUnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Transfer
{
    [Serializable]
    public class ModelPlayToServer
    {
        public DateTime UpdateTime { get; set; }
        public List<WorldObjectEntry> WObjects { get; set; }
        public List<WorldObjectEntry> WObjectsToDelete { get; set; }
        public byte[] SaveFileData { get; set; }
        public bool SingleSave { get; set; }
        public long LastTick { get; set; }
        public PlayerGameProgress GameProgress { get; set; }
        public List<string> GetPlayersInfo { get; set; }
        public List<WorldObjectOnline> WObjectOnlineList { get; set; }
        public List<WorldObjectOnline> WObjectOnlineToAdd { get; set; }
        public List<WorldObjectOnline> WObjectOnlineToDelete { get; set; }

        public List<FactionOnline> FactionOnlineList { get; set; }
        public List<FactionOnline> FactionOnlineToAdd { get; set; }
        public List<FactionOnline> FactionOnlineToDelete { get; set; }
    }
}
