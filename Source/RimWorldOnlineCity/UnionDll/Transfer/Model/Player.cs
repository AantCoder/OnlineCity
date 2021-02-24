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

        public bool EnablePVP { get; set; }

        public string DiscordUserName { get; set; }

        public string EMail { get; set; }

        public string AboutMyText { get; set; }

        public Grants Grants { get; set; }

    }

    [Serializable]
    public class PlayerGameProgress
    {
        public int ColonistsCount { get; set; }
        public int ColonistsDownCount { get; set; }
        public int ColonistsBleedCount { get; set; }
        /// <summary>
        /// Сколько пешек имеют 8 из 12 навыков 20 уровня. Предположительно всегда должно быть = 0. Если оно равно ColonistsCount, значит это чит
        /// </summary>
        public int PawnMaxSkill { get; set; }
        public int KillsHumanlikes { get; set; }
        public int KillsMechanoids { get; set; }
        public string KillsBestHumanlikesPawnName { get; set; }
        public string KillsBestMechanoidsPawnName { get; set; }
        public int KillsBestHumanlikes { get; set; }
        public int KillsBestMechanoids { get; set; }
    } 
}
