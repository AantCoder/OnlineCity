using RimWorld;
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
            parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatSmall, Current.Game.AnyPlayerHomeMap);
            parms.customLetterLabel = "Жуки!";
            parms.customLetterText = "Кто-то приманил жуков на поверхность с помощью низкочастотных вибраций";
            parms.forced = true;  //игнорировать все условия для события
            parms.faction = Find.FactionManager.OfInsects;
            parms.target = Find.CurrentMap;
            parms.points = CalculatePoints();
            //parms.points = StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap) * mult >= StorytellerUtility.GlobalPointsMax ? StorytellerUtility.GlobalPointsMax : StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap) * mult;
            return parms;
        }
    }
}
