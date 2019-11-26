using System;
using System.ComponentModel.DataAnnotations;

namespace OC.DiscordBotServer.Models
{
    /// <summary>
    /// Discord Chanel to Server
    /// </summary>
    public class Chanel2Server
    {
        [Key]
        public ulong Chanel2ServerId { get; set; }
        [Required]
        public long IP { get; set; }
        [Required]
        public int Port { get; set; }
        public DateTime LastOnlineTime { get; set; }
        /// <summary>
        /// Id Discrord user
        /// </summary>
        [DataType("UINT64")]
        public ulong LinkCreator { get; set; }
    }
}
