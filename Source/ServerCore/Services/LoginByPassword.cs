using System.Linq;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class LoginByPassword : IGenerateResponseContainer
    {
        public int RequestTypePackage => 3;

        public int ResponseTypePackage => 4;

        public ModelContainer GenerateModelContainer(ModelContainer request, ref PlayerServer player)
        {
            if (player != null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = login((ModelLogin)request.Packet, ref player);
            return result;
        }

        private ModelStatus login(ModelLogin packet, ref PlayerServer player)
        {
            if (packet.Login == "system") return null;
            player = Repository.GetData.PlayersAll
                .FirstOrDefault(p => p.Public.Login == packet.Login);

            if (player != null)
            {
                if (player.Pass != packet.Pass) //todo
                    player = null;
            }

            if (player == null)
            {
                return new ModelStatus()
                {
                    Status = 1,
                    Message = "User or password incorrect"
                };
            }

            return new ModelStatus()
            {
                Status = 0,
                Message = null
            };
        }
    }
}
