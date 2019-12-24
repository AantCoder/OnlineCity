using Model;
using ServerOnlineCity.Model;
using System;
using System.Linq;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class AttackOnlineInitiator : IGenerateResponseContainer
    {
        public int RequestTypePackage => 27;

        public int ResponseTypePackage => 28;

        public ModelContainer GenerateModelContainer(ModelContainer request, ref PlayerServer player)
        {
            if (player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = attackOnlineInitiator((AttackInitiatorToSrv)request.Packet, ref player);
            return result;
        }

        public AttackInitiatorFromSrv attackOnlineInitiator(AttackInitiatorToSrv fromClient, ref PlayerServer player)
        {
            lock (player)
            {
                var timeNow = DateTime.UtcNow;
                var data = Repository.GetData;
                var res = new AttackInitiatorFromSrv()
                {
                };

                //инициализируем общий объект
                if (fromClient.StartHostPlayer != null)
                {
                    if (player.AttackData != null)
                    {
                        res.ErrorText = "There is an active attack";
                        return res;
                    }
                    var hostPlayer = data.PlayersAll.FirstOrDefault(p => p.Public.Login == fromClient.StartHostPlayer);

                    if (hostPlayer == null)
                    {
                        res.ErrorText = "Player not found";
                        return res;
                    }

                    if (hostPlayer.AttackData != null)
                    {
                        res.ErrorText = "The player participates in the attack";
                        return res;
                    }

                    var att = new AttackServer();
                    var err = att.New(player, hostPlayer, fromClient);
                    if (err != null)
                    {
                        res.ErrorText = err;
                        return res;
                    }
                }
                if (player.AttackData == null)
                {
                    res.ErrorText = "Unexpected error, no data";
                    return res;
                }

                //передаем управление общему объекту
                res = player.AttackData.RequestInitiator(fromClient);

                return res;
            }
        }
    }
}