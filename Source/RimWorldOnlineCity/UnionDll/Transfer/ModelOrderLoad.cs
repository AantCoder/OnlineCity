using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Transfer
{
    [Serializable]
    public class ModelOrderLoad
    {
        public int Status { get; set; }

        public string Message { get; set; }

        public List<OrderTrade> Orders { get; set; }

    }
}
