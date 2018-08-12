using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
    [Serializable]
    public class ChatPost
    {
        public string OwnerLogin { get; set; }

        public DateTime Time { get; set; }

        public string Message { get; set; }

        /// <summary>
        /// Показывать только данному игроку, если не заданно показывать всем.
        /// Например ответ на /help напишет здесь имя игрока, а в OwnerLogin слово system
        /// </summary>
        public string OnlyForPlayerLogin { get; set; }
    }
}
