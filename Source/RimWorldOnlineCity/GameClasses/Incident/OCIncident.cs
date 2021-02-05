using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transfer;
using RimWorld;
using Verse;
using Transfer.ModelMails;
using RimWorld.Planet;
using OCUnion;

namespace RimWorldOnlineCity
{
    public abstract class OCIncident
    {
        public int mult = 1;
        public IncidentStrategys strategy = IncidentStrategys.ImmediateAttack;
        public IncidentArrivalModes arrivalMode = IncidentArrivalModes.EdgeWalkIn;
        public string faction = null;
        public IncidentParms parms;
        public WorldObject place;

        public abstract bool TryExecuteEvent();

        public static RaidStrategyDef GetStrategy(IncidentStrategys strat)
        {
            return RaidStrategyDefOf.ImmediateAttack;
        }

        public static PawnsArrivalModeDef GetArrivalMode(IncidentArrivalModes arrive)
        {
            switch (arrive)
            {
                case IncidentArrivalModes.EdgeWalkIn:
                    return PawnsArrivalModeDefOf.EdgeWalkIn;
                case IncidentArrivalModes.RandomDrop:
                    return PawnsArrivalModeDefOf.RandomDrop;
                case IncidentArrivalModes.CenterDrop:
                    return PawnsArrivalModeDefOf.CenterDrop;
                default:
                    return PawnsArrivalModeDefOf.EdgeWalkIn;
            }
        }
        public Faction GetFaction()
        {
            switch (faction)
            {
                case "mech":
                    return Find.FactionManager.OfMechanoids;
                case "pirate":
                    /*Faction fac = null;
                    bool flag = false;
                    while (!flag)  //так делать очень плохо
                    {
                        fac = Find.FactionManager.RandomEnemyFaction(false, false, true, TechLevel.Industrial);
                        if(fac.def.techLevel <= TechLevel.Archotech && fac.def.techLevel >= TechLevel.Medieval)
                        {
                            flag = true;
                        }
                        
                    }
                    return fac; */  // может оно не глючит?
                    //todo: поиск начинает глючить при добавлении фракций из модов
                    return Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.defName == "Pirate" && f.def.techLevel >= TechLevel.Industrial && f.def.techLevel < TechLevel.Archotech);
                case "tribe":
                    return Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.defName == "Tribe" && f.def.techLevel <= TechLevel.Medieval);
                default:
                    return Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.defName == "Tribe" && f.def.techLevel <= TechLevel.Medieval); ;
            }
        }
        
        public float CalculatePoints()
        {
            Loger.Log("IncidentLod OCIncident.CalculatePoints 1");
            var target = (place as Settlement)?.Map ?? Find.CurrentMap;

            float points = StorytellerUtility.DefaultThreatPointsNow(target);
            //Можно переопределить формулу в каждом потомке ИЛИ проверять остальные параметры и добавлять множитель
            var resultPoints = points * mult
                * (float)SessionClientController.Data.GeneralSettings.IncidentPowerPrecent / 100f;

            Loger.Log($"CalculatePoints(). points={(int)points} resultPoints={resultPoints}");
            Loger.Log("IncidentLod OCIncident.CalculatePoints 2");
            return resultPoints;
        }

        public static int CalculateRaidCost(long serverId, int mult)
        {
            Loger.Log("IncidentLod OCIncident.CalculateRaidCost 1");
            //var serverId = UpdateWorldController.GetServerInfo(wo).ServerId;
            var target = UpdateWorldController.GetOtherByServerId(serverId) as BaseOnline;
            if (target == null) return -1;            
            var costs = target.Player.CostWorldObjects(serverId);
            var cost = costs.MarketValue + costs.MarketValuePawn;
            if (cost <= 0) return -1;

            //{цена поселения защитника/100 000}^(2/3) * 100 * lvl
            var raidCost = (int)(Math.Pow(cost / 100000f, 2f / 3f) * 100f * (float)mult
                * (float)SessionClientController.Data.GeneralSettings.IncidentCostPrecent / 100f);

            Loger.Log($"CalculateRaidCost({serverId}, {mult}). targetCost={(int)cost} raidCost={raidCost}");
            Loger.Log("IncidentLod OCIncident.CalculateRaidCost 2");
            return raidCost;
        }
    }
}
