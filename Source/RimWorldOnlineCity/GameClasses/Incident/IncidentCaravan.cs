using RimWorld;
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
            parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatSmall, Current.Game.AnyPlayerHomeMap);
            parms.customLetterLabel = "Trade caravan";
            parms.customLetterText = "trade caravan arrived";
            parms.faction = null;
            parms.forced = true;  //игнорировать все условия для события
            parms.target = Find.CurrentMap;
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
