using System.Net;

/// <summary>
/// for using SQL Lite DB install https://marketplace.visualstudio.com/items?itemName=ErikEJ.SQLServerCompactSQLiteToolbox
/// </summary>
namespace OC.DiscordBotServer.Helpers
{
    public static class Helper
    {
        public const string PREFIX = "!OC"; // Online City 

        public static IPEndPoint TryParseStringToIp(string value)
        {
            int port;
            var lastIndex = value.IndexOf(":");
            if (lastIndex > 0)
            {
                var strPort = value.Substring(lastIndex);
                if (!int.TryParse(strPort, out port) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
                {
                    return null;
                }
            }
            else
            {
                port = Transfer.SessionClient.DefaultPort;
            }

            lastIndex = lastIndex > 0 ? lastIndex : value.Length;
            var ipString = value.Substring(0, lastIndex);

            if (!IPAddress.TryParse(ipString, out IPAddress ip))
            {
                return null;
            }

            return new IPEndPoint(ip, port);
        }
    }
}