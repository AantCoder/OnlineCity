using Model;
using OCUnion;
using OCUnion.Transfer.Types;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using System.Collections.Generic;
using Transfer;

namespace ServerOnlineCity.ChatService
{
    internal class ExitChatCmd : IChatCmd
    {
        public string CmdID => "exitchat";

        public Grants GrantsForRun => Grants.UsualUser;

        string IChatCmd.Help => ChatManager.prefix + "exitchat : exit from a private chat";

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> param)
        {
            var myLogin = player.Public.Login;
            if (!chat.OwnerMaker)
            {
                return ChatManager.PostCommandPrivatPostActivChat(ChatCmdResult.AccessDeny, myLogin, chat, "From a shared channel, you can not leave");
            }

            chat.Posts.Add(new ChatPost()
            {
                Time = DateTime.UtcNow,
                Message = "User " + myLogin + " left the channel.",
                OwnerLogin = "system"
            });

            chat.PartyLogin.Remove(myLogin);
            var r = player.Chats.Remove(chat);
            Loger.Log("Server exitChat remove" + (r ? "1" : "0"));
            Repository.Get.ChangeData = true;

            return new ModelStatus();
        }
    }
}