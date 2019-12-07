using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class IntVec3S
    {
        public short x;
        public short y;
        public short z;

        public IntVec3S(IntVec3 vec)
        {
            x = (short)vec.x;
            y = (short)vec.y;
            z = (short)vec.z;
        }

        public IntVec3 Get()
        {
            return new IntVec3(x, y, z);
        }
    }
}
