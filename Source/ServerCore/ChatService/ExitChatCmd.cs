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

        private readonly ChatManager _chatManager;

        public ExitChatCmd(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> param)
        {
            var myLogin = player.Public.Login;
            if (chat.Id == 1)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.AccessDeny, myLogin, chat, "From a shared channel, you can not leave");
            }

            var r = player.Chats.Remove(chat);
            lock (chat)
            {
                chat.Posts.Add(new ChatPost()
                {
                    Time = DateTime.UtcNow,
                    Message = "User " + myLogin + " left the channel.",
                    OwnerLogin = "system"
                });

                chat.PartyLogin.Remove(myLogin);
                chat.LastChanged = DateTime.UtcNow;
            }
            Loger.Log("Server exitChat remove" + (r ? "1" : "0"));
            Repository.Get.ChangeData = true;

            return new ModelStatus();
        }
    }
}