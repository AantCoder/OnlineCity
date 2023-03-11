using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class ModelExchengeStorage
    {
        public int Tile { get; set; }
        public List<ThingTrade> AddThings { get; set; }
        public List<ThingTrade> DeleteThings { get; set; }
        public int TileTo { get; set; }
        public int Cost { get; set; }
        public int Dist { get; set; }

    }
}
