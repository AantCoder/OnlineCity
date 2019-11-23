using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OC.DiscordBotServer.Models
{
    /// <summary>
    /// link between Discrord User on some channel Discord and OCUser
    /// </summary>
    public class OCUser
    {
        public ulong Id { get; set; }
        public ulong DiscordIdChanel { get; set; }
        public string OCLogin { get; set; }
        public DateTime LastActiveTime { get; set; }
    }
}