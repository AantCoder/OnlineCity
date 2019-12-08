using Harmony;
using OCUnion;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Verse;
using Verse.AI;

namespace RimWorldOnlineCity.GameClasses
{
    public static class GameAttackTrigger_Patch
    {
        public static Dictionary<Map, GameAttacker> ActiveAttacker = new Dictionary<Map, GameAttacker>();
        public static Dictionary<Map, GameAttackHost> ActiveAttackHost = new Dictionary<Map, GameAttackHost>();

        private static Func<Pawn_JobTracker, Pawn> GetPawnFromPawn_JobTracker = null;

        public static Pawn GetPawn(this Pawn_JobTracker keeper)
        {
            /*
            FieldInfo fieldInfo = typeof(Pawn_JobTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic);
            Pawn result = (Pawn)fieldInfo.GetValue(keeper);
            return result;
            */
            if (GetPawnFromPawn_JobTracker == null)
            {
                ParameterExpression keeperArg = Expression.Parameter(typeof(Pawn_JobTracker), "keeper"); // SecretKeeper keeper argument
                Expression secretAccessor = Expression.Field(keeperArg, "pawn"); // keeper._secret
                var lambda = Expression.Lambda<Func<Pawn_JobTracker, Pawn>>(secretAccessor, keeperArg);
                GetPawnFromPawn_JobTracker = lambda.Compile(); // Получается функция return result = keeper._secret;
            }
            return GetPawnFromPawn_JobTracker(keeper);
        }

    }

    /// <summary>
    /// Хост следит за уничтожением объектов, чтобы передать их
    /// </summary>
    [HarmonyPatch(typeof(Thing))]
    [HarmonyPatch("Destroy")]
    public static class Thing_Destroy_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(Thing __instance)
        {
            if (GameAttackTrigger_Patch.ActiveAttackHost.Count == 0) return;
            if (__instance is Mote) return;
            if (__instance is Projectile) return;
            if (__instance is Plant) return;
            if (__instance is Filth) return;

