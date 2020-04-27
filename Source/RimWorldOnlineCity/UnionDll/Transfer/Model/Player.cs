using OCUnion;
using System;

namespace Model
{
    [Serializable]
    public class Player
    {
        public string Login { get; set; }
        
        /// <summary>
        /// int faster then string ;-)
        /// </summary>
        public int Id { get; set; }

        public string ServerName { get; set; }

        //public bool ExistMap { get; set; }

        public DateTime LastSaveTime { get; set; }

        public DateTime LastOnlineTime { get; set; }

        public DateTime LastPVPTime { get; set; }

        public long LastTick { get; set; }

        public bool EnablePVP { get; set; } = true; //todo После доработки интерфейса убрать

        public string DiscordUserName { get; set; }

        public string EMail { get; set; }

        public string AboutMyText { get; set; }

        public Grants Grants { get; set; }
    }
}
