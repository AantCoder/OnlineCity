using Model;
using System;

namespace Transfer
{
    [Serializable]
    public class ModelInfo
    {
        public Player My { get; set; }
        public bool IsAdmin { get; set; }
        public string VersionInfo { get; set; }
        public long VersionNum { get; set; }
        public string ServerName { get; set; }
        /// <summary>
        /// Будет ли выполняться проверка хеша файлов на клиенте
        /// </summary>
        public bool IsModsWhitelisted { get; set; }

        public bool NeedCreateWorld { get; set; }

        public string Seed { get; set; }
        public int MapSize { get; set; }
        public float PlanetCoverage { get; set; }
        public int Difficulty { get; set; }

        public DateTime ServerTime { get; set; }

        /// <summary>
        /// Описание сервера, которые читается с настроек, используется в дискорд боте
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Заполняется только при WorldLoad() GetInfo = 3
        /// </summary>
        public byte[] SaveFileData { get; set; }
    }
}
