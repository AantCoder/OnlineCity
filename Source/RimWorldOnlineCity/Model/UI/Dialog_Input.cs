using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorldOnlineCity
{
    public class Dialog_Input : Window
    {
        private bool ModeOkCancel = false;
        private string TitleText;
        private string PrintText;
        private bool NeedFockus = true;

        public string InputText = "";
        public bool ResultOK = false;
        public Action PostCloseAction;

        public override Vector2 InitialSize
        {
            get { return new Vector2(400f, 300f); }
        }

        public Dialog_Input()
        {
            closeOnCancel = false;
            closeOnAccept = false;
            doCloseButton = false;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            layer = WindowLayer.SubSuper;
            /*
            closeOnEscapeKey = true;
            doCloseButton = false;
            doCloseX = true;
            resizeable = false;
            draggable = true;
            */
        }

        /// <summary>
        /// Старт в режиме ввода значения
        /// </summary>
        public Dialog_Input(string title, string printText, string inputTextStart)
            : this()
        {
            TitleText = title;
            PrintText = printText;
            InputText = inputTextStart;
        }
        
        /// <summary>
        /// Старт в режиме Да/Нет
        /// </summary>
        public Dialog_Input(string title, string printText)
            : this()
        {
            TitleText = title;
            PrintText = printText;
            ModeOkCancel = true;
        }

        public override void PreOpen()
        {
            base.PreOpen();
        }

        public override void PostClose()
        {
            base.PostClose();
            if (!ResultOK) InputText = null;
            if (PostCloseAction != null) PostCloseAction();
        }

        public override void DoWindowContents(Rect inRect)
        {
            const float mainListingSpacing = 6f;

            var btnSize = new Vector2(140f, 40f);
            var buttonYStart = inRect.height - btnSize.y;

            var ev = Event.current;
            if (Widgets.ButtonText(new Rect(0, buttonYStart, btnSize.x, btnSize.y), "OCity_DialogInput_Ok".Translate())
                || ev.isKey && ev.type == EventType.keyDown && ev.keyCode == KeyCode.Return)
            {
                ResultOK = true;
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.width - btnSize.x, buttonYStart, btnSize.x, btnSize.y), "OCity_DialogInput_Cancele".Translate()))
            {
                Close();
            }

            var mainListing = new Listing_Standard();
            mainListing.verticalSpacing = mainListingSpacing;
            mainListing.Begin(inRect);
            Text.Font = GameFont.Medium;
            mainListing.Label(TitleText);

            Text.Font = GameFont.Small;
            mainListing.GapLine();
            mainListing.Gap();

            var textEditSize = new Vector2(150f, 25f);

            Widgets.Label(new Rect(0, 70f, inRect.width, 120f), PrintText);

            if (!ModeOkCancel)
            {
                GUI.SetNextControlName("StartTextField");
                InputText = GUI.TextField(new Rect(0, 70f + 90f, 300f, 25f), InputText, 1000);
            }

            if (NeedFockus)
            {
                NeedFockus = false;
                GUI.FocusControl("StartTextField");
            }

            mainListing.End();
        }
        
    }
}
