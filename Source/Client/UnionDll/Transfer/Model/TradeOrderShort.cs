using Model;
using OCUnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transfer
{
    [Serializable]
    public class TradeOrderShort : TradeWorldObjectEntry
    {

        public TradeOrderShort()
        { 
        }
        public TradeOrderShort(TradeWorldObjectEntry trade)
        {
            this.Id = trade.Id;
            this.Type = trade.Type;
            this.Tile = trade.Tile;
            this.Name = trade.Name;
            this.PlaceServerId = trade.PlaceServerId;
            this.LoginOwner = trade.LoginOwner;
            this.UpdateTime = trade.UpdateTime;
        }

        public override string ToString()
        {
            return "OCity_Dialog_Exchenge_Trade_Orders".TranslateCache() + " " 
                + "OCity_Caravan_Player".TranslateCache(Name, LoginOwner);
        }
    }
}
