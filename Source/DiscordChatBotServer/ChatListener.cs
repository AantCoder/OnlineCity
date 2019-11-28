using Discord;
using Discord.WebSocket;
using Model;
using OCUnion;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OC.DiscordBotServer
{
    sealed public class ChatListener
    {
        private volatile bool IsWork = false;
        private readonly ApplicationContext _applicationContext;
        private readonly DiscordSocketClient _dc;
        private readonly BotDataContext _botDataContext;

        public ChatListener(ApplicationContext applicationContext, DiscordSocketClient dc, BotDataContext botDataContext)
        {
            _applicationContext = applicationContext;
            _dc = dc;
            _botDataContext = botDataContext;
        }

        private bool TranslateChatToDiscord(ulong channelId, IReadOnlyList<ChatPost> messages)
        {
            var channel = _dc.GetChannel(channelId) as IMessageChannel;
            if (channel == null)
            {
                return false;
            }
            string ChatText = string.Empty;

            if (messages.Count > 0)
            {
                var time = "";
#if DEBUG
                Func<ChatPost, string> getPost = (cp) => "[" + cp.OwnerLogin + cp.Time.ToString(" dd.MM HH:mm") + "]: " + cp.Message;
#else
                Func<ChatPost, string> getPost = (cp) => "[" + cp.OwnerLogin + "]: " + cp.Message;
#endif

                ChatText = messages
                    .Aggregate("", (r, i) => (r == "" ? "" : r + Environment.NewLine) + getPost(i));
            }

            if (string.IsNullOrEmpty(ChatText))
            {
                return true;
            }

            channel.SendMessageAsync(ChatText);
#if DEBUG
            Console.WriteLine(ChatText);
#endif
            return true;
        }

        public void UpdateChats()
        {
            if (IsWork || !(_dc.ConnectionState == ConnectionState.Connected))
            {
                return;
            }

            IsWork = true;
            try
            {
                foreach (var bindServer in _applicationContext.DiscrordToOCServer)
                {
                    var onlineCityServer = bindServer.Value;
                    if (!onlineCityServer.IsLogined)
                    {
                        // to do Attempt to Connect Every 10 minuts
                        continue;
                    }

                    var chats = onlineCityServer.GetChatMessages();
                    if (chats != null && TranslateChatToDiscord(bindServer.Key, chats))
                    {
                        _botDataContext.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                Loger.Log(ex.ToString());
            }

            IsWork = false;
        }
    }
}
