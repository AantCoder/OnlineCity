using Model;
using OCUnion;
using RimWorldOnlineCity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Transfer;
using Util;

namespace Chat
{
    public class SCC
    {
        public static ClientData Data { get; set; }
        //public static WorkTimer Timers { get; set; }
        public static Player My { get; set; }
        public static TimeSpan ServerTimeDelta { get; set; }
        
        public static void Init()
        {
            //MainHelper.DebugMode = true;
            //if (MainHelper.DebugMode)
            try
            {
                var workPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                    , @"..\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\OnlineCity");
                Directory.CreateDirectory(workPath);
                Loger.PathLog = workPath;
            }
            catch { }

            Loger.Log("Chat Init");
            ClientData.UIInteraction = false;
        }


        public static string Login(string addr, string login, string password)
        {
            var msgError = Connect(addr);
            if (msgError != null) return msgError;

            var logMsg = "Login: " + login;
            Loger.Log("Chat " + logMsg);
            My = null;
            var pass = new CryptoProvider().GetHash(password);

            var connect = SessionClient.Get;
            if (!connect.Login(login, pass))
            {
                logMsg = "Login fail: " + connect.ErrorMessage;
                Loger.Log("Chat " + logMsg);
                MessageBox.Show(connect.ErrorMessage, "Login fail");
                //Close();
                return connect.ErrorMessage;
            }
            else
            {
                logMsg = "Login OK";
                Loger.Log("Chat " + logMsg);
                InitConnected();
            }
            return null;
        }
        
        public static string Connect(string addr)
        {
            //TimersStop();
            //todo Предварительная проверка корректности
            int port = 0;
            if (addr.Contains(":")
                && int.TryParse(addr.Substring(addr.LastIndexOf(":") + 1), out port))
            {
                addr = addr.Substring(0, addr.LastIndexOf(":"));
            }

            var logMsg = "Connecting to server. Addr: " + addr + ". Port: " + (port == 0 ? SessionClient.DefaultPort : port).ToString();
            Loger.Log("Chat " + logMsg);
            var connect = SessionClient.Get;
            if (!connect.Connect(addr, port))
            {
                logMsg = "Connection fail: " + connect.ErrorMessage;
                Loger.Log("Chat " + logMsg);
                MessageBox.Show(connect.ErrorMessage, "Connection fail");
                //Close();
                return connect.ErrorMessage;
            }
            else
            {
                logMsg = "Connection OK";
                Loger.Log("Chat " + logMsg);
            }
            return null;
        }

        public static void Command(Action<SessionClient> netAct)
        {
            var connect = SessionClient.Get;
            netAct(connect);
        }
        public static void Disconnected(string msg = "Ошибка соединения.")
        {
            Loger.Log("Chat Disconected :( ");
            GameExit.BeforeExit = null;
            //TimersStop();
            SessionClient.Get.Disconnect();
            MessageBox.Show(msg, "Соединение прервано");
        }

        public static void UpdateChats()
        {
            //Loger.Log("Chat UpdateChating...");
            Command((connect) =>
            {
                //Loger.Log("Chat T4");
                ModelUpdateChat dc;
                //Data.ChatsTime = DateTime.Now.Date;
                //Loger.Log("Chat UpdateChats f0");
                dc = connect.UpdateChat(Data.ChatsTime);
                if (dc != null)
                {
                    //Loger.Log("Chat UpdateChats f1");
                    Data.LastServerConnectFail = false;
                    Data.LastServerConnect = DateTime.Now;
                    /*
                    Loger.Log("Chat UpdateChats: " + dc.Chats.Count.ToString() + " - "
                        + (Data.ChatsTime.Ticks / 1000000000m).ToString() + " - "
                        + (dc.Time.Ticks / 1000000000m).ToString() //dc.Time.ToString(Loger.Culture)
                        + "   " + (dc.Chats.Count == 0 ? "" : dc.Chats[0].Posts.Count.ToString()));
                    */
                    Data.ApplyChats(dc);
                }
                else
                {
                    //Loger.Log("Chat UpdateChats f2");
                    Data.LastServerConnectFail = true;
                    if (!Data.ServerConnected) Disconnected();
                }
                //todo Сделать сброс крутяшки после обновления чата (см. Dialog_MainOnlineCity)
            });
        }

        /// <summary>
        /// После успешной регистрации или входа
        /// </summary>
        /// 
        public static void InitConnected()
        {
            Loger.Log("Chat InitConnected()");
            Data = new ClientData();
            //TimersStop();
            //Timers = new WorkTimer();
            
            var connect = SessionClient.Get;
            var serverInfo = connect.GetInfo(true);
            My = serverInfo.My;
            ServerTimeDelta = serverInfo.ServerTime - DateTime.UtcNow;

            Loger.Log("Chat IsAdmin=" + serverInfo.IsAdmin + " Seed=" + serverInfo.Seed + " ExistMap=" + My.ExistMap);

            SessionClientController.My = My;
            SessionClientController.Data = Data;
        }

    }
}
