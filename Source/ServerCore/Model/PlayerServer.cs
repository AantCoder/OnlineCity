using Model;
using OCUnion.Transfer;
using OCUnion.Transfer.Types;
using ServerOnlineCity.Services;
using System;
using System.Collections.Generic;
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

        public bool IsAdmin;

        public Guid DiscordToken;

        /// <summary>
        /// Причина разрыва соединения
        /// </summary>
        [NonSerialized]
        public DisconnectReason ExitReason;

        /// <summary>
        /// Разрешаем ли загрузку мира: только если файлы Steam и моды идентичные
        /// </summary>
        [NonSerialized]
        public ApproveLoadWorldReason ApproveLoadWorldReason;

        /// Key: ChatId, Value: lastPostIndex, last changed list of Logins
        public Dictionary<Chat, ModelUpdateTime> Chats;

        public DateTime SaveDataPacketTime;

        public DateTime LastUpdateTime;

        public List<ModelMail> Mails = new List<ModelMail>();

        /// <summary>
        /// Письма из уже ушедшие игроку, но ещё ожидающие сохранения его игры после получения. 
        /// Если его не будет, то при загрузке мира письма будут переведены назад в Mails для повторной отправки
        /// </summary>
        public List<ModelMail> MailsConfirmationSave = new List<ModelMail>();

        [NonSerialized]
        public long LastTickIncidents;

        [NonSerialized]
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

        private PlayerServer()
        {
            ExitReason = DisconnectReason.AllGood;
            ApproveLoadWorldReason = ApproveLoadWorldReason.LoginOk;
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
                if (serverId != 0 && data.WorldObjects[i].ServerId != serverId) continue;

                values.MarketValue += data.WorldObjects[i].MarketValue;
                values.MarketValuePawn += data.WorldObjects[i].MarketValuePawn;
                if (data.WorldObjects[i].Type == WorldObjectEntryType.Base)
                    values.BaseCount++;
                else
                    values.CaravanCount++;
            }

            return values;
        }

        public float AllCostWorldObjects()
        {
            var costAll = CostWorldObjects();
            if (costAll.BaseCount + costAll.CaravanCount == 0) return 0;
            if (costAll.MarketValue + costAll.MarketValuePawn == 0) return -1; //какой-то сбой отсутствия данных
            return costAll.MarketValue + costAll.MarketValuePawn;
        }

        public bool GetKeyReconnect()
        {
            if ((DateTime.UtcNow - KeyReconnectTime).TotalMinutes < 30
                && !string.IsNullOrEmpty(KeyReconnect1))
                return false;

            KeyReconnectTime = DateTime.UtcNow;
            var rnd = new Random((int)(DateTime.UtcNow.Ticks & int.MaxValue));
            var key = "o6*#fn`~ыggTgj0&9 gT54Qa[g}t,23rfr4*vcx%%4/\"d!2" + rnd.Next(int.MaxValue).ToString()
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
            Mails = new List<ModelMail>();
            MailsConfirmationSave = new List<ModelMail>();

            Repository.DropUserFromMap(Public.Login);
            Repository.GetSaveData.DeletePlayerData(Public.Login);
            Public.LastSaveTime = DateTime.MinValue;
            Repository.Get.ChangeData = true;
        }
    }
}
