using OCUnion;
using ServerCore.Model;
using ServerOnlineCity.Model;
using System;
using System.Text.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using Transfer;
using Util;
using System.Text;
using OCUnion.Transfer.Model;
using OCUnion.Common;
using OCUnion.Transfer;

namespace ServerOnlineCity
{
    public class ServerManager
    {
        public int MaxActiveClientCount = 10000; //todo провверить корректность дисконнекта
        public static ServerSettings ServerSettings = new ServerSettings();
        public static IReadOnlyDictionary<string, ModelFileInfo> ModFilesDict;
        public static IReadOnlyDictionary<string, ModelFileInfo> SteamFilesDict;

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
        }

        public void Start(string path)
        {
            //var jsonFile = Path.Combine(Directory.GetCurrentDirectory(), "Settings.json");
            var jsonFile = Path.Combine(path, "Settings.json");
            if (!File.Exists(jsonFile))
            {
                using (StreamWriter file = File.CreateText(jsonFile))
                {
                    var jsonText = JsonSerializer.Serialize(ServerSettings);
                    file.WriteLine(jsonText);
                }

                Console.WriteLine("Created Settings.json, server was been stopped");
                Console.WriteLine($"RU: Настройте сервер, заполните {jsonFile}");
                Console.WriteLine("Enter some key");
                Console.ReadKey();
                return;
            }
            else
            {
                try
                {
                    using (var fs = new StreamReader(jsonFile, Encoding.UTF8))
                    {
                        var jsonString = fs.ReadToEnd();
                        ServerSettings = JsonSerializer.Deserialize<ServerSettings>(jsonString);
                    }

                    ServerSettings.WorkingDirectory = path;
                    var results = new List<ValidationResult>();
                    var context = new ValidationContext(ServerSettings);
                    if (!Validator.TryValidateObject(ServerSettings, context, results, true))
                    {
                        foreach (var error in results)
                        {
                            Console.WriteLine(error.ErrorMessage);
                            Loger.Log(error.ErrorMessage);
                        }

                        Console.ReadKey();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine($"RU: Проверьте настройки сервера {jsonFile}");
                    Console.WriteLine("EN: Check Settings.json");
                    Console.ReadKey();
                    return;
                }
            }

            MainHelper.OffAllLog = false;
            Loger.PathLog = path;
            Loger.IsServer = true;

            var rep = Repository.Get;
            rep.SaveFileName = Path.Combine(path, "World.dat");
            rep.Load();
            CheckDiscrordUser();
            createFilesDictionary();

            //общее обслуживание
            rep.Timer.Add(1000, DoWorld);

            //сохранение, если были изменения
            rep.Timer.Add(ServerSettings.SaveInterval, () =>
            {
                rep.Save(true);
            });

            //ActiveClientCount = 0;

            Connect = new ConnectServer();
            Connect.ConnectionAccepted = ConnectionAccepted;

            Loger.Log($"Server starting on port: {ServerSettings.Port}");
            Connect.Start(null, ServerSettings.Port);
        }

        private void createFilesDictionary()
        {
            if (!ServerSettings.IsModsWhitelisted)
            {
                return;
            }

            // 1. Создаем словарь со всеми файлами
            Loger.Log($"Calc hash {ServerSettings.ModsDirectory}");
            var modFiles = FileChecker.GenerateHashFiles(ServerSettings.ModsDirectory, Directory.GetDirectories(ServerSettings.ModsDirectory));
            Loger.Log($"Calc hash {ServerSettings.SteamWorkShopModsDir}");
            ///!!!!!!!!!!!!!!!! STEAM FOLDER CHECK SWITCH HERE  !!!!!!!!!!!!!!!
            // 1. Если будем использовать steamworkshop диреторию, эти две строчки ниже закомментировать 
            // 2. remove JsobIgnrore atribbute in ServerSettings  
            ServerSettings.SteamWorkShopModsDir = Environment.CurrentDirectory;
            ///!!!!!!!!!!!!!!!! STEAM FOLDER CHECK SWITCH HERE  !!!!!!!!!!!!!!!
            var steamFiles = FileChecker.GenerateHashFiles(ServerSettings.SteamWorkShopModsDir, new string [0]);

            var modFilesDict = new Dictionary<string, ModelFileInfo>(modFiles.Count);
            var steamFilesDict = new Dictionary<string, ModelFileInfo>(steamFiles.Count);

            addFiles(modFilesDict, modFiles);
            addFiles(steamFilesDict, steamFiles);
            ModFilesDict = modFilesDict;
            SteamFilesDict = steamFilesDict;

            ServerSettings.AppovedFolderAndConfig = new ModelModsFiles()
            {
                Files = new List<ModelFileInfo>(3), // 0 - list Folders in Mods dir, 1 -list Folders in Steam dir , 3-  ModsConfig.xml  
                                                    // FoldersTree = new FoldersTree(),
            };

            // 2. Создаем файлы со списком разрешенных папок, которые отправим клиенту
            var files = ServerSettings.AppovedFolderAndConfig.Files;
            var modsFolders = new ModelFileInfo() // 0 
            {
                FileName = "ApprovedMods.txt",
                Hash = FileChecker.CreateListFolder(ServerSettings.ModsDirectory)
            };
            files.Add(modsFolders);

            var steamFolders = new ModelFileInfo() // 1 
            {
                FileName = "ApprovedSteamWorkShop.txt",
                Hash = FileChecker.CreateListFolder(ServerSettings.SteamWorkShopModsDir)
            };
            files.Add(steamFolders);

            var fName = Path.Combine(ServerSettings.WorkingDirectory, "ModsConfig.xml");
            files.Add(new ModelFileInfo() // 1 
            {
                FileName = "ModsConfig.xml",
                Hash = Encoding.UTF8.GetBytes(File.ReadAllText(fName))
            });

            ServerSettings.ModsDirConfig = new ModelModsFiles()
            {
                IsSteam = false,
                Files = new List<ModelFileInfo>() { modsFolders },
                FoldersTree = FoldersTree.GenerateTree(ServerSettings.ModsDirectory),
            };

            ServerSettings.SteamDirConfig = new ModelModsFiles()
            {
                IsSteam = true,
                Files = new List<ModelFileInfo>() { steamFolders },
                FoldersTree = FoldersTree.GenerateTree(ServerSettings.SteamWorkShopModsDir),
            };
        }

        private void addFiles(Dictionary<string, ModelFileInfo> dict, List<ModelFileInfo> files)
        {
            foreach (var file in files)
            {
                dict[file.FileName] = file;
            }
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
            HashSet<string> allLogins = null;
            //Есть ли какие-то изменения в списках пользователей
            bool changeInPlayers = false;
            ///Обновляем кто кого видит
            foreach (var player in Repository.GetData.PlayersAll)
            {
                var pl = player.PublicChat.PartyLogin;
                if (pl.Count == Repository.GetData.PlayersAll.Count) continue;

                changeInPlayers = true;

                if (player.IsAdmin
                    || true //todo переделать это на настройки сервера "в чате доступны все, без учета зон контакта"
                    )
                {
                    if (allLogins == null) allLogins = new HashSet<string>(Repository.GetData.PlayersAll.Select(p => p.Public.Login));
                    lock (player)
                    {
                        ///админы видят всех: добавляем кого не хватает
                        var plAdd = new HashSet<string>(allLogins);
                        plAdd.ExceptWith(player.PublicChat.PartyLogin);

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

            if (changeInPlayers)
            {
                Repository.GetData.UpdatePlayersAllDic();
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
