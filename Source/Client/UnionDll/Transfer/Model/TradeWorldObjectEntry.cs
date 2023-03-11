using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transfer.ModelMails;

namespace Model
{
    public enum TradeWorldObjectEntryType
    {
        TradeOrder,
        ThingsPlayer
    }

    /// <summary>
    /// Класс используется как родитель и самостоятельно как облегченная копия TradeOrder, для загрузки всех ордеров на карту планеты
    /// </summary>
    [Serializable]
    public class TradeWorldObjectEntry : IModelPlace
    {
        public long Id { get; set; }
        public TradeWorldObjectEntryType Type { get; set; }
        public int Tile { get; set; }
        public long PlaceServerId { get; set; }
        public string Name { get; set; }



        public string LoginOwner { get; set; }

        /// <summary>
        /// Только для сервера (не конфеденциально)
        /// </summary>
        public DateTime UpdateTime { get; set; }

        public TradeWorldObjectEntry()
        { }

    }
}
