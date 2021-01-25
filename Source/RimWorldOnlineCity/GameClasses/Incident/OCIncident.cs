using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transfer;
using RimWorld;
using Verse;

namespace RimWorldOnlineCity
{
    public abstract class OCIncident
    {
        public float mult = 1;
        public IncidentStrategys strategy = IncidentStrategys.ImmediateAttack;
        public IncidentArrivalModes arrivalMode = IncidentArrivalModes.EdgeWalkIn;
        public string faction = null;

        public abstract bool TryExecuteEvent();

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
