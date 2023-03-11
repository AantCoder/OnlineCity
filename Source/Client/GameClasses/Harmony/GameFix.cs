using HarmonyLib;
using OCUnion;
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

        ////Заплатка от зависания, когда неверно удаляют holdingOwner
        //[HarmonyPatch(typeof(ThingOwner))]
        //[HarmonyPatch("ClearAndDestroyContents")]
        //public static class ThingOwner_ClearAndDestroyContents_Patch
        //{
        //    [HarmonyPrefix]
        //    public static void Prefix(ThingOwner __instance)
        //    {
        //        var that = __instance as ThingOwner<Thing>;
        //        if (that == null) return;
        //        for (int i = that.Count - 1; i >= 0; i--)
        //        {
        //            if (that[i].holdingOwner != that)
        //            {
        //                Loger.Log("PatchClearAndDestroyContents " + that[i].Label);
        //                that[i].holdingOwner = that;
        //            }
        //        }
        //    }
        //}



    }
}
