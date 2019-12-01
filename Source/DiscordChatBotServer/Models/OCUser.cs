using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OC.DiscordBotServer.Models
{
    /// <summary>
    /// link between Discrord User on some channel Discord and OCUser
    /// </summary>
    // https://qarchive.ru/371018_privjazka__unsigned_long___uint64__v_zajavlenii_sqlite3__
    // sql lite не поддерживает unsigned  long
    public class OCUser
    {
        // Primary Key;   IdChanel, IdUser
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong DiscordIdChanel { get; set; }
        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong UserId { get; set; }
        public string OCLogin { get; set; }
        public DateTime LastActiveTime { get; set; }
        //public string Token { get; set; }
    }
}