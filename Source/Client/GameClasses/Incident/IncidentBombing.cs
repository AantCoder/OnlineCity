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
    public class IncidentBombing : OCIncident //попытка использовать уже существующий ивент
    {
        public int cost;

        //protected IncidentParms Parms => Find.CurrentMap != null ? StorytellerUtility.DefaultParmsNow(incident.category, Find.CurrentMap) : null;

        public override bool TryExecuteEvent()
		{
			var map = GetTarget();

			if (!TryFindCell(out var cell, map)) // здесь берётся целевая точка
			{
				return false;
			}
			System.Random random = new System.Random();
			int num = mult;		//количество метеоритов
			int num2 = random.Next(10, 30);   // радиус разлёта от целевой клетки
			List<Thing> list = new List<Thing>();
			IntVec3 intVec = cell;
			ThingDef meteor = ThingDefOf.MeteoriteIncoming; //здесь глубоко зарыт разрешённый материал для метеорита
			for (int i = 0; i < num; i++)	// 1 итерация - 1 метеорит
			{
				intVec += (Rand.InsideUnitCircleVec3 * num2).ToIntVec3();  // точка удара с поправкой на разлёт
				
                while (!intVec.InBounds(map) || intVec.Fogged(map)) // рероллы пока не выпадет точка в радиусе разлёта, удовлетворяющая условию
				{
					intVec += (Rand.InsideUnitCircleVec3 * num2).ToIntVec3();

				}
				list = ThingSetMakerDefOf.Meteorite.root.Generate();	// ??
				SkyfallerMaker.SpawnSkyfaller(meteor, list, intVec, map);  //залп!

				intVec = cell;
			}

			string label = "В УКРЫТИЕ";
			string text = "Началась орбитальная бомбардировка";
			Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.ThreatBig, new TargetInfo(cell, map));
			return true;
        }

		private bool TryFindCell(out IntVec3 cell, Map map) //todo: новый алгоритм поиска цели. Этот из ванильного падения метеорита
		{
			int maxMineables = 1;
			//CellFinder.TryFindRandomCellInRegion();
			return CellFinderLoose.TryFindSkyfallerCell(ThingDefOf.MeteoriteIncoming, map, out cell, 10, default(IntVec3), -1, allowRoofedCells: true, allowCellsWithItems: false, allowCellsWithBuildings: true, colonyReachable: true, avoidColonistsIfExplosive: false, alwaysAvoidColonists: false, delegate (IntVec3 x)
			{
				int num = Mathf.CeilToInt(Mathf.Sqrt(maxMineables));
				CellRect cellRect = CellRect.CenteredOn(x, num, num);
				int num2 = 0;
				foreach (IntVec3 item in cellRect)
				{
					if (item.InBounds(map) && item.Standable(map))
					{
						num2++;
					}
				}
				return num2 >= maxMineables;
			});
		}
	}
}
