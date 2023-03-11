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
    class IncidentEclipse : OCIncident
    {
        public override bool TryExecuteEvent()
        {
            Map map = GetTarget();
            //int duration = Mathf.RoundToInt(1 * 60000f); // 1 день состояния
            int duration = Mathf.RoundToInt(2 * hour * mult); // 1 час
            GameCondition_NoSunlight eclipse = (GameCondition_NoSunlight)GameConditionMaker.MakeCondition(GameConditionDefOf.Eclipse, duration);
            string label = "OC_Incidents_Eclipse_Label".Translate();
            string text = "OC_Incidents_Eclipse_Text".Translate() + ". " + "OC_Incident_Atacker".Translate() + " " + attacker;
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NegativeEvent);
            map.gameConditionManager.RegisterCondition(eclipse);
            return true;
        }
    }
}
