using Model;
using OCUnion.Transfer.Types;
using ServerOnlineCity.ChatService;
using ServerOnlineCity.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Transfer;

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

        public static ModelStatus PostCommandPrivatPostActivChat(ChatCmdResult reason, string login, Chat chat, string msg)
        {
            chat.Posts.Add(new ChatPost()
            {
                Time = DateTime.UtcNow,
                Message = msg,
                OwnerLogin = "system",
                OnlyForPlayerLogin = login
            });

            return new ModelStatus()
            {
                Status = (int)reason,
                Message = msg,
            };
        }

        public static ModelStatus PostCommandAddPlayer(PlayerServer player, Chat chat, string who)
        {
            // проверка на корректность данных: это не системный чат, мы хозяин чата, такой пользователь существует.
            var myLogin = player.Public.Login;
            if (chat.PartyLogin.Any(p => p == who))
            {
                return PostCommandPrivatPostActivChat(ChatCmdResult.PlayerHere, myLogin, chat, "The player is already here");
            }

            var newPlayer = Repository.GetPlayerByLogin(who);
            if (newPlayer == null)
                return PostCommandPrivatPostActivChat(ChatCmdResult.UserNotFound, myLogin, chat, "User " + who + " not found");

            if (!player.PublicChat.PartyLogin.Any(p => p == who))
                return PostCommandPrivatPostActivChat(ChatCmdResult.UserNotFound, myLogin, chat, "Can not access " + who + " player");

            if (!chat.OwnerMaker) // Вопрос к автору, что это такое ?
                return PostCommandPrivatPostActivChat(ChatCmdResult.CantAccess, myLogin, chat, "People can not be added to a shared channel");

            if (chat.OwnerLogin != player.Public.Login && !player.IsAdmin)
                return PostCommandPrivatPostActivChat(ChatCmdResult.AccessDeny, myLogin, chat, "You can not add people");

            var msg = "User " + newPlayer.Public.Login + " entered the channel.";
            chat.Posts.Add(new ChatPost()
            {
                Time = DateTime.UtcNow,
                Message = msg,
                OwnerLogin = "system"
            });

            chat.PartyLogin.Add(newPlayer.Public.Login);
            newPlayer.Chats.Add(chat);
            Repository.Get.ChangeData = true;

            return new ModelStatus()
            {
                Status = 0,
                Message = msg,
            };
        }

        private List<ChatPost> chatPosts;

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

        /// <summary>
        /// Check User (for compability with a Discord) user Grants and Run Cmd
        /// </summary>
        /// <param name="login"></param>
        /// <param name="cmd"></param>
        /// <returns></returns>        
        public static ModelStatus TryGetCmdForUser(string login, string command, out IChatCmd result)
        {
            result = null;
            var user = Repository.GetPlayerByLogin(login);
            if (user == null)
            {
                return new ModelStatus()
                {
                    Message = $"user for login {login} not found",
                    Status = (int)ChatCmdResult.UserNotFound,
                };
            }

            if (ChatManager.ChatCmds.TryGetValue(command, out IChatCmd cmd))
            {
                if (((int)cmd.GrantsForRun & (int)user.Public.Grants) > 0)
                {
                    result = cmd;

                    return new ModelStatus()
                    {
                        Status = 0,
                        Message = string.Empty
                    };
                }
                else
                {
                    return new ModelStatus()
                    {
                        Status = (int)ChatCmdResult.AccessDeny,
                        Message = $"user {user.Public.Login} does not have permission for run " + command,
                    };
                }
            }

            return new ModelStatus()
            {
                Status = (int)ChatCmdResult.CommandNotFound,
                Message = "Command not found: " + command
            };
        }
    }
}