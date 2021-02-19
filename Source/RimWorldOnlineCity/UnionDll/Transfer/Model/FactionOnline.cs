using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    [Serializable]
    public class FactionOnline
    {
        public string Name { get; set; }
        public string LabelCap { get; set; }
        public string DefName { get; set; }
        public int loadID { get; set; }
    }
}
