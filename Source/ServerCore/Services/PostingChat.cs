using System;
using System.Collections.Generic;
using System.Linq;
using Transfer;
using Model;
using OCUnion;
using ServerOnlineCity.Model;
using ServerOnlineCity.ChatService;
using OCUnion.Transfer.Types;
using OCUnion.Common;
using OCUnion.Transfer.Model;

namespace ServerOnlineCity.Services
{
    public sealed class PostingChat : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request19PostingChat;

        public int ResponseTypePackage => (int)PackageType.Response20PostingChat;

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
            if (context.PossiblyIntruder)
            {
                context.Disconnect("Possibly intruder");
                return null;
            }
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

            Grants acceptedGrants;
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

                acceptedGrants = context.Player.Public.Grants & player.Public.Grants;
            }
            else
            {
                player = context.Player;
                acceptedGrants = player.Public.Grants;
            }

            if (pc.Message[0] == '/')
            {
                //разбираем аргументы в кавычках '. Удвоенная кавычка указывает на её символ.
                string command;
                List<string> argsM;
                ChatUtils.ParceCommand(pc.Message, out command, out argsM);

                var result = ChatManager.TryGetCmdForUser(player.Public.Login, acceptedGrants, command, out IChatCmd cmd);
                if (result.Status > 0)
                    return result;

                // аdditionally check Grants for DiscordUser: some commands for example: create chat, get token doesn't permitted from discord

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
                    OwnerLogin = player.Public.Login,
                    DiscordIdMessage = pc.IdDiscordMsg,
                });
            }

            return new ModelStatus()
            {
                Status = 0,
                Message = null
            };
        }

    }
}