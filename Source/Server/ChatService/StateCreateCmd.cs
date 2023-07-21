using OCUnion;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using Model;
using System.Collections.Generic;
using Transfer;
using OCUnion.Transfer.Types;
using OCUnion.Transfer.Model;

namespace ServerOnlineCity.ChatService
{
    internal sealed class StateCreateCmd : IChatCmd
    {
        public string CmdID => "statecreate";

        public Grants GrantsForRun => Grants.UsualUser;

        public string Help => ChatManager.prefix + "statecreate {Name} : Create new State";

        private readonly ChatManager _chatManager;

        public StateCreateCmd(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM, ServiceContext context)
        {
            var myLogin = player.Public.Login;
            if (argsM.Count < 1)
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "No new state name specified");

            if (!string.IsNullOrEmpty(player.Public.StateName))
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "The player is already in the state");

            var name = (argsM[0] ?? "").Trim();
            
            var errorValid = Repository.GetData.NameValidator.TextValidator(name);
            if (errorValid != null)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "State name " + errorValid);
            }
            if (!Repository.GetData.NameValidator.CheckFree(name))
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "This name already exists");
            }

            var state = new State()
            {
                Name = name,
            };
            var stateHead = new StatePosition()
            {
                StateName = name,
                Name = "Head",
                RightAddPlayer = true,
                RightEdit = true,
                RightExcludePlayer = true,
                RightHead = true,
            };
            player.Public.StateName = name;
            player.Public.StatePositionName = stateHead.Name;

            ChatManager.Instance.PublicChat.LastChanged = System.DateTime.UtcNow;
            Repository.GetData.States.Add(state);
            Repository.GetData.StatePositions.Add(stateHead);
            Repository.GetData.UpdateStatesDic();
            Repository.Get.ChangeData = true;
            Loger.Log($"Create state {name} by {myLogin} successfully register on this server.", Loger.LogLevel.REGISTER);

            return new ModelStatus();
        }
    }
}
