using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Net;

namespace DiscordChatBotServer.Commands
{
    public class RegCommand : ICommand
    {
        public bool CanExecute(SocketCommandContext context, out string message)
        {
            // RU: Проверка на дурака: такой сервер не зарегистрирован, и сервер живой
            // RU: Регистрируем сервер и сохраняем параметры: в виде таблицы: IDканала, IPserver
            var s = context.Message.Content.Trim();
            var serverAdr = tryParseStringToIp(s);

            if (serverAdr == null)
            {
                message = "Invalid server IP adress or port";
                return false;
            }




            return true;
        }

        public string Execute(SocketCommandContext parameter)
        {

            _regCmd.Execute(Context);


            var ipServer = Context.Message;
            if (!true)
            {
                await ReplyAsync("Hello world");
            }

            int idChanel = -1;
            string ip = string.Empty;

            if (Helper.RegServerOnline(idChanel, ip) && Helper.RegServerOffline(idChanel, ip))
            {
                await ReplyAsync($"Сongratulation! Server ({ipServer}) related to the channel {this.Context.Channel.Name}");
            }
            else
            {
                await ReplyAsync($"Server not related");
            }
        }

        private IPEndPoint tryParseStringToIp(string value)
        {
            int port;
            var lastIndex = value.LastIndexOf(":");
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
