using Discord.Commands;
using Discord.WebSocket;
using OC.DiscordBotServer.Common;
using OC.DiscordBotServer.Languages;
using OC.DiscordBotServer.Models;
using OC.DiscordBotServer.Repositories;
using OCUnion.Transfer;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OC.DiscordBotServer
{
    internal class DiscordManager
    {
        private readonly DiscordSocketClient _discordClient;
        private readonly ApplicationContext _app;
        IRepository<OCUser> _userRepository;
        IRepository<Chanel2Server> _serversRepository;

        public DiscordManager(DiscordSocketClient discordClient, ApplicationContext application, IRepository<OCUser> userRepository, IRepository<Chanel2Server> serversRepository)
        {
            this._discordClient = discordClient;
            _app = application;
            _userRepository = userRepository;
            _serversRepository = serversRepository;
        }

        public void ChannelDestroyedCommand(ulong channel)
        {
            if (!_app.UserOnServers.TryGetValue(channel, out ConcurrentDictionary<ulong, OCUser> users))
            {
                return;
            }

            if (!_app.DiscrordToOCServer.TryRemove(channel, out SessionClientWrapper clientWrapper))
            {
                return;
            }

            var chanel2Server = clientWrapper.Chanel2Server;
            var ipEndPoint = chanel2Server.GetIPEndPoint();

            if (clientWrapper.IsLogined)
            {
                clientWrapper.Disconnected();
                clientWrapper.Dispose();
            }

            if (!_app.OCServerToDiscrord.TryRemove(ipEndPoint, out ulong tmp))
            {
                return;
            }

            _serversRepository.Delete(new Chanel2Server[] { chanel2Server });
            _userRepository.Delete(users.Values);

            // to do :send private message to all [users] registred on this channel
        }

        internal string SrvInfoCommand(SocketCommandContext context, string param)
        {
            var sb = new StringBuilder();
            var author = context.Message.Author.Id;

            switch (param)
            {
                // "srvinfo all: информация по всем серверам, отсортированные 1.По количеству пользователей от максимального к минимальному, по дате поcледнего онлайна"
                case "all":
                    {
                        //foreach (var sessionClient in _app.DiscrordToOCServer.Values)
                        //{
                        //    //sessionClient.
                        //}

                        return "";
                    }
                //все где я зарегестрирован\n"
                case "my":
                    {
                        var servers = new List<ulong>();
                        foreach (var kv in _app.UserOnServers)
                        {
                            if (kv.Value.Values.Any(x => x.UserId == author))
                            {
                                servers.Add(kv.Key);
                            }
                        }

                        return getServersInfo(servers, ServerInfoType.Full);
                    }
            }

            var sessionClient = _app.TryGetSessionClientByIP(param);
            if (sessionClient == null)
            {
                return Translator.ErrInvalidIP;
            }

            return sessionClient.GetDescription(ServerInfoType.FullWithDescription);
        }

        private string getServersInfo(IEnumerable<ulong> servers, ServerInfoType infoType)
        {
            var sb = new StringBuilder();
            foreach (var y in servers)
            {
                if (!_app.DiscrordToOCServer.TryGetValue(y, out SessionClientWrapper sessionClient))
                {
                    continue;
                }

                sessionClient.GetDescription(infoType);
            }

            return "";
        }
    }
}