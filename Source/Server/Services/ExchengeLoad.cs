using System;
using System.Collections.Generic;
using System.Linq;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Common;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class ExchengeLoad : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request25;

        public int ResponseTypePackage => (int)PackageType.Response26;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = exchengeLoad(context, request.Packet as ModelOrderLoadRequest);
            return result;
        }

        private ModelOrderLoad exchengeLoad(ServiceContext context, ModelOrderLoadRequest filters)
        {
            lock (context.Player)
            {
                var timeNow = DateTime.UtcNow;
                var res = new ModelOrderLoad()
                {
                    Status = 0,
                    Message = null
                };

                res.Orders = getOrders(context.Player, filters);

                return res;
            }
        }

        private List<TradeOrder> getOrders(PlayerServer player, ModelOrderLoadRequest filters)
        {
            //Список игроков кого видим
            var ps = StaticHelper.PartyLoginSee(player);
            var data = Repository.GetData;
            //все доступные
            var orders = data.Orders
                        .Where(o => player.Public.Login == o.Owner.Login
                            || ps.Contains(o.Owner.Login)
                                && (o.PrivatPlayers == null || o.PrivatPlayers.Count == 0 || o.PrivatPlayers.Any(p => p.Login == player.Public.Login)));
            //фильтры запроса
            if (filters.Tiles != null /*&& filters.Tiles.Count > 0*/) //если массив задан, то фильтр выбран, просто могло не попать ни одной точки
            {
                var tiles = new HashSet<int>(filters.Tiles);
                orders = orders.Where(o => tiles.Contains(o.Tile));
            }
            if (!string.IsNullOrEmpty(filters.FilterBuy) && !string.IsNullOrEmpty(filters.FilterSell))
            {
                var fb = filters.FilterBuy.ToLower();
                var fs = filters.FilterSell.ToLower();
                orders = orders.Where(o => 
                    o.BuyThings.Any(t => t.DefName.ToLower() == fb)
                    || o.SellThings.Any(t => t.DefName.ToLower() == fs));
            }
            else if (!string.IsNullOrEmpty(filters.FilterBuy))
            {
                var fb = filters.FilterBuy.ToLower();
                orders = orders.Where(o => o.BuyThings.Any(t => t.DefName.ToLower() == fb));
            }
            else if (!string.IsNullOrEmpty(filters.FilterSell))
            {
                var fs = filters.FilterSell.ToLower();
                orders = orders.Where(o => o.SellThings.Any(t => t.DefName.ToLower() == fs));
            }

            //обрезаем массивные данные для сокращения трафика SellThings[s].Data = null;
            //они будут получены клиентом через AnyLoad по DataHash
            var fillOrders = orders.ToList();
            for (int i = 0; i < fillOrders.Count; i++)
            {
                var order = fillOrders[i] = fillOrders[i].Clone();

                for (int s = 0; s < order.SellThings.Count; s++)
                {
                    order.SellThings[s].Data = null;
                }
            }

            return fillOrders;
        }
    }
}
