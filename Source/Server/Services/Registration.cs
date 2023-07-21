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

            var errorValid = Repository.GetData.NameValidator.TextValidator(packet.Login);
            if (errorValid != null)
            { 
                return new ModelStatus()
                {
                    Status = 1,
                    Message = "Login " + errorValid,
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

            if (ServerManager.ServerSettings.PlayerNeedApproveInDiscord && (packet.DiscordUserName ?? "").Length <= 2)
            {
                return new ModelStatus()
                {
                    Status = 1,
                    Message = "OC_LoginForm_NeedApproveText1",
                    // На данном сервере необходимо подтверждение в дискорде. Укажите своё имя в дискорде, зарегистрируйтесь здесь, а затем подтвердите регистрацию в дискорде в ответе на запрос от бота.
                };
            }

            packet.Login = packet.Login.Trim();
            if (!Repository.GetData.NameValidator.CheckFree(packet.Login))
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
            context.Player.Public.DiscordUserName = packet.DiscordUserName;

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
            Repository.GetData.UpdatePlayersAllDic();
            Repository.Get.ChangeData = true;
            Loger.Log($"Player {packet.Login} version: {packet.Version.ToString()}");
            Loger.Log($"Player {packet.Login} successfully register on this server.", Loger.LogLevel.REGISTER);

            if (!isAdmin && ServerManager.ServerSettings.PlayerNeedApprove && !context.Player.Approve)
            {
                Loger.Log($"Player {packet.Login} need approve.", Loger.LogLevel.REGISTER);
                context.Player = null;
                return new ModelStatus()
                {
                    Status = 2,
                    Message = "User not approve"
                };
            }
            else
            {
                context.Player.Approve = true;
                Loger.Log("Server Auto Approve player " + context.Player.Public.Login);
            }

            return new ModelStatus()
            {
                Status = 0,
                Message = null
            };
        }
    }
}