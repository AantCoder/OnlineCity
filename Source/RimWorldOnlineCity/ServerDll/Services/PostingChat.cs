using System;
using System.Collections.Generic;
using System.Linq;
using Transfer;
using Model;
using OCServer.Model;
using OCUnion;

namespace OCServer.Services
{
    public class PostingChat 
    {
        public ModelStatus GetModelStatus(ref PlayerServer player, ModelPostingChat pc)
        {
            var timeNow = DateTime.UtcNow;
            if (string.IsNullOrEmpty(pc.Message))
                return new ModelStatus()
                {
                    Status = 0,
                    Message = null
                };

            var chat = player.Chats.FirstOrDefault(ct => ct.Id == pc.ChatId);
            if (chat == null)
                return new ModelStatus()
                {
                    Status = 1,
                    Message = "Chat not available"
                };

            if (pc.Message[0] == '/')
            {
                var s = pc.Message.Split(new char[] { ' ' }, 2);
                var command = s[0].Trim().ToLower();
                var args = s.Length == 1 ? "" : s[1];
                //разбираем аргументы в кавычках '. Удвоенная кавычка указывает на её символ.
                var argsM = SplitBySpace(args);

                // обработка команд чата {
                switch (command)
                {
                    case "/help":
                        PostCommandPrivatPostActivChat(player, chat, ChatHelpText
                            + (player.IsAdmin ? ChatHelpTextAdmin : ""));
                        break;
                    case "/createchat":
                        Loger.Log("Server createChat");
                        if (argsM.Count < 1) PostCommandPrivatPostActivChat(player, chat, "No new channel name specified");
                        else
                        {
                            var nChat = new Chat()
                            {
                                Name = argsM[0],
                                OwnerLogin = player.Public.Login,
                                OwnerMaker = true,
                                PartyLogin = new List<string>() { player.Public.Login, "system" },
                                Id = Repository.GetData.GetChatId(),
                            };

                            nChat.Posts.Add(new ChatPost()
                            {
                                Time = DateTime.UtcNow,
                                Message = "User " + player.Public.Login + " created a channel " + argsM[0],
                                OwnerLogin = "system"
                            });

                            player.Chats.Add(nChat);

                            if (argsM.Count > 1)
                                PostCommandAddPlayer(player, nChat, argsM[1]);
                            Repository.Get.ChangeData = true;
                        }

                        break;
                    case "/exitchat":
                        Loger.Log("Server exitChat OwnerMaker=" + (chat.OwnerMaker ? "1" : "0"));
                        if (!chat.OwnerMaker)
                        { PostCommandPrivatPostActivChat(player, chat, "From a shared channel, you can not leave"); }
                        else
                        {
                            chat.Posts.Add(new ChatPost()
                            {
                                Time = DateTime.UtcNow,
                                Message = "User " + player.Public.Login + " left the channel.",
                                OwnerLogin = "system"
                            });

                            chat.PartyLogin.Remove(player.Public.Login);
                            var r = player.Chats.Remove(chat);
                            Loger.Log("Server exitChat remove" + (r ? "1" : "0"));
                            Repository.Get.ChangeData = true;
                        }

                        break;
                    case "/renamechat":
                        if (!chat.OwnerMaker)
                        {
                            PostCommandPrivatPostActivChat(player, chat, "You can not rename a shared channel");
                        }

                        if (argsM.Count < 1)
                        {
                            PostCommandPrivatPostActivChat(player, chat, "No new name specified");
                        }
                        else if (chat.OwnerLogin != player.Public.Login && !player.IsAdmin)
                        {
                            PostCommandPrivatPostActivChat(player, chat, "Operation is not available to you");
                        }
                        else
                        {
                            chat.Posts.Add(new ChatPost()
                            {
                                Time = DateTime.UtcNow,
                                Message = "The channel was renamed to " + argsM[0],
                                OwnerLogin = "system"
                            });

                            Loger.Log("Server renameChat " + chat.Name + " -> " + argsM[0]);
                            chat.Name = argsM[0];
                            Repository.Get.ChangeData = true;
                        }

                        break;
                    case "/addplayer":
                        Loger.Log("Server addPlayer");
                        if (argsM.Count < 1)
                        {
                            PostCommandPrivatPostActivChat(player, chat, "Player name is empty");
                        }
                        else
                        {
                            PostCommandAddPlayer(player, chat, argsM[0]);
                        }

                        break;
                    case "/killmyallplease":
                        Loger.Log("Server killmyallplease OwnerMaker=" + (chat.OwnerMaker ? "1" : "0"));
                        if (chat.OwnerMaker)
                            PostCommandPrivatPostActivChat(player, chat, "Operation only for the shared channel");
                        else
                        {
                            chat.Posts.Add(new ChatPost()
                            {
                                Time = DateTime.UtcNow,
                                Message = "User " + player.Public.Login + " deleted settlements.",
                                OwnerLogin = "system"
                            });

                            var data = Repository.GetData;
                            lock (data)
                            {
                                for (int i = 0; i < data.WorldObjects.Count; i++)
                                {
                                    var item = data.WorldObjects[i];
                                    if (item.LoginOwner != player.Public.Login) continue;
                                    //удаление из базы
                                    item.UpdateTime = timeNow;
                                    data.WorldObjects.Remove(item);
                                    data.WorldObjectsDeleted.Add(item);
                                }
                            }

                            player.SaveDataPacket = null;
                            Loger.Log("Server killmyallplease " + player.Public.Login);
                            player = null; ///  значение передается по ссылке, и успешно обнуляет у передающего класса
                            Repository.Get.ChangeData = true;
                        }

                        break;
                    case "/killhimplease":
                        Loger.Log("Server killhimplease OwnerMaker=" + (chat.OwnerMaker ? "1" : "0"));
                        if (!player.IsAdmin)
                            PostCommandPrivatPostActivChat(player, chat, "Command only for admin");
                        else
                        if (argsM.Count < 1)
                            PostCommandPrivatPostActivChat(player, chat, "Player name is empty");
                        else
                        {
                            var killPlayer = Repository.GetData.PlayersAll
                                .FirstOrDefault(p => p.Public.Login == argsM[0]);
                            if (killPlayer == null)
                                PostCommandPrivatPostActivChat(player, chat, "User " + argsM[0] + " not found");
                            else
                            {
                                chat.Posts.Add(new ChatPost()
                                {
                                    Time = DateTime.UtcNow,
                                    Message = "User " + killPlayer.Public.Login + " deleted settlements.",
                                    OwnerLogin = "system"
                                });
                                var data = Repository.GetData;
                                lock (data)
                                {
                                    for (int i = 0; i < data.WorldObjects.Count; i++)
                                    {
                                        var item = data.WorldObjects[i];
                                        if (item.LoginOwner != killPlayer.Public.Login) continue;
                                        //удаление из базы
                                        item.UpdateTime = timeNow;
                                        data.WorldObjects.Remove(item);
                                        data.WorldObjectsDeleted.Add(item);
                                    }
                                }
                                killPlayer.SaveDataPacket = null;
                                Repository.Get.ChangeData = true;
                                Loger.Log("Server killhimplease " + killPlayer.Public.Login);
                            }
                        }
                        break;
                    case "/Discord":
                        Loger.Log($"User {player.Public.Login} request Discord token");
                        if (!player.IsAdmin || player.Public.Login == Repository.DISCORD)
                        {
                            PostCommandPrivatPostActivChat(player, chat, "Command only for admin");
                        }
                        else
                        {
                            var token = Repository.GetData.PlayersAll.FirstOrDefault(p => p.Public.Login == Repository.DISCORD)?.Pass;
                            PostCommandPrivatPostActivChat(player, chat, token);
                        }
                        break;
                    default:
                        PostCommandPrivatPostActivChat(player, chat, "Command not found: " + command);
                        return new ModelStatus()
                        {
                            Status = 2,
                            Message = "Command not found: " + command
                        };
                }
                // } обработка команд чата
            }
            else
            {
                Loger.Log("Server post " + player.Public.Login /*+ " " + timeNow.Ticks*/ + ":" + pc.Message);
                var mmsg = pc.Message;
                if (mmsg.Length > 2048) mmsg = mmsg.Substring(0, 2048);
                chat.Posts.Add(new ChatPost()
                {
                    Time = timeNow,
                    Message = mmsg,
                    OwnerLogin = player.Public.Login
                });

            }

            return new ModelStatus()
            {
                Status = 0,
                Message = null
            };

        }

