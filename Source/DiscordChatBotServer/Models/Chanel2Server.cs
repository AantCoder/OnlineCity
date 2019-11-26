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
        public ulong Id { get; set; }
        [Required]
        public long IP { get; set; }
        [Required]
        public int Port { get; set; }
        public DateTime LastOnlineTime { get; set; }
        /// <summary>
        /// Id Discrord user
        /// </summary>
        public ulong LinkCreator { get; set; }
        public string PasswordHash { get; set; }
        public string Password { get; set; }
    }
}