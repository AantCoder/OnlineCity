using System;
using System.Net;
using OC.DiscordBotServer.Models;
using Discord.Commands;
using Transfer;
using System.Net.Sockets;
using OCUnion;

namespace OC.DiscordBotServer.Commands
{
    public class RegCommand : ICommand
    {
        private readonly ApplicationContext _appContext;
        private readonly DataContext _botContext;

        public RegCommand(ApplicationContext appContext, DataContext botContext)
        {
            _appContext = appContext;
            _botContext = botContext;
        }

        protected string CanExecute(SocketCommandContext context, string ip, string token, out IPEndPoint serverAdr)
        {
            // RU: Проверка на дурака: такой сервер не зарегистрирован, и сервер живой
            // RU: Регистрируем сервер и сохраняем параметры: в виде таблицы: IDканала, IPserver
            serverAdr = tryParseStringToIp(ip);
            if (serverAdr == null)
            {
                return Languages.Translator.ErrInvalidIP;
            }

            if (_appContext.OCServerToDiscrord.TryGetValue(serverAdr, out Chanel2Server server2Chanel))
            {
                return string.Format(Languages.Translator.ErrTryAddToExistChannel, server2Chanel.Id, serverAdr.ToString());
            }

            //_idChannel = context.Channel.Id;
            //if (_appContext.DiscrordToOCServer.TryGetValue(_idChannel, out Chanel2Server chanel2Server))
            //{
            //    serverAdr = new IPEndPoint(chanel2Server.IP, chanel2Server.Port)
            //      .Address.ToString();
            //    return string.Format(Languages.Translator.ErrTryAddToExistChannel, context.Channel.Name, serverAdr);
            //}

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
                    return Languages.Translator.ErrInvalidDiscordToken;
                }

                // 		Guid.NewGuid().ToString()	"eed1dbee-f0c9-476e-b8c4-e848bdafa4c0"	string
                //connectClient = ServerPing(_serverAdr);

                //if (connectClient == null)
                //{
                //    return Languages.Translator.ErrServerNotAvailable;
                //}
                //client.Login(ip,)

                var channelToServer = new Chanel2Server()
                {
                    Id = context.Channel.Id,
                    IP = BitConverter.ToUInt32(serverAdr.Address.GetAddressBytes(), 0),
                    Port = serverAdr.Port
                };

                lock (_appContext)
                {
                    _appContext.DiscrordToOCServer[context.Channel.Id] = channelToServer;
                    _appContext.OCServerToDiscrord[serverAdr] = channelToServer;
                }

                context.Message.DeleteAsync();

                _botContext.Chanel2Servers.Add(channelToServer);
                return string.Format(Languages.Translator.InfServerReg, serverAdr.ToString(), context.Channel.Name);
            }
            catch (Exception ex)
            {
                Loger.Log(ex.ToString());
                return "Internal error";
            }
        }

        private IPEndPoint tryParseStringToIp(string value)
        {
            int port;
            var lastIndex = value.IndexOf(":");
            if (lastIndex > 0)
            {
                var strPort = value.Substring(lastIndex);
                if (!int.TryParse(strPort, out port) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
                {
                    return null;
                }
            }
            else
            {
                port = Transfer.SessionClient.DefaultPort;
            }

            lastIndex = lastIndex > 0 ? lastIndex : value.Length;
            var ipString = value.Substring(0, lastIndex);

            if (!IPAddress.TryParse(ipString, out IPAddress ip))
            {
                return null;
            }

            return new IPEndPoint(ip, port);
        }
    }
}
