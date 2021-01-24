using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorldOnlineCity
{
    public class IncidentRaid : OCIncident //попытка использовать уже существующий ивент
    {
        public int cost;

        //protected IncidentParms Parms => Find.CurrentMap != null ? StorytellerUtility.DefaultParmsNow(incident.category, Find.CurrentMap) : null;

        public override bool TryExecuteEvent()
        {
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatSmall, Current.Game.AnyPlayerHomeMap);
            parms.raidStrategy = GetStrategy(strategy);
            parms.raidArrivalMode = GetArrivalMode(arrivalMode);
            parms.customLetterLabel = "test raid";
            parms.customLetterText = "teast raid again";
            parms.biocodeApparelChance = 1f;
            parms.biocodeWeaponsChance = 1f;
            parms.raidNeverFleeIndividual = true;
            parms.faction = GetFaction();
            parms.forced = true;  //игнорировать все условия для события
            parms.target = Find.CurrentMap;
            parms.points = StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap) * mult;
            //parms.points = StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap) * mult >= StorytellerUtility.GlobalPointsMax ? StorytellerUtility.GlobalPointsMax : StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap) * mult;

            if (!IncidentDefOf.RaidEnemy.Worker.TryExecute(parms))
            {
                Messages.Message($"Failed_Test_Raid", MessageTypeDefOf.RejectInput);
                return false;
            }
            return true;
        }

        public static RaidStrategyDef GetStrategy(Transfer.IncidentStrategys strat)
        {
            return RaidStrategyDefOf.ImmediateAttack;
        }

        public static PawnsArrivalModeDef GetArrivalMode(Transfer.IncidentArrivalModes arrive)
        {
            switch (arrive)
            {
                case Transfer.IncidentArrivalModes.EdgeWalkIn:
                    return PawnsArrivalModeDefOf.EdgeWalkIn;
                case Transfer.IncidentArrivalModes.RandomDrop:
                    return PawnsArrivalModeDefOf.RandomDrop;
                case Transfer.IncidentArrivalModes.CenterDrop:
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
                    return fac; */

                   return Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.defName == "Pirate" && f.def.techLevel >= TechLevel.Industrial && f.def.techLevel < TechLevel.Archotech);
                case "tribe":
                    return Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.defName == "Tribe" && f.def.techLevel <= TechLevel.Medieval);
                default:
                    return null;
            }
        }
    }
}
