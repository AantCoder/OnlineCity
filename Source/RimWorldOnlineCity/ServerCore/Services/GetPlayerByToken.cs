using System;
using System.Linq;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    class GetPlayerByToken : IGenerateResponseContainer
    {
        public byte RequestTypePackage => 9;

        public byte ResponseTypePackage => 10;

        public ModelContainer GenerateModelContainer(ModelContainer modelContainer, ref PlayerServer player)
        {
            if (player == null) return null;
            var packet = (Guid)modelContainer.Packet;
            var result = new ModelContainer { TypePacket = ResponseTypePackage };

            lock (player)
            {
                var data = Repository.GetData;
                var playerServer = data.PlayersAll.FirstOrDefault(p => packet.Equals(p.DiscordToken));
                result.Packet = playerServer?.Public;
                return result;
            }
        }
    }
}
