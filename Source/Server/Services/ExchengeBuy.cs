using OCUnion.Transfer.Model;
using ServerOnlineCity.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Transfer;
using Model;
using OCUnion;

namespace ServerOnlineCity.Services
{
    internal sealed class ExchengeBuy : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request23;

        public int ResponseTypePackage => (int)PackageType.Response24;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = exchengeBuy((ModelOrderBuy)request.Packet, context);
            return result;
        }

        private ModelStatus exchengeBuy(ModelOrderBuy buy, ServiceContext context)
        {
            lock (context.Player)
            {
                var data = Repository.GetData;

                lock (data)
                {
                    if (!data.OrderOperator.OrdersById.TryGetValue(buy.OrderId, out var order))
                    {
                        return new ModelStatus()
                        {
                            Status = 1,
                            Message = "Order not found"
                        };
                    }

                    if (!data.OrderOperator.ImplementTradeByStorage(order, context.Player, buy.Count))
                    {
                        //только для логов:
                        var storage = data.OrderOperator.GetStorage(order.Tile, context.Player.Public, false);
                        Loger.Log($"Server exchengeBuy Operation not possible! order={order}" + Environment.NewLine + Environment.NewLine
                            + $" storage=" + storage?.Things?.ToStringLabel() ?? "null", Loger.LogLevel.EXCHANGE);

                        return new ModelStatus()
                        {
                            Status = 2,
                            Message = "Operation not possible"
                        };
                    }

                    Repository.Get.ChangeData = true;
                }
                return new ModelStatus()
                {
                    Status = 0,
                    Message = null
                };
            }
        }
    }
}
