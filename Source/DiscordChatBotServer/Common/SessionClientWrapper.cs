using Model;
using OC.DiscordBotServer.Languages;
using OC.DiscordBotServer.Models;
using OCUnion;
using OCUnion.Transfer;
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
                if (!_sessionClient.Login("Discord", pass))
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
            ModelUpdateChat dc;

            dc = _sessionClient.UpdateChat(Data.ChatsTime);
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
            Data.LastServerConnect = DateTime.UtcNow;
            var lastMessage = string.Empty;
            Data.ApplyChats(dc, ref lastMessage);
            var result = new List<ChatPost>(dc.Chats[0].Posts.Where(x => x.Time > Chanel2Server.LastOnlineTime));
            Chanel2Server.LastOnlineTime = Data.ChatsTime;
            Chanel2Server.LastCheckTime = Data.ChatsTime;

            return result;
        }

        public void Disconnected(string msg = "Error Connection.")
        {
            _sessionClient.Disconnect();
            // to do : Notify that the server Disconnected 
        }

        public bool SendMessage(string message)
        {
            if (!_sessionClient.PostingChat(1, message))
            {
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

        /// <summary>
        /// Correctly close connection in SessionClient.Client
        /// </summary>
        public void Dispose()
        {
            _sessionClient.Disconnect();
        }
    }
}