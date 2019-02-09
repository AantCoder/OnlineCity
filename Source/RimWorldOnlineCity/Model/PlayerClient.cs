using Model;
using OCUnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorldOnlineCity
{
    public class PlayerClient
    {
        public Player Public;

        public bool Online =>
            Public.LastOnlineTime == DateTime.MinValue ? Public.LastSaveTime > DateTime.UtcNow.AddMinutes(-17) :
            Public.LastOnlineTime > (DateTime.UtcNow + SessionClientController.Data.ServetTimeDelta).AddSeconds(-10);

        public List<CaravanOnline> WObjects;

        public string GetTextInfo()
        {
            if (TextInfoTime < DateTime.UtcNow.AddSeconds(-30))
            {
                TextInfoTime = DateTime.UtcNow;
                TextInfo = GetTextInfoCalc();
            }
            return TextInfo;
        }

        private string TextInfo = "";
        private DateTime TextInfoTime = DateTime.MinValue;

        private string GetTextInfoCalc()
        {
            float marketValue = 0;
            float marketValuePawn = 0;
            int baseCount = 0;
            int caravanCount = 0;
            string listOut = "";
            if (WObjects != null)
            {
                foreach (var wo in WObjects)
                {
                    marketValue += wo.OnlineWObject.MarketValue;
                    marketValuePawn += wo.OnlineWObject.MarketValuePawn;
                    if (wo is BaseOnline)
                    {
                        baseCount++;
                    }
                    else
                        caravanCount++;
                    listOut += Environment.NewLine + Environment.NewLine + wo.GetInspectString();
                }
            }
            var info = (
                "OCity_PlayerClient_LastTick".Translate() + Environment.NewLine
                + "OCity_PlayerClient_LastSaveTime".Translate() + Environment.NewLine
                + "OCity_PlayerClient_baseCount".Translate() + Environment.NewLine
                + "OCity_PlayerClient_caravanCount".Translate() + Environment.NewLine
                + "OCity_PlayerClient_marketValue".Translate() + Environment.NewLine
                + "OCity_PlayerClient_marketValuePawn".Translate()
                ).Translate(new object[]
                    {
                        Public.LastTick / 3600000
                        , Public.LastTick / 60000
                        , Public.LastSaveTime == DateTime.MinValue ? "OCity_PlayerClient_LastSaveTimeNon".Translate() : Public.LastSaveTime.ToGoodUtcString()
                        , baseCount
                        , caravanCount
                        , marketValue.ToStringMoney()
                        , marketValuePawn.ToStringMoney()
                    });
            return info + listOut;
        }
    }
}
