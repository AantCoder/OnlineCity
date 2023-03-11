using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transfer
{
    [Serializable]
    public class ModelOrderLoadRequest
    {
        public List<int> Tiles { get; set; }
        public string FilterBuy { get; set; }
        public string FilterSell { get; set; }
    }
}
