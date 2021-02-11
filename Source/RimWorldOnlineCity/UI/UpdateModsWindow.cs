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

        public static string WindowTitle { get; set; }
        public static string HashStatus { get; set; }

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
        }

        public override void PreOpen()
        {
            base.PreOpen();
        }

        public override void PostClose()
        {
            base.PostClose();
            if (!ResultOK) InputText = null;
        }

        public override void DoWindowContents(Rect inRect)
        {
            const float mainListingSpacing = 6f;

            var btnSize = new Vector2(140f, 40f);
            var buttonYStart = inRect.height - btnSize.y;

            var ev = Event.current;
            if (Widgets.ButtonText(new Rect(inRect.width - btnSize.x, buttonYStart, btnSize.x, btnSize.y), "OC_OK".Translate()))
            {
                Close();
            }

            var mainListing = new Listing_Standard();
            mainListing.verticalSpacing = mainListingSpacing;
            mainListing.Begin(inRect);
            Text.Font = GameFont.Medium;
            mainListing.Label(WindowTitle);

            Text.Font = GameFont.Small;
            mainListing.GapLine();
            mainListing.Gap();

            var textEditSize = new Vector2(150f, 25f);
            var rect = new Rect(0, 70f, inRect.width, textEditSize.y);
            mainListing.Label(HashStatus, -1f, null);
            if (NeedFocus)
            {
                NeedFocus = false;
                GUI.FocusControl("StartTextField");
            }

            mainListing.End();
        }
    }
}
