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
            var arrivalParam = ParseArrivalMode(incidentParams != null && incidentParams.Count > 0 ? incidentParams[0] : "walk");
            var factionParam = ParseFaction(incidentParams != null && incidentParams.Count > 1 ? incidentParams[1] : null);

            parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatSmall, target);
            parms.raidStrategy = GetStrategy();
            parms.raidArrivalMode = GetArrivalMode(arrivalParam);
            parms.customLetterLabel = "OC_Incidents_Raid_Label".Translate();
            parms.customLetterText = "OC_Incidents_Raid_Text".Translate() + ". " + "OC_Incident_Atacker".Translate() + " " + attacker;
            parms.biocodeApparelChance = 1f;
            parms.biocodeWeaponsChance = 1f;
            parms.dontUseSingleUseRocketLaunchers = false;
            parms.generateFightersOnly = false;
            parms.raidNeverFleeIndividual = true;
            parms.faction = GetFaction(factionParam);
            parms.forced = true;  //игнорировать все условия для события
            parms.target = target;
            parms.points = CalculatePoints();
            //parms.points = StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap) * mult >= StorytellerUtility.GlobalPointsMax ? StorytellerUtility.GlobalPointsMax : StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap) * mult;
            return parms;
        }

        public static string ParseArrivalMode(string arg)
        {
            arg = (arg ?? "").ToLower().Trim();
            switch (arg)
            {
                case "random":
                case "air":
                case "walk":
                    return arg;
                default:
                    return "walk";
            }
        }
        public static string ParseFaction(string arg)
        {
            arg = (arg ?? "").ToLower().Trim();
            switch (arg.ToLower().Trim())
            {
                case "mech":
                case "pirate":
                case "tribe":
                case "randy":
                    return arg;
                default:
                    return "tribe";
            }
        }

        public static RaidStrategyDef GetStrategy()
        {
            return RaidStrategyDefOf.ImmediateAttack;
        }

        public static PawnsArrivalModeDef GetArrivalMode(string arriveParam)
        {
            switch (arriveParam)
            {
                case "walk":
                    return PawnsArrivalModeDefOf.EdgeWalkIn;
                case "random":
                    return PawnsArrivalModeDefOf.RandomDrop;
                case "air":
                    return PawnsArrivalModeDefOf.CenterDrop;
                default:
                    return PawnsArrivalModeDefOf.EdgeWalkIn;
            }
        }
    }
}
