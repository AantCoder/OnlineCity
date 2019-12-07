using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Transfer
{
    [Serializable]
    public class ModelContainer
    {
        public int TypePacket { get; set; }

        public object Packet { get; set; }
    }
}
