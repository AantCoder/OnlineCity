using Model;
using OCUnion.Transfer;
using OCUnion.Transfer.Types;
using System;
using System.Collections.Generic;
using Transfer;

namespace ServerOnlineCity.Model
{
    [Serializable]
    public class PlayerServer
    {
        public Player Public;

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

        public Chat PublicChat
        {
            get { return Chats[0]; }
        }

        public List<Chat> Chats;

        public static List<ChatPost> PublicPosts = new List<ChatPost>();

        public DateTime SaveDataPacketTime;

        public DateTime LastUpdateTime;

        public List<ModelMailTrade> Mails = new List<ModelMailTrade>();

        [NonSerialized]
        public AttackServer AttackData;
      
        private PlayerServer()
        { }

        public PlayerServer(string login)
        {
            ExitReason = DisconnectReason.AllGood;
            ApproveLoadWorldReason = ApproveLoadWorldReason.LoginOk;

            Public = new Player()
            {
                Login = login
            };

            var publicChat = new Chat()
            {
                Id = 1,
                Name = "Public",
                OwnerLogin = login,
                OwnerMaker = false,
                PartyLogin = new List<string>() { login, "system" },
                Posts = PublicPosts
            };

            Chats = new List<Chat>() { publicChat };
        }
    }
}
