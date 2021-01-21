using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace OC_Incidents
{
    public class Raid  //попытка использовать уже существующий ивент
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
            parms.points = StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap) * mult;

            if (!IncidentDefOf.RaidEnemy.Worker.TryExecute(parms))
            {
                Messages.Message($"Failed_Test_Raid", MessageTypeDefOf.RejectInput);
                return false;
            }
            return true;
        }
    }

    class Caravan
    {
        public bool TryExecuteEvent()
        {
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatSmall, Current.Game.AnyPlayerHomeMap);
            parms.customLetterLabel = "Trade caravan";
            parms.customLetterText = "trade caravan arrived";
            parms.faction = null;
            parms.forced = true;  //игнорировать все условия для события
            parms.target = Find.CurrentMap;
            parms.points = StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap);

            if (!IncidentDefOf.TraderCaravanArrival.Worker.TryExecute(parms))
            {
                Messages.Message($"Failed_Test_quest", MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        }
    }

    class Infistation
    {
        public bool TryExecuteEvent()
        {
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatSmall, Current.Game.AnyPlayerHomeMap);
            parms.customLetterLabel = "test infistation";
            parms.customLetterText = "test infistation";
            parms.faction = null;
            parms.forced = true;  //игнорировать все условия для события
            parms.target = Find.CurrentMap;
            parms.points = StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap);

            if (!IncidentDefOf.Infestation.Worker.TryExecute(parms))
            {
                Messages.Message($"Failed_Test_quest", MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        }
    }

    class Quest
    {
        public bool TryExecuteEvent()
        {
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatSmall, Current.Game.AnyPlayerHomeMap);
            parms.customLetterLabel = "test quest";
            parms.customLetterText = "test quest";
            parms.faction = null;
            parms.forced = true;  //игнорировать все условия для события
            parms.target = Find.CurrentMap;
            parms.points = StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap);

            if (!IncidentDefOf.GiveQuest_Random.Worker.TryExecute(parms))
            {
                Messages.Message($"Failed_Test_quest", MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        }
    }

    public class ChunkDrop
    {
        public bool TryExecuteEvent()
        {
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatSmall, Current.Game.AnyPlayerHomeMap);
            parms.customLetterLabel = "test chunk drop";
            parms.customLetterText = "test chunk drop";
            parms.faction = null;
            parms.forced = true;  //игнорировать все условия для события
            parms.target = Find.CurrentMap;
            parms.points = StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap);

            if (!IncidentDefOf.ShipChunkDrop.Worker.TryExecute(parms))
            {
                Messages.Message($"Failed_Test_chunk_drop", MessageTypeDefOf.RejectInput);
                return false;
            }
            return true;
        }
    }
}
