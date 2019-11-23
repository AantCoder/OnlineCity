using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OC.DiscordBotServer.Models
{
    /// <summary>
    /// Discord Chanel to Server
    /// </summary>
    public class Chanel2Server 
    {
        public ulong Id { get; set; }
        public long IP { get; set; }
        public int Port { get; set; }
        public DateTime LastOnlineTime { get; set; }
        /// <summary>
        /// Id Discrord user
        /// </summary>
        public ulong LinkCreator { get; set; }
    }
}
