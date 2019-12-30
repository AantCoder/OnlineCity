using System;
using System.Linq;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class GetPlayerByToken : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.RequestPlayerByToken;

        public int ResponseTypePackage => (int)PackageType.ResponsePlayerByToken;

        public ModelContainer GenerateModelContainer(ModelContainer modelContainer, ServiceContext context)
        {
            if (context.Player == null) return null;
            var packet = (Guid)modelContainer.Packet;
            var result = new ModelContainer { TypePacket = ResponseTypePackage };

            lock (context.Player)
            {
                var data = Repository.GetData;
                var playerServer = data.PlayersAll.FirstOrDefault(p => packet.Equals(p.DiscordToken));
                result.Packet = playerServer?.Public;
                return result;
            }
        }
    }
}
