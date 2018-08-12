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

        public List<CaravanOnline> WObjects;

        public string GetTextInfo()
        {
            float marketValue = 0;
            float marketValuePawn = 0;
            int baseCount = 0;
            int caravanCount = 0;
            if (WObjects != null)
            {
                foreach (var wo in WObjects)
                {
                    marketValue += wo.OnlineWObject.MarketValue;
                    marketValuePawn += wo.OnlineWObject.MarketValuePawn;
                    if (wo is BaseOnline)
                        baseCount++;
                    else
                        caravanCount++;
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
            return info;
        }
    }
}
