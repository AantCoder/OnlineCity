using Model;
using ServerOnlineCity.ChatService;
using ServerOnlineCity.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ServerOnlineCity.Services
{
    class ChatManager
    {
        public static IReadOnlyDictionary<string, IChatCmd> ChatCmds;
        public const char prefix = '/';

        static ChatManager()
        {
            var d = new Dictionary<string, IChatCmd>();
            foreach (var type in Assembly.GetEntryAssembly().GetTypes())
            {
                if (!type.IsClass)
                {
                    continue;
                }

                if (type.GetInterfaces().Any(x => x == typeof(IChatCmd)))
                {
                    var t = (IChatCmd)Activator.CreateInstance(type);
                    d[prefix + t.CmdID] = t;
                }
            }

            ChatCmds = d;
        }

        public const string InvalidCommand = "The command is not available";

        //[Obsolete ("нет необходимости передавать PlayerServer и Chat, используйте PostPrivateChat в дальнейшем")]
        public static void PostCommandPrivatPostActivChat( PlayerServer player, Chat chat, string msg)
        {
            chat.Posts.Add(new ChatPost()
            {
                Time = DateTime.UtcNow,
                Message = msg,
                OwnerLogin = "system",
                OnlyForPlayerLogin = player.Public.Login
            });
        }

        public static void PostCommandAddPlayer(PlayerServer player, Chat chat, string who)
        {
            if (chat.PartyLogin.Any(p => p == who))
            {
                PostCommandPrivatPostActivChat(player, chat, "The player is already here");
                return;
            }

            var newPlayer = Repository.GetData.PlayersAll
                .FirstOrDefault(p => p.Public.Login == who);
            if (newPlayer == null)
                PostCommandPrivatPostActivChat(player, chat, "User " + who + " not found");
            else if (!player.PublicChat.PartyLogin.Any(p => p == who))
                PostCommandPrivatPostActivChat(player, chat, "Can not access " + who + " player");
            else if (!chat.OwnerMaker)
                PostCommandPrivatPostActivChat(player, chat, "People can not be added to a shared channel");
            else if (chat.OwnerLogin != player.Public.Login && !player.IsAdmin)
                PostCommandPrivatPostActivChat(player, chat, "You can not add people");
            else
            {
                chat.Posts.Add(new ChatPost()
                {
                    Time = DateTime.UtcNow,
                    Message = "User " + newPlayer.Public.Login + " entered the channel.",
                    OwnerLogin = "system"
                });

                chat.PartyLogin.Add(newPlayer.Public.Login);
                newPlayer.Chats.Add(chat);
                Repository.Get.ChangeData = true;
            }
        }

        private List<ChatPost> chatPosts;

        private void PostPrivateChat(string message)
        {

        }

        /// <summary>
        /// Добавляем сообщение в чат 
        /// </summary>
        /// <param name="chatPost"></param>
        /// <returns></returns>
        public int TryAddPost(ChatPost chatPost, PlayerServer player)
        {
            if (chatPost.IdChat == 0)
            {

                return -1;

                //  Бессмысленно писать сообщения а не команды в системный приватный чат, никто их не увидит
            }

            //// HashSet<long> playerChats
            //if (!player.Chats.Contains(chatPost.IdChat))
            //{
            //    // 
            //    return -1;
            //}

            lock (chatPosts)
            {
                chatPosts.Add(chatPost);
            }

            return -1;
        }

        public void TryCreateChat(PlayerServer player, Chat chat, string who)
        {
            // public static void PostCommandAddPlayer(PlayerServer player, Chat chat, string who)
        }

        public void TryAddPlayer(PlayerServer player, Chat chat, string who)
        {
            // проверка на корректность данных: это не системный чат, мы хозяин чата, такой пользователь существует.
            var newPlayer = Repository.GetData.PlayersAll
                .FirstOrDefault(p => p.Public.Login == who);

            if (newPlayer == null)
                PostCommandPrivatPostActivChat(player, chat, "User " + who + " not found");


            if (chat.PartyLogin.Any(p => p == who))
            {
                PostCommandPrivatPostActivChat(player, chat, "The player is already here");
            }




            else if (!player.PublicChat.PartyLogin.Any(p => p == who))
                PostCommandPrivatPostActivChat(player, chat, "Can not access " + who + " player");
            else if (!chat.OwnerMaker)
                PostCommandPrivatPostActivChat(player, chat, "People can not be added to a shared channel");
            else if (chat.OwnerLogin != player.Public.Login && !player.IsAdmin)
                PostCommandPrivatPostActivChat(player, chat, "You can not add people");
            else
            {
                chat.Posts.Add(new ChatPost()
                {
                    Time = DateTime.UtcNow,
                    Message = "User " + newPlayer.Public.Login + " entered the channel.",
                    OwnerLogin = "system"
                });

                chat.PartyLogin.Add(newPlayer.Public.Login);
                newPlayer.Chats.Add(chat);
                Repository.Get.ChangeData = true;
            }
        }
    }
}