        private void PostCommandPrivatPostActivChat(PlayerServer player, Chat chat, string msg)
        {
            chat.Posts.Add(new ChatPost()
            {
                Time = DateTime.UtcNow,
                Message = msg,
                OwnerLogin = "system",
                OnlyForPlayerLogin = player.Public.Login
            });
        }

        private void PostCommandAddPlayer(PlayerServer player, Chat chat, string who)
        {
            if (chat.PartyLogin.Any(p => p == who))
            {
                PostCommandPrivatPostActivChat(player, chat, "The player is already here");
            }
            else
            {
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
        }

        public static List<string> SplitBySpace(string args)
        {
            int i = 0;
            var argsM = new List<string>();
            while (i + 1 < args.Length)
            {
                if (args[i] == '\'')
                {
                    int endK = i;
                    bool exit;
                    do //запускаем поиск след кавычки снова, если после найденной ещё одна
                    {
                        exit = true;
                        endK = args.IndexOf('\'', endK + 1);
                        if (endK >= 0 && endK + 1 < args.Length && args[endK + 1] == '\'')
                        {
                            //это двойная кавычка - пропускаем её
                            endK++;
                            exit = false;
                        }
                    }

                    while (!exit);

                    if (endK >= 0)
                    {
                        argsM.Add(args.Substring(i + 1, endK - i - 1).Replace("''", "'"));
                        i = endK + 1;
                        continue;
                    }
                }

                var ni = args.IndexOf(" ", i);
                if (ni >= 0)
                {
                    //условие недобавления для двойного пробела
                    if (ni > i) argsM.Add(args.Substring(i, ni - i));
                    i = ni + 1;
                    continue;
                }
                else
                {
                    break;
                }
            }
            if (i < args.Length)
            {
                argsM.Add(args.Substring(i));
            }

            return argsM;
        }

        private const string ChatHelpText = @"Available commands: 
/help
/createChat
/exitChat
/addPlayer
/renameChat

";
        private const string ChatHelpTextAdmin = @"/killmyallplease
/killhimplease

";
    }
}