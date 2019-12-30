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

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = attackOnlineHost((AttackHostToSrv)request.Packet, context);
            return result;
        }

        private AttackHostFromSrv attackOnlineHost(AttackHostToSrv fromClient, ServiceContext context)
        {
            lock (context.Player)
            {
                var timeNow = DateTime.UtcNow;
                var data = Repository.GetData;
                var res = new AttackHostFromSrv()
                {
                };

                if (context.Player.AttackData == null)
                {
                    res.ErrorText = "Unexpected error, no data";
                    return res;
                }

                //передаем управление общему объекту
                res = context.Player.AttackData.RequestHost(fromClient);

                return res;
            }
        }
    }
}