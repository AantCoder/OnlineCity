using OCUnion;
using System;
using System.IO;
using System.Net;

namespace OC.Chat
{
    public class SCC
    {
        public static ChatProvider ChatProv { get; set; } = new ChatProvider();

        public static void Init()
        {
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
            var StrIp = addr.Split(':');
            var port = Transfer.SessionClient.DefaultPort;
            if (StrIp.Length == 2)
            {
                if (!int.TryParse(StrIp[1], out port))
                {
                    return "Invalid IP adress or port";
                }
            }

            if (!IPAddress.TryParse(StrIp[0],out IPAddress ipAdres))
            {
                return "Invalid IP adress";
            }

            var endPoint = new IPEndPoint(ipAdres, port);

            return ChatProv.Login(endPoint, login, password);
        }
    }
}