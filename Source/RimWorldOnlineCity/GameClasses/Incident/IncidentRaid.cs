﻿using RimWorld;
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
            parms.faction = null;
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
                case Transfer.IncidentArrivalModes.RandomDrop:
                    return PawnsArrivalModeDefOf.RandomDrop;
                case Transfer.IncidentArrivalModes.CenterDrop:
                    return PawnsArrivalModeDefOf.CenterDrop;
                default:
                    return PawnsArrivalModeDefOf.EdgeWalkIn;
            }
        }
    }
}
