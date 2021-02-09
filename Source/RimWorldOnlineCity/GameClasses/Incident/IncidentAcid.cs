using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity
{
    class IncidentAcid : OCIncident
    {
        public override bool TryExecuteEvent()
        {
            if (!IncidentDefOf.ToxicFallout.Worker.TryExecute(GetParms()))
            {
                Messages.Message($"Failed_Acid_Rain", MessageTypeDefOf.RejectInput);
                return false;
            }
            return true;
        }
        private IncidentParms GetParms()
        {
            var target = (place as Settlement)?.Map ?? Find.CurrentMap;

            parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatSmall, target);
            parms.customLetterLabel = "OC_Incidents_Acid_Label".Translate();
            parms.customLetterText = "OC_Incidents_Acid_Text".Translate();
            parms.faction = GetFaction();
            parms.forced = true;  //игнорировать все условия для события
            parms.target = target;
            parms.points = CalculatePoints();
            //parms.points = StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap) * mult >= StorytellerUtility.GlobalPointsMax ? StorytellerUtility.GlobalPointsMax : StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap) * mult;
            return parms;
        }
    }
}
