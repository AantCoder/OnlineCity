using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chat
{
    public class ChatMan
    {
        public string Login(string addr, string login, string pass)
        {
            var msgError = SCC.Login(addr, login, pass);
            return msgError;
        }

        public string GetChat(int index)
        {
            SCC.UpdateChats();
            string ChatText;
            if (index >= 0 && SCC.Data.Chats.Count > index)
            {
                Func<ChatPost, string> getPost = (cp) => "[" + cp.OwnerLogin + "]: " + cp.Message;

                var selectCannal = SCC.Data.Chats[index];
                ChatText = selectCannal.Posts
                    .Aggregate("", (r, i) => (r == "" ? "" : r + Environment.NewLine) + getPost(i));
            }
            else
                ChatText = "";
            return ChatText;
        }

        public void Send(int index, string text, Action after = null) 
        {
            var selectCannal = SCC.Data.Chats[index];

            SCC.Command((connect) =>
            {
                connect.PostingChat(selectCannal.Id, text);
                if (after != null) after();
            });
        }
    }
}
