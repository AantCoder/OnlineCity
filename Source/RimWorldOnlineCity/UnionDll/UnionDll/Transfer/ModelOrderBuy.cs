using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Transfer
{
    [Serializable]
    public class ModelOrderBuy
    {
        public long OrderId { get; set; }

        public int Count { get; set; }

        public List<ThingEntry> Things { get; set; }
    }
}
