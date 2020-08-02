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
    internal sealed class AbandonMySettlementCmd : IChatCmd
    {
        public string CmdID => "killmyallplease";

        public Grants GrantsForRun => Grants.UsualUser | Grants.DiscordBot;

        public string Help => ChatManager.prefix + "killmyallplease : Abandon My Settlement";

        private readonly ChatManager _chatManager;

        public AbandonMySettlementCmd(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> param)
        {
            if (chat.OwnerMaker)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.OnlyForPublicChannel, player.Public.Login, chat, "Operation only for the shared channel");
            }

            var msg = "User " + player.Public.Login + " deleted settlements.";
            _chatManager.AddSystemPostToPublicChat(msg);

            Repository.DropUserFromMap(player.Public.Login);
            Repository.GetSaveData.DeletePlayerData(player.Public.Login);
            player.Public.LastSaveTime = DateTime.MinValue;
            Loger.Log("Server killmyallplease " + player.Public.Login);
            player = null; ///  значение передается по ссылке, и успешно обнуляет у передающего класса
            Repository.Get.ChangeData = true;

            return new ModelStatus()
            {
                Status = 0,
            };
        }
    }
}
