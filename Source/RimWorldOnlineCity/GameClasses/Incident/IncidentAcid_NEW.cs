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
	class IncidentAcid_NEW : OCIncident
	{
		public override bool TryExecuteEvent()
		{
			Map map = Find.CurrentMap;
			//int duration = Mathf.RoundToInt(1 * 60000f); // 1 день состояния
			int duration = Mathf.RoundToInt((day * mult)/2);
			GameConditionDef def = new GameConditionDef
			{
				label = "Химическая атака",
				conditionClass = typeof(OC_GameCondition_Acid),
				canBePermanent = false,
				preventRain = false,
				temperatureOffset = 0,
			};
			OC_GameCondition_Acid acid = (OC_GameCondition_Acid)GameConditionMaker.MakeCondition(def, duration);
			//acid.SkyGlow = 0.2f; //свет  !!опасно менять, поскольку может сломать небо!!
			acid.PlantKillChance = 0.5f; //урон по растениям
			acid.ToxicPerDay = 10f;  //урон по пешкам
			acid.CorpseRotProgressAdd = 5000f; //порча трупов
			acid.CheckInterval = 1250;  //тиков кд между добавлением отравления по всем thing
			string label = "Хим атака";
			string text = "";
			Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NegativeEvent);
			map.gameConditionManager.RegisterCondition(acid);

			if (map.weatherManager.curWeather.rainRate > 0.1f)
			{
				map.weatherDecider.StartNextWeather();
			}
			return true;
		}
	}

	class OC_GameCondition_Acid : GameCondition
	{
		private const float MaxSkyLerpFactor = 0.5f;

		private const float SkyGlow = 0.85f;//уровень света

		private SkyColorSet ToxicFalloutColors = new SkyColorSet(new ColorInt(216, 255, 0).ToColor, new ColorInt(234, 200, 255).ToColor, new Color(0.6f, 0.8f, 0.5f), SkyGlow);

		private List<SkyOverlay> overlays = new List<SkyOverlay>
		{
			new WeatherOverlay_Fallout()
		};
		// это ванильные значения
		public int CheckInterval = 3451;

		public float ToxicPerDay = 0.5f;  //отравление в день

		public float PlantKillChance = 0.0065f;  //урон по растениям

		public float CorpseRotProgressAdd = 3000f; //гниение трупов

		public override int TransitionTicks => 5000;

		public override void Init()
		{
			LessonAutoActivator.TeachOpportunity(ConceptDefOf.ForbiddingDoors, OpportunityType.Critical);
			LessonAutoActivator.TeachOpportunity(ConceptDefOf.AllowedAreas, OpportunityType.Critical);
		}

		public override void GameConditionTick()
		{
			List<Map> affectedMaps = base.AffectedMaps;
			if (Find.TickManager.TicksGame % CheckInterval == 0)
			{
				for (int i = 0; i < affectedMaps.Count; i++)
				{
					DoPawnsToxicDamage(affectedMaps[i]);
				}
			}
			for (int j = 0; j < overlays.Count; j++)
			{
				for (int k = 0; k < affectedMaps.Count; k++)
				{
					overlays[j].TickOverlay(affectedMaps[k]);
				}
			}
		}

		private void DoPawnsToxicDamage(Map map)
		{
			List<Pawn> allPawnsSpawned = map.mapPawns.AllPawnsSpawned;
			for (int i = 0; i < allPawnsSpawned.Count; i++)
			{
				DoPawnToxicDamage(allPawnsSpawned[i]);
			}
		}

		public static void DoPawnToxicDamage(Pawn p)
		{
			if ((!p.Spawned || !p.Position.Roofed(p.Map)) && p.RaceProps.IsFlesh)
			{
				float num = 0.028758334f;
				num *= p.GetStatValue(StatDefOf.ToxicSensitivity);
				if (num != 0f)
				{
					float num2 = Mathf.Lerp(0.85f, 1.15f, Rand.ValueSeeded(p.thingIDNumber ^ 0x46EDC5D));
					num *= num2;
					HealthUtility.AdjustSeverity(p, HediffDefOf.ToxicBuildup, num);
				}
			}
		}

		public override void DoCellSteadyEffects(IntVec3 c, Map map)
		{
			if (c.Roofed(map))
			{
				return;
			}
			List<Thing> thingList = c.GetThingList(map);
			for (int i = 0; i < thingList.Count; i++)
			{
				Thing thing = thingList[i];
				if (thing is Plant)
				{
					if (thing.def.plant.dieFromToxicFallout && Rand.Value < PlantKillChance)
					{
						thing.Kill();
					}
				}
				else if (thing.def.category == ThingCategory.Item)
				{
					CompRottable compRottable = thing.TryGetComp<CompRottable>();
					if (compRottable != null && (int)compRottable.Stage < 2)
					{
						compRottable.RotProgress += CorpseRotProgressAdd;
					}
				}
			}
		}

		public override void GameConditionDraw(Map map)
		{
			for (int i = 0; i < overlays.Count; i++)
			{
				overlays[i].DrawOverlay(map);
			}
		}

		public override float SkyTargetLerpFactor(Map map)
		{
			return GameConditionUtility.LerpInOutValue(this, TransitionTicks, ToxicPerDay);
		}

		public override SkyTarget? SkyTarget(Map map)
		{
			return new SkyTarget(0.85f, ToxicFalloutColors, 1f, 1f);
		}

		public override float AnimalDensityFactor(Map map)
		{
			return 0f;
		}

		public override float PlantDensityFactor(Map map)
		{
			return 0f;
		}

		public override bool AllowEnjoyableOutsideNow(Map map)
		{
			return false;
		}

		public override List<SkyOverlay> SkyOverlays(Map map)
		{
			return overlays;
		}
	}
}
