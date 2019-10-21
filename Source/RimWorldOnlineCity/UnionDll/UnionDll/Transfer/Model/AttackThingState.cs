using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class AttackThingState
    {
        public enum PawnHealthState : byte
        {
            Dead = 0,
            Down = 1,
            Mobile = 2
        }

        public int HostThingID { get; set; }

        public IntVec3S Position { get; set; }

        public int StackCount { get; set; }

        public int HitPoints { get; set; }

        public PawnHealthState DownState { get; set; }

        public override string ToString()
        {
            return $"(hostId={HostThingID}, Pos=({Position.x},{Position.z}), HitP={HitPoints}, Cnt={StackCount} {DownState.ToString()})";
        }
    }

}
