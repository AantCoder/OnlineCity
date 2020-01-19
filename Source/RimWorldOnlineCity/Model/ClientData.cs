using Model;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Transfer;
using Verse;

namespace RimWorldOnlineCity
{
    public class ClientData
    {
        public DateTime ChatsTime = DateTime.MinValue;
        public DateTime UpdateTime = DateTime.MinValue;
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
        public bool LastServerConnectFail = false;
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

        public int DelaySaveGame = 15;

        public bool DisableDevMode;

        public bool BackgroundSaveGameOff;

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
            ChatsTime = updateDate.Time;
            int newPost = 0;
            var newStr = "";
            if (Chats != null) 
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
                    }
                }
            }
            Chats = updateDate.Chats;
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
