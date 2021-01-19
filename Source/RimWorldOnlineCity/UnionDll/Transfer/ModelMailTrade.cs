using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Transfer
{
    /// <summary>
    /// Послыка от каравана другого игрока
    /// </summary>
    [Serializable]
    public class ModelMailTrade
    {
        public ModelMailTradeType Type { get; set; }

        public Player From { get; set; }
        public Player To { get; set; }
        public int Tile { get; set; }
        public List<ThingEntry> Things { get; set; }
        public long PlaceServerId { get; set; }

        public string ContentString()
        {
            return Things == null ? "" : Things.Aggregate("", (r, i) => r + Environment.NewLine + i.Name + " x" + i.Count);
        }
    }

    public enum ModelMailTradeType
    {
        CreateThings = 0,
        DeleteByServerId,
        AttackCancel,
        AttackTechnicalVictory,
        StartEvent
    }
}
