using System;
using System.ComponentModel.DataAnnotations;

namespace OC.DiscordBotServer.Models
{
    /// <summary>
    /// link between Discrord User on some channel Discord and OCUser
    /// </summary>
    // https://qarchive.ru/371018_privjazka__unsigned_long___uint64__v_zajavlenii_sqlite3__s__
    // sql lite не поддерживает unsigned  long
    public class OCUser
    {
        [Key]
        public ulong Id { get; set; }
        public ulong DiscordIdChanel { get; set; }
        public string OCLogin { get; set; }
        public DateTime LastActiveTime { get; set; }
    }
}