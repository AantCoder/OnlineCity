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
    internal class StateExcludeCmd : IChatCmd
    {
        public string CmdID => "stateexclude";

        public Grants GrantsForRun => Grants.UsualUser;

        public string Help => ChatManager.prefix + "stateexclude {UserLogin} : exclude player from state";

        private readonly ChatManager _chatManager;

        public StateExcludeCmd(ChatManager chatManager)
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
            if (myStatePosition?.RightExcludePlayer != true)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "Your position in the state does not allow you to do this.");
            }
            var actStatePosition = Repository.GetStatePosition(actPlayer.Public);
            if (myStatePosition?.RightHead != true && actStatePosition?.RightHead == true)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "Your position in the state does not allow you to do this.");
            }

            //если удалился последний глава, то государство удаляется
            if (actStatePosition?.RightHead == true
                && !Repository.GetData.GetStatePlayers(actPlayer.Public.StateName)
                    .Any(p => p != actPlayer && Repository.GetStatePosition(p.Public)?.RightHead == true))
            {
                foreach (var p in Repository.GetData.GetStatePlayers(actPlayer.Public.StateName))
                {
                    p.Public.StateName = null;
                    p.Public.StatePositionName = null;
                }

                Repository.GetData.StatePositions = Repository.GetData.StatePositions.Where(sp => sp.StateName == actPlayer.Public.StateName).ToList();
                Repository.GetData.States.Remove(myState);
            }

            actPlayer.Public.StateName = null;
            actPlayer.Public.StatePositionName = null;
            Repository.GetData.UpdateStatesDic();
            Repository.Get.ChangeData = true;
            Loger.Log($"{myLogin} exclude player {actPlayer.Public.Login} from state {myState.Name}");

            return new ModelStatus();
        }
    }
}
