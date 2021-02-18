﻿using Model;
using OCUnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Transfer.ModelMails;

namespace Transfer
{
    [Serializable]
    public class ModelPlayToClient
    {
        //public long TypeInfo { get; set; }
        public DateTime UpdateTime { get; set; }
        public List<WorldObjectEntry> WObjects { get; set; }
        public List<WorldObjectEntry> WObjectsToDelete { get; set; }
        public List<ModelMail> Mails { get; set; }
        public List<Player> PlayersInfo { get; set; }
        public bool AreAttacking { get; set; }
        public bool NeedSaveAndExit { get; set; }
        public string KeyReconnect { get; set; }
        public List<WorldObjectOnline> WObjectOnlineList { get; set; }
        public List<WorldObjectOnline> WObjectOnlineToAdd { get; set; }
        public List<WorldObjectOnline> WObjectOnlineToDelete { get; set; }

        public List<FactionOnline> FactionOnlineList { get; set; }
        public List<FactionOnline> FactionOnlineToAdd { get; set; }
        public List<FactionOnline> FactionOnlineToDelete { get; set; }
    }
}
