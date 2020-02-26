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
    internal sealed class SetGrantsForUser : IChatCmd
    {
        public string CmdID => "grants";

        public Grants GrantsForRun => Grants.SuperAdmin | Grants.DiscordBot | Grants.Moderator;

        public string Help => ChatManager.prefix + "grants add {UserLogin} {RoleName} or {roleNumber}: add grants for user" + Environment.NewLine +
            ChatManager.prefix + "grants revoke {UserLogin} {RoleName} or {roleNumber}: revoke grants from user" + Environment.NewLine +
            ChatManager.prefix + "grants type {UserLogin}: type grants for user";

        private readonly ChatManager _chatManager;

        public SetGrantsForUser(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM)
        {
            var myLogin = player.Public.Login;

            if (argsM.Count < 1)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, Help);
            }
            var subCmd = argsM[0]?.ToLower();

            if (argsM.Count < 2)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.PlayerNameEmpty, myLogin, chat, "Player name is empty");
            }

            var anotherPlayer = Repository.GetPlayerByLogin(argsM[1]);
            if (anotherPlayer == null)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.UserNotFound, myLogin, chat, $"Player {argsM[1]} not found :-(");
            }

            Grants newGrants = Grants.NoPermissions;
            if ("add".Equals(subCmd) || "revoke".Equals(subCmd))
            {
                if (argsM.Count < 3)
                {
                    return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.IncorrectSubCmd, myLogin, chat, $"role can be empty");
                }

                newGrants = GetGrantsByStr(argsM[2]);
                if (newGrants.Equals(Grants.NoPermissions))
                {
                    return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.RoleNotFound, myLogin, chat, $"retype role, this {argsM[2]} not found");
                }
            }

            // anotherPlayer может стать null ?
            switch (subCmd)
            {
                case "add":
                    {
                        var msg = $"User {myLogin} add grants {newGrants.ToString()} to {anotherPlayer.Public.Login}";
                        Loger.Log(msg);
                        lock (anotherPlayer.Public)
                        {
                            anotherPlayer.Public.Grants = anotherPlayer.Public.Grants | newGrants;
                        }

                        Repository.Get.ChangeData = true;
                        return new ModelStatus()
                        {
                            Status = 0,
                            Message = msg,
                        };
                    }
                case "revoke":
                    {
                        var msg = $"User {myLogin} revoke grants {newGrants.ToString()} from {anotherPlayer.Public.Login}";
                        Loger.Log(msg);
                        lock (anotherPlayer.Public)
                        {
                            anotherPlayer.Public.Grants = anotherPlayer.Public.Grants & ~newGrants;
                        }

                        Repository.Get.ChangeData = true;
                        return new ModelStatus()
                        {
                            Status = 0,
                            Message = msg,
                        };
                    }
                case "type":
                    {
                        return _chatManager.PostCommandPrivatPostActivChat(0, myLogin, chat, $"User {anotherPlayer.Public.Login} have:" + anotherPlayer.Public.Grants.ToString());
                    }
                default:
                    {
                        return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, $"cmd '/grants {argsM[0]}' not found");
                    }
            }
        }

        private static Grants GetGrantsByStr(string value)
        {
            if (int.TryParse(value, out int res) && Enum.IsDefined(typeof(Grants), res))
            {
                return (Grants)res;
            }

            if (Enum.TryParse<Grants>(value, out Grants newGrants))
            {
                return newGrants;
            }

            return Grants.NoPermissions;
        }
    }
}