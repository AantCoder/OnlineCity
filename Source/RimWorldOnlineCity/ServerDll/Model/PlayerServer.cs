using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Transfer;

namespace OCServer.Model
{
    [Serializable]
    public class PlayerServer
    {
        public Player Public;

        public string Pass;

        public bool IsAdmin;

        public Chat PublicChat
        {
            get { return Chats[0]; }
        }

        public List<Chat> Chats;

        public static List<ChatPost> PublicPosts = new List<ChatPost>();

        public byte[] SaveDataPacket;

        public DateTime LastUpdateTime;

        public List<ModelMailTrade> Mails = new List<ModelMailTrade>();

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
