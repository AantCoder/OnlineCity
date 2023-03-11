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
    public class PanelInfoPlayer : DialogControlBase
    {
        public PlayerClient player;

        private string InfoTabTitle = "";//"OCity_Dialog_HelloLAN".Translate();
        private string InfoTabLogin = null;
        private TextBox InfoBox = new TextBox()
        {
            Text = "OCity_InfoTabText".Translate()
        };
        //private Vector2 InfoScrollPosition;

        public PanelInfoPlayer(PlayerClient player)
        { 
            this.player = player;
            InfoTabTitle = "OCity_Dialog_ChennelPlayerInfoTitle".Translate() + player.Public.Login;
            InfoTabLogin = player.Public.Login;
            InfoBox.Text = player.GetTextInfo();
        }

        public void Init()
        { 
        }



        public void Drow(Rect inRect)
        {
            if (string.IsNullOrEmpty(InfoTabTitle) && string.IsNullOrEmpty(InfoBox.Text))
            {
                try
                {
                    var pl = SessionClientController.Data.Players[SessionClientController.My.Login];
                    InfoTabTitle = "OCity_Dialog_ChennelPlayerInfoTitle".Translate() + SessionClientController.My.Login;
                    InfoTabLogin = SessionClientController.My.Login;
                    InfoBox.Text = pl.GetTextInfo();
                }
                catch
                {
                    Loger.Log("Dialog_MainOnlineCity DoTab1Contents Exception: "
                        + $" Login={SessionClientController.My.Login} cnt={SessionClientController.Data.Players.Count}",
                        Loger.LogLevel.ERROR
                        );

                    throw;
                }
            }

            //иконка игрока. мб лучше бурать
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
