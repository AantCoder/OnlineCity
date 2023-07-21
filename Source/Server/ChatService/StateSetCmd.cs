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
    internal class StateSetCmd : IChatCmd
    {
        public string CmdID => "stateset";

        public Grants GrantsForRun => Grants.UsualUser;

        public string Help => ChatManager.prefix + "stateset {UserLogin} {PositionName/del}: set position for player";

        private readonly ChatManager _chatManager;

        public StateSetCmd(ChatManager chatManager)
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
            if (myState != Repository.GetStateByName(actPlayer.Public.StateName))
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "Player is not in your state");
            }

            var myStatePosition = Repository.GetStatePosition(player.Public);
            if (myStatePosition?.RightEdit != true)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "Your position in the state does not allow you to do this.");
            }
            var actStatePosition = Repository.GetStatePosition(actPlayer.Public);
            if (myStatePosition?.RightHead != true && actStatePosition?.RightHead == true)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "Your position in the state does not allow you to do this.");
            }

            if (argsM.Count < 2)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "Position name or \"del\" is empty");
            }
            var setStatePosition = Repository.GetStatePositionByName(player.Public.StateName, argsM[1]);
            if (setStatePosition == null && argsM[1].ToLower() != "del")
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "Position " + argsM[1] + " not found");
            }

            actPlayer.Public.StatePositionName = argsM[1].ToLower() == "del" ? null : setStatePosition.Name;
            Repository.GetData.UpdateStatesDic();
            Repository.Get.ChangeData = true;
            Loger.Log($"{myLogin} set player {actPlayer.Public.Login} on position {actPlayer.Public.StatePositionName} in state {myState.Name}");

            return new ModelStatus();
        }
    }
}
