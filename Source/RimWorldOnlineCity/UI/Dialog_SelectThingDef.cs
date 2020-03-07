using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorldOnlineCity.UI
{
    public class Dialog_SelectThingDef : Window
    {
        public Action PostCloseAction;

        public ThingDef SelectThingDef = null;
        public FloatRange SelectHitPointsPercents = FloatRange.ZeroToOne;
        public QualityRange SelectQualities = QualityRange.All;

        private ThingFilter thingFilter = new ThingFilter();
        private Vector2 scrollPosition;

        public void ClearFilter()
        {
            thingFilter = new ThingFilter();
            thingFilter.AllowedQualityLevels = new QualityRange(QualityCategory.Normal, QualityCategory.Legendary);
            thingFilter.AllowedHitPointsPercents = new FloatRange(0.8f, 1);
            thingFilter.SetDisallowAll(null, null);
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(350f, 700f); }
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
            if (thingFilter.AllowedThingDefs.Count() > 1)
            {
                thingFilter = new ThingFilter();
                thingFilter.AllowedQualityLevels = SelectQualities;
                thingFilter.AllowedHitPointsPercents = SelectHitPointsPercents;
                thingFilter.SetDisallowAll(null, null);
            }
            SelectThingDef = thingFilter.AllowedThingDefs.FirstOrDefault();
            SelectQualities = thingFilter.AllowedQualityLevels;
            SelectHitPointsPercents = thingFilter.AllowedHitPointsPercents;

            const float mainListingSpacing = 6f;

            var btnSize = new Vector2(140f, 40f);
            var buttonYStart = inRect.height - btnSize.y;

            /*
            var ev = Event.current;
            if (Widgets.ButtonText(new Rect(0, buttonYStart, btnSize.x, btnSize.y), "OCity_DialogInput_Ok".Translate())
                || ev.isKey && ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Return)
            */
            if (SelectThingDef != null)
            {
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.width - btnSize.x, buttonYStart, btnSize.x, btnSize.y), "OCity_DialogInput_Cancele".Translate()))
            {
                Close();
            }

            //выше кнопок
            Rect workRect = new Rect(inRect.x, inRect.y, inRect.width, buttonYStart - mainListingSpacing);

            ThingFilterUI.DoThingFilterConfigWindow(workRect, ref this.scrollPosition, thingFilter, null, 8);
            
        }

    }
}
