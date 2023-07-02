using Model;
using OCUnion;
using OCUnion.Transfer;
using OCUnion.Transfer.Types;
using ServerOnlineCity.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Transfer;
using Transfer.ModelMails;
using Util;

namespace ServerOnlineCity.Model
{
    [Serializable]
    public class PlayerServer : IPlayerEx
    {
        public Player Public { get; set; }

        //повторить логику на сервере тут: PlayerClient
        public bool Online =>
            Public.LastOnlineTime == DateTime.MinValue ? Public.LastSaveTime > DateTime.UtcNow.AddMinutes(-17) :
            Public.LastOnlineTime > (DateTime.UtcNow).AddSeconds(-10);

        public int MinutesIntervalBetweenPVP => ServerManager.ServerSettings.MinutesIntervalBetweenPVP;

        public string Pass;

        public bool IsAdmin { get => (Public.Grants & (Grants.SuperAdmin | Grants.Moderator)) > Grants.NoPermissions; }

        public bool Approve { get; set; }

        public Guid DiscordToken;

        public float CashlessBalance;

        /// <summary>
        /// Рассчитывается UpdateStorageBalance при каждом запросе пользователя
        /// </summary>
        [NonSerialized]
        public float StorageBalance;

        /// <summary>
        /// Причина разрыва соединения
        /// </summary>
        [NonSerialized]
        public DisconnectReason ExitReason;

        /// <summary>
        /// Разрешаем ли загрузку мира: только если файлы Steam и моды идентичные
        /// </summary>
        [NonSerialized]
        public bool ApproveLoadWorldReason;

        /// Key: ChatId, Value: lastPostIndex, last changed list of Logins
        [XmlIgnore]
        public Dictionary<Chat, ModelUpdateTime> Chats;

        public DateTime SaveDataPacketTime;

        public DateTime LastUpdateTime;

        [XmlIgnore]
        public List<ModelMail> Mails = new List<ModelMail>();

        /// <summary>
        /// Письма из уже ушедшие игроку, но ещё ожидающие сохранения его игры после получения. 
        /// Если его не будет, то при загрузке мира письма будут переведены назад в Mails для повторной отправки
        /// </summary>
        [XmlIgnore]
        public List<ModelMail> MailsConfirmationSave = new List<ModelMail>();

        /// <summary>
        /// Письма с командами. Они непосредственно не отправляются игроку, а 
        /// запускают специальный обработчик перед отправкой Mails, который может создать обычное письмо ModelMail.
        /// Смотри класс PlayInfo.
        /// Можно использовать для любого фонового механизма для игрока, который требует обработки раз в 5 секунд (время синхронизации планеты)
        /// </summary>
        [XmlIgnore]
        public List<IFunctionMail> FunctionMails = new List<IFunctionMail>();

        //[NonSerialized]
        //public long LastTickIncidents;

        /// <summary>
        /// Торговые склады игрока по разным местам. Изменять только через ExchengeOperator
        /// </summary>
        [XmlIgnore]
        public List<TradeThingStorage> TradeThingStorages = new List<TradeThingStorage>();

        [NonSerialized]
        [XmlIgnore]
        public AttackServer AttackData;

        /// <summary>
        /// По умолчанию когда =0 - принимается 15 минут
        /// </summary>
        public int SettingDelaySaveGame;

        /// <summary>
        /// Записывать ли логи в файл на клиенте, по умолчанию отключено, для быстродействия
        /// </summary>
        public bool SettingEnableFileLog;

        /// <summary>
        /// Когда можно будет поменять галку "Учавствую в PVP"
        /// </summary>
        public DateTime TimeChangeEnablePVP;

        /// <summary>
        /// Когда последний раз нападали
        /// </summary>
        public DateTime PVPHostLastTime;

        //[NonSerialized]
        private DateTime KeyReconnectTime;

        //[NonSerialized]
        public string KeyReconnect1;

        [NonSerialized]
        private string KeyReconnect2;

        public string IntruderKeys;

        [NonSerialized]
        [XmlIgnore]
        public PlayerGameProgress GameProgressLast;

