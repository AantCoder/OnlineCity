using OCUnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transfer.ModelMails;
using UnityEngine;
using Verse;
using RimWorld;

namespace RimWorldOnlineCity.UI
{
    public class Dialog_Scenario : AsyncDialog<string>
    {
        public override Vector2 InitialSize
        {
            get { return new Vector2(400f, 200f); }
        }

        public static Task<string> GetAsync()
        {
            var completionSource = new TaskCompletionSource<string>();
            _ = ModBaseData.Scheduler.Schedule(() =>
            {
                Find.WindowStack.Add(new Dialog_Scenario(completionSource));
            });
            return completionSource.Task;

        }

        private Dialog_Scenario(TaskCompletionSource<string> completionSource) : base(completionSource)
        {
            closeOnCancel = false;
            closeOnAccept = false;
            doCloseButton = false;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            //layer = WindowLayer.SubSuper;
            resizeable = false;
            draggable = false;
        }

        private string TitleText = "OCity_Dialog_Choose_Scenario".Translate();
        private bool NeedFockus = true;

        public string InputText = "";

        public string InputScenario { get; private set; }
        public string InputScenarioKey { get; private set; }


        private Dictionary<string, Scenario> ScenarioList = null;
        public override void DoWindowContents(Rect inRect)
        {
            const float mainListingSpacing = 6f;

            var btnSize = new Vector2(140f, 40f);
            var buttonYStart = inRect.height - btnSize.y;

            var ev = Event.current;
            if (Widgets.ButtonText(new Rect(0, buttonYStart, btnSize.x, btnSize.y), "OCity_Dialog_CreateWorld_BtnOk".Translate())
                || ev.isKey && ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Return)
            {
                if (string.IsNullOrEmpty(InputScenario) || string.Equals("none", InputScenario))
                {
                    var errText = checkInvalidValue(string.IsNullOrEmpty(InputScenario) || string.Equals("none", InputScenario));

                    Find.WindowStack.Add(new Dialog_Input("OCity_Dialog_CreateScen_Err".Translate(), "OCity_Dialog_CreateScen_Err2".Translate(errText), true));
                }
                else
                {
                    Accept(InputScenario);
                }
            }

            var mainListing = new Listing_Standard();
            mainListing.verticalSpacing = mainListingSpacing;
            mainListing.Begin(inRect);
            Text.Font = GameFont.Medium;
            mainListing.Label(TitleText);

            Text.Font = GameFont.Small;
            mainListing.GapLine();
            mainListing.Gap();

            mainListing.Gap(6f);

            if (ScenarioList == null) ScenarioList = GameUtils.AllowedScenarios();
            if (mainListing.ButtonTextLabeled("OCity_Dialog_CreateWorld_Scenario".Translate(), InputScenario))
            {
                List<FloatMenuOption> floatList1 = new List<FloatMenuOption>();
                foreach (var s in ScenarioList)
                {
                    floatList1.Add(new FloatMenuOption(s.Value.name, delegate
                    {
                        InputScenario = s.Value.name;
                        InputScenarioKey = s.Key;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
                Find.WindowStack.Add(new FloatMenu(floatList1));
            }

            mainListing.Gap(6f);

            if (NeedFockus)
            {
                NeedFockus = false;
                GUI.FocusControl("StartTextField");
            }

            mainListing.End();
        }

        private string checkInvalidValue(bool isScenarioErr)
        {
            List<string> text = new List<string>();
            if (isScenarioErr){ 
                text.Add("Scenario"); 
            }
            return string.Join(", ", text);
        }
    }
}
