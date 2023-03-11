using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace RimWorldOnlineCity
{
    class IncidentPlague : OCIncident
    {
        public override bool TryExecuteEvent()
        {
            //IncidentWorker_DiseaseHuman worker = new IncidentWorker_DiseaseHuman();
            var def = DefDatabase<IncidentDef>.GetNamed("Disease_Plague");
            if (!def.Worker.TryExecute(GetParms()))
            {
                Messages.Message($"Failed_Plague", MessageTypeDefOf.RejectInput);
                return false;
            }
            return true;
        }
        private IncidentParms GetParms()
        {
            var target = GetTarget();

            parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatSmall, target);
            parms.customLetterLabel = "Эпидемия!".Translate();
            parms.customLetterText = "на нас выпустили хворь!".Translate() + ". " + "OC_Incident_Atacker".Translate() + " " + attacker;
            parms.forced = true;  //игнорировать все условия для события
            parms.target = target;
            parms.points = CalculatePoints();
            //parms.points = StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap) * mult >= StorytellerUtility.GlobalPointsMax ? StorytellerUtility.GlobalPointsMax : StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap) * mult;
            return parms;
        }
    }
}
