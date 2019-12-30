using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
    [Serializable]
    public class ChatPost
    {
        public int IdChat { get; set; }

        //[Obsolete ("В дальнейшем будет использоваться OwnerId")]
        public string OwnerLogin { get; set; }

        public int OwnerId { get; set; }

        public DateTime Time { get; set; }

        public string Message { get; set; }

        /// <summary>
        /// Показывать только данному игроку, если не заданно показывать всем.
        /// Например ответ на /help напишет здесь имя игрока, а в OwnerLogin слово system
        /// </summary>
        //[Obsolete ("Для системных сообщений будет использоваться чат с Id=0, для приватного сообщения пользователю надо будет создать канал")]
        public string OnlyForPlayerLogin { get; set; }

        /// <summary>
        /// Служебное поле. Если оно !=0 значит сообщение пришло из Discord и его не надо отправлять туда обратно :-)
        /// </summary>
        public ulong DiscordIdMessage { get; set; }
    }
}
