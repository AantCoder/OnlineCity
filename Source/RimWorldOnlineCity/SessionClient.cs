using Model;
using OCUnion;
using System.Collections.Generic;
using Transfer;

namespace RimWorldOnlineCity
{
    /// <summary>
    /// Специфический для игры класс SessionClient
    /// </summary>
    public class SessionClient : Transfer.SessionClient
    {
        private static SessionClient Single = new SessionClient();

        public static SessionClient Get => Single;

        public ModelInfo WorldLoad()
        {
            Loger.Log("Client WorldLoad (GetInfo 3)");
            var packet = new ModelInt() { Value = 3 };
            var stat = TransObject<ModelInfo>(packet, 5, 6);
            return stat;
        }


        public bool CreateWorld(ModelCreateWorld packet)
        {
            Loger.Log("Client CreateWorld");
            var stat = TransObject<ModelStatus>(packet, 7, 8);

            if (stat != null && stat.Status != 0)
            {
                ErrorMessage = stat.Message;
                return false;
            }
            return stat != null;
        }

        /*
        public bool CreatePlayerMap(ModelCreatePlayerMap packet)
        {
            Loger.Log("Client CreatePlayerMap");
            var stat = TransObject<ModelStatus>(packet, 9, 10);
            if (stat != null && stat.Status != 0)
            {
                ErrorMessage = stat.Message;
                return false;
            }
            return stat != null;
        }
        */

        public bool SendThings(List<ThingEntry> sendThings, string myLogin, string onlinePlayerLogin, long serverId, int tile)
        {
            Loger.Log("Client SendThings");
            var packet = new ModelMailTrade()
            {
                From = new Player() { Login = myLogin },
                To = new Player() { Login = onlinePlayerLogin },
                Tile = tile,
                PlaceServerId = serverId,
                Things = sendThings
            };
            var stat = TransObject<ModelStatus>(packet, 15, 16);

            if (stat != null && stat.Status != 0)
            {
                ErrorMessage = stat.Message;
                return false;
            }

            return stat != null;
        }

        public bool ExchengeEdit(OrderTrade order)
        {
            Loger.Log("Client ExchengeEdit " + order.ToString());
            var stat = TransObject<ModelStatus>(order, 21, 22);

            if (stat != null && stat.Status != 0)
            {
                ErrorMessage = stat.Message;
                return false;
            }

            return stat != null;
        }

        public bool ExchengeBuy(ModelOrderBuy buy)
        {
            Loger.Log("Client ExchengeBuy id=" + buy.OrderId.ToString() + " count=" + buy.Count.ToString());
            var stat = TransObject<ModelStatus>(buy, 23, 24);

            if (stat != null && stat.Status != 0)
            {
                ErrorMessage = stat.Message;
                return false;
            }

            return stat != null;
        }

        public List<OrderTrade> ExchengeLoad()
        {
            Loger.Log("Client ExchengeLoad");
            var stat = TransObject<ModelOrderLoad>(new ModelStatus(), 25, 26);

            if (stat != null && stat.Status != 0)
            {
                ErrorMessage = stat.Message;
                return null;
            }

            return stat.Orders;
        }

        public AttackInitiatorFromSrv AttackOnlineInitiator(AttackInitiatorToSrv fromClient)
        {
            Loger.Log("Client AttackOnlineInitiator " + fromClient.State);
            var stat = TransObject<AttackInitiatorFromSrv>(fromClient, 27, 28);

            return stat;
        }

        public AttackHostFromSrv AttackOnlineHost(AttackHostToSrv fromClient)
        {
            Loger.Log("Client AttackOnlineHost " + fromClient.State);
            var stat = TransObject<AttackHostFromSrv>(fromClient, 29, 30);

            return stat;
        }
    }
}
