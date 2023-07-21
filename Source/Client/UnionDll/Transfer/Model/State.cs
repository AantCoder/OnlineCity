using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class State
    {
        public string Name { get; set; }

        public string Color { get; set; }

        public string Description { get; set; }

        public int MarketValueRanking { get; set; }

        public int MarketValueRankingLast { get; set; }

    }
}
