using HarmonyLib;
using OCUnion;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimWorldOnlineCity.GameClasses
{
    public static class GameAttackTrigger_Patch
    {
        /// <summary>
        /// Принудительно устанавливает коэффициент скорости. Отключено, если меньше 0
        /// </summary>
        public static float ForceSpeed = -1f;
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
            if (GameAttackTrigger_Patch.ActiveAttacker.Count == 0
                && GameAttackTrigger_Patch.ActiveAttackHost.Count == 0) return;
            if (__instance is Explosion) return;
            if (__instance is Mote) return;
            if (__instance is Projectile) return;
            //if (__instance is Plant) return;
            if (__instance is Filth) return;

            var that = __instance;
            if (that.Map == null) return;
            GameAttacker client;
            if (GameAttackTrigger_Patch.ActiveAttacker.TryGetValue(that.Map, out client))
            {
                client.UIEventChange(that, true);
            }
            GameAttackHost clientHost;
            if (!GameAttackTrigger_Patch.ActiveAttackHost.TryGetValue(that.Map, out clientHost)) return;
            clientHost.UIEventChange(that, true);
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
            if (GameAttackTrigger_Patch.ActiveAttacker.Count == 0
                && GameAttackTrigger_Patch.ActiveAttackHost.Count == 0) return;
            if (__instance is Explosion) return;
            if (__instance is Mote) return;
            if (__instance is Projectile) return;
            //if (__instance is Plant) return;
            if (__instance is Filth) return;

            if (__instance is Corpse) return;
            if (__instance is Pawn) return; //не отслеживаем пешек! в остальном это копия Thing_Destroy_Patch выше

            var that = __instance;
            if (that.Map == null) return;
            GameAttacker client;
            if (GameAttackTrigger_Patch.ActiveAttacker.TryGetValue(that.Map, out client))
            {
                client.UIEventChange(that, true);
            }
            GameAttackHost clientHost;
            if (!GameAttackTrigger_Patch.ActiveAttackHost.TryGetValue(that.Map, out clientHost)) return;
            clientHost.UIEventChange(that, true);
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
            if (__instance is Explosion) return;
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
            if (__instance is Explosion) return;
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
            if (GameAttackTrigger_Patch.ActiveAttacker.Count == 0
                && GameAttackTrigger_Patch.ActiveAttackHost.Count == 0) return;
            if (__instance is Explosion) return;
            if (__instance is Mote) return;
            if (__instance is Projectile) return;
            //if (__instance is Plant) return;
            if (__instance is Filth) return;

            if (__instance is Pawn) return; //есть отдельный цикл по всем пешкам

            //Loger.Log("HostAttackUpdate SpawnSetup " + __instance.GetType().ToString() + " " + __instance.Label + " respawningAfterLoad=" + respawningAfterLoad);

            var that = __instance;
            if (that.Map == null) return;
            GameAttacker client;
            if (GameAttackTrigger_Patch.ActiveAttacker.TryGetValue(that.Map, out client))
            {
                client.UIEventChange(that, false, true);
            }
            GameAttackHost clientHost;
            if (!GameAttackTrigger_Patch.ActiveAttackHost.TryGetValue(that.Map, out clientHost)) return;
            clientHost.UIEventChange(that, false, true);
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

    /// <summary>
    /// Следим за командами атакующего игрока
    /// </summary>
    [HarmonyPatch(typeof(ITab_Pawn_Gear))]
    [HarmonyPatch("InterfaceDrop")]
    public static class ITab_Pawn_Gear_InterfaceDrop_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(ITab_Pawn_Gear __instance, Thing t)
        {
            if (GameAttackTrigger_Patch.ActiveAttacker.Count == 0) return true;
            var pawn = (t.ParentHolder as Pawn_InventoryTracker)?.pawn;
            if (pawn == null) return true;
            if (pawn.Map == null) return true;

            //Если дропается вещь не из инвентаря (например оружие или одежда), то это делается джобом а не здесь
            if (!pawn.inventory.innerContainer.Any(tt => tt == t)) return true;

            GameAttacker client;
            if (GameAttackTrigger_Patch.ActiveAttacker.TryGetValue(pawn.Map, out client))
            {
                return client.UIEventInventoryDrop(t);
            }
            return true;
        }
    }

    /*
    [HarmonyPatch(typeof(ThingOwner), new Type[0])]
    [HarmonyPatch("TryDrop")]
    [HarmonyPatch(new Type[] { typeof(Thing ), typeof(IntVec3 ), typeof(Map ), typeof(ThingPlaceMode ), typeof(Thing), typeof(Action<Thing, int>), typeof(Predicate < IntVec3 >) })]
    public static class ThingOwner_TryDrop_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Thing thing, IntVec3 dropLoc, Map map, ThingPlaceMode mode, out Thing lastResultingThing, Action<Thing, int> placedAction = null, Predicate<IntVec3> nearPlaceValidator = null)
        {
            Loger.Log("TryDrop");
            lastResultingThing = null;
            return true;
        }
    }
    */
    /*
    /// <summary>
    /// Следим за командами атакующего игрока
    /// </summary>
    [HarmonyPatch(typeof(ITab_Pawn_Gear))]
    [HarmonyPatch("InterfaceDrop")]
    public static class ITab_Pawn_Gear_InterfaceDrop_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(Thing thing)
        {
            Loger.Log("InterfaceDrop");
        }
    }

    /// <summary>
    /// Следим за командами атакующего игрока
    /// </summary>
    [HarmonyPatch(typeof(GenDrop))]
    [HarmonyPatch("TryDropSpawn")]
    public static class GenDrop_TryDropSpawn_Patch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            try
            {
                Log.Warning("TryDropSpawn1");
                Loger.Log("TryDropSpawn1");
            }
            catch
            {
                Thread.Sleep(20);
                Loger.Log("TryDropSpawn11");
            }
        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                Log.Warning("TryDropSpawn2");
                Loger.Log("TryDropSpawn2");
            }
            catch
            {
                Thread.Sleep(20);
                Loger.Log("TryDropSpawn22");
            }
        }
    }
    */

    /// <summary>
    /// Прехватываем управление коэффициентом скорости игры
    /// </summary>
    [HarmonyPatch(typeof(TickManager))]
    [HarmonyPatch("TickRateMultiplier", MethodType.Getter)]
    public static class TickManager_TickRateMultiplier
    {
        [HarmonyPostfix]
        public static void Postfix(ref float __result)
        {
            if (GameAttackTrigger_Patch.ForceSpeed < 0f) return;
            __result = GameAttackTrigger_Patch.ForceSpeed;
        }
    }

    /// <summary>
    /// Перехватываем скорость передвижения пешки 
    /// </summary>
    [HarmonyPatch(typeof(Pawn))]
    [HarmonyPatch("TicksPerMove")]
    public static class Pawn_TicksPerMove
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance, ref int __result)
        {
            if (GameAttackTrigger_Patch.ActiveAttackHost.Count == 0) return;

            GameAttackHost clientHost;
            if (GameAttackTrigger_Patch.ActiveAttackHost.TryGetValue(__instance.Map, out clientHost))
            {
                clientHost.ControlPawnMoveSpeed(__instance, ref __result);
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
