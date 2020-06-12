using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Verse;

namespace RimWorldOnlineCity.GameClasses.Harmony
{
    public static class GameFix
    {
        /*
        [HarmonyPatch(typeof(ThingOwnerUtility))]
        [HarmonyPatch("GetAllThingsRecursively<Thing>", new Type[] {
                typeof(Verse.Map),
                typeof(Verse.ThingRequest),
                typeof(List<Thing>),
                typeof(System.Boolean),
                typeof(System.Predicate<Thing>),
                typeof(System.Boolean)
            })]
        public static class ThingOwnerUtility_GetAllThingsRecursively_Patch
        {
            private static Object SuncObj = new Object();

            [HarmonyPrefix]
            public static void Prefix()
            {
                Monitor.Enter(SuncObj);
            }

            [HarmonyPostfix]
            public static void Postfix()
            {
                Monitor.Exit(SuncObj);
            }
        }
        */

        //GetAllThingsRecursively(IThingHolder holder, List<Thing> outThings, bool allowUnreal = true, Predicate<IThingHolder> passCheck = null)
        [HarmonyPatch(typeof(ThingOwnerUtility))]
        [HarmonyPatch("GetAllThingsRecursively", new Type[] {
                typeof(IThingHolder),
                typeof(List<Thing>),
                typeof(bool),
                typeof(Predicate<IThingHolder>)
            })]
        public static class ThingOwnerUtility_GetAllThingsRecursively_Patch
        {
            public static Object SuncObj = new Object();

            [HarmonyPrefix]
            public static void Prefix()
            {
                Monitor.Enter(SuncObj);
            }

            [HarmonyPostfix]
            public static void Postfix()
            {
                Monitor.Exit(SuncObj);
            }
        }

        //GetAllThingsRecursively(IThingHolder holder, List<Thing> outThings, bool allowUnreal = true, Predicate<IThingHolder> passCheck = null)
        [HarmonyPatch(typeof(WealthWatcher))]
        [HarmonyPatch("CalculateWealthItems")]
        public static class WealthWatcher_CalculateWealthItems_Patch
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                Monitor.Enter(ThingOwnerUtility_GetAllThingsRecursively_Patch.SuncObj);
            }

            [HarmonyPostfix]
            public static void Postfix()
            {
                Monitor.Exit(ThingOwnerUtility_GetAllThingsRecursively_Patch.SuncObj);
            }
        }
    }
}
