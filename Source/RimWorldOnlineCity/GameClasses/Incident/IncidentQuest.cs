using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorldOnlineCity
{
    public class IncidentQuest : OCIncident //попытка использовать уже существующий ивент
    {
        public override bool TryExecuteEvent()
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
}
