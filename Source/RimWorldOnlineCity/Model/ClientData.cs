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

        public Dictionary<string, PlayerClient> Players = new Dictionary<string, PlayerClient>();

        public byte[] SaveFileData;

        public long LastSaveTick;

        public bool ServerConnected
        {
            get
            {
                return SessionClient.Get.IsLogined
                    && (LastServerConnect == DateTime.MinValue
                        || (DateTime.UtcNow - LastServerConnect).Seconds < 8);
            }
        }

        public DateTime LastServerConnect = DateTime.MinValue;
        public bool LastServerConnectFail = false;
        public int ChatCountSkipUpdate = 0;
        public static bool UIInteraction = true;

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
                if (newStr.Length > 50) newStr = newStr.Substring(0, 49) + "OCity_ClientData_ChatDot".Translate();
                Messages.Message("OCity_ClientData_Chat".Translate() + newStr, MessageTypeDefOf.NeutralEvent);
            }
            return newPost > 0;
        }
    }
}
