using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
    [Serializable]
    public class Chat
    {
        public long Id;
    
        public string OwnerLogin;

        public string Name;

        /// <summary>
        /// Создан человеком (можно добавлять людей), 
        /// иначе автоматический из всех кто доступен владельцу (это его общий чат)
        /// </summary>
        public bool OwnerMaker;
        
        public List<string> PartyLogin;

        public List<ChatPost> Posts = new List<ChatPost>();

        public Chat()
        {
        }
    }
}
