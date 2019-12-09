using System.Collections.Generic;

namespace ServerCore.Model
{
    public class ServerSettings
    {
        public string ServerName = "Another OnlineCity Server";
        public int SaveInterval = 10000;
        public int Port = 8888;
        public bool IsModsWhitelisted;
        public List<string> ModsID = new List<string>();
    }
}
