using HugsLib.Utils;
using RimWorld;
using System;
using System.IO;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld.Planet;
using Transfer;
using OCUnion;

namespace RimWorldOnlineCity
{
    public class MainButtonWorker_OC : MainButtonWorker
    {
        public static MainButtonWorker_OC Single = null;
        public static void ShowOnStart()
        {
            if (Single != null) Single.def.buttonVisible = true;
        }

        /// <summary>
        /// Первая инициализация проекта. Остальная после входа в игру (после логина или регистрации).
        /// </summary>
        public MainButtonWorker_OC()
        {
            SessionClientController.Init();
        }

        public override void DoButton(Rect rect)
        {
            Single = this;
            if (!MainHelper.DebugMode)
                this.def.buttonVisible = SessionClient.Get.IsLogined;
            base.DoButton(rect);
        }
        
        public override void Activate()
        {
            Find.WindowStack.Add(new Dialog_MainOnlineCity());
        }


    }

    public class MainTabWindow_OC : MainTabWindow_Menu
    {
    }
}
