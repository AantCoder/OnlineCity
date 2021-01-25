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
    public class Dialog_CreateWorld : Window
    {
        private string TitleText = "OCity_Dialog_CreateWorld_Create".Translate();
        private bool NeedFockus = true;

        public string InputText = "";
        public bool ResultOK = false;
        public Action PostCloseAction;

        public string InputSeed { get; private set; }
        public string InputScenario { get; private set; }
        public string InputDifficulty { get; private set; }
        public string InputMapSize { get; private set; }
        public float InputPlanetCoverage { get; private set; }

        public override Vector2 InitialSize
        {
            // get { return new Vector2(400f, 350f); }
            get { return new Vector2(400f, 350f); } //новое поле ввода с описанием +50
        }

        public Dialog_CreateWorld()
        {
            closeOnCancel = false;
            closeOnAccept = false;
            doCloseButton = false;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            layer = WindowLayer.SubSuper;
            /*
            resizeable = false;
            draggable = true;
            */

            InputSeed = "";
            InputScenario = "none";
            InputDifficulty = "none";
            InputMapSize = "300";
            InputPlanetCoverage = Prefs.DevMode ? 5f : 30f;
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
                || ev.isKey && ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Return)
            {
                if (string.IsNullOrEmpty(InputSeed) 
                    || string.IsNullOrEmpty(InputScenario) || string.Equals("none", InputScenario)
                    || string.IsNullOrEmpty(InputDifficulty) || string.Equals("none", InputDifficulty)
                    || !(((int)InputPlanetCoverage) >= 5 && ((int)InputPlanetCoverage <= 100))
                    )
                {
                    var errText = checkInvalidValue(string.IsNullOrEmpty(InputSeed), 
                        string.IsNullOrEmpty(InputScenario) || string.Equals("none", InputScenario), 
                        string.IsNullOrEmpty(InputDifficulty) || string.Equals("none", InputDifficulty),
                        !(((int)InputPlanetCoverage) >= 5 && ((int)InputPlanetCoverage <= 100)));
                 
                    Find.WindowStack.Add(new Dialog_Input("OCity_Dialog_CreateWorld_Err".Translate(), "OCity_Dialog_CreateWorld_Err2".Translate(errText), true));
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

            mainListing.Label("OCity_Dialog_CreateWorld_Seed".Translate(), -1f, null);
            InputSeed = mainListing.TextEntry(InputSeed, 1);

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

            
            mainListing.Gap(6f);
            var sList = ScenarioLister.AllScenarios().ToList();
            if (mainListing.ButtonTextLabeled("OCity_Dialog_CreateWorld_Scenario".Translate(), InputScenario))
            {
                List<FloatMenuOption> floatList1 = new List<FloatMenuOption>();
                foreach (var s in sList)
                {
                    floatList1.Add(new FloatMenuOption(s.name, delegate
                    {
                        InputScenario = s.name;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
                Find.WindowStack.Add(new FloatMenu(floatList1));
            }

            mainListing.Gap(6f);
            if (mainListing.ButtonTextLabeled("OCity_Dialog_CreateWorld_Difficulty".Translate(), InputDifficulty))
            {
                List<FloatMenuOption> floatList1 = new List<FloatMenuOption>();
                foreach (DifficultyDef difficultyDef in DefDatabase<DifficultyDef>.AllDefs)
                {
                    if(difficultyDef.LabelCap != "Custom")
                        floatList1.Add(new FloatMenuOption(difficultyDef.LabelCap, delegate
                        { 
                            InputDifficulty = difficultyDef.LabelCap;
                        }, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
                Find.WindowStack.Add(new FloatMenu(floatList1));
            }

            //это уже не надо, каждый выбирает при старте
            //Widgets.Label(rect, "OCity_Dialog_CreateWorld_MapSize".Translate());  
            //rect.y += textEditSize.y;
            //InputMapSize = GUI.TextField(rect, InputMapSize, 3);
            //rect.y += textEditSize.y;

            mainListing.Label("OCity_Dialog_CreateWorld_PercentWorld".Translate(Mathf.Round(InputPlanetCoverage).ToString()), -1f, null);
            if (Prefs.DevMode)
            {
                InputPlanetCoverage = mainListing.Slider(Mathf.Round(InputPlanetCoverage), 5f, 100f);
            }
            else
            {
                InputPlanetCoverage = mainListing.Slider(Mathf.Round(InputPlanetCoverage), 30f, 100f);
            }


            if (NeedFockus)
            {
                NeedFockus = false;
                GUI.FocusControl("StartTextField");
            }

            mainListing.End();
        }
       
        private string checkInvalidValue(bool isSeedErr, bool isScenarioErr, bool isDiffErr, bool isCoverageErr)
        {
            List<string> text = new List<string>();
            if (isSeedErr)
            {   text.Add("Seed"); }
            if (isScenarioErr)
            {   text.Add("Scenario"); }
            if (isDiffErr)
            {   text.Add("Difficulty"); }
            if (isCoverageErr)
            {   text.Add("Coverage"); }
            return string.Join(", ", text);
        }
    }
}
