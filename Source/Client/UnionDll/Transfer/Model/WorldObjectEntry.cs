using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Model
{
    public enum WorldObjectEntryType
    {
        Base,
        Caravan
    }

    [Serializable]
    public class WorldObjectEntry : IModelPlace
    {
        //На клиенте данные от других игроков заполняются с сервера, а свои заполняются самостоятельно
        //Кроме полей MarketValueStorage и MarketValueBalance - значения в своих объектах заполняются с отдельно
        public WorldObjectEntryType Type { get; set; }
        public int Tile { get; set; }
        public string Name { get; set; }
        public long PlaceServerId { get; set; }
        public float FreeWeight { get; set; }
        public float MarketValue { get; set; }
        public float MarketValuePawn { get; set; }
        public float MarketValueStorage { get; set; }
        public float MarketValueBalance { get; set; }
        public float MarketValueTotal => MarketValue + MarketValuePawn + MarketValueStorage + MarketValueBalance;

        public string LoginOwner { get; set; }

        /// <summary>
        /// Только для сервера (не конфеденциально)
        /// </summary>
        public DateTime UpdateTime { get; set; }
    }
}
