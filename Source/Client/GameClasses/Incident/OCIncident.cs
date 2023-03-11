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
using Model;

namespace RimWorldOnlineCity
{
    public abstract class OCIncident
    {
        public string attacker;
        public int mult = 1;
        public IncidentStrategys strategy = IncidentStrategys.ImmediateAttack;
        public IncidentArrivalModes arrivalMode = IncidentArrivalModes.EdgeWalkIn;
        public string faction = null;
        public string param = null;
        public IncidentParms parms;
        public WorldObject place;

        public const float hour = 2500f;
        public const float day = 60000f;

        public abstract bool TryExecuteEvent();

        protected Map GetTarget()
        {
            return (place as Settlement)?.Map ?? Find.CurrentMap;
        }

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
            try
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
                                        //todo? поиск начинает глючить при добавлении фракций из модов
                        return Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.defName == "Pirate" && f.def.permanentEnemy == true && f.def.humanlikeFaction == true && f.def.techLevel >= TechLevel.Industrial && f.def.techLevel < TechLevel.Archotech);
                    case "randy":
                        return Find.FactionManager.RandomEnemyFaction(false, true, true);
                    case "tribe":
                    default:
                        return Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.permanentEnemy == true && f.def.humanlikeFaction == true && f.def.techLevel <= TechLevel.Medieval);
                }
            }
            catch
            {
                Loger.Log("IncidentGenerate Error: faction not found");
                return null;
            }
        }

        /// <summary>
        /// Расчитывает стоимость инциндента, и изымает её.
        /// </summary>
        /// <param name="command">Текстовая команда для чата</param>
        /// <param name="onliCheck">Только рассчитать, без действия.</param>
        /// <error>Ошибка, или результат при onliCheck</error>
        /// <returns>Строка с количество требуемой стоимости</returns>
        public static string GetCostOnGameByCommand(string command, bool onliCheck, out string error)
        {
            Loger.Log("IncidentLod OCIncident.GetCostOnGameByCommand 1 command:" + command);
            //разбираем аргументы в кавычках '. Удвоенная кавычка указывает на её символ.
            string cmd;
            List<string> args;
            ChatUtils.ParceCommand(command, out cmd, out args);

            if (args.Count < 3 || args.Any(a => a.Contains("cost=")))
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
            int goldClient = -1;
            int goldServer = -1;

            if (cost > 0)
            {

                SessionClientController.Command((connect) =>
                {
                    goldServer = connect.ExchengeInfo_GetCountThing(ThingTrade.CreateTrade(ThingDefOf.Gold, 1, 0, 1));
                });

                goldClient = GameUtils.FindThings(ThingDefOf.Gold, 0, true);

                if (goldClient >= 0 && goldServer >= 0) gold = goldClient + goldServer;
            }
            if (cost == 0)
            {
                goldClient = 0;
                goldServer = 0;
                gold = 0;
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

            Loger.Log("IncidentLod OCIncident.GetCostOnGameByCommand 2 cost=" + cost);
            Loger.TransLog("IncidentLod cost=" + cost + " Command: " + command);

            /*
            //отнимаем нужное кол-во денег(золото или серебро... или что-нибудь ещё)
            GameUtils.FindThings(ThingDefOf.Gold, cost, false);

            Loger.Log("IncidentLod OCIncident.GetCostOnGameByCommand 3");
            //принудительное сохранение
            if (!SessionClientController.Data.BackgroundSaveGameOff)
                SessionClientController.SaveGameNow(true);
            Loger.Log("IncidentLod ChatController.AfterStartIncident 4");
            */

            //непосредственно действие заказа рейда
            Action goRaid = () =>
            {
                Loger.TransLog("IncidentLod go=" + cost);

                try
                {
                    var mainCannal = SessionClientController.Data.Chats[0];
                    SessionClientController.Command((connect) =>
                    {
                        var res = connect.PostingChat(mainCannal.Id, command + " cost=" + cost.ToString(), true);

                        if (res.Status == 0)
                        {
                            Loger.Log("IncidentLod Raid go " + cost, Loger.LogLevel.EXCHANGE);
                            Find.WindowStack.Add(new Dialog_MessageBox("OC_Incidents_OCIncident_GoldPay".Translate(cost)));
                        }
                        else
                        {
                            //выводим сообщение с ошибкой
                            var errorMessage = string.IsNullOrEmpty(res.Message) ? "Error call" : res.Message.ServerTranslate().ToString();
                            Loger.TransLog("IncidentLod error:" + errorMessage);
                            Loger.Log("IncidentLod Raid errorMessage:" + errorMessage, Loger.LogLevel.ERROR);
                            Find.WindowStack.Add(new Dialog_MessageBox(errorMessage));
                        }
                    });
                    Loger.TransLog("IncidentLod go end");
                }
                catch (Exception exp)
                {
                    Loger.Log("IncidentLod Raid Exception " + exp.ToString());
                }

            };
            //опрашиваем сервер, сколько нужно докинуть на торговый склад
            var countToServer = cost - goldServer;

            if (countToServer > 0)
            {
                //ищим нужные объекты
                var inGame = GameUtils.FindThings(ThingDefOf.Gold, countToServer, true, out var thingSelect);
                Loger.Log($"IncidentLod OCIncident.GetCostOnGameByCommand 3. {countToServer} {thingSelect.Count}");
                if (inGame < countToServer || thingSelect.Count == 0)
                {
                    error = "Не хватает вещей на карте. В наличии".NeedTranslate() + " " + inGame + ". " + "Требуется".NeedTranslate() + " " + countToServer;
                    Loger.Log($"IncidentLod OCIncident.GetCostOnGameByCommand error send to server: need {inGame}/{countToServer}");
                    return null;
                }
                Loger.Log("IncidentLod OCIncident.GetCostOnGameByCommand 4");
                //кидаем на торговый склад
                var fromWorldObject = thingSelect.First().Key.Map.Parent;
                var toWorldObject = new TradeThingsOnline() { Tile = fromWorldObject.Tile }; //для торгового склада требуется только Tile, поэтому не важно есть он или будет создан
                ExchengeUtils.MoveSelectThings(fromWorldObject, toWorldObject, thingSelect, () => 
                {
                    goRaid();
                });
            }
            else
                goRaid();
            Loger.Log("IncidentLod OCIncident.GetCostOnGameByCommand 5");

            error = null;
            return null;
        }
        
        public float CalculatePoints()
        {
            Loger.Log("IncidentLod OCIncident.CalculatePoints 1");
            var target = GetTarget();

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

            if (type == "def") return 0;

            //var serverId = UpdateWorldController.GetServerInfo(wo).ServerId;
            var target = UpdateWorldController.GetOtherByServerId(serverId) as BaseOnline;
            if (target == null) return -1;            
            var costs = target.Player.CostWorldObjects(serverId);
            var cost = costs.MarketValueTotal;
            if (cost <= 0) return -1;

            //{цена поселения защитника/100 000}^(2/3) * 100 * lvl ^(3/2) старое
            //Рейд до 500к = (цена поселения защитник/ 100 000)^(1 / 2) * 80.000 
            //Рейд от 1кк до 5кк = (цена поселения защитник/ 100 000)^(2, 2 / 5, 05) * 27.2864

            // * lvl ^ (9.42 / 8) * модификаторы 
            float type_mult = 1f;
            switch (type)
            {
                case "plague":
                    type_mult = 3f;
                    break;
                case "inf":
                    type_mult = 3f;
                    break;
                case "acid":
                    type_mult = 2f;
                    break;
                case "eclipse":
                    type_mult = 0.5f;
                    break;
                case "emp":
                    type_mult = 1.2f;
                    break;
                case "raid":
                    float arr_mult = 1f;
                    float fac_mult = 1f;
                    switch (arrivalModes)
                    {
                        case "random":
                            arr_mult = 1.5f;
                            break;
                        case "air":
                            arr_mult = 1.2f;
                            break;
                        default:
                            arr_mult = 1f;
                            break;
                    }
                    switch (faction)
                    {
                        case "mech":
                            fac_mult = 3.5f;
                            break;
                        case "pirate":
                            fac_mult = 2f;
                            break;
                        case "randy":
                            fac_mult = 2.5f;
                            break;
                        default:
                            fac_mult = 1f;
                            break;
                    }
                    type_mult = fac_mult * arr_mult;
                    break;
                case "def":
                    type_mult = 0f;
                    break;
                default:
                    type_mult = 1f;
                    break;
            }
            var options_mult = (float)Math.Pow((float)mult, 9.42f / 8f) * SessionClientController.Data.GeneralSettings.IncidentCostPrecent / 100f * type_mult;

            float raidCost = cost <= 500000f ? (int)((float)Math.Pow(cost / 100000f, 1f / 2f) * 246f)
                    : cost <= 5000000f ? (int)((float)Math.Pow(cost / 100000f, 2.2f / 5.05f) * 272.864f)
                    : 15000;
            raidCost *= options_mult;


            if (raidCost > 100000) raidCost = 100000;
            if (raidCost > 0 && raidCost < 1) raidCost = 100;

            Loger.Log($"IncidentLod OCIncident.CalculateRaidCost({serverId}, {mult}). targetCost={(int)cost} raidCost={raidCost}");

            return SessionClientController.Data.IsAdmin && Prefs.DevMode ? 1 : (int)raidCost ;
        }
    }
}
