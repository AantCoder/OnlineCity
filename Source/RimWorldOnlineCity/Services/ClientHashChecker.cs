using OCUnion;
using OCUnion.Common;
using OCUnion.Transfer.Model;
using OCUnion.Transfer.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Verse;

namespace RimWorldOnlineCity.Services
{
    sealed class ClientHashChecker : IOnlineCityClientService<ApproveLoadWorldReason>
    {
        public PackageType RequestTypePackage => PackageType.Request35ListFiles;
        public PackageType ResponseTypePackage => PackageType.Response36ListFiles;

        private static List<ModelFileInfo> SteamFiles;
        private static List<ModelFileInfo> ModsFiles;
        private static List<ModelFileInfo> ModsConfigsFiles;

        /// <summary>
        /// Поток который считает хеш сумму файлов
        /// </summary>
        private static Thread CheckHashModsThread;
        private readonly Transfer.SessionClient _sessionClient;

        public static string SteamFolder { get; private set; }

        public ClientHashChecker(Transfer.SessionClient sessionClient)
        {
            _sessionClient = sessionClient;
        }

        /// <summary>
        /// Генерируем запрос серверу в зависимости от контекста
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public ApproveLoadWorldReason GenerateRequestAndDoJob(object context)
        {
            Loger.Log("Send hash to server");
            var modsResCheck = _sessionClient.TransObject2<ModelModsFiles>(generateHashFiles(FolderType.ModsFolder), RequestTypePackage, ResponseTypePackage);
            var steamCheck = _sessionClient.TransObject2<ModelModsFiles>(generateHashFiles(FolderType.SteamFolder), RequestTypePackage, ResponseTypePackage);
            var modsConfigCheck = _sessionClient.TransObject2<ModelModsFiles>(generateHashFiles(FolderType.ModsConfigPath), RequestTypePackage, ResponseTypePackage);

            Loger.Log($"Send hash {GenFilePaths.ModsFolderPath}");
            ApproveLoadWorldReason result = ApproveLoadWorldReason.LoginOk;
            if (modsResCheck.Files.Count > 0)
            {
                result = result | ApproveLoadWorldReason.ModsFilesFail;
                FileChecker.FileSynchronization(GenFilePaths.ModsFolderPath, modsResCheck);
            }

            Loger.Log($"Send hash {SteamFolder}");
            if (steamCheck.Files.Count > 0)
            {
                result = result | ApproveLoadWorldReason.ModsSteamWorkShopFail;
                FileChecker.FileSynchronization(SteamFolder, steamCheck);
            }

            Loger.Log($"Send hash {GenFilePaths.ConfigFolderPath}");
            if (modsConfigCheck.Files.Count > 0)
            {
                result = result | ApproveLoadWorldReason.NotAllConfigsOnClient;
                FileChecker.FileSynchronization(GenFilePaths.ConfigFolderPath, modsConfigCheck);
            }

            return result;
        }

        private static bool CheckHashModsThreadRun;
        private static object CheckHashModsThreadSunc = new Object();

        /// <summary>
        /// Send hash files of loaded mods to server and update it if hash different
        /// </summary>
        public static void StartGenerateHashFiles(string modsFileName, string steamFileName)
        {
            var modsListFolder = File.ReadAllLines(modsFileName);
            var steamListFolder = File.ReadAllLines(steamFileName);

            Loger.Log("Start Hash:" + modsFileName);
            Loger.Log("Start Hash:" + steamFileName);
            // может быть есть лучше вариант, как указать папку модов со steam ???
            SteamFolder = GenFilePaths.ModsFolderPath.Replace("common\\RimWorld\\Mods", "workshop\\content\\294100");
            Loger.Log("GenFilePaths.ModsConfigFilePath = " + GenFilePaths.ConfigFolderPath);
            if (SteamFolder.Equals(GenFilePaths.ModsFolderPath) || !Directory.Exists(SteamFolder))
            {
                var steamFolder = Path.Combine(SessionClientController.ConfigPath, "workshop");
                Log.Message($"Directory {SteamFolder} not found, using {steamFolder}");
                if (!Directory.Exists(steamFolder))
                {
                    Log.Message($"Create {steamFolder}");
                    Log.Message("Таян наверное расстроится, скорее всего используется пиратская версия игры");
                    Directory.CreateDirectory(steamFolder);
                }

                SteamFolder = steamFolder;
            }

            Loger.Log("SteamFolder=" + SteamFolder);

            CheckHashModsThread = new Thread(() =>
            {
                lock (CheckHashModsThreadSunc)
                {
                    try
                    {
                        Loger.Log($"GenerateHashFiles {GenFilePaths.ModsFolderPath}");
                        ClientHashChecker.ModsFiles = FileChecker.GenerateHashFiles(GenFilePaths.ModsFolderPath, modsListFolder);
                        Loger.Log($"GenerateHashFiles {SteamFolder}");
                        ClientHashChecker.SteamFiles = FileChecker.GenerateHashFiles(SteamFolder, steamListFolder);
                        Loger.Log($"ModsConfigsFiles {GenFilePaths.ConfigFolderPath}");

                        var dir = new DirectoryInfo(GenFilePaths.ConfigFolderPath);
                        var parent = dir.Parent.FullName;
                        var configNameFolder = dir.Name;
                        var files = FileChecker.GenerateHashFiles(parent, new string[] { configNameFolder });
                        files.ForEach(f => f.FileName = new FileInfo(f.FileName).Name);
                        ClientHashChecker.ModsConfigsFiles = files.Where(x => !FileChecker.IgnoredConfigFiles.Contains(new FileInfo(x.FileName).Name)).ToList();
#if DEBUG
                        foreach (var file in files)
                        {
                            Loger.Log(file.FileName);
                        }
#endif
                    }
                    catch (Exception exc)
                    {
                        Loger.Log($"GenerateHashFiles Exception {exc.ToString()}");
                    }
                    CheckHashModsThreadRun = false;
                }
            });

            CheckHashModsThreadRun = true;
            CheckHashModsThread.IsBackground = true;
            CheckHashModsThread.Start();
        }

        private ModelModsFiles generateHashFiles(FolderType folderType)
        {
            if (CheckHashModsThreadRun)
            {
                Loger.Log("Wait...");
                lock (CheckHashModsThreadSunc)
                {
                    Loger.Log("Wait end");
                }
            }

            List<ModelFileInfo> files = null;
            switch (folderType)
            {
                case FolderType.ModsFolder: { files = ModsFiles; break; }
                case FolderType.SteamFolder: { files = SteamFiles; break; }
                case FolderType.ModsConfigPath: { files = ModsConfigsFiles; break; }
            }

            return
                new ModelModsFiles()
                {
                    FolderType = folderType,
                    Files = files,
                };
        }
    }
}

