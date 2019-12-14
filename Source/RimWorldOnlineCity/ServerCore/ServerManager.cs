using Newtonsoft.Json;
using ServerOnlineCity.Model;
using ServerCore.Model;
using OCUnion;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Transfer;
using System.Reflection;
using Util;
using ServerOnlineCity.Services;
using System.Collections.Generic;

namespace ServerOnlineCity
{
    public class ServerManager
    {
        public event Action<string> LogMessage;
        private ConnectServer Connect = null;
        private int _ActiveClientCount;
        private string _path;
        public static IReadOnlyDictionary<int, IGenerateResponseContainer> ServiceDictionary { get; private set; }

        public ServerManager(string path)
        {
            _path = path;
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            AppDomain.CurrentDomain.AssemblyResolve += Missing_AssemblyResolver;

            DependencyInjection();
        }

        private Assembly Missing_AssemblyResolver(object sender, ResolveEventArgs args)
        {
            var asm = args.Name.Split(",")[0];
            var a = Assembly.Load(asm);
            return a;
        }

        private void DependencyInjection()
        {
            //may better way use a native .Net Core DI
            var d = new Dictionary<int, IGenerateResponseContainer>();
            foreach (var type in Assembly.GetEntryAssembly().GetTypes())
            {
                if (!type.IsClass)
                {
                    continue;
                }

                if (type.GetInterfaces().Any(x => x == typeof(IGenerateResponseContainer)))
                {
                    var t = (IGenerateResponseContainer)Activator.CreateInstance(type);
                    d[t.RequestTypePackage] = t;
                }
            }

            ServiceDictionary = d;
        }

        public int ActiveClientCount
        {
            get { return _ActiveClientCount; }
            private set { _ActiveClientCount = value; }
        }

        public int MaxActiveClientCount = 10000; //todo провверить корректность дисконнекта
        public static ServerSettings ServerSettings;

        public void Start(int port = SessionClient.DefaultPort)
        {
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Settings.json")))
            {
                ServerSettings = new ServerSettings();
                ServerSettings.Port = port;

                using (StreamWriter file = File.CreateText(Path.Combine(_path, "Settings.json")))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Formatting = Formatting.Indented;
                    serializer.Serialize(file, ServerSettings);
                }
            }
            else
            {
                ServerSettings = JsonConvert.DeserializeObject<ServerSettings>(File.ReadAllText(Path.Combine(_path, "Settings.json")));
            }

            Loger.PathLog = _path;
            Loger.IsServer = true;

            Loger.Log($"Server starting on port: {ServerSettings.Port}");

            var rep = Repository.Get;
            rep.SaveFileName = Path.Combine(_path, "World.dat");
            rep.Load();
            CheckDiscrordUser();

            //общее обслуживание
            rep.Timer.Add(1000, DoWorld);

            //сохранение, если были изменения
            rep.Timer.Add(10000, () =>
            {
                rep.Save(true);
            });

            ActiveClientCount = 0;

            LogMessage?.Invoke("Start server in port " + port.ToString());

            Connect = new ConnectServer();

            Connect.ConnectionAccepted = ConnectionAccepted;
            //throw new Exception("test");
            Connect.Start(null, port);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd-hh-mm");
            var fileName = Path.Combine(_path, "!UnhandledException" + date + ".log");
            File.WriteAllText(fileName, e.ExceptionObject.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// check and create if it is necessary DiscrordUser
        /// </summary>
        private void CheckDiscrordUser()
        {
            var isDiscordBotUser = Repository.GetData.PlayersAll.Any(p => Repository.DISCORD == p.Public.Login);
            if (isDiscordBotUser)
            {
                return;
            }

            var guid = Guid.NewGuid();
            var player = new PlayerServer(Repository.DISCORD)
            {
                Pass = new CryptoProvider().GetHash(guid.ToString()),
                DiscordToken = guid,
                IsAdmin = true, // возможно по умолчанию запретить ?
            };

            player.Public.Grants = (uint)Grants.GameMaster + (uint)Grants.Moderator;
            Repository.GetData.PlayersAll.Add(player);
            Repository.Get.Save(false);
        }

        /// <summary>
        /// Общее обслуживание мира
        /// </summary>
        private void DoWorld()
        {
            //Loger.Log("Server DoWorld");
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
                // To do Здесь надо сообщить причину дисконнекта, а то получается сбрасываем коннект и всё
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
                if (LogMessage != null) LogMessage("New connect");

                using (var ss = new SessionServer())
                {
                    ss.LogMessage += LogMessage;
                    ss.Do(client);
                }
            }
            catch (Exception e)
            {
                var errorText = ExceptionUtil.ExceptionLog(e, "Server Exception");
                if (!(e is SocketException) && !(e.InnerException is SocketException))
                {
                    if (LogMessage != null) LogMessage(errorText);
                }
            }
            //if (LogMessage != null) LogMessage("End connect");

            Interlocked.Decrement(ref _ActiveClientCount);
        }
    }
}