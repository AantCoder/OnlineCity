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
    class IncidentEMP : OCIncident
    {
        public override bool TryExecuteEvent()
        {
            Map map = Find.CurrentMap;
            //int duration = Mathf.RoundToInt(1 * 60000f); // 1 день состояния
            int duration = Mathf.RoundToInt(hour * mult); // 1 час
            GameCondition_DisableElectricity emp = (GameCondition_DisableElectricity)GameConditionMaker.MakeCondition(GameConditionDefOf.SolarFlare, duration);
            string label = "OC_Incidents_EMP_Label".Translate();
            string text = "OC_Incidents_EMP_Text".Translate();
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NegativeEvent);
            map.gameConditionManager.RegisterCondition(emp);

            return true;
        }
    }
}