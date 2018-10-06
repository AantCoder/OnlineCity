using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using HugsLib.Utils;

namespace RimWorldOnlineCity
{
    public class Dialog_CreateWorld : Window
    {
        private string TitleText = "OCity_Dialog_CreateWorld_Create".Translate();
        private bool NeedFockus = true;

        public string InputText = "";
        public bool ResultOK = false;
        public Action PostCloseAction;

        public string InputSeed { get; private set; }
        public string InputDifficulty { get; private set; }
        public string InputMapSize { get; private set; }
        public string InputPlanetCoverage { get; private set; }

        public override Vector2 InitialSize
        {
            get { return new Vector2(400f, 350f); } //новое поле ввода с описанием +50
        }

        public Dialog_CreateWorld()
        {
            closeOnEscapeKey = true;
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

            InputSeed = "";
            InputDifficulty = "2";
            InputMapSize = "300";
            InputPlanetCoverage = "100";
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
            if (Widgets.ButtonText(new Rect(0, buttonYStart, btnSize.x, btnSize.y), "OCity_Dialog_CreateWorld_BtnOk".Translate())
                || ev.isKey && ev.type == EventType.keyDown && ev.keyCode == KeyCode.Return)
            {
                int ii;
                if (string.IsNullOrEmpty(InputSeed)
                    || !int.TryParse(InputDifficulty, out ii) || ii < 0 || ii > 2
                    || !int.TryParse(InputPlanetCoverage, out ii) || ii < 5 || ii > 100
                    )
                {
                    Find.WindowStack.Add(new Dialog_Message("OCity_Dialog_CreateWorld_Err".Translate(), "OCity_Dialog_CreateWorld_Err2".Translate(), null, null));
                }
                else
                {
                    ResultOK = true;
                    Close();
                }
            }

            if (Widgets.ButtonText(new Rect(inRect.width - btnSize.x, buttonYStart, btnSize.x, btnSize.y), "OCity_Dialog_CreateWorld_BtnCancel".Translate()))
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
            var rect = new Rect(0, 70f, inRect.width, textEditSize.y);

            Widgets.Label(rect, "OCity_Dialog_CreateWorld_Seed".Translate());
            rect.y += textEditSize.y;
            GUI.SetNextControlName("StartTextField");
            InputSeed = GUI.TextField(rect, InputSeed, 100);
            rect.y += textEditSize.y;

            /*
            Widgets.Label(rect, "Общее количество осадков");
            rect.y += textEditSize.y;
            Input = GUI.TextField(rect, Input, 100);
            rect.y += textEditSize.y;
            */
            /*
            Widgets.Label(rect, "Общая темпиратура");
            rect.y += textEditSize.y;
            Input = GUI.TextField(rect, Input, 100);
            rect.y += textEditSize.y;
            */

            Widgets.Label(rect, "OCity_Dialog_CreateWorld_Difficulty".Translate());
            rect.y += textEditSize.y;
            InputDifficulty = GUI.TextField(rect, InputDifficulty, 1);
            rect.y += textEditSize.y;

            //это уже не надо, каждый выбирает при старте
            //Widgets.Label(rect, "OCity_Dialog_CreateWorld_MapSize".Translate());  
            //rect.y += textEditSize.y;
            //InputMapSize = GUI.TextField(rect, InputMapSize, 3);
            //rect.y += textEditSize.y;

            Widgets.Label(rect, "OCity_Dialog_CreateWorld_PercentWorld".Translate());
            rect.y += textEditSize.y;
            InputPlanetCoverage = GUI.TextField(rect, InputPlanetCoverage, 3);
            rect.y += textEditSize.y;

            if (NeedFockus)
            {
                NeedFockus = false;
                GUI.FocusControl("StartTextField");
            }

            mainListing.End();
        }
        
    }
}
