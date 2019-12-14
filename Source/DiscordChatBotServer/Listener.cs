using Discord;
using Discord.WebSocket;
using Model;
using OCUnion;
using System;
using System.Collections.Generic;
using System.Text;

namespace OC.DiscordBotServer
{
    sealed public class Listener
    {
        private volatile bool IsUpdatingBotStatus = false;
        private volatile bool IsUpdatingChats = false;
        private readonly ApplicationContext _applicationContext;
        private readonly DiscordSocketClient _dc;
        private readonly BotDataContext _botDataContext;

        public Listener(ApplicationContext applicationContext, DiscordSocketClient dc, BotDataContext botDataContext)
        {
            _applicationContext = applicationContext;
            _dc = dc;
            _botDataContext = botDataContext;
        }

        private bool TranslateChatToDiscordAsync(ulong channelId, IReadOnlyList<ChatPost> messages)
        {
            var channel = _dc.GetChannel(channelId) as IMessageChannel;
            if (channel == null)
            {
                return false;
            }

            const int MAX_LENGTH_NESSAGE = 2000 - 100;
            var sb = new StringBuilder(MAX_LENGTH_NESSAGE);
            foreach (var chatPost in messages)
            {
                if (chatPost.Message.Length + sb.Length > MAX_LENGTH_NESSAGE)
                {
                    // Limit for max Message Length = 2000 
                    // Limit for max Message per Second =5 ( variable) 
                    // https://github.com/discordapp/discord-api-docs/blob/master/docs/topics/Rate_Limits.md

                    Console.WriteLine(sb.ToString());
                    var res = channel.SendMessageAsync(sb.ToString());
                    res.Wait();
                    sb = new StringBuilder(MAX_LENGTH_NESSAGE);
                }

                sb.AppendLine("[" + chatPost.OwnerLogin + chatPost.Time.ToString(" dd.MM HH:mm") + "]: " + chatPost.Message);
            }

            if (sb.Length > 0)
            {
                var t2 = channel.SendMessageAsync(sb.ToString());
                t2.Wait();
            }

            return true;
        }

        public void UpdateChats()
        {
            if (IsUpdatingChats || !(_dc.ConnectionState == ConnectionState.Connected))
            {
                return;
            }

            IsUpdatingChats = true;
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
                    if (chats != null && TranslateChatToDiscordAsync(bindServer.Key, chats))
                    {
                        _botDataContext.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                Loger.Log(ex.ToString());
            }

            IsUpdatingChats = false;
        }

        /// <summary>
        /// Check each server for online & Update Bot Status 
        /// </summary>
        public void UpdateBotStatus() 
        {
            IsUpdatingBotStatus = true; // Этого по идее не нужно, т.к. запуск раз в 10 минут и за это время цикл должен успеть отработь
            if (IsUpdatingBotStatus || !(_dc.ConnectionState == ConnectionState.Connected))
            {
                return;
            }

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

                }
            }
            catch (Exception ex)
            {
                Loger.Log(ex.ToString());
            }

            IsUpdatingBotStatus = false;
        }
    }
}
