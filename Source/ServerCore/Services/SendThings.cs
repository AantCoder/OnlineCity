using System;
using System.Linq;
using OCUnion;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Model;
using Transfer;
using Transfer.ModelMails;

namespace ServerOnlineCity.Services
{
    internal sealed class SendThings : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request15;

        public int ResponseTypePackage => (int)PackageType.Response16;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = sendThings((ModelMailTrade)request.Packet, context);
            return result;
        }

        public ModelStatus sendThings(ModelMailTrade packet, ServiceContext context)
        {
            PlayerServer toPlayer;

            lock (context.Player)
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
                packet.Created = DateTime.UtcNow;
                packet.NeedSaveGame = true;
            }
            lock (toPlayer)
            {
                toPlayer.Mails.Add(packet);
            }
            Loger.Log($"Mail SendThings {packet.From.Login}->{packet.To.Login} {packet.ContentString()}");
            return new ModelStatus()
            {
                Status = 0,
                Message = "Load shipped"
            };
        }
    }
}
