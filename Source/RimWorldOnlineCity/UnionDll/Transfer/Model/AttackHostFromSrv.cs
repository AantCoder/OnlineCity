﻿using OCUnion.Transfer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
    [Serializable]
    public class AttackHostFromSrv
    {
        public string ErrorText { get; set; }

        public int State { get; set; }
        
        public string StartInitiatorPlayer { get; set; }

        public long HostPlaceServerId { get; set; }

        public long InitiatorPlaceServerId { get; set; }

        public List<ThingEntry> Pawns { get; set; }

        public List<AttackPawnCommand> UpdateCommand { get; set; }

        public List<int> NeedNewThingIDs { get; set; }

        public bool TestMode { get; set; }

        public DateTime SetPauseOnTime { get; set; }

        public bool VictoryHost { get; set; }

        public bool TerribleFatalError { get; set; }
    }
}
