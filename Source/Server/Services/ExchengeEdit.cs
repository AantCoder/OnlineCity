using System;
using System.Collections.Generic;
using System.Linq;
using Model;
using OCUnion;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Common;
using ServerOnlineCity.Model;
using Transfer;
using Transfer.ModelMails;

namespace ServerOnlineCity.Services
{
    internal sealed class ExchengeEdit : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request21;

        public int ResponseTypePackage => (int)PackageType.Response22;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = exchengeEdit((TradeOrder)request.Packet, context);
            return result;
        }

        private ModelStatus exchengeEdit(TradeOrder order, ServiceContext context)
        {
            try
            {
                if (context.Player == null) return null;
                lock (context.Player)
                {
                    var timeNow = DateTime.UtcNow;

                    var data = Repository.GetData;

                    if (context.Player.Public.Login != order.Owner.Login)
                    {
                        return new ModelStatus()
                        {
                            Status = 1,
                            Message = "OC_ExchangeEdit_err1"
                        };
                    }

                    if (order.Id == 0)
                    {
                        //создать новый

                        //актуализируем
                        order.Created = timeNow;
                        order.UpdateTime = timeNow;

                        order.Owner = context.Player.Public;

                        if (order.PrivatPlayers == null) order.PrivatPlayers = new List<Player>();
                        order.PrivatPlayers = order.PrivatPlayers
                            .Select(pp => data.PlayersAll.FirstOrDefault(p => p.Public.Login == pp.Login)?.Public)
                            .ToList();
                        if (order.PrivatPlayers.Any(pp => pp == null))
                        {
                            return new ModelStatus()
                            {
                                Status = 2,
                                Message = "OC_ExchangeEdit_err2"
                            };
                        }

                        lock (data)
                        {
                            if (!data.OrderOperator.OrderAdd(order))
                            {
                                //только для логов:
                                var storage = data.OrderOperator.GetStorage(order.Tile, context.Player.Public, false);
                                Loger.Log($"Server exchengeEdit Cancel OrderAdd! order={order}" + Environment.NewLine + Environment.NewLine
                                    + $" storage=" + storage?.Things?.ToStringLabel() ?? "null", Loger.LogLevel.EXCHANGE);

                                return new ModelStatus()
                                {
                                    Status = 4,
                                    Message = "OC_ExchangeEdit_err4"
                                };
                            }
                            else
                            {
                                HelperMailMessadge.Send( //todo Локализация
                                    Repository.GetData.PlayerSystem
                                    , context.Player
                                    , "OC_ExchangeEdit_OrderPlaced"
                                    , "OC_ExchangeEdit_OrderPlaced" + "."+ Environment.NewLine
                                        + "OC_ExchangeEdit_Laps" + ": " + order.CountReady + ".\n" + Environment.NewLine
                                        + "OC_ExchangeEdit_YouSell" + order.SellThings.ToStringLabel() + Environment.NewLine
                                        + "OC_ExchangeEdit_YouBuy" + order.BuyThings.ToStringLabel() + Environment.NewLine
                                        + (order.PrivatPlayers == null || order.PrivatPlayers.Count == 0
                                            ? "OC_ExchangeEdit_ForAll"
                                            : "OC_ExchangeEdit_PrivateOrder" + order.PrivatPlayers.Aggregate("", (r, i) => r + ", " + i.Login))
                                    , ModelMailMessadge.MessadgeTypes.GreyGoldenLetter
                                    , order.Tile
                                    );
                            }
                        }
                        Loger.Log("Server ExchengeEdit " + context.Player.Public.Login + " Add Id = " + order.Id.ToString(), Loger.LogLevel.EXCHANGE);
                    }
                    else
                    {
                        //проверяем на существование
                        lock (data)
                        {
                            var id = order.Id > 0 ? order.Id : -order.Id;
                            TradeOrder dataOrder;
                            if (!data.OrderOperator.OrdersById.TryGetValue(id, out dataOrder)
                                || dataOrder == null
                                || context.Player.Public.Login != dataOrder.Owner.Login)
                            {
                                return new ModelStatus()
                                {
                                    Status = 3,
                                    Message = "OC_ExchangeEdit_err3"
                                };
                            }

                            if (order.Id > 0)
                            {
                                //редактирование 

                                //актуализируем
                                order.Created = timeNow;

                                order.Owner = context.Player.Public;

                                if (order.PrivatPlayers == null) order.PrivatPlayers = new List<Player>();
                                order.PrivatPlayers = order.PrivatPlayers
                                    .Select(pp => data.PlayersAll.FirstOrDefault(p => p.Public.Login == pp.Login)?.Public)
                                    .ToList();
                                if (order.PrivatPlayers.Any(pp => pp == null))
                                {
                                    return new ModelStatus()
                                    {
                                        Status = 4,
                                        Message = "OC_ExchangeEdit_err2"
                                    };
                                }

                                Loger.Log("Server ExchengeEdit " + context.Player.Public.Login + " Edit Id = " + order.Id.ToString(), Loger.LogLevel.EXCHANGE);
                                if (!data.OrderOperator.OrderUpdate(order, dataOrder))
                                {
                                    //только для логов:
                                    var storage = data.OrderOperator.GetStorage(order.Tile, context.Player.Public, false);
                                    Loger.Log($"Server exchengeEdit Cancel OrderUpdate! order={order}" + Environment.NewLine + Environment.NewLine
                                        + $" storage=" + storage?.Things?.ToStringLabel() ?? "null", Loger.LogLevel.EXCHANGE);

                                    return new ModelStatus()
                                    {
                                        Status = 4,
                                        Message = "OC_ExchangeEdit_err4"
                                    };
                                }
                                else
                                {
                                    HelperMailMessadge.Send( //todo Локализация
                                        Repository.GetData.PlayerSystem
                                        , context.Player
                                        , "OC_ExchangeEdit_OrderRedacted"
                                        , "OC_ExchangeEdit_OrderRedacted" + ". "+ Environment.NewLine 
                                            + "OC_ExchangeEdit_Laps" + ": " + order.CountReady + ".\n " + Environment.NewLine
                                            + "OC_ExchangeEdit_YouSell" + order.SellThings.ToStringLabel() + Environment.NewLine
                                            + "OC_ExchangeEdit_YouBuy" + order.BuyThings.ToStringLabel() + Environment.NewLine
                                            + (order.PrivatPlayers == null || order.PrivatPlayers.Count == 0 
                                                ? "OC_ExchangeEdit_ForAll"
                                                : "OC_ExchangeEdit_PrivateOrder" + order.PrivatPlayers.Aggregate("", (r, i) => r + ", " + i.Login))
                                        , ModelMailMessadge.MessadgeTypes.GreyGoldenLetter
                                        , order.Tile
                                        );
                                }
                            }
                            else
                            {
                                //Удаление
                                Loger.Log("Server ExchengeEdit " + context.Player.Public.Login + " Delete Id = " + order.Id.ToString(), Loger.LogLevel.EXCHANGE);

                                data.OrderOperator.OrderRemove(dataOrder);

                                HelperMailMessadge.Send( //todo Локализация
                                    Repository.GetData.PlayerSystem
                                    , context.Player
                                    , "OC_ExchangeEdit_OrderDeleted"
                                    , "OC_ExchangeEdit_OrderDeleted" + ". " + Environment.NewLine 
                                        + "OC_ExchangeEdit_Laps" + ": " + order.CountReady + ". " + Environment.NewLine
                                        + "OC_ExchangeEdit_YouSell" + order.SellThings.ToStringLabel() + Environment.NewLine
                                        + "OC_ExchangeEdit_YouBuy" + order.BuyThings.ToStringLabel() + Environment.NewLine
                                        + (order.PrivatPlayers == null || order.PrivatPlayers.Count == 0
                                            ? "OC_ExchangeEdit_ForAll"
                                            : "OC_ExchangeEdit_PrivateOrder" + order.PrivatPlayers.Aggregate("", (r, i) => r + ", " + i.Login))
                                    , ModelMailMessadge.MessadgeTypes.GreyGoldenLetter
                                    , order.Tile
                                    );
                            }
                        }
                    }

                    Repository.Get.ChangeData = true;

                    return new ModelStatus()
                    {
                        Status = 0,
                        Message = null
                    };
                }
            }
            catch (Exception exp)
            {
                ExceptionUtil.ExceptionLog(exp, "Server ExchengeEdit login=" + context?.Player?.Public?.Login);
                throw;
            }
        }
    }
}