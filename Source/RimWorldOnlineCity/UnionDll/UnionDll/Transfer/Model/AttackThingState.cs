using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

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

        public AttackThingState()
        {
        }

        public AttackThingState(Thing mp)
        {
            //А применяется созданый здесь контейнер в GameUtils.ApplyState
            HostThingID = mp.thingIDNumber;
            StackCount = mp.stackCount;
            Position = new IntVec3S(mp.Position);
            HitPoints = mp.HitPoints;
            var pawn = mp as Pawn;
            if (pawn != null)
            {
                //Loger.Log("Client AttackThingState " + pawn.health.State.ToString() + " thing=" + pawn.Label + " ID=" + pawn.thingIDNumber);
                DownState = (AttackThingState.PawnHealthState)(int)pawn.health.State;
            }
            else
                DownState = PawnHealthState.Mobile;
        }

        public static int GetHash(Thing mp)
        {
            return ((mp.Position.x % 50) * 50 + mp.Position.z % 50)
                + mp.stackCount * 10000
                + mp.HitPoints * 100000;
        }

        public override string ToString()
        {
            return $"(hostId={HostThingID}, Pos=({Position.x},{Position.z}), HitP={HitPoints}, Cnt={StackCount} {DownState.ToString()})";
        }
    }

}
