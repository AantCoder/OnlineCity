using HarmonyLib;
using HugsLib;
using Model;
using OCUnion;
using OCUnion.Common;
using RimWorld;
using RimWorld.Planet;
using RimWorldOnlineCity.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity.GameClasses.Harmony
{
    internal class GamePresetFiles
    {

        private static string PrepareKey(byte[] data)
        {
            var key = FileChecker.GetCheckSum(data);
            key = FileChecker.GetCheckSum(key + "dfd%>*<" + (ModBaseData.GlobalData?.LastIP?.Value ?? "##"));
            key = key.Length > 30 ? key.Substring(4, 16) : key;
            return key;
        }

        [HarmonyPatch(typeof(GameDataSaveLoader))]
        [HarmonyPatch("SaveIdeo")]
        internal class GameDataSaveLoader_SaveIdeo_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(Ideo ideo, string absFilePath)
            {
                if (Current.Game == null) return;
                if (!SessionClient.Get.IsLogined) return;
                try
                {
                    var key = PrepareKey(File.ReadAllBytes(absFilePath));
                    Loger.Log("PresetSaveIdeo: " + absFilePath + " " + (ModBaseData.GlobalData?.LastIP?.Value ?? "##") + " " + key);

                    var list = ModBaseData.GlobalData?.LastCash?.Value ?? "";
                    list += "|" + key;
                    if (ModBaseData.GlobalData?.LastCash != null)
                    {
                        ModBaseData.GlobalData.LastCash.Value = list;
                        HugsLibController.SettingsManager.SaveChanges();
                    }
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(GameDataSaveLoader))]
        [HarmonyPatch("TryLoadIdeo")]
        internal class GameDataSaveLoader_TryLoadIdeo_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(string absPath, out Ideo ideo, ref bool __result)
            {
                ideo = null;
                __result = false;
                if (Current.Game == null) return true;
                if (!SessionClient.Get.IsLogined) return true;
                if (!SessionClientController.Data.DisableDevMode) return true;

                try
                {
                    var key = PrepareKey(File.ReadAllBytes(absPath));
                    Loger.Log("PresetLoadIdeo: " + absPath + " " + (ModBaseData.GlobalData?.LastIP?.Value ?? "##") + " " + key);

                    var list = ModBaseData.GlobalData?.LastCash?.Value ?? "";
                    var ll = new HashSet<string>(list.Split('|'));
                    if (ll.Contains(key)) return true;

                    var msg = "OCity_GamePresetFiles_IdeologyNotCreatedDuringANetworkGame".Translate(); //В сетевой игре можно загружать только сохраненные при создании сетевой игры на этом же сервере
                    Find.WindowStack.Add(new Dialog_Input("OCity_Dialog_CreateWorld_BtnCancel".Translate(), msg, true));
                    return false;
                }
                catch 
                {
                    return false;
                }
            }
        }

        [HarmonyPatch(typeof(GameDataSaveLoader))]
        [HarmonyPatch("SaveXenotype")]
        internal class GameDataSaveLoader_SaveXenotype_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(CustomXenotype xenotype, string absFilePath)
            {
                if (Current.Game == null) return;
                if (!SessionClient.Get.IsLogined) return;
                try
                {
                    var key = PrepareKey(File.ReadAllBytes(absFilePath));
                    Loger.Log("PresetSaveXenotype: " + absFilePath + " " + (ModBaseData.GlobalData?.LastIP?.Value ?? "##") + " " + key);

                    var list = ModBaseData.GlobalData?.LastCash?.Value ?? "";
                    list += "|" + key;
                    if (ModBaseData.GlobalData?.LastCash != null)
                    {
                        ModBaseData.GlobalData.LastCash.Value = list;
                        HugsLibController.SettingsManager.SaveChanges();
                    }
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(GameDataSaveLoader))]
        [HarmonyPatch("TryLoadXenotype")]
        internal class GameDataSaveLoader_TryLoadXenotype_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(string absPath, out CustomXenotype xenotype, ref bool __result)
            {
                xenotype = null;
                __result = false;
                if (Current.Game == null) return true;
                if (!SessionClient.Get.IsLogined) return true;
                if (!SessionClientController.Data.DisableDevMode) return true;

                try
                {
                    var key = PrepareKey(File.ReadAllBytes(absPath));
                    Loger.Log("PresetLoadXenotype: " + absPath + " " + (ModBaseData.GlobalData?.LastIP?.Value ?? "##") + " " + key);

                    var list = ModBaseData.GlobalData?.LastCash?.Value ?? "";
                    var ll = new HashSet<string>(list.Split('|'));
                    if (ll.Contains(key)) return true;

                    var msg = "OCity_GamePresetFiles_XenotypeNotCreatedDuringANetworkGame".Translate();
                    Find.WindowStack.Add(new Dialog_Input("OCity_Dialog_CreateWorld_BtnCancel".Translate(), msg, true));
                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

    }
}
