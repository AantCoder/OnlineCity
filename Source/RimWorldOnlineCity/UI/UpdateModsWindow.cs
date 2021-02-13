using OCUnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity.UI
{
    class UpdateModsWindow : Window
    {
        private bool NeedFocus = true;

        public string InputText = "";
        public bool ResultOK = false;

        public static string WindowsTitle { get; set; }
        public static string Title { get; set; }
        public static string HashStatus { get; set; }
        public static List<string> SummaryList { get; set; }
        public static bool CompletedAndClose { get; set; }

        public bool HideOK { get; set; }
        public Action OnCloseed { get; set; }

        public override Vector2 InitialSize
        {           
            get { return new Vector2(400f, 350f); } //новое поле ввода с описанием +50
        }

        public UpdateModsWindow()
        {
            //Widgets.FillableBarLabeled ()
            closeOnCancel = false;
            closeOnAccept = false;
            doCloseButton = false;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            layer = WindowLayer.SubSuper;
            CompletedAndClose = false;

            if (WindowsTitle == null) WindowsTitle = "OC_Mods_Wait".Translate(); // Подождите перед сетевой игрой
        }

        public override void PreOpen()
        {
            base.PreOpen();
        }

        public override void PostClose()
        {
            base.PostClose();
            if (!ResultOK) InputText = null;
            if (OnCloseed != null) OnCloseed();
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (CompletedAndClose)
            {
                CompletedAndClose = false;
                Close();
                return;
            }

            const float mainListingSpacing = 6f;

            var btnSize = new Vector2(140f, 40f);
            var buttonYStart = inRect.height - btnSize.y;

            var ev = Event.current;
            if (!HideOK && Widgets.ButtonText(new Rect(inRect.width - btnSize.x, buttonYStart, btnSize.x, btnSize.y), "OC_OK".Translate()))
            {
                Close();
            }

            var mainListing = new Listing_Standard();
            mainListing.verticalSpacing = mainListingSpacing;
            mainListing.Begin(inRect);
            Text.Font = GameFont.Medium;
            mainListing.Label(WindowsTitle);

            Text.Font = GameFont.Small;
            mainListing.GapLine();
            mainListing.Gap();

            mainListing.Label(Title);
            mainListing.Gap();

            var textEditSize = new Vector2(150f, 25f);
            var rect = new Rect(0, 70f, inRect.width, textEditSize.y);
            mainListing.Label(HashStatus, -1f, null);
            mainListing.Gap();

            if (SummaryList != null)
                mainListing.Label("OC_Mods_Complete".Translate() + Environment.NewLine + string.Join(Environment.NewLine, SummaryList));

            if (NeedFocus)
            {
                NeedFocus = false;
                GUI.FocusControl("StartTextField");
            }

            mainListing.End();
        }
    }
}
