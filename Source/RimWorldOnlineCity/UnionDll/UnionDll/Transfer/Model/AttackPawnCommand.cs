using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class AttackPawnCommand
    {
        public enum PawnCommand : byte
        {
            Wait_Combat = 0,
            Goto = 1,
            Attack = 2,
            AttackMelee = 3
        }

        public int HostPawnID { get; set; }
        public PawnCommand Command { get; set; }
        public IntVec3S TargetPos { get; set; }
        public int TargetID { get; set; }

    }
}
