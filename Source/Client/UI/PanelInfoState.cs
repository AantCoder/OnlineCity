using Model;
using OCUnion;
using OCUnion.Transfer;
using OCUnion.Transfer.Model;
using RimWorldOnlineCity.Services;
using RimWorldOnlineCity.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity
{
    public class PanelInfoState : DialogControlBase
    {
        //todo
        public State state;

        private string InfoTabTitle = "";//"OCity_Dialog_HelloLAN".Translate();
        private string InfoTabLogin = null;
        private PanelText InfoBox = new PanelText()
        {
            PrintText = "OCity_InfoTabText".Translate()
        };
        //private Vector2 InfoScrollPosition;

        public PanelInfoState(State state)
        {
            this.state = state;
            Init(this.state);
        }

        private void Init(State s)
        {
            //InfoTabTitle = "OCity_Dialog_ChennelPlayerInfoTitle".Translate() + pl.Public.Login;
            //InfoTabLogin = pl.Public.Login;
            //InfoBox.PrintText = pl.GetTextInfoExtended();
            InfoTabTitle = s.Name;
        }

        public void Drow(Rect inRect)
        {
            if (string.IsNullOrEmpty(InfoTabTitle) && string.IsNullOrEmpty(InfoBox.PrintText)) return;

            //иконка игрока. мб лучше убрать
            var iconArea = new Rect(inRect.width - 256f - 30f, inRect.y + 30f, 256f, 256f);
            var iconImage = InfoTabLogin == null ? GeneralTexture.TradeButtonIcon : GeneralTexture.Get.ByName("pl_" + InfoTabLogin);
            GUI.DrawTexture(iconArea, iconImage);

            Text.Font = GameFont.Medium;
            Widgets.Label(inRect, InfoTabTitle);

            Text.Font = GameFont.Small;
            var chatAreaOuter = new Rect(inRect.x + 50f, inRect.y + 40f, inRect.width - 50f, inRect.height - 30f - 40f);
            InfoBox.Drow(chatAreaOuter);
        }
    }
}
