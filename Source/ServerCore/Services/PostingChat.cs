using System;
using System.Collections.Generic;
using System.Linq;
using Transfer;
using Model;
using OCUnion;
using ServerOnlineCity.Model;
using ServerOnlineCity.ChatService;
using OCUnion.Transfer.Types;

namespace ServerOnlineCity.Services
{
    public sealed class PostingChat : IGenerateResponseContainer
    {
        public int RequestTypePackage => 19;

        public int ResponseTypePackage => 20;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null)
                return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = GetModelStatus((ModelPostingChat)request.Packet, context);
            return result;
        }

        public ModelStatus GetModelStatus(ModelPostingChat pc, ServiceContext context)
        {
            var timeNow = DateTime.UtcNow;
            if (string.IsNullOrEmpty(pc.Message))
            {
                return new ModelStatus()
                {
                    Status = 0,
                    Message = null
                };
            }

            var chat = context.Player.Chats.Keys.FirstOrDefault(ct => ct.Id == pc.IdChat);

            if (chat == null)
            {
                return new ModelStatus()
                {
                    Status = 1,
                    Message = "Chat not available"
                };
            }

            if (pc.Message[0] == '/')
            {
                var s = pc.Message.Split(new char[] { ' ' }, 2);
                var command = s[0].Trim().ToLower();
                var args = s.Length == 1 ? "" : s[1];
                //разбираем аргументы в кавычках '. Удвоенная кавычка указывает на её символ.
                var argsM = SplitBySpace(args);

                PlayerServer player;
                // обработка команд чата 
                if ("discord".Equals(context.Player.Public.Login.ToLower()))
                {
                    player = Repository.GetPlayerByLogin(pc.Owner);
                    if (player == null)
                    {
                        return new ModelStatus()
                        {
                            Status = (int)ChatCmdResult.UserNotFound,
                            Message = $"user {pc.Owner} not found",
                        };
                    }
                }
                else
                {
                    player = context.Player;
                }

                var result = ChatManager.TryGetCmdForUser(player.Public.Login, command, out IChatCmd cmd);
                if (result.Status > 0)
                    return result;

                return cmd.Execute(ref player, chat, argsM);
            }
            else
            {
                Loger.Log("Server post " + context.Player.Public.Login + ":" + pc.Message);
                var mmsg = pc.Message;
                if (mmsg.Length > 2048) mmsg = mmsg.Substring(0, 2048);
                chat.Posts.Add(new ChatPost()
                {
                    Time = timeNow,
                    Message = mmsg,
                    OwnerLogin = context.Player.Public.Login
                });
            }

            return new ModelStatus()
            {
                Status = 0,
                Message = null
            };
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
    }
}