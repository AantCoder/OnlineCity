using Model;
using OCUnion;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using System.Collections.Generic;

namespace ServerOnlineCity.ChatService
{
    internal sealed class AbandonMySettlementCmd : IChatCmd
    {
        public string CmdID => "killmyallplease";

        public Grants GrantsForRun => Grants.UsualUser;

        public string Help => ChatManager.prefix + "killmyallplease : Abandon My Settlement";

        public void Execute(ref PlayerServer player, Chat chat, List<string> param)
        {
            //Loger.Log("Server killmyallplease OwnerMaker=" + (chat.OwnerMaker ? "1" : "0"));
            if (chat.OwnerMaker)
                ChatManager.PostCommandPrivatPostActivChat(player, chat, "Operation only for the shared channel");
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
                        item.UpdateTime = DateTime.UtcNow;
                        data.WorldObjects.Remove(item);
                        data.WorldObjectsDeleted.Add(item);
                    }
                }

                player.SaveDataPacket = null;
                Loger.Log("Server killmyallplease " + player.Public.Login);
                player = null; ///  значение передается по ссылке, и успешно обнуляет у передающего класса
                Repository.Get.ChangeData = true;
            }
        }
    }
}
