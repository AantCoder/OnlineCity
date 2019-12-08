using System;

namespace Model
{
    [Serializable]
    public class Player
    {
        public string Login { get; set; }

        public string ServerName { get; set; }

        //public bool ExistMap { get; set; }

        public DateTime LastSaveTime { get; set; }

        public DateTime LastOnlineTime { get; set; }

        public long LastTick { get; set; }

        /// <summary>
        /// Grants for user for Example: 1 -admin, 2  - Moderator ( Can Kick users) , 4 - GameMaster (Can Create Events), 8 - Can Rename settlements e t.c 
        /// </summary>
        public uint Grants { get; set; }
    }
}
