using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordChatBotServer
{
    public class ApplicationContext
    {
        public ConcurrentDictionary<int, string> OCServer { get; set; } = new ConcurrentDictionary<int,string> ();
    }
}
