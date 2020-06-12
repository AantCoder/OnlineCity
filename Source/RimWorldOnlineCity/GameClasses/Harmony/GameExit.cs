using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Profile;

namespace RimWorldOnlineCity
{
    public class GameExit
    {
        public static Action BeforeExit = null;
    }
    
    [HarmonyPatch(typeof(MemoryUtility))]
    [HarmonyPatch("ClearAllMapsAndWorld")]
    internal class MemoryUtility_ClearAllMapsAndWorld_Patch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (GameExit.BeforeExit != null) GameExit.BeforeExit();
        }
    }

    [HarmonyPatch(typeof(GenScene))]
    [HarmonyPatch("GoToMainMenu")]
    internal class GenScene_GoToMainMenu_Patch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (GameExit.BeforeExit != null) GameExit.BeforeExit();
        }
    }

    [HarmonyPatch(typeof(PlayDataLoader))]
    [HarmonyPatch("ClearAllPlayData")]
    internal class PlayDataLoader_ClearAllPlayData_Patch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (GameExit.BeforeExit != null) GameExit.BeforeExit();
        }
    }

    [HarmonyPatch(typeof(Root_Entry))]
    [HarmonyPatch("Start")]
    internal class Root_Entry_Start_Patch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (GameExit.BeforeExit != null) GameExit.BeforeExit();
        }
    }

    [HarmonyPatch(typeof(UIRoot_Entry))]
    [HarmonyPatch("DoMainMenu")]
    internal class UIRoot_Entry_DoMainMenu_Patch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (GameExit.BeforeExit != null) GameExit.BeforeExit();
        }
    }
    
}
