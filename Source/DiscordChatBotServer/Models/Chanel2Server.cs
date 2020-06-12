using OC.DiscordBotServer.Helpers;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace OC.DiscordBotServer.Models
{
    /// <summary>
    /// Discord Chanel to Server
    /// </summary>
    public class Chanel2Server
    {
        [Key]
        public ulong Id { get; set; }
        [Required]
        public string IP { get; set; }
        [Required]
        public int Port { get; set; }
        public DateTime LastOnlineTime { get; set; }
        public DateTime LastCheckTime { get; set; }
        /// <summary>
        /// Id Discrord user
        /// </summary>
        public ulong LinkCreator { get; set; }
        public string Token { get; set; }

        /// <summary>
        /// For request public chat from this index
        /// </summary>
        public int LastRecivedPostIndex { get; set; }

        public IPEndPoint GetIPEndPoint() => Helper.TryParseStringToIp($"{IP}:{Port}");
    }
}