using Model;
using OCUnion;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Transfer;
using Verse;

namespace RimWorldOnlineCity
{
    public class ClientData
    {
        public ModelUpdateTime ChatsTime = new ModelUpdateTime() { Time = DateTime.MinValue };

        public DateTime UpdateTime = DateTime.MinValue;

        public string KeyReconnect;

        /// <summary>
        /// Разница между UtcNow клиента и сервера + время передачи от сервера к клиенту (половина пинга)
        /// </summary>
        public TimeSpan ServetTimeDelta = new TimeSpan(0);
        /// <summary>
        /// Время обновления данных чата
        /// </summary>
        public TimeSpan Ping = new TimeSpan(0);

        public List<Chat> Chats;

        public int ChatNotReadPost;

        public Dictionary<string, PlayerClient> Players = new Dictionary<string, PlayerClient>();

        private PlayerClient MyEx_p = null;

        public PlayerClient MyEx
        {
            get
            {
                if (MyEx_p == null)
                {
                    if (SessionClientController.My == null
                        || !Players.TryGetValue(SessionClientController.My.Login, out MyEx_p)
                        ) return null;
                }
                return MyEx_p;
            }
            set
            {
                MyEx_p = value;
            }
        }

        public float CashlessBalance;

        public float StorageBalance;

        public byte[] SaveFileData;

        public bool SingleSave;

        public long LastSaveTick;

        public bool ServerConnected
        {
            get
            {
                return SessionClient.Get.IsLogined
                    && (LastServerConnect == DateTime.MinValue
                        || (DateTime.UtcNow - LastServerConnect).TotalSeconds < 8);
            }
        }

        public DateTime LastServerConnect = DateTime.MinValue;

        /// <summary>
        /// Истина, если нет ответа на пинг (пропала связь)
        /// </summary>
        public bool LastServerConnectFail = false;
        /// <summary>
        /// Не реагировать на зависание потока таймера, устанавливается при тяжелых задачах (пока только загрузка ПВП)
        /// </summary>
        public bool DontCheckTimerFail = false;
        /// <summary>
        /// Событие после рекконекта, назначается перед командой, для которой нужно проконтролировать, что она отправлена успешно.
        /// Команда будет выполнена в основном потоке
        /// Если это событие возникает, значит был рекконект, и команду возможно нужно отправить ещё раз
        /// </summary>
        public Action ActionAfterReconnect = null;

        public int CountReconnectBeforeUpdate = 0;
        /// <summary>
        /// Проверка на зависание потока таймера увеличивается по времени, устанавливается при передачи больших пакетов (сохранения)
        /// </summary>
        public bool AddTimeCheckTimerFail = false;
        public int ChatCountSkipUpdate = 0;
        public static bool UIInteraction = false; //говорят уведомления слева сверху мешают, поэтому выключено (можно сделать настройку если кому надо будет)

        /// <summary>
        /// Если не null, значит сейчас режим атаки на другое поселение online
        /// </summary>
        public GameAttacker AttackModule { get; set; } = null;

        /// <summary>
        /// Если не null, значит сейчас режим атаки кого-то на наше поселение online
        /// </summary>
        public GameAttackHost AttackUsModule { get; set; } = null;

        public string ServerName { get; set; }

        public int DelaySaveGame { get; set; } = 15;

        public bool IsAdmin { get; set; }

        public bool DisableDevMode { get; set; }

        public int MinutesIntervalBetweenPVP { get; set; }

        public DateTime TimeChangeEnablePVP { get; set; }

        public bool ProtectingNovice { get; set; }

        public bool BackgroundSaveGameOff { get; set; }

        public ServerGeneralSettings GeneralSettings { get; set; }

        public Dictionary<Pair<int, int>, int> DistanceBetweenTileCache { get; set; } = new Dictionary<Pair<int, int>, int>();

        public Faction FactionPirate
        {
            get
            {
                if (FactionPirateData == null)
                    FactionPirateData = Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.defName == "Pirate")
                    ?? Find.FactionManager.OfAncientsHostile;
                return FactionPirateData;
            }
        }

        private Faction FactionPirateData = null;

        public bool ApplyChats(ModelUpdateChat updateDate)
        {
            //переводим сообщения с сервера
            for (int ic = 0; ic < updateDate.Chats.Count; ic++)
            {
                for (int ip = 0; ip < updateDate.Chats[ic].Posts.Count; ip++)
                {
                    updateDate.Chats[ic].Posts[ip].Message = ChatController.ServerCharTranslate(updateDate.Chats[ic].Posts[ip].Message);
                }
            }

            int newPost = 0;
            var newStr = "";
            if (Chats != null)
            {
                lock (Chats)
                {
                    foreach (var chat in updateDate.Chats)
                    {
                        var cur = Chats.FirstOrDefault(c => c.Id == chat.Id);
                        if (cur != null)
                        {
                            cur.Posts.AddRange(chat.Posts);
                            var newPosts = chat.Posts.Where(p => p.OwnerLogin != SessionClientController.My.Login).ToList();
                            newPost += newPosts.Count;
                            if (newStr == "" && newPosts.Count > 0) newStr = chat.Name + ": " + newPosts[0].Message;
                            chat.Posts = cur.Posts;
                            cur.Name = chat.Name;
                            // это только для ускорения, сервер не передает список пати логинов, если ничего не изменилось
                            // т.к. передать 3000+ логинов по 8 байт, это уже несколько пакетов
                            if (chat.PartyLogin != null && chat.PartyLogin.Count > 0)
                            {
                                cur.PartyLogin = chat.PartyLogin;
                            }
                        }
                        else
                        {
                            Chats.Add(chat);
                        }
                    }

                    var ids = updateDate.Chats.Select(x => x.Id);
                    Chats.RemoveAll(x => !ids.Contains(x.Id));
                }
            }
            else
            {
                Chats = updateDate.Chats;
            }

            if (UIInteraction && newPost > 0)
            {
                GameMessage(newStr);
            }

            ChatNotReadPost += newPost;
            return newPost > 0;
        }

        private void GameMessage(string newStr)
        {
            if (newStr.Length > 50) newStr = newStr.Substring(0, 49) + "OCity_ClientData_ChatDot".Translate();
            Messages.Message("OCity_ClientData_Chat".Translate() + newStr, MessageTypeDefOf.NeutralEvent);
        }
    }
}
