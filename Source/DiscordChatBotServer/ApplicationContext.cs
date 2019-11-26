using OC.DiscordBotServer.Models;
using OC.DiscordBotServer.Repositories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

namespace OC.DiscordBotServer
{
    public class ApplicationContext
    {
        private readonly IRepository<OCUser> _repositoryOCUser;
        private readonly IRepository<Chanel2Server> _repositoryChanel2Server;

        /// <summary>
        /// Id DiscordChannel => Chanel2Server
        /// </summary>
        public ConcurrentDictionary<ulong, Chanel2Server> DiscrordToOCServer { get; set; } = new ConcurrentDictionary<ulong, Chanel2Server>();
        /// <summary>
        /// Online City Server => Discord Server
        /// </summary>
        public ConcurrentDictionary<IPEndPoint, ulong> OCServerToDiscrord { get; set; } = new ConcurrentDictionary<IPEndPoint, ulong>();
        /// <summary>
        /// Each row contains Users per server 
        /// </summary>
        public ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, OCUser>> UserOnServers = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, OCUser>>();

        public ApplicationContext(IRepository<OCUser> userRepository, IRepository<Chanel2Server> serversRepository)
        {
            _repositoryOCUser = userRepository;
            _repositoryChanel2Server = serversRepository;

            foreach (var server in serversRepository.GetAll())
            {
                RegisterNewServer(server, false);
            }

            foreach (var user in userRepository.GetAll())
            {
                UserOnServers[user.DiscordIdChanel][user.UserId] = user;
            }
        }

        internal bool RegisterNewServer(Chanel2Server server, bool SaveInDataBase)
        {
            var endPoint = new IPEndPoint(server.IP, server.Port);
            var result =
            OCServerToDiscrord.TryAdd(endPoint, server.Id) &&
            DiscrordToOCServer.TryAdd(server.Id, server) &&

            UserOnServers.TryAdd(server.Id, new ConcurrentDictionary<ulong, OCUser>());

            if (!SaveInDataBase || !result)
            {
                return result;
            }

            return _repositoryChanel2Server.AddNewItem(server); ;
        }
    }
}