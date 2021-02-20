using System.Linq;
using OCUnion;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class Registration : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request1Register;

        public int ResponseTypePackage => (int)PackageType.Response2Register;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player != null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = registration((ModelLogin)request.Packet, context);
            return result;
        }

        private ModelStatus registration(ModelLogin packet, ServiceContext context)
        {
            packet.Email = Repository.CheckIsIntruder(context, packet.Email, packet.Login);

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
            context.Player.Public.EMail = packet.Email;

            context.Player.Public.Grants = Grants.UsualUser;
            if (isAdmin)
            {
                context.Player.Public.Grants = context.Player.Public.Grants | Grants.Moderator | Grants.SuperAdmin;
            }

            context.Logined();
            
            ChatManager.Instance.PublicChat.LastChanged = System.DateTime.UtcNow;
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