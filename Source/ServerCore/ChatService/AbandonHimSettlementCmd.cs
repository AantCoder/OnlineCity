using Model;
using OCUnion;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServerOnlineCity.ChatService
{
    internal sealed class AbandonHimSettlementCmd : IChatCmd
    {
        public string CmdID => "killhimplease";

        public Grants GrantsForRun => Grants.SuperAdmin;

        public string Help => ChatManager.prefix + "killhimplease {UserLogin}: Drop user Settlement and delete him from a server";

        public void Execute(ref PlayerServer player, Chat chat, List<string> argsM)
        {
            //Loger.Log("Server killhimplease OwnerMaker=" + (chat.OwnerMaker ? "1" : "0"));
            if (!player.IsAdmin)
            {
                ChatManager.PostCommandPrivatPostActivChat(player, chat, "Command only for admin");
                return;
            }

            if (argsM.Count < 1)
            {
                ChatManager.PostCommandPrivatPostActivChat(player, chat, "Player name is empty");
                return;
            }


            var killPlayer = Repository.GetData.PlayersAll
                .FirstOrDefault(p => p.Public.Login == argsM[0]);
            if (killPlayer == null)
                ChatManager.PostCommandPrivatPostActivChat(player, chat, "User " + argsM[0] + " not found");
            else
            {
                chat.Posts.Add(new ChatPost()
                {
                    Time = DateTime.UtcNow,
                    Message = "User " + killPlayer.Public.Login + " deleted settlements.",
                    OwnerLogin = "system"
                });

                var data = Repository.GetData;
                lock (data)
                {
                    for (int i = 0; i < data.WorldObjects.Count; i++)
                    {
                        var item = data.WorldObjects[i];
                        if (item.LoginOwner != killPlayer.Public.Login) continue;
                        //удаление из базы
                        item.UpdateTime = DateTime.UtcNow;
                        data.WorldObjects.Remove(item);
                        data.WorldObjectsDeleted.Add(item);
                    }
                }

                Repository.GetSaveData.DeletePlayerData(killPlayer.Public.Login);
                Repository.Get.ChangeData = true;
                Loger.Log("Server killhimplease " + killPlayer.Public.Login);
            }
        }
    }
}