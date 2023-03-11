using Model;
using OCUnion;
using OCUnion.Transfer.Model;
using OCUnion.Transfer.Types;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Transfer;

namespace ServerOnlineCity.ChatService
{
    internal sealed class AvatarCmd : IChatCmd
    {
        public string CmdID => "avatar";

        public Grants GrantsForRun => Grants.SuperAdmin | Grants.Moderator | Grants.DiscordBot;

        public string Help => ChatManager.prefix + "avatar {UserLogin}: Delete user avatar";

        private readonly ChatManager _chatManager;

        public AvatarCmd(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM, ServiceContext context)
        {
            var ownLogin = player.Public.Login;

            if (argsM.Count < 1)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.PlayerNameEmpty, ownLogin, chat, "Player name is empty");
            }

            var iconPlayer = Repository.GetPlayerByLogin(argsM[0]);
            if (iconPlayer == null)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.UserNotFound, ownLogin, chat, "User " + argsM[0] + " not found");
            }

            var info = new ModelFileSharing()
            {
                Category = FileSharingCategory.PlayerIcon,
                Name = iconPlayer.Public.Login
            };

            var imageNull = new Bitmap(10, 10);
            using (var data = new MemoryStream())
            {
                imageNull.Save(data, ImageFormat.Png);
                info.Data = data.ToArray();
            }

            if (!Repository.GetFileSharing.SaveFileSharing(context.Player, info))
            {
                var msg = "Server avatar " + iconPlayer.Public.Login + " Error";
                Loger.Log(msg);
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.AccessDeny, ownLogin, chat, msg);
            }
            else
            {
                var msg = "Server avatar " + iconPlayer.Public.Login + " Null ok";
                Loger.Log(msg);
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.Ok, ownLogin, chat, msg);
            }
        }
    }
}