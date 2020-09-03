using Model;
using OC.DiscordBotServer.Languages;
using OC.DiscordBotServer.Models;
using OCUnion;
using OCUnion.Transfer;
using OCUnion.Transfer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Transfer;
using Util;

namespace OC.DiscordBotServer.Common
{
    public class SessionClientWrapper : IDisposable
    {
        public const string DiscrodLogin = "discord";

        private readonly SessionClient _sessionClient;
        public Chanel2Server Chanel2Server { get; }
        private ClientData Data { get; set; }
        public Player My { get; set; }

        //public event EventHandler DisconnectedEvent;
        public SessionClientWrapper(Chanel2Server serverData)
        {
            Chanel2Server = serverData;
            _sessionClient = new SessionClient();
        }

        public SessionClientWrapper(Chanel2Server serverData, SessionClient sessionClient)
        {
            Chanel2Server = serverData;
            _sessionClient = sessionClient;

            updateClientData();
        }

        public bool IsLogined => _sessionClient.IsLogined;

        public bool ConnectAndLogin()
        {
            var t = _sessionClient.Connect(Chanel2Server.IP, Chanel2Server.Port);
            if (!t)
            {
                return false;
            }

            var pass = new CryptoProvider().GetHash(Chanel2Server.Token);

            lock (_sessionClient)
            {
                if (!_sessionClient.Login(DiscrodLogin, pass))
                {
                    return false;
                }

                updateClientData();
            }

            return true;
        }

        public void UpdateLastTimeTryToConnect()
        {
            Chanel2Server.LastCheckTime = DateTime.UtcNow;
        }

        private void updateClientData()
        {
            var serverInfo = _sessionClient.GetInfo(OCUnion.Transfer.ServerInfoType.FullWithDescription);
            My = serverInfo.My;
            Data = new ClientData(My.Login, _sessionClient);
            Data.ServetTimeDelta = serverInfo.ServerTime - DateTime.UtcNow;
        }

        public IReadOnlyList<ChatPost> GetChatMessages()
        {
            var dc = _sessionClient.UpdateChat
                (
                 new ModelUpdateTime()
                 {
                     Value = Chanel2Server.LastRecivedPostIndex,
                     Time = Chanel2Server.LastCheckTime,
                 }
                );

            if (dc == null)
            {
                Data.LastServerConnectFail = true;
                if (!Data.ServerConnected)
                {
                    Disconnected();
                }

                return null;
            }

            Data.LastServerConnectFail = false;
            Data.LastServerConnect = Chanel2Server.LastOnlineTime = Chanel2Server.LastCheckTime = DateTime.UtcNow;
            var result = dc.Chats
                .FirstOrDefault(x => x.Id == 1)?.Posts
                .Where(x => x.DiscordIdMessage == 0);

            return result.ToList().AsReadOnly();
        }

        public void Disconnected(string msg = "Error Connection.")
        {
            _sessionClient.Disconnect();
            // to do : Notify that the server Disconnected 
        }

        public bool SendMessage(string message, bool isPrivate)
        {
            if (IsLogined)
            {
                var res = _sessionClient.PostingChat(isPrivate ? 0 : 1, message);
                Loger.Log(_sessionClient.ErrorMessage);
                return false;
            }

            return true;
        }

        public Player GetPlayerByToken(Guid guidToken)
        {
            return _sessionClient.GetPlayerByToken(guidToken);
        }

        /*
        IP: 194.87.95.90
        Main Official server
        Location: Moscow
        Language: Multilingual
        Hosted by: @Aant
        */
        public string GetDescription(ServerInfoType infoType)
        {
            var sb = new StringBuilder();
            sb.AppendLine("****************");
            sb.Append("IP: ");
            sb.Append(Chanel2Server.IP);
            if (Chanel2Server.Port != SessionClient.DefaultPort)
            {
                sb.Append(":");
                sb.Append(Chanel2Server.Port);
            }

            sb.AppendLine("Hosted by: @Aant");
            if (!_sessionClient.IsLogined)
            {
                return Translator.ErrServerNotAvailable;
            }

            var serverInfo = _sessionClient.GetInfo(ServerInfoType.FullWithDescription);

            if (serverInfo == null)
            {
                return Translator.ErrServerNotAvailable;
            }

            sb.AppendLine(serverInfo.Description);


            if (infoType != ServerInfoType.FullWithDescription)
            {
                sb.AppendLine("Difficulty: " + serverInfo.Difficulty);
                sb.AppendLine("MapSize: " + serverInfo.MapSize);
                sb.AppendLine("PlanetCoverage: " + serverInfo.PlanetCoverage);
                sb.AppendLine("Seed:" + serverInfo.Seed);
                sb.AppendLine("VersionInfo" + serverInfo.VersionInfo);
                sb.AppendLine("VersionNum" + serverInfo.VersionNum);
            }

            sb.AppendLine("****************");
            sb.AppendLine();
            sb.AppendLine();

            return sb.ToString();
        }

        public ModelStatus PostingChat(string owner, string msg, ulong messageId, bool isPrivate = false)
        {
            var packet = new ModelPostingChat()
            {
                IdChat = isPrivate ? 0 : 1, // system chat: for private command in game
                Message = msg,
                Owner = owner,
                IdDiscordMsg = messageId,

            };

            return _sessionClient.TransObject2<ModelStatus>(packet, PackageType.Request19PostingChat, PackageType.Response20PostingChat);
        }

        /// <summary>
        /// Correctly close connection in SessionClient.Client
        /// </summary>
        public void Dispose()
        {
            _sessionClient.Disconnect();
        }
    }
}