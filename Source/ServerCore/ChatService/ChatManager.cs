using Model;
using OCUnion;
using OCUnion.Transfer.Types;
using ServerOnlineCity.ChatService;
using ServerOnlineCity.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal class ChatManager
    {
        public static IReadOnlyDictionary<string, IChatCmd> ChatCmds;
        public const char prefix = '/';
        public const string InvalidCommand = "The command is not available";

        private object _chatPostsLocker = new object();

        public static ChatManager Instance { get; }

        public int MaxChatId => _maxChatId;

        private int _maxChatId;

        public int GetChatId() 
        {

            Interlocked.Increment(ref _maxChatId);
            return _maxChatId;
        }

        public Chat PublicChat { get; private set; }

        static ChatManager()
        {
            Instance = new ChatManager();
            var d = new Dictionary<string, IChatCmd>();
            foreach (var type in Assembly.GetEntryAssembly().GetTypes())
            {
                if (!type.IsClass)
                {
                    continue;
                }

                if (type.GetInterfaces().Any(x => x == typeof(IChatCmd)))
                {
                    var t = (IChatCmd)Activator.CreateInstance(type, Instance);
                    d[prefix + t.CmdID] = t;
                }
            }

            ChatCmds = d;
        }

        internal void AddSystemPostToPublicChat(string msg)
        {
            PublicChat.Posts.Add
                (
                new ChatPost()
                {
                    Message = msg,
                    OwnerLogin = "system",
                    Time = DateTime.UtcNow,
                }
                );
        }

        public void NewChatManager(int CurentMaxChatId, Chat publicChat)
        {
            _maxChatId = CurentMaxChatId;
            PublicChat = publicChat;
        }

        public ModelStatus PostCommandPrivatPostActivChat(ChatCmdResult reason, string login, Chat chat, string msg)
        {
            var post = new ChatPost()
            {
                Time = DateTime.UtcNow,
                Message = msg,
                OwnerLogin = "system",
                OnlyForPlayerLogin = login
            };

            chat.Posts.Add(post);

            return new ModelStatus()
            {
                Status = (int)reason,
                Message = msg,
            };
        }

        public ModelStatus PostCommandAddPlayer(PlayerServer player, Chat chat, string who)
        {
            string myLogin = player.Public.Login;
            // проверка на корректность данных: это не системный чат, мы хозяин чата, такой пользователь существует.
            var newPlayer = Repository.GetPlayerByLogin(who);
            if (newPlayer == null)
                return PostCommandPrivatPostActivChat(ChatCmdResult.UserNotFound, myLogin, chat, "User " + who + " not found");

            if (!chat.OwnerMaker) // Вопрос к автору, что это такое, если отработает следующее условие:  The player is already here?
                return PostCommandPrivatPostActivChat(ChatCmdResult.CantAccess, myLogin, chat, "People can not be added to a shared channel");

            var isAdmin = (player.Public.Grants & Grants.SuperAdmin) > 0;
            if (chat.OwnerLogin != myLogin && !isAdmin)
                return PostCommandPrivatPostActivChat(ChatCmdResult.AccessDeny, myLogin, chat, "You can not add people");

            //if (!player.PublicChat.PartyLogin.Any(p => p == who))
            //    return PostCommandPrivatPostActivChat(ChatCmdResult.UserNotFound, myLogin, chat, "Can not access " + who + " player");

            lock (chat)
            {
                if (chat.PartyLogin.Any(p => p == who))
                {
                    return PostCommandPrivatPostActivChat(ChatCmdResult.PlayerHere, myLogin, chat, "The player is already here");
                }

                newPlayer.Chats.Add(chat, new ModelUpdateTime() { Value = -1 });
                chat.PartyLogin.Add(newPlayer.Public.Login);
            }

            var msg = "User " + newPlayer.Public.Login + " entered the channel.";
            chat.Posts.Add(new ChatPost()
            {
                Message = msg,
                OwnerLogin = "system",
                Time = DateTime.UtcNow,
            });

            Repository.Get.ChangeData = true;

            return new ModelStatus()
            {
                Status = 0,
                Message = msg,
            };
        }


        public Chat CreateChat(Chat chat)
        {
            Interlocked.Increment(ref _maxChatId);
            chat.Id = _maxChatId;
            return chat;
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