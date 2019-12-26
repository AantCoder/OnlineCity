using System;
using System.Collections.Generic;
using System.Linq;
using Model;
using OCUnion;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class ExchengeEdit : IGenerateResponseContainer
    {
        public int RequestTypePackage => 21;

        public int ResponseTypePackage => 22;

        public ModelContainer GenerateModelContainer(ModelContainer request, ref PlayerServer player)
        {
            if (player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = exchengeEdit((OrderTrade)request.Packet, ref player);
            return result;
        }

        private ModelStatus exchengeEdit(OrderTrade order, ref PlayerServer player)
        {
            if (player == null) return null;
            lock (player)
            {
                var timeNow = DateTime.UtcNow;

                var data = Repository.GetData;

                if (player.Public.Login != order.Owner.Login)
                {
                    return new ModelStatus()
                    {
                        Status = 1,
                        Message = "Ошибка. Ордер другого игрока"
                    };
                }

                if (order.Id == 0)
                {
                    //создать новый

                    //актуализируем
                    order.Created = timeNow;

                    order.Owner = player.Public;

                    if (order.PrivatPlayers == null) order.PrivatPlayers = new List<Player>();
                    order.PrivatPlayers = order.PrivatPlayers
                        .Select(pp => data.PlayersAll.FirstOrDefault(p => p.Public.Login == pp.Login)?.Public)
                        .ToList();
                    if (order.PrivatPlayers.Any(pp => pp == null))
                    {
                        return new ModelStatus()
                        {
                            Status = 2,
                            Message = "Ошибка. Указан несуществующий игрок"
                        };
                    }

                    order.Id = Repository.GetData.GetChatId();

                    lock (data)
                    {
                        data.Orders.Add(order);
                    }
                }
                else
                {
                    //проверяем на существование
                    lock (data)
                    {
                        var id = order.Id > 0 ? order.Id : -order.Id;
                        var dataOrder = data.Orders.FirstOrDefault(o => o.Id == id);
                        if (dataOrder == null
                            || player.Public.Login != dataOrder.Owner.Login)
                        {
                            return new ModelStatus()
                            {
                                Status = 3,
                                Message = "Ошибка. Ордер не найден"
                            };
                        }

                        if (order.Id > 0)
                        {
                            //редактирование 

                            //актуализируем
                            order.Created = timeNow;

                            order.Owner = player.Public;

                            if (order.PrivatPlayers == null) order.PrivatPlayers = new List<Player>();
                            order.PrivatPlayers = order.PrivatPlayers
                                .Select(pp => data.PlayersAll.FirstOrDefault(p => p.Public.Login == pp.Login)?.Public)
                                .ToList();
                            if (order.PrivatPlayers.Any(pp => pp == null))
                            {
                                return new ModelStatus()
                                {
                                    Status = 4,
                                    Message = "Ошибка. Указан несуществующий игрок"
                                };
                            }

                            Loger.Log("Server ExchengeEdit " + player.Public.Login + " Edit Id = " + order.Id.ToString());
                            data.Orders[data.Orders.IndexOf(dataOrder)] = order;
                        }
                        else
                        {
                            //Удаление
                            Loger.Log("Server ExchengeEdit " + player.Public.Login + " Delete Id = " + order.Id.ToString());
                            data.Orders.Remove(dataOrder);
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
    }
}