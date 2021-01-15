using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace OC_Incidents
{
    public class Raid_worker : IncidentWorker_RaidEnemy  //все методы генерации инцидентов с доступом protected. делаем наследника чтобы добраться до них
    {
        public int cost; // задумывался как параметр стоимости рейда

        
        private const float costMult = 1.5f; //модификатор
       
        public bool TryExecuteEvent()  // открытый метод для вызова инцидента
        {
			IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatSmall, Current.Game.AnyPlayerHomeMap);
			parms.customLetterDef = LetterDefOf.NegativeEvent;   //подделку параметров нужно довести до ума.
			if (!TryExecuteWorker(parms))
            {
                Messages.Message($"Failed_Test_Raid", MessageTypeDefOf.RejectInput);
                return false;
            }
            return true;
        }

        /*public bool TryExecuteEvent()  //попытка вызвать инцидент с помощью вложенного воркера-наследника
        {
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatSmall, Current.Game.AnyPlayerHomeMap);
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            parms.customLetterLabel = "test raid";
            parms.customLetterText = "test raid again";
            parms.faction = null; //выбирается рандомно при null. откуда их специально вручную брать?
            parms.forced = true;  //игнорировать все необходимые условия для события
            parms.target = Find.CurrentMap;
            parms.points = StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap) * costMult;
            
            IncidentWorker worker = new IncidentWorker_RaidEnemy();
            worker.def = IncidentDefOf.RaidEnemy;
            if (!worker.TryExecute(parms))
            {
                Messages.Message($"Failed_Test_Raid", MessageTypeDefOf.RejectInput);
                return false;
            }
            return true;
        }*/

        protected override bool TryExecuteWorker(IncidentParms parms) //попытка вывести оповещение приводит к исключению, поэтому пришлось переопределить и убрать сообщение
        {
			ResolveRaidPoints(parms);
			if (!TryResolveRaidFaction(parms))
			{
				return false;
			}
			PawnGroupKindDef combat = PawnGroupKindDefOf.Combat;
			ResolveRaidStrategy(parms, combat);
			ResolveRaidArriveMode(parms);
			parms.raidStrategy.Worker.TryGenerateThreats(parms);
			if (!parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms))
			{
				return false;
			}
			float points = parms.points;
			parms.points = AdjustedRaidPoints(parms.points, parms.raidArrivalMode, parms.raidStrategy, parms.faction, combat);
			List<Pawn> list = parms.raidStrategy.Worker.SpawnThreats(parms);
			if (list == null)
			{
				list = PawnGroupMakerUtility.GeneratePawns(IncidentParmsUtility.GetDefaultPawnGroupMakerParms(combat, parms)).ToList();
				if (list.Count == 0)
				{
					Log.Error("Got no pawns spawning raid from parms " + parms);
					return false;
				}
				parms.raidArrivalMode.Worker.Arrive(list, parms);
			}
			GenerateRaidLoot(parms, points, list);
			TaggedString letterLabel = GetLetterLabel(parms);
			TaggedString letterText = GetLetterText(parms, list);
			PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(list, ref letterLabel, ref letterText, GetRelatedPawnsInfoLetterText(parms), informEvenIfSeenBefore: true);
			List<TargetInfo> list2 = new List<TargetInfo>();
			if (parms.pawnGroups != null)
			{
				List<List<Pawn>> list3 = IncidentParmsUtility.SplitIntoGroups(list, parms.pawnGroups);
				List<Pawn> list4 = list3.MaxBy((List<Pawn> x) => x.Count);
				if (list4.Any())
				{
					list2.Add(list4[0]);
				}
				for (int i = 0; i < list3.Count; i++)
				{
					if (list3[i] != list4 && list3[i].Any())
					{
						list2.Add(list3[i][0]);
					}
				}
			}
			else if (list.Any())
			{
				foreach (Pawn item in list)
				{
					list2.Add(item);
				}
			}
			Map map = (Map)parms.target;
			IntVec3 intVec = DropCellFinder.RandomDropSpot(map);
			Find.LetterStack.ReceiveLetter("костыльное сообщение", "some text", LetterDefOf.NegativeEvent, new TargetInfo(intVec, map));
			
			//SendStandardLetter(letterLabel, letterText, GetLetterDef(), parms, list2); //плохо подделаны параметры?
			parms.raidStrategy.Worker.MakeLords(parms, list);
			LessonAutoActivator.TeachOpportunity(ConceptDefOf.EquippingWeapons, OpportunityType.Critical);
			if (!PlayerKnowledgeDatabase.IsComplete(ConceptDefOf.ShieldBelts))
			{
				for (int j = 0; j < list.Count; j++)
				{
					if (list[j].apparel.WornApparel.Any((Apparel ap) => ap is ShieldBelt))
					{
						LessonAutoActivator.TeachOpportunity(ConceptDefOf.ShieldBelts, OpportunityType.Critical);
						break;
					}
				}
			}
			return true;
		}
    }
}
