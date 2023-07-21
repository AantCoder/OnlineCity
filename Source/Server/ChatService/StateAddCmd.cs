using OCUnion;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using Model;
using System.Collections.Generic;
using Transfer;
using OCUnion.Transfer.Types;
using OCUnion.Transfer.Model;
using ServerOnlineCity;

namespace ServerOnlineCity.ChatService
{
    internal class StateAddCmd : IChatCmd
    {
        public string CmdID => "stateadd";

        public Grants GrantsForRun => Grants.UsualUser;

        public string Help => ChatManager.prefix + "stateadd {UserLogin} : Add player to state";

        private readonly ChatManager _chatManager;

        public StateAddCmd(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM, ServiceContext context)
        {
            var myLogin = player.Public.Login;

            if (argsM.Count < 1)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.PlayerNameEmpty, myLogin, chat, "Player name is empty");
            }
            var actPlayer = Repository.GetPlayerByLogin(argsM[0]);
            if (actPlayer == null)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.UserNotFound, myLogin, chat, "User " + argsM[0] + " not found");
            }

            var myState = Repository.GetStateByName(player.Public.StateName);
            if (myState == null)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "You are not in the state");
            }
            if (Repository.GetStateByName(actPlayer.Public.StateName) != null)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "User already in the state");
            }

            var myStatePosition = Repository.GetStatePosition(player.Public);
            if (myStatePosition?.RightAddPlayer != true)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "Your position in the state does not allow you to do this.");
            }

            actPlayer.Public.StateName = myState.Name;
            actPlayer.Public.StatePositionName = null;
            Repository.GetData.UpdateStatesDic();
            Repository.Get.ChangeData = true;
            Loger.Log($"{myLogin} add player {actPlayer.Public.Login} to state {myState.Name}");

            return new ModelStatus();
        }
    }
}
