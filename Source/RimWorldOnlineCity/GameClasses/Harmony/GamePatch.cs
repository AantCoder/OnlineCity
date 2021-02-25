using HarmonyLib;
using OCUnion;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace RimWorldOnlineCity.GameClasses.Harmony
{
    /// ////////////////////////////////////////////////////////////

    //Следим за включением режима разработчика, если он отключен
    [HarmonyPatch(typeof(PrefsData))]
    [HarmonyPatch("Apply")]
    internal class PrefsData_Apply_Patch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Current.Game == null) return;
            if (!SessionClient.Get.IsLogined) return;

            if (SessionClientController.Data.DisableDevMode)
            {
                if (Prefs.DevMode) Prefs.DevMode = false;
            }
        }

    }

    /// ////////////////////////////////////////////////////////////

    //Выключаем настройки рассказчика
    [HarmonyPatch(typeof(Page_SelectStorytellerInGame))]
    [HarmonyPatch("DoWindowContents")]
    internal class Page_SelectStorytellerInGame_DoWindowContents_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Page_SelectStorytellerInGame __instance)
        {
            if (Current.Game == null) return true;
            if (!SessionClient.Get.IsLogined) return true;
            if (Prefs.DevMode) return true; //чтобы разрешить тем, у кого есть право на админку

            if (SessionClientController.Data.GeneralSettings.DisableGameSettings)
            {
                Loger.Log("Page_SelectStorytellerInGame_DoWindowContents_Patch DisableGameSettings");
                __instance.Close();
                return false;
            }

            return true;
        }

    }

    /// ////////////////////////////////////////////////////////////

    //Выключаем настройки модов
    [HarmonyPatch(typeof(HugsLib.Utils.HugsLibUtility))]
    [HarmonyPatch("OpenModSettingsDialog")]
    internal class HugsLibUtility_OpenModSettingsDialog_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (Current.Game == null) return true;
            if (!SessionClient.Get.IsLogined) return true;
            if (Prefs.DevMode) return true; //чтобы разрешить тем, у кого есть право на админку

            if (SessionClientController.Data.GeneralSettings.DisableGameSettings)
            {
                Loger.Log("HugsLibUtility_OpenModSettingsDialog_Patch DisableGameSettings");
                return false;
            }

            return true;
        }

    }

    /// ////////////////////////////////////////////////////////////

    //Меняем начальный год
    [HarmonyPatch(typeof(GenDate), "Year")]
    internal class GenDatePatch
    {
        public static void Postfix(long absTicks, float longitude, ref int __result)
        {
            if (!SessionClient.Get.IsLogined) return;

            int needYear = SessionClientController.Data.GeneralSettings.StartGameYear;
            if (needYear < 0 || needYear == 5500) return;

            var longAdj = GenDate.TimeZoneAt(longitude) * 2500L;
            __result = needYear + (int)((absTicks + longAdj) / 3600000f);
        }
    }

    /// ////////////////////////////////////////////////////////////

    //Подключаемся к разрешению ссылок (Например Faction_10) при загрузке кусочков сейвов: при создании вещщей в GameXMLUtils.FromXml
    //public T ObjectWithLoadID<T>(string loadID)
    //[HarmonyPatch(typeof(LoadedObjectDirectory), new Type[] { typeof(Faction) })]
    //[HarmonyPatch("ObjectWithLoadID")]
    /* крашит игру
    [HarmonyPatch()]
    public class LoadedObjectDirectory_ObjectWithLoadID_Patch
    {
        static MethodBase TargetMethod()
        {
            return typeof(LoadedObjectDirectory).GetMethod("ObjectWithLoadID").MakeGenericMethod(typeof(Faction));
        }

        [HarmonyPrefix]
        public static bool Prefix(string loadID, ref object __result)
        {
            if (!GameXMLUtils.FromXmlIsActive) return true;
            if (Current.Game == null) return true;
            if (loadID == null) return true;

            if (loadID.StartsWith("Faction_"))
            {
                Loger.Log("LoadedObjectDirectory_ObjectWithLoadID_Patch " + loadID);
                var faction = Find.FactionManager.AllFactions.FirstOrDefault(f => f.GetUniqueLoadID() == loadID);
                if (faction != null)
                {
                    __result = faction;
                    return false;
                }
                return true;
            }
            return true;
        }
    }
    */
    /// ////////////////////////////////////////////////////////////

    //Выключаем настройки модов
    [HarmonyPatch(typeof(CrossRefHandler))]
    [HarmonyPatch("ResolveAllCrossReferences")]
    public class CrossRefHandler_ResolveAllCrossReferences_Patch
    {
        public static List<IExposable> crossReferencingExposables = new List<IExposable>();

        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (!GameXMLUtils.FromXmlIsActive) return true;
            if (Current.Game == null) return true;

            if (Scribe.loader?.crossRefs?.crossReferencingExposables == null) return true;

            Scribe.loader.crossRefs.crossReferencingExposables.AddRange(crossReferencingExposables
                .Where(e => !Scribe.loader.crossRefs.crossReferencingExposables.Any(ee => ee == e))
                .ToList());

            crossReferencingExposables = new List<IExposable>();

            return true;
        }

    }

    /// ////////////////////////////////////////////////////////////
}
