using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Transfer
{
    [Serializable]
    public class ModelPlayToClient
    {
        //public long TypeInfo { get; set; }
        public DateTime UpdateTime { get; set; }
        public List<WorldObjectEntry> WObjects { get; set; }
        public List<WorldObjectEntry> WObjectsToDelete { get; set; }
        public List<ModelMailTrade> Mails { get; set; }
        public List<Player> PlayersInfo { get; set; }
        public bool AreAttacking { get; set; }
    }
}
