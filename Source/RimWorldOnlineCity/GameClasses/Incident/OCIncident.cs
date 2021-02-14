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
using OCUnion.Common;

namespace RimWorldOnlineCity
{
    public abstract class OCIncident
    {
        public string attacker;
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
            switch (faction.ToLower().Trim())
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
                    return Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.defName == "Pirate" && f.def.permanentEnemy == true && f.def.humanlikeFaction == true && f.def.techLevel >= TechLevel.Industrial && f.def.techLevel < TechLevel.Archotech);
                case "tribe": default:
                    return Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.permanentEnemy == true && f.def.humanlikeFaction == true && f.def.techLevel <= TechLevel.Medieval);
                }
        }

        /// <summary>
        /// Расчитывает стоимость инциндента, и изымает её.
        /// </summary>
        /// <param name="command">Текстовая команда для чата</param>
        /// <param name="onliCheck">Только рассчитать, не изымая.</param>
        /// <error>Ошибка, или результат при onliCheck</error>
        /// <returns>Строка с количество требуемой стоимости</returns>
        public static string GetCostOnGameByCommand(string command, bool onliCheck, out string error)
        {
            Loger.Log("IncidentLod OCIncident.GetCostOnGameByCommand 1 command:" + command);
            //разбираем аргументы в кавычках '. Удвоенная кавычка указывает на её символ.
            string cmd;
            List<string> args;
            ChatUtils.ParceCommand(command, out cmd, out args);

            if (args.Count < 3)
            {
                error = "OC_Incidents_OCIncident_WrongArg".Translate().ToString();
                Loger.Log("IncidentLod OCIncident.GetCostOnGameByCommand error:" + error);
                return null;
            }

            // /call raid '111' 1 10 air tribe

            //проверка, что денег хватает
            int cost = OCIncident.CalculateRaidCost(args[0].ToLower(), Int64.Parse(args[2])
                , args.Count > 3 ? Int32.Parse(args[3]) : 1
                , args.Count > 4 ? args[4].ToLower() : null
                , args.Count > 5 ? args[5].ToLower() : null);
            int gold = -1;

            Map map = null;
            if (cost > 0)
            {
                gold = GameUtils.FindThings(ThingDefOf.Gold, 0, false);
            }

            if (cost < 0 || gold < 0 || gold < cost)
            {
                error = cost < 0 || gold < 0
                    ? "OC_Incidents_OCIncident_WealthErr".Translate().ToString() + $" cost={cost} gold={gold}"
                    : "OC_Incidents_OCIncident_GoldErr".Translate(gold, cost, cost - gold).ToString();
                Loger.Log("IncidentLod OCIncident.GetCostOnGameByCommand error:" + error);
                return null;
            }

            if (onliCheck)
            {
                error = null;
                return "OC_Incidents_OCIncident_NotEnoughGold".Translate(cost);
            }

            Loger.Log("IncidentLod OCIncident.GetCostOnGameByCommand 2");

            //отнимаем нужное кол-во денег(золото или серебро... или что-нибудь ещё)
            GameUtils.FindThings(ThingDefOf.Gold, cost, false);

            Loger.Log("IncidentLod OCIncident.GetCostOnGameByCommand 3");
            //принудительное сохранение
            if (!SessionClientController.Data.BackgroundSaveGameOff)
                SessionClientController.SaveGameNow(true);
            Loger.Log("IncidentLod ChatController.AfterStartIncident 4");

            error = null;
            return "OC_Incidents_OCIncident_GoldPay".Translate(cost);
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

        public static int CalculateRaidCost(string type, long serverId, int mult, string arrivalModes, string faction)
        {
            Loger.Log("IncidentLod OCIncident.CalculateRaidCost 1");
            //var serverId = UpdateWorldController.GetServerInfo(wo).ServerId;
            var target = UpdateWorldController.GetOtherByServerId(serverId) as BaseOnline;
            if (target == null) return -1;            
            var costs = target.Player.CostWorldObjects(serverId);
            var cost = costs.MarketValue + costs.MarketValuePawn;
            if (cost <= 0) return -1;

            //{цена поселения защитника/100 000}^(2/3) * 100 * lvl ^(3/2)
            var raidCost = (int)((float)Math.Pow(cost / 100000f, 2f / 3f) * 100f 
                * (float)Math.Pow((float)mult, 3f / 2f) //* (float)mult
                * (float)SessionClientController.Data.GeneralSettings.IncidentCostPrecent / 100f
                * (type == "inf" ? 3f 
                    : type == "acid" ? 2f
                    : type == "raid" 
                    ?   (arrivalModes == "random" ? 1.5f : arrivalModes == "air" ? 1.4f : 1f)
                      * (faction == "mech" ? 5f : faction == "pirate" ? 2f : 1f)
                    : 1f)
                );

            Loger.Log($"IncidentLod OCIncident.CalculateRaidCost({serverId}, {mult}). targetCost={(int)cost} raidCost={raidCost}");

            return SessionClientController.Data.IsAdmin ? 1 : raidCost;
        }
    }
}
