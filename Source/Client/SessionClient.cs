using Model;
using OCUnion;
using OCUnion.Transfer;
using OCUnion.Transfer.Model;
using System.Collections.Generic;
using Transfer;
using Transfer.ModelMails;

namespace RimWorldOnlineCity
{
    /// <summary>
    /// Специфический для игры класс SessionClient
    /// </summary>
    public class SessionClient : Transfer.SessionClient
    {
        private static SessionClient Single = new SessionClient();

        public static SessionClient Get => Single;

        public static void Recreate(SessionClient newClient)
        {
            Single.Disconnect();
            Single = newClient;
        }

        public ModelInfo WorldLoad()
        {
            Loger.Log("Client WorldLoad (GetInfo 3)");
            var packet = new ModelInt() { Value = (long)ServerInfoType.SendSave };
            var stat = TransObject<ModelInfo>(packet, (int)PackageType.Request5UserInfo, (int)PackageType.Response6UserInfo);
            return stat;
        }

        public bool CreateWorld(ModelCreateWorld packet)
        {
            Loger.Log("Client CreateWorld");
            var stat = TransObject<ModelStatus>(packet, (int)PackageType.Request7CreateWorld, (int)PackageType.Response8WorldCreated);

            if (stat != null && stat.Status != 0)
            {
                ErrorMessage = stat.Message;
                return false;
            }
            return stat != null;
        }

        public bool SendThings(List<ThingEntry> sendThings, string myLogin, string onlinePlayerLogin, long serverId, int tile)
        {
            Loger.Log("Client SendThings " + (sendThings?.ToStringLabel() ?? "null"));
            if ((sendThings?.Count ?? 0) == 0)
            {
                return false;
            }
            var packet = new ModelMailTrade()
            {
                From = new Player() { Login = myLogin },
                To = new Player() { Login = onlinePlayerLogin },
                Tile = tile,
                PlaceServerId = serverId,
                Things = sendThings
            };
            var stat = TransObject<ModelStatus>(packet, (int)PackageType.Request15, (int)PackageType.Response16);

            if (stat != null && stat.Status != 0)
            {
                ErrorMessage = stat.Message;
                return false;
            }

            return stat != null;
        }

        public bool ExchengeEdit(TradeOrder order)
        {
            Loger.Log("Client ExchengeEdit " + order.ToString(), Loger.LogLevel.EXCHANGE);
            var stat = TransObject<ModelStatus>(order, (int)PackageType.Request21, (int)PackageType.Response22);

            if (stat != null && stat.Status != 0)
            {
                ErrorMessage = stat.Message;
                return false;
            }

            return stat != null;
        }

        public bool ExchengeBuy(long orderId, int count)
        {
            Loger.Log("Client ExchengeBuy id=" + orderId.ToString() + " count=" + count.ToString(), Loger.LogLevel.EXCHANGE);
            var stat = TransObject<ModelStatus>(new ModelOrderBuy() { OrderId = orderId, Count = count }
                , (int)PackageType.Request23, (int)PackageType.Response24);

            if (stat != null && stat.Status != 0)
            {
                ErrorMessage = stat.Message;
                return false;
            }

            return stat != null;
        }

        public List<TradeOrder> ExchengeLoad(List<int> tiles, string filterBuy, string filterSell)
        {
            Loger.Log("Client ExchengeLoad", Loger.LogLevel.EXCHANGE);
            var packet = new ModelOrderLoadRequest()
            {
                Tiles = tiles, 
                FilterBuy = filterBuy,
                FilterSell = filterSell
            };

            var stat = TransObject<ModelOrderLoad>(packet, (int)PackageType.Request25, (int)PackageType.Response26);

            if (stat != null && stat.Status != 0)
            {
                ErrorMessage = stat.Message;
                return null;
            }

            return stat.Orders;
        }

        public AttackInitiatorFromSrv AttackOnlineInitiator(AttackInitiatorToSrv fromClient)
        {
            //Loger.Log("Client AttackOnlineInitiator " + fromClient.State);
            var stat = TransObject<AttackInitiatorFromSrv>(fromClient, (int)PackageType.Request27, (int)PackageType.Response28);

            return stat;
        }

        public AttackHostFromSrv AttackOnlineHost(AttackHostToSrv fromClient)
        {
            //Loger.Log("Client AttackOnlineHost " + fromClient.State);
            var stat = TransObject<AttackHostFromSrv>(fromClient, (int)PackageType.Request29, (int)PackageType.Response30);

            return stat;
        }

        public bool ExchengeStorage(List<ThingTrade> addThings, List<ThingTrade> deleteThings, int tile, int tileTo = 0, int cost = 0, int dist = 0)
        {
            Loger.Log("Client ExchengeStorage", Loger.LogLevel.EXCHANGE);
            var packet = new ModelExchengeStorage()
            {
                AddThings = addThings,
                DeleteThings = deleteThings,
                Tile = tile,
                TileTo = tileTo,
                Cost = cost,
                Dist = dist,
            };
            var stat = TransObject<ModelStatus>(packet, (int)PackageType.Request47Storage, (int)PackageType.Response48Storage);
            
            if (stat != null && stat.Status != 0)
            {
                ErrorMessage = stat.Message;
                return false;
            }

            return stat != null;
        }

        public int ExchengeInfo_GetCountThing(ThingTrade thing)
        {
            var packet = new ModelExchengeInfo()
            {
                Request = ModelExchengeInfoRequest.GetCountThing,
                Thing = thing,
            };
            var stat = TransObject<ModelExchengeInfo>(packet, (int)PackageType.Request53ExchengeInfo, (int)PackageType.Response54ExchengeInfo);

            if (stat != null && stat.Status != 0)
            {
                ErrorMessage = stat.Message;
                return -1;
            }

            return stat == null ? -1 : stat.Result;
        }

    }
}
