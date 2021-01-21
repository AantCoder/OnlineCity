using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace OC_Incidents
{
    public class Raid_worker : Def  //попытка использовать уже существующий ивент
    {
        public int cost;

        //protected IncidentParms Parms => Find.CurrentMap != null ? StorytellerUtility.DefaultParmsNow(incident.category, Find.CurrentMap) : null;

        public bool TryExecuteEvent(float mult = 1)
        {
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatSmall, Current.Game.AnyPlayerHomeMap);
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            parms.customLetterLabel = "test raid";
            parms.customLetterText = "teast raid again";
            parms.faction = null;
            parms.forced = true;  //игнорировать все условия для события
            parms.target = Find.CurrentMap;
            parms.points = StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap) * mult >= StorytellerUtility.GlobalPointsMax ? StorytellerUtility.GlobalPointsMax : StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap) * mult;

            if (!IncidentDefOf.RaidEnemy.Worker.TryExecute(parms))
            {
                Messages.Message($"Failed_Test_Raid", MessageTypeDefOf.RejectInput);
                return false;
            }
            return true;
        }
    }
}