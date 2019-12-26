using Transfer;

namespace ServerCore.Model
{
    public class ServerSettings
    {
        public string ServerName = "Another OnlineCity Server";
        public int SaveInterval = 10000;
        public int Port = SessionClient.DefaultPort;
    }
}