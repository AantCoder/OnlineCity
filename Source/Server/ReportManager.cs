using Model;
using ServerOnlineCity.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ServerOnlineCity
{
    public class ReportManager
    {
        public Func<DateTime, string> DateTimeToStr = dt => dt == DateTime.MinValue ? "" : dt.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture);

        public string GetPlayerStatistic(PlayerServer player)
        { 
            var costAll = player.CostWorldObjects();

            var newLine = $"{player.Public.Login};" +
                $"{DateTimeToStr(player.Public.LastOnlineTime)};" +
                $"{(int)(DateTime.UtcNow - player.Public.LastOnlineTime).TotalDays};" +
                $"{(int)(player.Public.LastTick / 60000)};" +
                $"{costAll.BaseCount};" +
                $"{costAll.CaravanCount};" +
                $"{costAll.MarketValueTotal};" +
                $"{costAll.MarketValue};" +
                $"{costAll.MarketValuePawn};" +
                $"{costAll.MarketValueBalance};" +
                $"{costAll.MarketValueStorage};" +
                $"{player.AttacksWonCount};" +
                $"{player.AttacksInitiatorCount};" +
                $"{player.GameProgress?.ColonistsCount};" +
                $"{player.GameProgress?.ColonistsDownCount};" +
                $"{player.GameProgress?.ColonistsBleedCount};" +
                $"{player.GameProgress?.PawnMaxSkill};" +
                $"{player.GameProgress?.KillsHumanlikes};" +
                $"{player.GameProgress?.KillsMechanoids};" +
                $"{player.GameProgress?.KillsBestHumanlikesPawnName};" +
                $"{player.GameProgress?.KillsBestHumanlikes};" +
                $"{player.GameProgress?.KillsBestMechanoidsPawnName};" +
                $"{player.GameProgress?.KillsBestMechanoids};" +
                $"{player.Public.Grants.ToString()};" +
                $"{(player.Public.EnablePVP ? 1 : 0)};" +
                $"{player.Public.EMail};" +
                $"{player.Public.DiscordUserName};" +
                $"{player.IntruderKeys};" +
                $"{player.StartMarketValue};" +
                $"{player.StartMarketValuePawn};" +
                $"{player.StatMaxDeltaGameMarketValue};" +
                $"{player.StatMaxDeltaGameMarketValuePawn};" +
                $"{player.StatMaxDeltaGameMarketValueBalance};" +
                $"{player.StatMaxDeltaGameMarketValueStorage};" +
                $"{player.StatMaxDeltaGameMarketValueTotal};" +
                $"{player.StatMaxDeltaRealMarketValue};" +
                $"{player.StatMaxDeltaRealMarketValuePawn};" +
                $"{player.StatMaxDeltaRealMarketValueBalance};" +
                $"{player.StatMaxDeltaRealMarketValueStorage};" +
                $"{player.StatMaxDeltaRealMarketValueTotal};" +
                $"{player.StatMaxDeltaRealTicks};" +
                $"{player.TotalRealSecond / 60f / 60f};"
                ;
            newLine = newLine.Replace(Environment.NewLine, " ")
                .Replace("/r", "").Replace("/n", "");

            return newLine;
        }

        public string GetAllPlayerStatisticsFile()
        {
            var content = $"Login;LastOnlineTime;LastOnlineDay;GameDays;BaseCount;CaravanCount;MarketValueTotal;MarketValue;MarketValuePawn;MarketValueBalance;MarketValueStorage" +
                $";AttacksWonCount;AttacksInitiatorCount;ColonistsCount;ColonistsDownCount;ColonistsBleedCount;PawnMaxSkill" +
                $";KillsHumanlikes;KillsMechanoids;KillsBestPawnHN;KillsBestPawnH;KillsBestPawnMN;KillsBestPawnM" +
                $";Grants;EnablePVP;EMail;DiscordUserName;IntruderKeys;StartMarketValue;StartMarketValuePawn" +
                $";MarketValueBy15Day;MarketValuePawnBy15Day;MarketValueBalanceBy15Day;MarketValueStorageBy15Day;MarketValueTotalBy15Day;MarketValueByHour;MarketValuePawnByHour;MarketValueBalanceByHour;MarketValueStorageByHour;MarketValueTotalByHour;TicksByHour;HourInGame" + Environment.NewLine;
            foreach (var player in Repository.GetData.GetPlayersAll)
            {
                var newLine = GetPlayerStatistic(player);

                content += newLine + Environment.NewLine;
            }

            return content;
        }

        public string GetDebugInfo()
        {
            var xml = new StringBuilder();
            xml.AppendLine("<Players>");
            foreach (var player in Repository.GetData.GetPlayersAll)
            {
                var tradeThingStorages = new StringBuilder();
                foreach (var storage in player.TradeThingStorages)
                {
                    var things = new StringBuilder();
                    foreach (var thing in storage.Things)
                    {
                        things.AppendLine("<Thing>" + thing.ToString() + "</Thing>");
                    }
                    tradeThingStorages.AppendLine(ToXML(storage, StringBuilderToStringXML(things, "Things")));
                }

                var simpleData = ToXML(player, StringBuilderToStringXML(tradeThingStorages, "TradeThingStorages"));
                xml.AppendLine(simpleData);
            }
            xml.AppendLine("</Players>");

            xml.AppendLine("<Orders>");
            foreach (var order in Repository.GetData.Orders.OrderBy(o => o.Owner.Login + "**" + o.Tile + "**" + o.Id))
            {
                var simpleData = ToXML(order, null, StringBuilderToStringXML(new StringBuilder(order.Owner.Login), "Owner")
                    + Environment.NewLine
                    + StringBuilderToStringXML(new StringBuilder(order.ToString()), "Short"));

                xml.AppendLine(simpleData);
            }
            xml.AppendLine("</Orders>");

            return xml.ToString();
        }

        private string StringBuilderToStringXML(StringBuilder xml, string tag, int space = 4, int spaceTag = 2)
            => StringBuilderToStringXML(xml.ToString().TrimEnd(), tag, space, spaceTag);
        private string StringBuilderToStringXML(string res, string tag, int space = 4, int spaceTag = 2)
        {
            res = new string(' ', space)
                + res.Replace(">" + Environment.NewLine, ">" + Environment.NewLine + new string(' ', space));

            if (tag != null)
            {
                res = new string(' ', spaceTag) + $"<{tag}>"
                    + Environment.NewLine + res
                    + Environment.NewLine + new string(' ', spaceTag) + $"</{tag}>";
            }

            return res;
        }

        private string ToXML<T>(T obj, string include = null, string includeBebin = null)
        {
            XmlSerializer serPlayer = new XmlSerializer(typeof(T));
            using (var mem = new MemoryStream())
            {
                serPlayer.Serialize(mem, obj);
                var serTxt = Encoding.UTF8.GetString(mem.ToArray());
                serTxt = serTxt.Replace(@"<?xml version=""1.0""?>" + Environment.NewLine, "")
                    .Replace(@" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""", "");

                if (include != null)
                {
                    var pos = serTxt.LastIndexOf(Environment.NewLine);
                    if (pos != -1)
                    {
                        pos += Environment.NewLine.Length;
                        serTxt = serTxt.Substring(0, pos) + include + Environment.NewLine + serTxt.Substring(pos);
                    }
                }

                if (includeBebin != null)
                {
                    var pos = serTxt.IndexOf(Environment.NewLine);
                    if (pos != -1)
                    {
                        pos += Environment.NewLine.Length;
                        serTxt = serTxt.Substring(0, pos) + includeBebin + Environment.NewLine + serTxt.Substring(pos);
                    }
                }

                return serTxt;
            }
            
        }
    }
}
