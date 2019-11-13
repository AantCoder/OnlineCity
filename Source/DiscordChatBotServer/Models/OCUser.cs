using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordChatBotServer.Models
{
    /// <summary>
    /// link between Discrord User on some channel Discord and OCUser
    /// </summary>
    public class OCUser
    {
        public int DiscordIdUser { get; set; }
        public int DiscordIdChanel { get; set; }
        public string OCLogin { get; set; }
    }
}
