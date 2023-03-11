using System;
using System.Collections.Generic;
using Model;
using OCUnion;
using OCUnion.Transfer.Types;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using Transfer;

namespace ServerOnlineCity.ChatService
{
    internal sealed class RenameChatCmd : IChatCmd
    {
        public string CmdID => "renamechat";

        public Grants GrantsForRun => Grants.UsualUser | Grants.SuperAdmin | Grants.Moderator;

        public string Help => ChatManager.prefix + "renamechat : rename private channel";

        private readonly ChatManager _chatManager;

        public RenameChatCmd(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM, ServiceContext context)
        {
            var myLogin = player.Public.Login;
            if (!chat.OwnerMaker)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.AccessDeny, myLogin, chat, "You can not rename a shared channel");
            }

            if (argsM.Count < 1)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.SetNameChannel, myLogin, chat, "No new name specified");
            }

            if (chat.OwnerLogin != player.Public.Login && !player.IsAdmin)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.AccessDeny, myLogin, chat, "Operation is not available to you");
            }

            chat.Posts.Add(new ChatPost()
            {
                Time = DateTime.UtcNow,
                Message = "The channel was renamed to " + argsM[0],
                OwnerLogin = "system"
            });

            chat.LastChanged = DateTime.UtcNow;
            Loger.Log("Server renameChat " + chat.Name + " -> " + argsM[0]);
            chat.Name = argsM[0];
            Repository.Get.ChangeData = true;

            return new ModelStatus();
        }
    }
}
