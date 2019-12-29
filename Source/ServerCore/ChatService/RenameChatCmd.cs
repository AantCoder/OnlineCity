using System;
using System.Collections.Generic;
using Model;
using OCUnion;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;

namespace ServerOnlineCity.ChatService
{
    internal sealed class RenameChatCmd : IChatCmd
    {
        public string CmdID => "renamechat";

        public Grants GrantsForRun => Grants.UsualUser;

        public string Help => ChatManager.prefix + "renamechat : rename private channel";

        public void Execute(ref PlayerServer player, Chat chat, List<string> argsM)
        {
            if (!chat.OwnerMaker)
            {
                ChatManager.PostCommandPrivatPostActivChat(player, chat, "You can not rename a shared channel");
            }

            if (argsM.Count < 1)
            {
                ChatManager.PostCommandPrivatPostActivChat(player, chat, "No new name specified");
            }
            else if (chat.OwnerLogin != player.Public.Login && !player.IsAdmin)
            {
                ChatManager.PostCommandPrivatPostActivChat(player, chat, "Operation is not available to you");
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
        }
    }
}
