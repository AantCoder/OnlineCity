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
    public class IncidentInfistation : OCIncident
    {
        public override bool TryExecuteEvent()
        {
            if (!IncidentDefOf.Infestation.Worker.TryExecute(GetParms()))
            {
                Messages.Message($"Failed_infestation", MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        }

        private IncidentParms GetParms()
        {
            var target = (place as Settlement)?.Map ?? Find.CurrentMap;

            parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatSmall, target);
            parms.customLetterLabel = "OC_Incidents_Inf_Label".Translate();
            parms.customLetterText = "OC_Incidents_Inf_Text".Translate();
            parms.forced = true;  //игнорировать все условия для события
            parms.faction = Find.FactionManager.OfInsects;
            parms.target = target;
            parms.points = CalculatePoints();
            //parms.points = StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap) * mult >= StorytellerUtility.GlobalPointsMax ? StorytellerUtility.GlobalPointsMax : StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap) * mult;
            return parms;
        }
    }
}
