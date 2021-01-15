using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace OC_Incidents
{
	public class Test_Incident : IncidentWorker //самопальный ивент. всё равно наследование от того же интерфейса что и у остальных
	{
		public bool TryExecuteEvent()
        {
			IncidentParms parms = new IncidentParms()
			{
				customLetterLabel = "test event",
				customLetterText = "teast event again",
				target = Current.Game.AnyPlayerHomeMap,
				points = StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap),
			};
			return TryExecuteWorker(parms);
        }
		protected override bool TryExecuteWorker(IncidentParms parms) //подобие ивента с грузовыми капсулами
		{
			Map map = (Map)parms.target;
			List<Thing> things = ThingSetMakerDefOf.ResourcePod.root.Generate(); //заменить вещи?
			IntVec3 intVec = DropCellFinder.RandomDropSpot(map);
			DropPodUtility.DropThingsNear(intVec, map, things, 110, canInstaDropDuringInit: false, leaveSlag: true);
			Find.LetterStack.ReceiveLetter("I can do my own events!", "some text", LetterDefOf.PositiveEvent, new TargetInfo(intVec, map));
			return true;
		}
	}

}
