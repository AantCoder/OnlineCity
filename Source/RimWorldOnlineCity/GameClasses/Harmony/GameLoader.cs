using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorldOnlineCity
{

    public class GameLoades
    {
        public static Action AfterLoad = null;
    }
    
    [HarmonyPatch(typeof(SavedGameLoaderNow))]
    [HarmonyPatch("LoadGameFromSaveFileNow")]
    [HarmonyPatch(new[] { typeof(string) })]
    internal class SavedGameLoader_LoadGameFromSaveFile_Patch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (GameLoades.AfterLoad != null) GameLoades.AfterLoad();
        }
    }
    
}
