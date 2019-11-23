using OC.DiscordBotServer.Models;
using System.Collections.Concurrent;
using System.Net;

namespace OC.DiscordBotServer
{
    public class ApplicationContext
    {
        /// <summary>
        /// Id DiscordChannel => Chanel2Server
        /// </summary>
        public ConcurrentDictionary<ulong, Chanel2Server> DiscrordToOCServer { get; set; } = new ConcurrentDictionary<ulong, Chanel2Server> ();
        /// <summary>
        /// 
        /// </summary>
        public ConcurrentDictionary<IPEndPoint, Chanel2Server> OCServerToDiscrord { get; set; } = new ConcurrentDictionary<IPEndPoint, Chanel2Server> ();
    }
}