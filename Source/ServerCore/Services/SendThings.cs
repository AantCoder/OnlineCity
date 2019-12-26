using System.Linq;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class SendThings : IGenerateResponseContainer
    {
        public int RequestTypePackage => 15;

        public int ResponseTypePackage => 16;

        public ModelContainer GenerateModelContainer(ModelContainer request, ref PlayerServer player)
        {
            if (player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = sendThings((ModelMailTrade)request.Packet, ref player);
            return result;
        }

        public ModelStatus sendThings(ModelMailTrade packet, ref PlayerServer player)
        {
            PlayerServer toPlayer;

            lock (player)
            {
                var data = Repository.GetData;

                toPlayer = data.PlayersAll.FirstOrDefault(p => p.Public.Login == packet.To.Login);
                if (toPlayer == null)
                {
                    return new ModelStatus()
                    {
                        Status = 1,
                        Message = "Destination not found"
                    };
                }

                packet.From = data.PlayersAll.Select(p => p.Public).FirstOrDefault(p => p.Login == packet.From.Login);
                packet.To = toPlayer.Public;
            }
            lock (toPlayer)
            {
                toPlayer.Mails.Add(packet);
            }
            return new ModelStatus()
            {
                Status = 0,
                Message = "Load shipped"
            };
        }
    }
}
