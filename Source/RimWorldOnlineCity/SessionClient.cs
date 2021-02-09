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
            Loger.Log("Client SendThings");
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

        public bool ExchengeEdit(OrderTrade order)
        {
            Loger.Log("Client ExchengeEdit " + order.ToString());
            var stat = TransObject<ModelStatus>(order, (int)PackageType.Request21, (int)PackageType.Response22);

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
            var stat = TransObject<ModelStatus>(buy, (int)PackageType.Request23, (int)PackageType.Response24);

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
            var stat = TransObject<ModelOrderLoad>(new ModelStatus(), (int)PackageType.Request25, (int)PackageType.Response26);

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
    }
}
