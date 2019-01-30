using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorldOnlineCity.UI;

namespace RimWorldOnlineCity
{
    public class Dialog_TextOut : Window
    {
        private string PrintText;
        private bool NeedFockus = true;
        private Vector2 ScrollPosition;

        public bool ResultOK = false;
        public Action PostCloseAction;

        public override Vector2 InitialSize
        {
            get { return new Vector2(650f, 800f); }
        }

        public Dialog_TextOut(string text)
        {
            doCloseButton = false;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            layer = WindowLayer.SubSuper;
            PrintText = text;
        }

        public override void PreOpen()
        {
            base.PreOpen();
        }

        public override void PostClose()
        {
            base.PostClose();
            if (PostCloseAction != null) PostCloseAction();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium; 

            //var chatTextSize = Text.CalcSize(ChatText);
            var chatAreaOuter = new Rect(inRect.x + 20f, inRect.y, inRect.width - 20f, inRect.height - 20f);
            var chatAreaInner = new Rect(0, 0
                , /*inRect.width - (inRect.x + 110f) */ chatAreaOuter.width - ListBox<string>.WidthScrollLine
                , 0/*chatTextSize.y*/);
            chatAreaInner.height = Text.CalcHeight(PrintText, chatAreaInner.width);

            ScrollPosition = GUI.BeginScrollView(chatAreaOuter, ScrollPosition, chatAreaInner);
            GUILayout.BeginArea(chatAreaInner);
            GUILayout.TextField(PrintText, "Label");
            GUILayout.EndArea();
            GUI.EndScrollView();
            
        }
        
    }
}
