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
    public class WorldObjectEntry
    {
        public WorldObjectEntryType Type { get; set; }
        public int Tile { get; set; }
        public string Name { get; set; }
        public long ServerId { get; set; }
        public float FreeWeight { get; set; }
        public float MarketValue { get; set; }
        public float MarketValuePawn { get; set; }

        public string LoginOwner { get; set; }

        /// <summary>
        /// Только для сервера (не конфеденциально)
        /// </summary>
        public DateTime UpdateTime { get; set; }
    }
}
