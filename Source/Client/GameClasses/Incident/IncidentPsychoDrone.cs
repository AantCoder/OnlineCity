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
	class IncidentPsychoDrone : OCIncident
	{
		public override bool TryExecuteEvent()
		{
			Map map = GetTarget();
			//int duration = Mathf.RoundToInt(1 * 60000f); // 1 день состояния
			int duration = Mathf.RoundToInt((day * mult) / 2);
			GameConditionDef def = GameConditionDefOf.PsychicDrone;
			def.label = "Психическое воздействие";
			def.conditionClass = typeof(OC_GameCondition_PsychoDrone);
			OC_GameCondition_PsychoDrone drone = (OC_GameCondition_PsychoDrone)GameConditionMaker.MakeCondition(def, duration);
			drone.gender = Gender.None;
			//drone.level = PsychicDroneLevel.BadExtreme;	настраивать за деньги
			//PsychicDroneLevel.GoodMedium; положительный
			//PsychicDroneLevel.BadLow;		слабый
			//PsychicDroneLevel.BadMedium;	средний
			//PsychicDroneLevel.BadHigh		сильный
			//PsychicDroneLevel.BadExtreme;	жесть

			string label = "Психическое воздействие";
			string text = "OC_Incidents_PsychoDrone_Text".Translate() + ". " + "OC_Incident_Atacker".Translate() + " " + attacker;
			Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NegativeEvent);
			map.gameConditionManager.RegisterCondition(drone);
			return true;
		}
	}

	class OC_GameCondition_PsychoDrone : GameCondition
    {
		public Gender gender = Gender.None;

		public PsychicDroneLevel level = PsychicDroneLevel.BadMedium;

		public const float MaxPointsDroneLow = 800f;

		public const float MaxPointsDroneMedium = 2000f;

	}

}
