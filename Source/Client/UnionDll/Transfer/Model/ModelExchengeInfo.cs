using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCUnion.Transfer.Model
{
    public enum ModelExchengeInfoRequest : int
    {
        GetCountThing,
    }

    [Serializable]
    public class ModelExchengeInfo
    {
        public ModelExchengeInfoRequest Request { get; set; }        
        public ThingTrade Thing { get; set; }


        public int Status { get; set; }
        public string Message { get; set; }
        public int Result { get; set; }
    }
}
