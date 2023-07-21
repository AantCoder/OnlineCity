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
using OCUnion.Transfer.Model;

namespace RimWorldOnlineCity
{
    public class Dialog_InfoState : Window
    {
        public PanelInfoState info;

        public override Vector2 InitialSize
        {
            get { return LastInitialSize; }
        }

        static Vector2 LastInitialSize = new Vector2(750f, 550f);
        static Vector2 LastInitialPos = new Vector2(-1f, -1f);

        private Dialog_InfoState(State state)
        {
            closeOnCancel = true;
            closeOnAccept = false;
            doCloseButton = false;
            doCloseX = true;
            resizeable = true;
            draggable = true;

            info = new PanelInfoState(state);
        }

        static public void ShowInfoState(string name)
        {
            if (SessionClientController.Data.States.TryGetValue(name, out var st))
                ShowInfoState(st);
        }

        static public void ShowInfoState(State state)
        {
            if (state == null) return;
            Find.WindowStack.Add(new Dialog_InfoState(state));
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
