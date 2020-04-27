using Model;
using OCUnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorldOnlineCity
{
    public class PlayerClient : IPlayerEx
    {
        public Player Public { get; set; }

        //повторить логику на сервере тут: PlayerServer
        public bool Online =>
            Public.LastOnlineTime == DateTime.MinValue ? Public.LastSaveTime > DateTime.UtcNow.AddMinutes(-17) :
            Public.LastOnlineTime > (DateTime.UtcNow + SessionClientController.Data.ServetTimeDelta).AddSeconds(-10);

        public List<CaravanOnline> WObjects;

        public string GetTextInfo()
        {
            if (TextInfoTime < DateTime.UtcNow.AddSeconds(-5))
            {
                TextInfoTime = DateTime.UtcNow;
                TextInfo = GetTextInfoCalc();
            }
            return TextInfo;
        }

        private string TextInfo = "";
        private DateTime TextInfoTime = DateTime.MinValue;

        public WorldObjectsValues CostWorldObjects(long serverId = 0)
        {
            var values = new WorldObjectsValues();
            if (WObjects != null)
            {
                foreach (var wo in WObjects)
                {
                    if (serverId != 0 && wo.OnlineWObject.ServerId != serverId) continue;
                    values.MarketValue += wo.OnlineWObject.MarketValue;
                    values.MarketValuePawn += wo.OnlineWObject.MarketValuePawn;
                    if (wo is BaseOnline)
                    {
                        values.BaseCount++;
                    }
                    else
                        values.CaravanCount++;
                    values.Details += Environment.NewLine + Environment.NewLine + wo.GetInspectString();
                }
            }
            return values;
        }

        private string GetTextInfoCalc()
        {
            var info0 = (Public.EnablePVP ? "OCity_PlayerClient_InvolvedInPVP".Translate() : "OCity_PlayerClient_NotInvolvedInPVP".Translate()).ToString() + Environment.NewLine
                + (string.IsNullOrEmpty(Public.DiscordUserName) ? "" : "OCity_PlayerClient_Discord".Translate().ToString() + Public.DiscordUserName + Environment.NewLine)
                + (string.IsNullOrEmpty(Public.EMail) ? "" : "OCity_PlayerClient_Email".Translate().ToString() + Public.EMail + Environment.NewLine)
                + (string.IsNullOrEmpty(Public.AboutMyText) ? "" : "OCity_PlayerClient_AboutMyself".Translate().ToString() + Environment.NewLine + Public.AboutMyText + Environment.NewLine)
                + Environment.NewLine;                

            var values = CostWorldObjects();
            string s = "OCity_PlayerClient_LastTick".Translate() + Environment.NewLine
                + "OCity_PlayerClient_LastSaveTime".Translate() + Environment.NewLine
                + "OCity_PlayerClient_baseCount".Translate() + Environment.NewLine
                + "OCity_PlayerClient_caravanCount".Translate() + Environment.NewLine
                + "OCity_PlayerClient_marketValue".Translate() + Environment.NewLine
                + "OCity_PlayerClient_marketValuePawn".Translate();
            var info1 = (s)
                .Translate(
                        Public.LastTick / 3600000
                        , Public.LastTick / 60000
                        , Public.LastSaveTime == DateTime.MinValue ? "OCity_PlayerClient_LastSaveTimeNon".Translate() : new TaggedString (Public.LastSaveTime.ToGoodUtcString())
                        , values.BaseCount
                        , values.CaravanCount
                        , values.MarketValue.ToStringMoney()
                        , values.MarketValuePawn.ToStringMoney()
                    );

            return info0 + info1 + values.Details;
        }
    }
}
