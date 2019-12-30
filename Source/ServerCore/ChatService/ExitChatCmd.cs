using Model;
using OCUnion;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using System.Collections.Generic;

namespace ServerOnlineCity.ChatService
{
    internal class ExitChatCmd : IChatCmd
    {
        public string CmdID => "exitchat";

        public Grants GrantsForRun => Grants.UsualUser;

        string IChatCmd.Help => ChatManager.prefix + "exitchat : exit from a private chat";

        public void Execute(ref PlayerServer player, Chat chat, List<string> param)
        {
            //Loger.Log("Server exitChat OwnerMaker=" + (chat.OwnerMaker ? "1" : "0"));
            if (!chat.OwnerMaker)
            {
                ChatManager.PostCommandPrivatPostActivChat(player, chat, "From a shared channel, you can not leave");
            }
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
        }
    }
}