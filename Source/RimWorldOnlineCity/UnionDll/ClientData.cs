using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Transfer;

namespace OCUnion
{
    /// <summary>
    /// Базовый класс ClientData независимый от римворлда и Verse 
    /// </summary>
    public class ClientData
    {        
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

        public byte[] SaveFileData;

        public long LastSaveTick;

        private readonly string _myLogin;

        private readonly SessionClient _sessionClient;

        public ClientData(string myLogin, SessionClient sessionClient)
        {
            _myLogin = myLogin;
            _sessionClient = sessionClient;
        }

        public bool ServerConnected
        {
            get
            {
                return _sessionClient.IsLogined
                    && (LastServerConnect == DateTime.MinValue
                        || (DateTime.UtcNow - LastServerConnect).TotalSeconds < 8);
            }
        }

        public DateTime LastServerConnect = DateTime.MinValue;
        public bool LastServerConnectFail = false;
        public int ChatCountSkipUpdate = 0;

        public bool ApplyChats(ModelUpdateChat updateDate, ref string newStr)
        {
            int newPost = 0;
            newStr = "";
            if (Chats != null)
            {
                foreach (var chat in updateDate.Chats)
                {
                    var cur = Chats.FirstOrDefault(c => c.Id == chat.Id);
                    if (cur != null)
                    {
                        cur.Posts.AddRange(chat.Posts);
                        var newPosts = chat.Posts.Where(p => p.OwnerLogin != _myLogin).ToList();
                        newPost += newPosts.Count;
                        if (newStr == "" && newPosts.Count > 0) newStr = chat.Name + ": " + newPosts[0].Message;
                        chat.Posts = cur.Posts;

                        if (chat.PartyLogin != null)
                        {
                            cur.PartyLogin = chat.PartyLogin;
                        }
                    }
                }
            }
            else
            {
                Chats = updateDate.Chats;
            }

            ChatNotReadPost += newPost;
            return newPost > 0;
        }
    }
}
