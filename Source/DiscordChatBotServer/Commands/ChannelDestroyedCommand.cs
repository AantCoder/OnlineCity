using Discord.WebSocket;
using OC.DiscordBotServer.Common;
using OC.DiscordBotServer.Models;
using OC.DiscordBotServer.Repositories;
using System.Collections.Concurrent;

namespace OC.DiscordBotServer
{
    internal class ChannelDestroyedCommand
    {
        private readonly DiscordSocketClient _discordClient;
        private readonly ApplicationContext _application;
        IRepository<OCUser> _userRepository;
        IRepository<Chanel2Server> _serversRepository;

        public ChannelDestroyedCommand(DiscordSocketClient discordClient,  ApplicationContext application, IRepository<OCUser> userRepository, IRepository<Chanel2Server> serversRepository)
        {
            this._discordClient = discordClient;
            _application = application;
            _userRepository = userRepository;
            _serversRepository = serversRepository;
        }

        public void Execute(ulong channel)
        {
            if (!_application.UserOnServers.TryGetValue(channel, out ConcurrentDictionary<ulong, OCUser> users))
            {
                return;
            }


            if (!_application.DiscrordToOCServer.TryRemove(channel, out SessionClientWrapper clientWrapper))
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


            if (!_application.OCServerToDiscrord.TryRemove(ipEndPoint, out ulong tmp))
            {
                return;
            }

            _serversRepository.Delete(new Chanel2Server[] { chanel2Server });
            _userRepository.Delete(users.Values);
           
            // to do :send private message to all [users] registred on this channel
        }
    }
}