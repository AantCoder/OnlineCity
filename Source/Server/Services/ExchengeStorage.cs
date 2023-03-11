using System;
using System.Collections.Generic;
using System.Linq;
using OCUnion;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Common;
using ServerOnlineCity.Model;
using Transfer;
using Model;

namespace ServerOnlineCity.Services
{
    internal sealed class ExchengeStorage : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request47Storage;

        public int ResponseTypePackage => (int)PackageType.Response48Storage;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = exchengeStorage(request.Packet as ModelExchengeStorage, context);
            return result;
        }

        private ModelStatus exchengeStorage(ModelExchengeStorage diff, ServiceContext context)
        {
            lock (context.Player)
            {
                var data = Repository.GetData;

                lock (data)
                {
                    if ((diff.AddThings?.Count ?? 0) > 0)
                    {
                        data.OrderOperator.SendToStorage(diff.Tile, context.Player, diff.AddThings);
                        Repository.Get.ChangeData = true;
                    }

                    if ((diff.DeleteThings?.Count ?? 0) > 0)
                    {
                        if (diff.TileTo != 0 && diff.Cost > 0 && diff.Dist > 0
                            && diff.Cost > context.Player.CashlessBalance)
                        {
                            Loger.Log($"Server exchengeStorage CargoDelivery tile {diff.Tile} to {diff.TileTo} cost={diff.Cost} dist={diff.Dist} costThings=" + diff.DeleteThings.Sum(t => t.GameCost * t.Count), Loger.LogLevel.EXCHANGE);
                            return new ModelStatus()
                            {
                                Status = 1,
                                Message = "Operation not possible (too much)"
                            };
                        }

                        if (data.OrderOperator.GetFromStorage(diff.Tile, context.Player, diff.DeleteThings) == null)
                        {
                            //только для логов:
                            var storage = data.OrderOperator.GetStorage(diff.Tile, context.Player.Public, false);
                            Loger.Log($"Server exchengeStorage Operation not possible! storage=" + storage.Things.ToStringThing(), Loger.LogLevel.EXCHANGE);

                            return new ModelStatus()
                            {
                                Status = 1,
                                Message = "Operation not possible"
                            };
                        }
                        else
                        {
                            if (diff.TileTo != 0 && diff.Cost > 0 && diff.Dist > 0)
                            {
                                data.OrderOperator.SendToStorage(diff.TileTo, context.Player, diff.DeleteThings);
                                context.Player.CashlessBalance -= diff.Cost;
                            }
                        }
                        Repository.Get.ChangeData = true;
                    }
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
