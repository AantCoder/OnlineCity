using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
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


}
