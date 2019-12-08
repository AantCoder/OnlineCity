using OC.DiscordBotServer.Common;
using OC.DiscordBotServer.Models;
using OC.DiscordBotServer.Repositories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using Transfer;

namespace OC.DiscordBotServer
{
    public class ApplicationContext
    {
        private readonly IRepository<OCUser> _repositoryOCUser;
        private readonly IRepository<Chanel2Server> _repositoryChanel2Server;

        /// <summary>
        /// Id DiscordChannel => Chanel2Server
        /// </summary>
        public ConcurrentDictionary<ulong, SessionClientWrapper> DiscrordToOCServer { get; set; } = new ConcurrentDictionary<ulong, SessionClientWrapper>();
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
                RegisterNewServer(server, new SessionClientWrapper(server));
            }

            foreach (var user in userRepository.GetAll())
            {
                UserOnServers[user.DiscordIdChanel][user.UserId] = user;
            }
        }

        public OCUser GetOCUser(ulong idChannel, ulong idUser) 
        {
            if (!UserOnServers.TryGetValue (idChannel , out ConcurrentDictionary<ulong, OCUser> OCUsers ))
            {
                return null;
            }

            if (!OCUsers.TryGetValue(idUser, out  OCUser OCUser))
            {
                return null;
            }

            return OCUser;
        }

        internal bool RegisterNewServer(Chanel2Server server, SessionClientWrapper sessionClient)
        {
            var apadr = IPAddress.Parse(server.IP);
            var endPoint = new IPEndPoint(apadr, server.Port);
            var result =
            OCServerToDiscrord.TryAdd(endPoint, server.Id) &&
            UserOnServers.TryAdd(server.Id, new ConcurrentDictionary<ulong, OCUser>());

            result &= DiscrordToOCServer.TryAdd(server.Id, sessionClient);

            // if sessionClient.IsLogined  then must be registred in DataBase
            if (sessionClient.IsLogined)
            {
                result &= _repositoryChanel2Server.AddNewItem(server);
            }
            else
            {
                result &= sessionClient.ConnectAndLogin();
            }

            return result;
        }
    }
}