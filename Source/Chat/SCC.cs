using Model;
using OC.ChatBridgeProvider;
using OCUnion;
using System;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Transfer;
using Util;

namespace Chat
{
    public class SCC
    {
        public static ChatProvider ChatProv { get; set; } = new ChatProvider();

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
        }

        public static string Login(string addr, string login, string password)
        {
            var ipAdres = IPAddress.Parse("194.87.95.90");
            var endPoint = new IPEndPoint(ipAdres, Transfer.SessionClient.DefaultPort);

            return ChatProv.Login(endPoint, login, password);
        }
    }
}
