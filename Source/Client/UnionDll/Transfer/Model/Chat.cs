using System;
using System.Collections.Generic;

namespace Model
{
    [Serializable]
    public class Chat
    {
        // 0 - приватный системый чат, сообщения не сохраняется и создается каждый раз новый для каждого пользователя
        // 1 - публичный чат
        public int Id;

        public string OwnerLogin;

        public string Name;

        /// <summary>
        /// Создан человеком (можно добавлять людей), 
        /// иначе автоматический из всех кто доступен владельцу (это его общий чат)
        /// </summary>
        public bool OwnerMaker;

        public List<string> PartyLogin;

        public List<ChatPost> Posts = new List<ChatPost>();

        public DateTime LastChanged;        

        public Chat()
        {
        }
    }
}
