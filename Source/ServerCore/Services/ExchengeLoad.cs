using System;
using System.Collections.Generic;
using System.Linq;
using ServerOnlineCity.Common;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class ExchengeLoad : IGenerateResponseContainer
    {
        public int RequestTypePackage => 25;

        public int ResponseTypePackage => 26;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = exchengeLoad(context);
            return result;
        }

        private ModelOrderLoad exchengeLoad(ServiceContext context)
        {
            lock (context.Player)
            {
                var timeNow = DateTime.UtcNow;
                var res = new ModelOrderLoad()
                {
                    Status = 0,
                    Message = null
                };

                res.Orders = getOrders(context.Player);

                return res;
            }
        }

        private List<OrderTrade> getOrders(PlayerServer player)
        {
            //Список игроков кого видим
            var ps = StaticHelper.PartyLoginSee(player);
            var data = Repository.GetData;
            return (data.Orders ?? new List<OrderTrade>())
                        .Where(o => player.Public.Login == o.Owner.Login
                            || ps.Any(p => p == o.Owner.Login)
                                && (o.PrivatPlayers.Count == 0 || o.PrivatPlayers.Any(p => p.Login == player.Public.Login)))
                        .ToList();
        }
    }
}
