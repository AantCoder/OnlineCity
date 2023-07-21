using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Transfer;
using RimWorldOnlineCity.UI;
using OCUnion;
using Model;

namespace RimWorldOnlineCity
{
    public class Dialog_InfoPlayer : Window
    {
        public PanelInfoPlayer info;

        public override Vector2 InitialSize
        {
            get { return LastInitialSize; }
        }

        static Vector2 LastInitialSize = new Vector2(750f, 750f);
        static Vector2 LastInitialPos = new Vector2(-1f, -1f);

        private Dialog_InfoPlayer(PlayerClient player)
        {
            closeOnCancel = true;
            closeOnAccept = false;
            doCloseButton = false;
            doCloseX = true;
            resizeable = true;
            draggable = true; 

            info = new PanelInfoPlayer(player);
        }

        static public void ShowInfo(string name)
        {
            if (SessionClientController.Data.Players.TryGetValue(name, out var pl))
                ShowInfoPlayer(pl);
            else
                Dialog_InfoState.ShowInfoState(name);
        }

        static public void ShowInfoPlayer(PlayerClient player)
        {
            if (player == null) return;
            Find.WindowStack.Add(new Dialog_InfoPlayer(player));
        }

        public override void PreOpen()
        {
            base.PreOpen();
            if (LastInitialPos.y == -1f && LastInitialPos.x == -1f)
            {
                windowRect.x = Screen.width - windowRect.width;
                LastInitialPos = windowRect.position;
            }
            else
                windowRect.Set(LastInitialPos.x, LastInitialPos.y, windowRect.width, windowRect.height);
        }

        public override void PostClose()
        {
            LastInitialSize = windowRect.size;
            LastInitialPos = windowRect.position;
        }

        public override void DoWindowContents(Rect inRect)
        {
            info.Drow(inRect);
        }

    }
}
