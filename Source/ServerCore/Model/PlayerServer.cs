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

        /// <summary>
        /// По умолчанию когда =0 - принимается 15 минут
        /// </summary>
        public int SettingDelaySaveGame;

        /// <summary>
        /// Записывать ли логи в файл на клиенте, по умолчанию отключено, для быстродействия
        /// </summary>
        public bool SettingEnableFileLog;

        /// <summary>
        /// Когда последний раз менял галку "Учавствую в PVP"
        /// </summary>
        public DateTime EnablePVPChangeTime;

        /// <summary>
        /// Когда последний раз нападали
        /// </summary>
        public DateTime PVPHostLastTime;

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
