using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorldOnlineCity
{
    public class IncidentChunkDrop : OCIncident //попытка использовать уже существующий ивент
    {
        public override bool TryExecuteEvent()
        {
            var target = (place as Settlement)?.Map ?? Find.CurrentMap;

            parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatSmall, target);
            parms.customLetterLabel = "test chunk drop";
            parms.customLetterText = "test chunk drop";
            parms.faction = null;
            parms.forced = true;  //игнорировать все условия для события
            parms.target = target;
            parms.points = CalculatePoints();

            if (!IncidentDefOf.ShipChunkDrop.Worker.TryExecute(parms))
            {
                Messages.Message($"Failed_Test_chunk_drop", MessageTypeDefOf.RejectInput);
                return false;
            }
            return true;
        }

    }
}