            var that = __instance;
            if (that.Map == null) return;
            GameAttackHost client;
            if (!GameAttackTrigger_Patch.ActiveAttackHost.TryGetValue(that.Map, out client)) return;
            client.UIEventChange(that, true);
        }
    }


    /// <summary>
    /// Хост следит за уничтожением объектов, чтобы передать их
    /// </summary>
    [HarmonyPatch(typeof(Thing))]
    [HarmonyPatch("DeSpawn")]
    public static class Thing_DeSpawn_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(Thing __instance)
        {
            if (GameAttackTrigger_Patch.ActiveAttackHost.Count == 0) return;
            if (__instance is Mote) return;
            if (__instance is Projectile) return;
            if (__instance is Plant) return;
            if (__instance is Filth) return;

            if (__instance is Corpse) return;
            if (__instance is Pawn) return; //не отслеживаем пешек! в остальном это копия Thing_Destroy_Patch выше

            var that = __instance;
            if (that.Map == null) return;
            GameAttackHost client;
            if (!GameAttackTrigger_Patch.ActiveAttackHost.TryGetValue(that.Map, out client)) return;
            client.UIEventChange(that, true);
        }
    }

    [HarmonyPatch(typeof(Thing))]
    [HarmonyPatch("PostApplyDamage")]
    public static class Thing_PostApplyDamage_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Thing __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (GameAttackTrigger_Patch.ActiveAttackHost.Count == 0) return;
            if (__instance is Mote) return;
            if (__instance is Projectile) return;
            if (__instance is Plant) return;
            if (__instance is Filth) return;

            //Loger.Log("HostAttackUpdate PostApplyDamage1 " + __instance.GetType().ToString() + " " + __instance.Label + " totalDamageDealt=" + totalDamageDealt + " " + dinfo.ToString());

            var that = __instance;
            if (that.Map == null) return;
            GameAttackHost client;
            if (!GameAttackTrigger_Patch.ActiveAttackHost.TryGetValue(that.Map, out client)) return;
            client.UIEventChange(that, false);
        }
    }

    [HarmonyPatch(typeof(ThingWithComps))]
    [HarmonyPatch("PostApplyDamage")]
    public static class ThingWithComps_PostApplyDamage_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Thing __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (GameAttackTrigger_Patch.ActiveAttackHost.Count == 0) return;
            if (__instance is Mote) return;
            if (__instance is Projectile) return;
            if (__instance is Plant) return;
            if (__instance is Filth) return;

            //Loger.Log("HostAttackUpdate PostApplyDamage2 " + __instance.GetType().ToString() + " " + __instance.Label + " totalDamageDealt=" + totalDamageDealt + " " + dinfo.ToString());

            var that = __instance;
            if (that.Map == null) return;
            GameAttackHost client;
            if (!GameAttackTrigger_Patch.ActiveAttackHost.TryGetValue(that.Map, out client)) return;
            client.UIEventChange(that, false);
        }
    }

    [HarmonyPatch(typeof(Thing))]
    [HarmonyPatch("SpawnSetup")]
    public static class Thing_SpawnSetup_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Thing __instance, Map map, bool respawningAfterLoad)
        {
            if (GameAttackTrigger_Patch.ActiveAttackHost.Count == 0) return;
            if (__instance is Mote) return;
            if (__instance is Projectile) return;
            if (__instance is Plant) return;
            if (__instance is Filth) return;

            if (__instance is Pawn) return; //есть отдельный цикл по всем пешкам

            //Loger.Log("HostAttackUpdate SpawnSetup " + __instance.GetType().ToString() + " " + __instance.Label + " respawningAfterLoad=" + respawningAfterLoad);

            var that = __instance;
            if (that.Map == null) return;
            GameAttackHost client;
            if (!GameAttackTrigger_Patch.ActiveAttackHost.TryGetValue(that.Map, out client)) return;
            client.UIEventChange(that, false, true);
        }
    }

    /// <summary>
    /// Следим за командами атакующего игрока
    /// </summary>
    [HarmonyPatch(typeof(Pawn_JobTracker))]
    [HarmonyPatch("StartJob")]
    public static class Pawn_JobTracker_StartJob_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn_JobTracker __instance)
        {
            if (GameAttackTrigger_Patch.ActiveAttacker.Count == 0
                && GameAttackTrigger_Patch.ActiveAttackHost.Count == 0) return;
            var that = __instance;
            var pawn = that.GetPawn();
            var curJob = that.curJob;
            if (pawn.Map == null) return;
            GameAttacker client;
            if (GameAttackTrigger_Patch.ActiveAttacker.TryGetValue(pawn.Map, out client))
            {
                client.UIEventNewJob(pawn, curJob);
            }
            GameAttackHost clientHost;
            if (GameAttackTrigger_Patch.ActiveAttackHost.TryGetValue(pawn.Map, out clientHost))
            {
                clientHost.UIEventNewJob(pawn, curJob);
            }
        }
    }

    /// <summary>
    /// Следим за командами атакующего игрока
    /// </summary>
    [HarmonyPatch(typeof(Pawn_JobTracker))]
    [HarmonyPatch("CleanupCurrentJob")]
    public static class Pawn_JobTracker_CleanupCurrentJob_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn_JobTracker __instance)
        {
            if (GameAttackTrigger_Patch.ActiveAttacker.Count == 0
                && GameAttackTrigger_Patch.ActiveAttackHost.Count == 0) return;
            var that = __instance;
            var pawn = that.GetPawn();
            if (pawn.Map == null) return;
            GameAttacker client;
            if (GameAttackTrigger_Patch.ActiveAttacker.TryGetValue(pawn.Map, out client))
            {
                client.UIEventNewJob(pawn, null);
            }
            GameAttackHost clientHost;
            if (GameAttackTrigger_Patch.ActiveAttackHost.TryGetValue(pawn.Map, out clientHost))
            {
                clientHost.UIEventNewJob(pawn, null);
            }
        }
    }

    /*
    /// <summary>
    /// Сохраняли стек вызовов для отладки
    /// </summary>
    [HarmonyPatch(typeof(MapDeiniter))]
    [HarmonyPatch("Deinit")]
    public static class MapDeiniter_Deinit_Patch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            var stack = "";
            var stackTrace = new StackTrace();
            var frames = stackTrace.GetFrames();
            foreach (var frame in frames)
            {
                var methodDescription = frame.GetMethod();
                stack += Environment.NewLine + methodDescription.Name;
            }
            Loger.Log("MapDeiniter!!! " + stack);
        }
    }
    */

}
