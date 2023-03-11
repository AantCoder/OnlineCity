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
    public class IncidentCaravan : OCIncident
    {
        public override bool TryExecuteEvent()
        {
            var target = GetTarget();

            parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatSmall, target);
            parms.customLetterLabel = "Trade caravan";
            parms.customLetterText = "trade caravan arrived";
            parms.faction = null;
            parms.forced = true;  //игнорировать все условия для события
            parms.target = target;
            parms.points = CalculatePoints();

            if (!IncidentDefOf.TraderCaravanArrival.Worker.TryExecute(parms))
            {
                Messages.Message($"Failed_Test_Caravan", MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        }
    }
}
