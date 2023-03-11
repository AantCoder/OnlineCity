using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Model
{

    [Serializable]
    public class TradeThingStorage : TradeWorldObjectEntry
    {
        [XmlIgnore]
        public List<ThingTrade> Things { get; set; } = new List<ThingTrade>();

    }
}
