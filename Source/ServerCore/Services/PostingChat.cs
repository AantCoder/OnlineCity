using System;
using System.Collections.Generic;
using System.Linq;
using Transfer;
using Model;
using OCUnion;
using ServerOnlineCity.Model;
using ServerOnlineCity.ChatService;

namespace ServerOnlineCity.Services
{
    public sealed class PostingChat : IGenerateResponseContainer
    {
        public int RequestTypePackage => 19;

        public int ResponseTypePackage => 20;

        public ModelContainer GenerateModelContainer(ModelContainer request, ref PlayerServer player)
        {
            if (player == null)
                return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = GetModelStatus((ModelPostingChat)request.Packet, ref player);
            return result;
        }

        public ModelStatus GetModelStatus(ModelPostingChat pc, ref PlayerServer player)
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

            var chat = player.Chats.FirstOrDefault(ct => ct.Id == pc.ChatId);

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

                // обработка команд чата 
                if (ChatManager.ChatCmds.TryGetValue(command, out IChatCmd cmd))
                {
                    Loger.Log(pc.Message);
                    cmd.Execute(ref player, chat, argsM);
                }
                else
                {
                    ChatManager.PostCommandPrivatPostActivChat(player, chat, "Command not found: " + command);
                    return new ModelStatus()
                    {
                        Status = 2,
                        Message = "Command not found: " + command
                    };
                }
            }
            else
            {
                Loger.Log("Server post " + player.Public.Login + ":" + pc.Message);
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