using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    [Serializable]
    public class WorldObjectOnline
    {
        public string Name { get; set; }
        public int Tile { get; set; }
        public string FactionGroup { get; set; }

    }
}
