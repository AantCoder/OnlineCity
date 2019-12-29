using Newtonsoft.Json;
using OCUnion;
using ServerCore.Model;
using ServerOnlineCity.Model;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using Transfer;
using Util;

namespace ServerOnlineCity
{
    public class ServerManager
    {
        public int MaxActiveClientCount = 10000; //todo провверить корректность дисконнекта
        public static ServerSettings ServerSettings = new ServerSettings();
        private ConnectServer Connect = null;
        private int _ActiveClientCount;

        public ServerManager()
        {
            AppDomain.CurrentDomain.AssemblyResolve += Missing_AssemblyResolver;
        }

        private Assembly Missing_AssemblyResolver(object sender, ResolveEventArgs args)
        {
            // var asm = args.Name.Split(",")[0];
            var asm = args.RequestingAssembly.FullName.Split(",")[0];
            var a = Assembly.Load(asm);
            return a;
        }

        public int ActiveClientCount
        {
            get { return _ActiveClientCount; }
            private set { _ActiveClientCount = value; }
        }

        public void Start(string path, int port = SessionClient.DefaultPort)
        {
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Settings.json")))
            {
                ServerSettings.Port = port;

                using (StreamWriter file = File.CreateText(Path.Combine(path, "Settings.json")))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Formatting = Formatting.Indented;
                    serializer.Serialize(file, ServerSettings);
                }
            }
            else
            {
                ServerSettings = JsonConvert.DeserializeObject<ServerSettings>(File.ReadAllText(Path.Combine(path, "Settings.json")));
            }

            Loger.PathLog = path;
            Loger.IsServer = true;

            Loger.Log($"Server starting on port: {ServerSettings.Port}");

            var rep = Repository.Get;
            rep.SaveFileName = Path.Combine(path, "World.dat");
            rep.Load();
            CheckDiscrordUser();

            //общее обслуживание
            rep.Timer.Add(1000, DoWorld);

            //сохранение, если были изменения
            rep.Timer.Add(ServerSettings.SaveInterval, () =>
            {
                rep.Save(true);
            });

            ActiveClientCount = 0;

            Connect = new ConnectServer();
            Connect.ConnectionAccepted = ConnectionAccepted;
            Connect.Start(null, ServerSettings.Port);
        }

        /// <summary>
        /// check and create if it is necessary DiscrordUser
        /// </summary>
        private void CheckDiscrordUser()
        {
            var isDiscordBotUser = Repository.GetData.PlayersAll.Any(p => "discord" == p.Public.Login);

            if (isDiscordBotUser)
            {
                return;
            }

            var guid = Guid.NewGuid();
            var player = new PlayerServer("discord")
            {
                Pass = new CryptoProvider().GetHash(guid.ToString()),
                DiscordToken = guid,
                IsAdmin = true, // возможно по умолчанию запретить ?                
            };

            player.Public.Grants = Grants.GameMaster | Grants.SuperAdmin | Grants.UsualUser | Grants.Moderator;
            player.Public.Id = Repository.GetData.GenerateMaxPlayerId(); // 0 - system, 1 - discord

            Repository.GetData.PlayersAll.Add(player);
            Repository.Get.Save();
        }

        /// <summary>
        /// Общее обслуживание мира
        /// </summary>
        private void DoWorld()
        {
            ///Обновляем кто кого видит
            foreach (var player in Repository.GetData.PlayersAll)
            {
                var pl = player.PublicChat.PartyLogin;
                if (pl.Count == Repository.GetData.PlayersAll.Count) continue;

                if (player.IsAdmin
                    || true //todo переделать это на настройки сервера "в чате доступны все, без учета зон контакта"
                    )
                {
                    lock (player)
                    {
                        ///админы видят всех: добавляем кого не хватает
                        var plAdd = Repository.GetData.PlayersAll
                            .Select(p => p.Public.Login)
                            .Where(p => !pl.Any(pp => pp == p))
                            .ToList();
                        if (plAdd.Count > 0) pl.AddRange(plAdd);
                    }
                }
                else
                {
                    ///определяем кого видят остальные 
                    //админов
                    var plNeed = Repository.GetData.PlayersAll
                        .Where(p => p.IsAdmin)
                        .Select(p => p.Public.Login)
                        .ToList();

                    //те, кто запустил спутники
                    //todo когда сделаем, то потом, может быть, стоит это убрать для тех кто не построил ещё хотя бы консоль связи

                    //и те кто географически рядом
                    //todo

                    //себя и system
                    if (!plNeed.Any(p => p == player.Public.Login)) plNeed.Add(player.Public.Login);
                    if (!plNeed.Any(p => p == "system")) plNeed.Add("system");

                    ///синхронизируем
                    lock (player)
                    {
                        pl.RemoveAll((pp) => !plNeed.Any(p => p == pp));
                        pl.AddRange(plNeed.Where(p => !pl.Any(pp => p == pp)));
                    }
                }
            }
        }

        public void Stop()
        {
            Connect.Stop();
        }

        private void ConnectionAccepted(ConnectClient client)
        {
            if (ActiveClientCount > MaxActiveClientCount)
            {
                client.Dispose();
                return;
            }

            Interlocked.Increment(ref _ActiveClientCount);
            ActiveClientCount++;
            var thread = new Thread(() => DoClient(client));
            thread.IsBackground = true;
            thread.Start();
        }

        private void DoClient(ConnectClient client)
        {
            try
            {
                Loger.Log("New connect");

                using (var ss = new SessionServer())
                {
                    ss.Do(client);
                }
            }
            catch (Exception e)
            {
                var errorText = ExceptionUtil.ExceptionLog(e, "Server Exception");
                if (!(e is SocketException) && !(e.InnerException is SocketException))
                {
                    Loger.Log(errorText);
                }
            }
            //if (LogMessage != null) LogMessage("End connect");

            Interlocked.Decrement(ref _ActiveClientCount);
        }
    }
}