        [NonSerialized]
        [XmlIgnore]
        public PlayerGameProgress GameProgressLastDay;

        public PlayerGameProgress GameProgress;

        [NonSerialized]
        public bool LastUpdateWithMail;

        public int AttacksWonCount;

        public int AttacksInitiatorCount;

        public float StartMarketValue;
        public float StartMarketValuePawn;

        public float LastMarketValue;
        public float LastMarketValuePawn;
        public float LastMarketValueBalance;
        public float LastMarketValueStorage;

        //за обновление
        public float DeltaMarketValue;
        public float DeltaMarketValuePawn;
        public float DeltaMarketValueBalance;
        public float DeltaMarketValueStorage;

        //накапливаем до срабатывания таймера
        public float SumDeltaGameMarketValue;
        public float SumDeltaGameMarketValuePawn;
        public float SumDeltaGameMarketValueBalance;
        public float SumDeltaGameMarketValueStorage;

        public float SumDeltaRealMarketValue;
        public float SumDeltaRealMarketValuePawn;
        public float SumDeltaRealMarketValueBalance;
        public float SumDeltaRealMarketValueStorage;

        public long SumDeltaRealTicks;

        public long SumDeltaRealSecond; //кол-во секунд реального времени, которое прошло не на паузе

        public long TotalRealSecond; //всего за игрой в этой колонии

        /* пиковые данных с обновлений каждые 5 сек не нужны
        //за игровой день
        public float DeltaGameMarketValue => DeltaMarketValue * 60000f / (float)(WLastTick - Public.LastTick);

        public float DeltaGameMarketValuePawn => DeltaMarketValuePawn * 60000f / (float)(WLastTick - Public.LastTick);

        //за реальную минуту
        public float DeltaRealMarketValue => DeltaMarketValue * 60000f / (float)(WLastUpdateTime - LastUpdateTime).TotalMilliseconds;

        public float DeltaRealMarketValuePawn => DeltaMarketValuePawn * 60000f / (float)(WLastUpdateTime - LastUpdateTime).TotalMilliseconds;
        */

        //Данные за 1 час и за 15 дней
        public float StatMaxDeltaGameMarketValue;
        public float StatMaxDeltaGameMarketValuePawn;
        public float StatMaxDeltaGameMarketValueBalance;
        public float StatMaxDeltaGameMarketValueStorage;
        public float StatMaxDeltaGameMarketValueTotal;

        public float StatMaxDeltaRealMarketValue;
        public float StatMaxDeltaRealMarketValuePawn;
        public float StatMaxDeltaRealMarketValueBalance;
        public float StatMaxDeltaRealMarketValueStorage;
        public float StatMaxDeltaRealMarketValueTotal;

        public long StatMaxDeltaRealTicks;

        [NonSerialized]
        public DateTime StatLastUpdateTime;

        [NonSerialized]
        public long StatLastTick;

        /// <summary>
        /// Если false, то не учитывать данные прошлого обновления, т.к. произошел сбой (не было данных) или сервер был перезагружен
        /// </summary>
        [NonSerialized]
        public bool LastUpdateIsGood;

        /// <summary>
        /// Рабочее поле на период обновление Delta
        /// </summary>
        [NonSerialized]
        public DateTime WLastUpdateTime;
        /// <summary>
        /// Рабочее поле на период обновление Delta
        /// </summary>
        [NonSerialized]
        public long WLastTick;

        private PlayerServer()
        {
            ExitReason = DisconnectReason.AllGood;
            ApproveLoadWorldReason = true;
        }

        public PlayerServer(string login)
        {
            Public = new Player()
            {
                Login = login
            };

            Chats = new Dictionary<Chat, ModelUpdateTime>(1);
            Chats.Add(ChatManager.Instance.PublicChat, new ModelUpdateTime() { Value = -1 });
        }

