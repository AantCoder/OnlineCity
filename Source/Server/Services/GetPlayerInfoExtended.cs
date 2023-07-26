using OCUnion.Transfer.Model;
using ServerCore.Model;
using ServerOnlineCity.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Transfer;
using Transfer.ModelMails;

namespace ServerOnlineCity.Services
{

    internal sealed class GetPlayerInfoExtended : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request55PlayerInfoExtended;

        public int ResponseTypePackage => (int)PackageType.Response56PlayerInfoExtended;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = getPlayerInfoExtended((ModelName)request.Packet, context);
            return result;
        }

        private ModelPlayerInfoExtended getPlayerInfoExtended(ModelName packet, ServiceContext context)
        {
            var player = Repository.GetPlayerByLogin(packet.Value);
            if (player == null) return new ModelPlayerInfoExtended();

            var result = new ModelPlayerInfoExtended();
            lock (player)
            {
                result.ColonistsCount = player.GameProgressLast?.ColonistsCount ?? 0;
                result.ColonistsNeedingTend = player.GameProgressLast?.ColonistsNeedingTend ?? 0;
                result.ColonistsDownCount = player.GameProgressLast?.ColonistsDownCount ?? 0;
                result.AnimalObedienceCount = player.GameProgressLast?.AnimalObedienceCount ?? 0;
                result.ExistsEnemyPawns = player.GameProgressLast?.ExistsEnemyPawns ?? false;

                result.MaxSkills = player.GameProgressLast?.Pawns?.FirstOrDefault()?.Skills?.ToList();
                if (result.MaxSkills != null)
                {
                    foreach (var pawn in player.GameProgressLast.Pawns)
                    {
                        for (int i = 0; i < pawn.Skills.Count; i++)
                            if (result.MaxSkills[i] < pawn.Skills[i]) result.MaxSkills[i] = pawn.Skills[i];
                    }
                }

                result.MarketValueHistory = player.MarketValueHistory;
                result.RankingCount = Repository.GetData.PlayersRanking.Count;
                result.MarketValueRanking = player.MarketValueRanking;
                result.MarketValueRankingLast = player.MarketValueRankingLast;

                result.FunctionMailsView = player.FunctionMails
                    .Where(m => m is FMailIncident)
                    .Cast<FMailIncident>()
                    .Select((m, index) => new { sort = m.NumberOrder * 1000000 + index, mi = m })
                    .OrderBy(a => a.sort)
                    .Select(a => a.mi)
                    .Select(m => new ModelMailStartIncident()
                    {
                        AlreadyStart = m.AlreadyStart,
                        IncidentType = m.Mail.IncidentType,
                        IncidentMult = m.Mail.IncidentMult,
                        PlaceServerId = m.Mail.PlaceServerId,
                    })
                    .ToList();

            }
            return result;
        }
    }
}
