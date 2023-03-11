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
    public class IncidentRaid : OCIncident //попытка использовать уже существующий ивент
    {
        public int cost;

        //protected IncidentParms Parms => Find.CurrentMap != null ? StorytellerUtility.DefaultParmsNow(incident.category, Find.CurrentMap) : null;

        public override bool TryExecuteEvent()
        {
            if (!IncidentDefOf.RaidEnemy.Worker.TryExecute(GetParms()))
            {
                Messages.Message($"Failed_Test_Raid", MessageTypeDefOf.RejectInput);
                return false;
            }
            return true;
        }

        private IncidentParms GetParms()
        {
            var target = GetTarget();

            parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatSmall, target);
            parms.raidStrategy = GetStrategy(strategy);
            parms.raidArrivalMode = GetArrivalMode(arrivalMode);
            parms.customLetterLabel = "OC_Incidents_Raid_Label".Translate();
            parms.customLetterText = "OC_Incidents_Raid_Text".Translate() + ". " + "OC_Incident_Atacker".Translate() + " " + attacker;
            parms.biocodeApparelChance = 1f;
            parms.biocodeWeaponsChance = 1f;
            parms.dontUseSingleUseRocketLaunchers = false;
            parms.generateFightersOnly = false;
            parms.raidNeverFleeIndividual = true;
            parms.faction = GetFaction();
            parms.forced = true;  //игнорировать все условия для события
            parms.target = target;
            parms.points = CalculatePoints();
            //parms.points = StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap) * mult >= StorytellerUtility.GlobalPointsMax ? StorytellerUtility.GlobalPointsMax : StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap) * mult;
            return parms;
        }
    }
}
