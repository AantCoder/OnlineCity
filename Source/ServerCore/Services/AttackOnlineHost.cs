using System;
using Model;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class AttackOnlineHost : IGenerateResponseContainer
    {
        public int RequestTypePackage => 29;

        public int ResponseTypePackage => 30;

        public ModelContainer GenerateModelContainer(ModelContainer request, ref PlayerServer player)
        {
            if (player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = attackOnlineHost((AttackHostToSrv)request.Packet, ref player);
            return result;
        }

        private AttackHostFromSrv attackOnlineHost(AttackHostToSrv fromClient, ref PlayerServer player)
        {
            lock (player)
            {
                var timeNow = DateTime.UtcNow;
                var data = Repository.GetData;
                var res = new AttackHostFromSrv()
                {
                };

                if (player.AttackData == null)
                {
                    res.ErrorText = "Unexpected error, no data";
                    return res;
                }

                //передаем управление общему объекту
                res = player.AttackData.RequestHost(fromClient);

                return res;
            }
        }
    }
}