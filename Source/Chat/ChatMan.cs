using Model;
using System;
using System.Linq;

namespace OC.Chat
{
    public class ChatMan
    {
        public string Login(string addr, string login, string pass)
        {
            if (string.IsNullOrEmpty(addr))
            {
                addr = "194.87.95.90";
            }

            var msgError = SCC.Login(addr, login, pass);
            return msgError;
        }

        public string GetChat(int index)
        {
            SCC.ChatProv.UpdateChats();
            string ChatText;
            if (index >= 0 && SCC.ChatProv.Data.Chats.Count > index)
            {
                Func<ChatPost, string> getPost = (cp) => "[" + cp.OwnerLogin + "]: " + cp.Message;

                var selectCannal = SCC.ChatProv.Data.Chats[index];
                ChatText = selectCannal.Posts
                    .Aggregate("", (r, i) => (r == "" ? "" : r + Environment.NewLine) + getPost(i));
            }
            else
                ChatText = "";
            return ChatText;
        }

        public void Send(int index, string text)
        {
            var selectCannal = SCC.ChatProv.Data.Chats[index];
            var res = SCC.ChatProv.SendMessage(text, selectCannal.Id);
            if (res != null && res.Status > 0)
            {

            }

            SCC.ChatProv.UpdateChats();
        }
    }
}