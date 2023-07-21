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
using System.Linq;

namespace ServerOnlineCity.ChatService
{
    internal class StatePositionCmd : IChatCmd
    {
        public string CmdID => "stateposition";

        public Grants GrantsForRun => Grants.UsualUser;

        public string Help => ChatManager.prefix + "stateposition {PositionName} {del/1/0 RightAddPlayer} {1/0 RightExcludePlayer} {1/0 RightEditRights} : create and changed a position in the state";

        private readonly ChatManager _chatManager;

        public StatePositionCmd(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM, ServiceContext context)
        {
            var myLogin = player.Public.Login;

            var myState = Repository.GetStateByName(player.Public.StateName);
            if (myState == null)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "You are not in the state");
            }

            var myStatePosition = Repository.GetStatePosition(player.Public);
            if (myStatePosition?.RightEdit != true)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "Your position in the state does not allow you to do this.");
            }

            if (argsM.Count < 1)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "Position name is empty");
            }
            var position = Repository.GetStatePositionByName(player.Public.StateName, argsM[0]);
            if (position?.Name == "Head")
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "Your position in the state does not allow you to do this.");
            }
            if (position == null)
            {
                if (argsM.Count >= 2 && argsM[1].ToLower() == "del")
                {
                    return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "Position " + argsM[0] + " not found");
                }
                else
                {
                    position = new StatePosition() { StateName = player.Public.StateName, Name = argsM[0] };
                    Repository.GetData.StatePositions.Add(position);
                    Repository.GetData.UpdateStatesDic();
                }
            }

            if (argsM.Count >= 2 && argsM[1].ToLower() == "del")
            {
                //удаляем роль
                foreach (var p in Repository.GetData.GetStatePlayers(player.Public.StateName)
                    .Where(p => p.Public.StatePositionName == position.Name))
                    p.Public.StatePositionName = null;

                Repository.GetData.StatePositions.Remove(position);
                Repository.GetData.UpdateStatesDic();

                Repository.Get.ChangeData = true;
                Loger.Log($"{myLogin} del position {position.Name} in state {myState.Name}");

                return new ModelStatus();
            }
            //меняем роль
            if (argsM.Count < 4)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "Parameters is empty");
            }
            if (argsM[1] != "0" && argsM[1] != "1"
                || argsM[2] != "0" && argsM[2] != "1"
                || argsM[3] != "0" && argsM[3] != "1")
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "Set 3 parameters to 0 or 1");
            }

            position.RightAddPlayer = argsM[1] != "0";
            position.RightExcludePlayer = argsM[2] != "0";
            position.RightEdit = argsM[3] != "0";

            Repository.Get.ChangeData = true;
            Loger.Log($"{myLogin} set position {position.Name} in state {myState.Name}");

            return new ModelStatus();
        }
    }
}
