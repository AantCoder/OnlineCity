using Model;
using OCUnion;
using System;
using System.Net;
using Transfer;
using Util;

namespace OC.ChatBridgeProvider
{
    /// <summary>
    /// Bridge class Chat between FW 3.5 and 4.6.1 
    /// </summary>
    public class ChatProvider
    {
        public ClientData Data { get; set; }
        public Player My { get; set; }
        public TimeSpan ServerTimeDelta { get; set; }

        public event EventHandler<StringWrapperEventArgument> DisconnectedEvent;

        private readonly Transfer.SessionClient _sessionClient;

        public ChatProvider()
        {
            Loger.Log("Chat Init");
            _sessionClient = new SessionClient();
        }

        public string Login(IPEndPoint addr, string login, string password)
        {
            var msgError = Connect(addr);
            if (msgError != null) return msgError;

            var logMsg = "Login: " + login;
            Loger.Log("Chat " + logMsg);
            My = null;
            var pass = new CryptoProvider().GetHash(password);

            if (!_sessionClient.Login(login, pass))
            {
                logMsg = "Login fail: " + _sessionClient.ErrorMessage;
                Loger.Log("Chat " + logMsg);

                return _sessionClient.ErrorMessage;
            }

            InitConnected();

            return null;
        }

        private string Connect(IPEndPoint addr)
        {
            var stringAdress = addr.Address.ToString();
            var logMsg = "Connecting to server. Addr: " + stringAdress + ":" + addr.Port.ToString();
            Loger.Log("Chat " + logMsg);

            if (!_sessionClient.Connect(stringAdress, addr.Port))
            {
                logMsg = "Connection fail: " + _sessionClient.ErrorMessage;
                //DiscordBotServer.Helpers.Loger.Log("Chat " + logMsg);
                return _sessionClient.ErrorMessage;
            }

            Loger.Log("Chat " + logMsg);

            return null;
        }

        public void Disconnected(string msg = "Error Connection.")
        {
            var login = _sessionClient.GetInfo(false).My.Login;
            //DiscordBotServer.Helpers.Loger.Log("Chat Disconected :( " + login);
            _sessionClient.Disconnect();
            DisconnectedEvent?.Invoke(this, new StringWrapperEventArgument() { Message = login });
        }

        public void UpdateChats()
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

                return;
            }

            Data.LastServerConnectFail = false;
            Data.LastServerConnect = DateTime.UtcNow;
            var lastMessage = string.Empty;
            Data.ApplyChats(dc, ref lastMessage);
        }

        public void SendMessage(string message, long idChanell = 0)
        {
            _sessionClient.PostingChat(idChanell, message);
        }

        /// <summary>
        /// После успешной регистрации или входа
        /// </summary>
        private void InitConnected()
        {
            // OC.DiscordBotServer.Helpers.Loger.Log("Chat Connection OK");
            var serverInfo = _sessionClient.GetInfo(true);
            My = serverInfo.My;
            Data = new ClientData(My.Login, _sessionClient);
            ServerTimeDelta = serverInfo.ServerTime - DateTime.UtcNow;

            // OC.DiscordBotServer.Helpers.Loger.Log("Chat IsAdmin=" + serverInfo.IsAdmin + " Seed=" + serverInfo.Seed);
        }
    }

    /// <summary>
    /// it's litlle smell of code, because FW 3.5 does not support string as argument for Event Delegate
    /// </summary>
    public class StringWrapperEventArgument : EventArgs
    {
        public string Message { get; internal set; }
    }
}