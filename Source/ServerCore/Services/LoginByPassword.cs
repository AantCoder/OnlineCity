using System.Linq;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class LoginByPassword : IGenerateResponseContainer
    {
        public int RequestTypePackage => 3;

        public int ResponseTypePackage => 4;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player != null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = login((ModelLogin)request.Packet, context);
            return result;
        }

        private ModelStatus login(ModelLogin packet, ServiceContext context)
        {
            if (packet.Login == "system") return null;
            context.Player = Repository.GetData.PlayersAll
                .FirstOrDefault(p => p.Public.Login == packet.Login);

            if (context.Player != null)
            {
                if (context.Player.Pass != packet.Pass) //todo
                    context.Player = null;
            }

            if (context.Player == null)
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
