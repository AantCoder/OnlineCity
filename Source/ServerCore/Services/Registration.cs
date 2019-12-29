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

        public ModelContainer GenerateModelContainer(ModelContainer request, ref PlayerServer player)
        {
            if (player != null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = registration((ModelLogin)request.Packet, ref player);
            return result;
        }

        private ModelStatus registration(ModelLogin packet, ref PlayerServer player)
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
            player = Repository.GetData.PlayersAll
                .FirstOrDefault(p => Repository.NormalizeLogin(p.Public.Login) == Repository.NormalizeLogin(packet.Login));
            if (player != null)
            {
                return new ModelStatus()
                {
                    Status = 1,
                    Message = "This login already exists"
                };
            }

            var isAdmin = Repository.GetData.PlayersAll.Count == 2;// 1 : system, 2 : discord и если  в этот момент добавляетесь Вы, voilà получаете админские права 
            player = new PlayerServer(packet.Login)
            {
                Pass = packet.Pass,
                IsAdmin = isAdmin,
            };

            player.Public.Grants = isAdmin ? Grants.SuperAdmin : Grants.UsualUser;

            Repository.GetData.PlayersAll.Add(player);
            Repository.Get.ChangeData = true;
            return new ModelStatus()
            {
                Status = 0,
                Message = null
            };
        }
    }
}