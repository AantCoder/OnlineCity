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
        private TextBox PrintBox = new TextBox();
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
            PrintBox.Text = text;
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
            
            var chatAreaOuter = new Rect(inRect.x + 20f, inRect.y, inRect.width - 20f, inRect.height - 20f);
            PrintBox.Drow(chatAreaOuter);
        }
        
    }
}