        public WorldObjectsValues CostWorldObjects(long serverId = 0)
        {
            var values = new WorldObjectsValues();

            var data = Repository.GetData;

            for (int i = 0; i < data.WorldObjects.Count; i++)
            {
                if (data.WorldObjects[i].LoginOwner != Public.Login) continue;
                if (serverId != 0 && data.WorldObjects[i].PlaceServerId != serverId) continue;

                values.MarketValue += data.WorldObjects[i].MarketValue;
                values.MarketValuePawn += data.WorldObjects[i].MarketValuePawn;
                values.MarketValueBalance += data.WorldObjects[i].MarketValueBalance;
                values.MarketValueStorage += data.WorldObjects[i].MarketValueStorage;
                if (data.WorldObjects[i].Type == WorldObjectEntryType.Base)
                {
                    values.BaseCount++;
                    values.BaseServerIds = (values.BaseServerIds == null ? "" : values.BaseServerIds + ",") + data.WorldObjects[i].PlaceServerId;
                }
                else
                    values.CaravanCount++;
            }

            return values;
        }

        [NonSerialized]
        private WorldObjectsValues CostWorldObjectsCache = null;
        [NonSerialized]
        private DateTime CostWorldObjectsCacheDate;

        public WorldObjectsValues CostWorldObjectsWithCache()
        {
            if (DateTime.UtcNow > CostWorldObjectsCacheDate)
            {
                var data = Repository.GetData;
                CostWorldObjectsCache = CostWorldObjects();
                CostWorldObjectsCacheDate = DateTime.UtcNow.AddSeconds(30);
            }
            return CostWorldObjectsCache;
        }

        public float AllCostWorldObjects()
        {
            var costAll = CostWorldObjects();
            if (costAll.BaseCount + costAll.CaravanCount == 0) return 0;
            if (costAll.MarketValueTotal == 0) return -1; //какой-то сбой отсутствия данных
            return costAll.MarketValueTotal;
        }

        public void UpdateStorageBalance()
        {
            var data = Repository.GetData;

            StorageBalance = data.OrderOperator.GetThingsInServer(this)
                ?.Sum(x => x.Item1.GameCost * x.Item1.Count * x.Item2) ?? 0;
        }

        public bool GetKeyReconnect()
        {
            if ((DateTime.UtcNow - KeyReconnectTime).TotalMinutes < 30
                && !string.IsNullOrEmpty(KeyReconnect1))
                return false;

            KeyReconnectTime = DateTime.UtcNow;
            var rnd = new Random((int)(DateTime.UtcNow.Ticks & int.MaxValue));
            var key = "o6*#fn`~ыggTgj0&9 gT54Qa[g}t,23rfr7*vcx%%4/\"d!2" + rnd.Next(int.MaxValue).ToString()
                + DateTime.UtcNow.Date.AddHours(DateTime.UtcNow.Hour).ToBinary().ToString()
                + Public.Login;
            var hash = new CryptoProvider().GetHash(key);

            KeyReconnect2 = KeyReconnect1;
            KeyReconnect1 = hash;

            return true;
        }

        public bool KeyReconnectVerification(string testKey)
        {
            if (string.IsNullOrEmpty(testKey)) return false;
            GetKeyReconnect();
            return KeyReconnect1 == testKey
                || KeyReconnect2 == testKey;
        }

        /// <summary>
        /// Полное удаление поселений игрока, не удаляет аккаунт и действия в чате
        /// </summary>
        public void AbandonSettlement()
        {
            Repository.DropUserFromMap(Public.Login);

            Mails = new List<ModelMail>();
            MailsConfirmationSave = new List<ModelMail>();
            FunctionMails = new List<IFunctionMail>();
            TradeThingStorages = new List<TradeThingStorage>();

            Repository.GetSaveData.DeletePlayerData(Public.Login);
            Public.LastSaveTime = DateTime.MinValue;
            Repository.Get.ChangeData = true;
        }

        /// <summary>
        /// Удаляет только не подтвержденных игроков, т.к. не удаляет игровые данные, данные чата и прочее
        /// </summary>
        public void Delete()
        {
            Repository.GetData.PlayersAll.Remove(this);
            Repository.GetData.UpdatePlayersAllDic();
            Repository.Get.ChangeData = true;
        }
    }
}
