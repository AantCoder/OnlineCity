using Model;
using OC.DiscordBotServer.Models;
using OCUnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transfer;
using Util;

namespace OC.DiscordBotServer.Common
{
    public class SessionClientWrapper : IDisposable
    {
        private readonly Transfer.SessionClient _sessionClient;
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

        private void updateClientData()
        {
            var serverInfo = _sessionClient.GetInfo(OCUnion.Transfer.ServerInfoType.Full);
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

            return result;
        }

        public void Disconnected(string msg = "Error Connection.")
        {
            _sessionClient.Disconnect();
            // to do : Notify that the server Disconnected 
        }

        public bool SendMessage(string message)
        {
            if (!_sessionClient.PostingChat(0, message))
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

        /// <summary>
        /// Correctly close connection in SessionClient.Client
        /// </summary>
        public void Dispose()
        {
            _sessionClient.Disconnect();
        }
    }
}
