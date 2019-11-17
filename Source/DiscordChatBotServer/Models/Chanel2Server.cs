using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordChatBotServer.Models
{
    /// <summary>
    /// Discord Chanel to Server
    /// </summary>
    public class Chanel2Server
    {
        public int Chanel { get; set; }
        public long IP { get; set; }
        public int Port { get; set; }
        public DateTime LastActiveTime { get; set; }
    }
}
