using OCUnion.Transfer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
    [Serializable]
    public class AttackInitiatorToSrv
    {
        public int State { get; set; }

        public bool TestMode { get; set; }

        public string StartHostPlayer { get; set; }

        public long HostPlaceServerId { get; set; }

        public long InitiatorPlaceServerId { get; set; }

        public List<ThingEntry> Pawns { get; set; }

        public List<AttackPawnCommand> UpdateCommand { get; set; }

        public List<int> NeedNewThingIDs { get; set; }

        public TimeSpan SetPauseOnTimeToHost { get; set; }

        public bool VictoryHostToHost { get; set; }

        public bool TerribleFatalError { get; set; }
    }
}
