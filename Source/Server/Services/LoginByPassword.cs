using System;
using System.Collections.Generic;
using System.Linq;
using OCUnion;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Model;
using Transfer;
using Transfer.ModelMails;

namespace ServerOnlineCity.Services
{
    internal sealed class LoginByPassword : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request3Login;

        public int ResponseTypePackage => (int)PackageType.Response4Login;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player != null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = login((ModelLogin)request.Packet, context);
            return result;
        }

        private ModelStatus login(ModelLogin packet, ServiceContext context)
        {
            Loger.Log($"Player {packet.Login} start login on server.", Loger.LogLevel.LOGIN);
            Loger.Log($"Player {packet.Login} client version {packet.Version.ToString()}.", Loger.LogLevel.LOGIN);
            packet.Email = Repository.CheckIsIntruder(context, packet.Email, packet.Login);
            
            if (packet.Login == "system") return null;

            var player = Repository.GetPlayerByLogin(packet.Login);
            
            if (player != null)
            {
                if (!string.IsNullOrEmpty(packet.KeyReconnect))
                {
                    if (!player.KeyReconnectVerification(packet.KeyReconnect))
                    {
                        Loger.Log("Reconnect " + player.Public.Login + " Fail", Loger.LogLevel.ERROR);
                        player = null;
                    }
                    else
                        Loger.Log("Reconnect " + player.Public.Login + " OK", Loger.LogLevel.WARNING);
                }
                else if (player.Pass != packet.Pass)
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

            //перед входом закрываем все подключения этого же игрока
            context.AllSessionAction(session =>
            {
                var sc = session.GetContext();
                if (sc == null 
                    || sc.Player?.Public?.Login != player.Public.Login
                    || sc == context) return;

                Loger.Log("Disconnect old session at relogin " + player.Public.Login, Loger.LogLevel.LOGIN);
                session.Dispose();
            });
            
            //действия перед входом
            player.ExitReason = OCUnion.Transfer.DisconnectReason.AllGood;
            player.ApproveLoadWorldReason = true;

            //При восстановлении подключения ничего не делаем
            if (string.IsNullOrEmpty(packet.KeyReconnect))
            {
                //если зашли по паролю, то сбрасываем ключ, для передачи клиенту нового
                player.KeyReconnect1 = null;
                //отмена атаки, если оба участника были отключены одновременно
                if (player.AttackData != null) player.AttackData.Finish();
                //удаление всех писем с командой на перезагрузку
                if (player.Mails != null)
                {
                    for (int i = 0; i < player.Mails.Count; i++)
                    {
                        if (player.Mails[i] is ModelMailAttackCancel)
                        {
                            player.Mails.RemoveAt(i--);
                        }
                    }
                }

                // обновляем словарь Номер чата, индекс последнего полученного сообщения
                if (packet.Login != "discord")
                {
                    foreach (var v in player.Chats.Values)
                    {
                        v.Value = -1;
                        v.Time = DateTime.MinValue;
                    }
                }
            }

            context.Player = player;

            context.Logined();
            Loger.Log($"$Player {packet.Login} was logged in to the server.", Loger.LogLevel.LOGIN);
            return new ModelStatus()
            {
                Status = 0,
                Message = null
            };
        }
    }
}
