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
            try
            {
                if (SessionClientController.Data.ChatNotReadPost > 0)
                {
                    var queueRect = new Rect(
                        rect.xMax - 20f - 6f,
                        0f,
                        20f,
                        20f).CenteredOnYIn(rect);
                    GameUtils.DrawLabel(queueRect, Color.white, Color.grey, SessionClientController.Data.ChatNotReadPost);
                }
            }
            catch
            {
            }
        }
        
        public override void Activate()
        {
            Dialog_MainOnlineCity.ShowHide();
        }


    }

    public class MainTabWindow_OC : MainTabWindow_Menu
    {
    }
}
