using OCUnion.Transfer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Model
{
    [Serializable]
    public class AttackHostToSrv
    {

        public int State { get; set; }

        public IntVec3S MapSize { get; set; }

        public List<IntVec3S> TerrainDefNameCell { get; set; }
        public List<string> TerrainDefName { get; set; }        
        public List<IntVec3S> ThingCell { get; set; }
        public List<ThingTrade> Thing { get; set; }

        public List<ThingEntry> NewPawns { get; set; }
        public List<int> NewPawnsId { get; set; }
        public List<int> Delete { get; set; }
        public List<AttackThingState> UpdateState { get; set; }
    }
}
