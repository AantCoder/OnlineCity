using OCUnion;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using Model;
using System.Collections.Generic;

namespace ServerOnlineCity.ChatService
{
    internal sealed class CreateChatCmd : IChatCmd
    {
        public string CmdID => "createchat";

        public Grants GrantsForRun => Grants.UsualUser;

        public string Help => ChatManager.prefix + "createchat :Create private chat";

        public void Execute(ref PlayerServer player, Chat chat, List<string> argsM)
        {
            //Loger.Log("Server createChat");
            if (argsM.Count < 1) ChatManager.PostCommandPrivatPostActivChat(player, chat, "No new channel name specified");
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
                {
                    ChatManager.PostCommandAddPlayer(player, nChat, argsM[1]);
                }

                Repository.Get.ChangeData = true;
            }
        }
    }
}
