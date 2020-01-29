using OCUnion;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using Model;
using System.Collections.Generic;
using Transfer;
using OCUnion.Transfer.Types;

namespace ServerOnlineCity.ChatService
{
    internal sealed class CreateChatCmd : IChatCmd
    {
        public string CmdID => "createchat";

        public Grants GrantsForRun => Grants.UsualUser;

        public string Help => ChatManager.prefix + "createchat :Create private chat";

        private readonly ChatManager _chatManager;

        public CreateChatCmd(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM)
        {
            var myLogin = player.Public.Login;
            if (argsM.Count < 1)
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.SetNameChannel, myLogin, chat, "No new channel name specified");

            var nChat = _chatManager.CreateChat(
            new Chat()
            {
                Name = argsM[0],
                OwnerLogin = myLogin,
                OwnerMaker = true,
                PartyLogin = new List<string>() { myLogin, "system" },
                LastChanged = DateTime.UtcNow,
            });

            nChat.Posts.Add(
                new ChatPost()
                {
                    Message = "Created a channel " + argsM[0],
                    OwnerLogin = myLogin,
                    Time = DateTime.UtcNow
                }
                );

            player.Chats.Add(nChat, new ModelUpdateTime() { Value = -1 });
            if (argsM.Count > 1)
            {
                _chatManager.PostCommandAddPlayer(player, nChat, argsM[1]);
            }

            Repository.Get.ChangeData = true;

            return new ModelStatus();
        }
    }
}
