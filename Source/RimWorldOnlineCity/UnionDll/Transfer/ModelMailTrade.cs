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

        #region For StartIncident

        public IncidentTypes IncidentType { get; set; }
        public float IncidentMult { get; set; }
        public IncidentStrategys IncidentStrategy { get; set; }
        public IncidentArrivalModes IncidentArrivalMode { get; set; }
        public string IncidentFaction { get; set; }

        #endregion

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
        StartIncident
    }


    #region For StartIncident

    public enum IncidentTypes
    {
        Raid,
        Caravan,
        ChunkDrop,
        Infistation,
        Quest,
    }
    public enum IncidentStrategys
    {
        ImmediateAttack
    }
    public enum IncidentArrivalModes
    {
        EdgeWalkIn
    }

    #endregion

}
