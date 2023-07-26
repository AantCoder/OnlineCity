using Model;
using OCUnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transfer.ModelMails
{

    [Serializable]
    public class ModelMailStartIncident : ModelMail, IModelPlace
    {
        public int Tile { get; set; }
        public long PlaceServerId { get; set; }

        public IncidentTypes IncidentType { get; set; }
        public int IncidentMult { get; set; }
        public List<string> IncidentParams { get; set; }
        /// <summary>
        /// Только для просмотра в интерфейсе тех, кто уже в очереди.
        /// </summary>
        public bool AlreadyStart { get; set; }

        public override string GetHash()
        {
            return $"T{Tile}P{PlaceServerId} {(int)IncidentType} {IncidentMult} " 
                + IncidentParams == null ? "" : string.Join(" ", IncidentParams);
        }
    }
}
