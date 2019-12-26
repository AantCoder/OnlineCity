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

        public ModelContainer GenerateModelContainer(ModelContainer request, ref PlayerServer player)
        {
            if (player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = exchengeLoad(ref player);
            return result;
        }

        private ModelOrderLoad exchengeLoad(ref PlayerServer player)
        {
            lock (player)
            {
                var timeNow = DateTime.UtcNow;
                var res = new ModelOrderLoad()
                {
                    Status = 0,
                    Message = null
                };

                res.Orders = getOrders(player);

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
