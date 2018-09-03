using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using HugsLib.Utils;

namespace RimWorldOnlineCity.UI
{
    public class Dialog_Exchenge : Window
    {
        public Action PostCloseAction;

        public float SizeOrders_Heigth;

        public float SizeAddThingList_Width;



        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(1024f, (float)Verse.UI.screenHeight);
            }
        }

        public Dialog_Exchenge()
        {
            SizeOrders_Heigth = InitialSize.y / 2f;
            SizeAddThingList_Width = InitialSize.x / 2f;
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
            var margin = 5f;
            var btnSize = new Vector2(140f, 35f);
            var buttonYStart = inRect.height - btnSize.y;

            if (Widgets.ButtonText(new Rect(inRect.width - btnSize.x, 0, btnSize.x, btnSize.y), "Закрыть".Translate()))
            {
                Close();
            }
            
            Rect rect = new Rect(0f, 0f, inRect.width, btnSize.y);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "Торговые ордера".Translate());
            
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            
            //область сверху - ордера
            var regionRectOut = new Rect(inRect.x, inRect.y + btnSize.y + margin, inRect.width, SizeOrders_Heigth);

            Widgets.DrawMenuSection(regionRectOut);

            var regionRect = regionRectOut.ContractedBy(margin * 2f);
            Widgets.Label(regionRect, @"HHdfljadfnvoadfnguofd
1
2
3
4
5 tyjdgjgdh");

            //область снизу слева
            regionRectOut = new Rect(regionRectOut.x
                , regionRectOut.yMax + margin
                , SizeAddThingList_Width
                , inRect.height - (regionRectOut.yMax + margin));

            Widgets.DrawMenuSection(regionRectOut);

            regionRect = regionRectOut.ContractedBy(margin * 2f);
            Widgets.Label(regionRect, @"HHdfpoopipioloio
1
2
3
4
5 ooooooooo");


            //область снизу справа

            regionRectOut = new Rect(regionRectOut.xMax + margin
                , regionRectOut.y
                , inRect.width - (regionRectOut.xMax + margin)
                , regionRectOut.height);

            Widgets.DrawMenuSection(regionRectOut);

            regionRect = regionRectOut.ContractedBy(margin * 2f);
            Widgets.Label(regionRect, @"33333333333333333
1
2
3
4
5 333333333333333");
        }



    }
}
