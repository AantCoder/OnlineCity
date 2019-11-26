using System;
using System.ComponentModel.DataAnnotations;

namespace OC.DiscordBotServer.Repositories.SQLLiteRepository.ModelMapping
{
    /// <summary>
    /// Discord Chanel to Server
    /// </summary>
    public class Chanel2Server
    {
        //public Chanel2Server(OC.DiscordBotServer.Models.Chanel2Server chanel2Server)
        //{
        //    Id = (long)chanel2Server.Id;
        //    IP = chanel2Server.IP;
        //    Port = chanel2Server.Port;
        //    LastOnlineTime = chanel2Server.LastOnlineTime;
        //    LinkCreator = (long)chanel2Server.LinkCreator;
        //}

        //public Chanel2Server()
        //{
        //}

        [Key]
        public int Id { get; set; }
        //[Required]
        //public long IP { get; set; }
        //[Required]
        //public int Port { get; set; }
        //public DateTime LastOnlineTime { get; set; }
        ///// <summary>
        ///// Id Discrord user
        ///// </summary>
        //public long LinkCreator { get; set; }
    }
}
