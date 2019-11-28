using System;
using System.Net;
using OC.DiscordBotServer.Models;
using Discord.Commands;
using Transfer;
using System.Net.Sockets;
using OCUnion;
using Util;
using OC.DiscordBotServer.Common;
using OC.DiscordBotServer.Helpers;

namespace OC.DiscordBotServer.Commands
{
    public class RegCommand : ICommand
    {
        private readonly ApplicationContext _appContext;

        public RegCommand(ApplicationContext appContext)
        {
            _appContext = appContext;
        }

        protected string CanExecute(SocketCommandContext context, string ip, string token, out IPEndPoint serverAdr)
        {
            // RU: Проверка на дурака: такой сервер не зарегистрирован, и сервер живой
            // RU: Регистрируем сервер и сохраняем параметры: в виде таблицы: IDканала, IPserver
            serverAdr = Helper.TryParseStringToIp(ip);
            if (serverAdr == null)
            {
                return Languages.Translator.ErrInvalidIP;
            }

            if (_appContext.DiscrordToOCServer.TryGetValue(context.Channel.Id, out SessionClientWrapper server2Chanel))
            {
                return string.Format(Languages.Translator.ErrTryAddToExistChannel, getChannelLinkByServerIP(serverAdr));
            }

            if (context.IsPrivate)
            {
                return Languages.Translator.InfResctrictInPrivate;
            }

            if (!Guid.TryParse(token, out Guid guidToken))
            {
                return Languages.Translator.ErrInvalidDiscordToken;
            }

            return string.Empty;
        }

        private object getChannelLinkByServerIP(IPEndPoint serverAdr)
        {
            return "{here must be link to Discrord Channel}";
        }

        public string Execute(SocketCommandContext context, string ip, string token)
        {
            try
            {
                IPEndPoint serverAdr;
                var message = CanExecute(context, ip, token, out serverAdr);
                if (!string.IsNullOrEmpty(message))
                {
                    return message;
                }

                try
                {
                    new TcpClient(serverAdr.Address.ToString(), serverAdr.Port);
                }
                catch
                (Exception ex)
                {
                    return Languages.Translator.ErrServerNotAvailable + "\n" + ex.Message;
                }

                var client = new SessionClient();
                if (!client.Connect(serverAdr.Address.ToString(), serverAdr.Port))
                {
                    return Languages.Translator.ErrServerNotAvailable;
                }

                var pass = new CryptoProvider().GetHash(token);
                if (!client.Login("Discord", pass))
                {
                    return Languages.Translator.ErrInvalidDiscordToken;
                }

                var channelToServer = new Chanel2Server()
                {
                    Id = context.Channel.Id,
                    IP = serverAdr.Address.ToString(),
                    Port = serverAdr.Port,
                    LinkCreator = context.Message.Author.Id,
                    LastOnlineTime = DateTime.UtcNow,
                    Token = token
                };

                _appContext.RegisterNewServer(channelToServer, new SessionClientWrapper(channelToServer, client));
                context.Message.DeleteAsync();

                return string.Format(Languages.Translator.InfServerReg, serverAdr.ToString(), context.Channel.Name);
            }
            catch (Exception ex)
            {
                Loger.Log(ex.ToString());
                return "Internal error";
            }
        }
    }
}
