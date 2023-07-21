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

        public int MinutesIntervalBetweenPVP => SessionClientController.Data.MinutesIntervalBetweenPVP;

        public List<CaravanOnline> WObjects;

        public string GetTextInfo()
        {
            UpdateTextInfoCalc();
            return TextInfo;
        }
        private string TextInfo = "";
        private string TextInfoExtended = "";
        private DateTime TextInfoTime = DateTime.MinValue;

        public PlayerClient Refrash()
        {
            if (!SessionClientController.Data.Players.ContainsKey(Public.Login)) return this;
            return SessionClientController.Data.Players[Public.Login];
        }

        public string GetTextInfoExtended()
        {
            UpdateTextInfoCalc();
            return TextInfoExtended;
        }

        public WorldObjectsValues CostAllWorldObjects()
        {
            if (AllWorldObjectsTime < DateTime.UtcNow.AddSeconds(-5))
            {
                AllWorldObjectsTime = DateTime.UtcNow;
                AllWorldObjects = CostWorldObjects();
            }
            return AllWorldObjects;
        }
        private WorldObjectsValues AllWorldObjects = null;
        private DateTime AllWorldObjectsTime = DateTime.MinValue;

        public WorldObjectsValues CostWorldObjects(long serverId = 0)
        {
            var values = new WorldObjectsValues();
            if (WObjects != null)
            {
                foreach (var wo in WObjects)
                {
                    if (serverId != 0 && wo.OnlineWObject.PlaceServerId != serverId) continue;
                    values.MarketValue += wo.OnlineWObject.MarketValue;
                    values.MarketValuePawn += wo.OnlineWObject.MarketValuePawn;
                    values.MarketValueBalance += wo.OnlineWObject.MarketValueBalance;
                    values.MarketValueStorage += wo.OnlineWObject.MarketValueStorage;
                    
                    if (wo is BaseOnline)
                    {
                        values.BaseCount++;
                    }
                    else
                        values.CaravanCount++;
                    values.Details += Environment.NewLine + Environment.NewLine + wo.GetInspectString();
                    values.DetailsExtended += Environment.NewLine + Environment.NewLine + wo.GetInspectExtendedString();
                }
            }
            return values;
        }

        private void UpdateTextInfoCalc()
        {
            if (TextInfoTime >= DateTime.UtcNow.AddSeconds(-5)) return;

            var info1 = string.IsNullOrEmpty(Public.StateName) ? "" :
                "OC_PlayerClient_In".Translate().ToString() + " " + Public.StateName
                //todo Константой вывести статус главы
                + (string.IsNullOrEmpty(Public.StatePositionName) ? "" : " " + "OC_PlayerClient_Position".Translate().ToString() + " " + Public.StatePositionName)
                + Environment.NewLine;

            var info1Extended = string.IsNullOrEmpty(Public.StateName) ? "" :
                "<:world_map height=18:> " + "OC_PlayerClient_In".Translate().ToString() + $" <@{Public.StateName}>" //ChatController.PrepareShortTag
                //todo Константой вывести статус главы, вывести ссылку на государство
                + (string.IsNullOrEmpty(Public.StatePositionName) ? "" : " " + "OC_PlayerClient_Position".Translate().ToString() + " " + Public.StatePositionName)
                + Environment.NewLine;

            var info2 = (SessionClientController.Data.GeneralSettings.EnablePVP 
                    ? (Public.EnablePVP ? "OCity_PlayerClient_InvolvedInPVP".Translate() : "OCity_PlayerClient_NotInvolvedInPVP".Translate()).ToString() + Environment.NewLine
                    : "")
                + (string.IsNullOrEmpty(Public.DiscordUserName) ? "" : "OCity_PlayerClient_Discord".Translate().ToString() + Public.DiscordUserName + Environment.NewLine)
                + (string.IsNullOrEmpty(Public.EMail) ? "" : "OCity_PlayerClient_Email".Translate().ToString() + Public.EMail + Environment.NewLine)
                + (string.IsNullOrEmpty(Public.AboutMyText) ? "" : "OCity_PlayerClient_AboutMyself".Translate().ToString() + Environment.NewLine + Public.AboutMyText + Environment.NewLine)
                + Environment.NewLine;

            AllWorldObjectsTime = DateTime.UtcNow;
            AllWorldObjects = CostWorldObjects();

            string s = "OCity_PlayerClient_LastTick".Translate() + Environment.NewLine
                + "OCity_PlayerClient_LastSaveTime".Translate() + Environment.NewLine
                + "OCity_PlayerClient_baseCount".Translate() + Environment.NewLine
                + "OCity_PlayerClient_caravanCount".Translate() + Environment.NewLine
                + "OCity_PlayerClient_marketValue".Translate() + Environment.NewLine
                + "OCity_PlayerClient_marketValuePawn".Translate() + Environment.NewLine
                + "OCity_PlayerClient_marketValueTrading".Translate();
            var info3 = (s)
                .Translate(
                        Public.LastTick / 3600000
                        , Public.LastTick / 60000
                        , Public.LastSaveTime == DateTime.MinValue ? "OCity_PlayerClient_LastSaveTimeNon".Translate() : new TaggedString (Public.LastSaveTime.ToGoodUtcString())
                        , AllWorldObjects.BaseCount
                        , AllWorldObjects.CaravanCount
                        , AllWorldObjects.MarketValue.ToStringMoney()
                        , AllWorldObjects.MarketValuePawn.ToStringMoney()
                        , (AllWorldObjects.MarketValueBalance + AllWorldObjects.MarketValueStorage).ToStringMoney()
                    )
                .ToString();
            var info3Extended = (s)
                .Translate(
                        Public.LastTick / 3600000
                        , Public.LastTick / 60000
                        , Public.LastSaveTime == DateTime.MinValue ? "OCity_PlayerClient_LastSaveTimeNon".Translate() : new TaggedString(Public.LastSaveTime.ToGoodUtcString())
                        , AllWorldObjects.BaseCount
                        , AllWorldObjects.CaravanCount
                        , "<:money_bag height=16:> " + AllWorldObjects.MarketValue.ToStringMoney()
                        , "<:busts_in_silhouette height=16:> " + AllWorldObjects.MarketValuePawn.ToStringMoney()
                        , "<:chart_increasing height=16:> " + (AllWorldObjects.MarketValueBalance + AllWorldObjects.MarketValueStorage).ToStringMoney()
                    )
                .ToString();

            TextInfo = info1 + info2 + info3 + AllWorldObjects.Details;
            TextInfoExtended = ChatController.PrepareShortTag(info1Extended + info2 + info3Extended + AllWorldObjects.DetailsExtended); //.Replace("<", "#(").Replace(">", ")#");
            TextInfoTime = DateTime.UtcNow;
        }

    }
}