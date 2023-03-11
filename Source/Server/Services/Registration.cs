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
            Loger.Log($"Player {packet.Login} attempt to register on the server.", Loger.LogLevel.REGISTER);

            if (packet.Login.Contains("\r")
                || packet.Login.Contains("\n")
                || packet.Login.Contains("\t")
                || packet.Login.Contains(" ")
                || packet.Login.Contains("*")
                || packet.Login.Contains("@")
                || packet.Login.Contains("/")
                || packet.Login.Contains("\\")
                || packet.Login.Contains("\"")
                || packet.Login.Contains("<")
                || packet.Login.Contains(">"))
            {
                return new ModelStatus()
                {
                    Status = 1,
                    Message = "Login cannot contain characters: space * @ / \\ \" < > "
                };
            }

            if (packet.Login.Trim().Length <= 2)
            {
                return new ModelStatus()
                {
                    Status = 1,
                    Message = "Login must be longer than 3 characters"
                };
            }

            if (packet.Pass.Length <= 2)
            {
                return new ModelStatus()
                {
                    Status = 1,
                    Message = "Password must be longer than 3 characters"
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
            };
            context.Player.Public.EMail = packet.Email;
            context.Player.Public.Version = packet.Version;
            //Обязательно сообщаем версию при подключении
            //Todo? server additions
            
            context.Player.Public.Grants = Grants.UsualUser;
            if (isAdmin)
            {
                context.Player.Public.Grants = context.Player.Public.Grants | Grants.Moderator | Grants.SuperAdmin;
            }

            context.Logined();
            
            ChatManager.Instance.PublicChat.LastChanged = System.DateTime.UtcNow;
            Repository.GetData.PlayersAll.Add(context.Player);
            Repository.Get.ChangeData = true;
            Loger.Log($"Player {packet.Login} version: {packet.Version.ToString()}");
            Loger.Log($"Player {packet.Login} successfully register on this server.", Loger.LogLevel.REGISTER);
            return new ModelStatus()
            {
                Status = 0,
                Message = null
            };
        }
    }
}