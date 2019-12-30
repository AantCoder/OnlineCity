using System.Linq;
using OCUnion;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class Registration : IGenerateResponseContainer
    {
        public int RequestTypePackage => 1;

        public int ResponseTypePackage => 2;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player != null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = registration((ModelLogin)request.Packet, context);
            return result;
        }

        private ModelStatus registration(ModelLogin packet, ServiceContext context)
        {
            if (packet.Login.Trim().Length < 2
                || packet.Pass.Length < 5)
            {
                return new ModelStatus()
                {
                    Status = 1,
                    Message = "Incorrect login or password"
                };
            }

            packet.Login = packet.Login.Trim();
            context.Player = Repository.GetData.PlayersAll
                .FirstOrDefault(p => Repository.NormalizeLogin(p.Public.Login) == Repository.NormalizeLogin(packet.Login));
            if (context.Player != null)
            {
                return new ModelStatus()
                {
                    Status = 1,
                    Message = "This login already exists"
                };
            }

            var isAdmin = Repository.GetData.PlayersAll.Count == 2;// 1 : system, 2 : discord и если  в этот момент добавляетесь Вы, voilà получаете админские права 
            context.Player = new PlayerServer(packet.Login)
            {
                Pass = packet.Pass,
                IsAdmin = isAdmin,
            };

            context.Player.Public.Grants = isAdmin ? Grants.SuperAdmin : Grants.UsualUser;

            Repository.GetData.PlayersAll.Add(context.Player);
            Repository.Get.ChangeData = true;
            return new ModelStatus()
            {
                Status = 0,
                Message = null
            };
        }
    }
}