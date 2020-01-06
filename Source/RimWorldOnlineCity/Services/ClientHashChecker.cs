using OCUnion;
using OCUnion.Common;
using OCUnion.Transfer.Model;
using OCUnion.Transfer.Types;
using System;
using System.Collections.Generic;
using System.IO;
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
            // По хорошему надо разнести на два вызова один для модов второй для стим с передачей параметров через контекст
            Loger.Log("Send hash to server");
            var modsResCheck = _sessionClient.TransObject2<ModelModsFiles>(generateHashFiles(false), RequestTypePackage, ResponseTypePackage);
            var steamCheck = _sessionClient.TransObject2<ModelModsFiles>(generateHashFiles(true), RequestTypePackage, ResponseTypePackage);

            ApproveLoadWorldReason result = ApproveLoadWorldReason.LoginOk;
            if (modsResCheck.Files.Count > 0)
            {
                result = result | ApproveLoadWorldReason.ModsFilesFail;
                FileChecker.FileSynchronization(GenFilePaths.CoreModsFolderPath, modsResCheck);
            }

            if (steamCheck.Files.Count > 0)
            {
                result = result | ApproveLoadWorldReason.ModsSteamWorkShopFail;
                FileChecker.FileSynchronization(SteamFolder, steamCheck);
            }

            return result;
        }

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
            SteamFolder = GenFilePaths.CoreModsFolderPath.Replace("common\\RimWorld\\Mods", "workshop\\content\\294100");
            if (!Directory.Exists(SteamFolder))
            {
                Log.Message($"Directory {SteamFolder} not found, multiplayer doesn't support not License Copy of Game");
                SteamFolder = String.Empty;
            }

            CheckHashModsThread = new Thread(() =>
           {
               ClientHashChecker.ModsFiles = FileChecker.GenerateHashFiles(GenFilePaths.CoreModsFolderPath, modsListFolder);
               Loger.Log($"GenerateHashFiles {GenFilePaths.CoreModsFolderPath}");
               ClientHashChecker.SteamFiles = FileChecker.GenerateHashFiles(SteamFolder, steamListFolder);
               Loger.Log($"GenerateHashFiles {SteamFolder}");
           });

            CheckHashModsThread.IsBackground = true;
            CheckHashModsThread.Start();
        }

        private ModelModsFiles generateHashFiles(bool isSteam)
        {
            if (CheckHashModsThread.ThreadState != ThreadState.Stopped)
            {
                CheckHashModsThread.Join();
            }

            return
                new ModelModsFiles()
                {
                    IsSteam = isSteam,
                    Files = isSteam ? SteamFiles : ModsFiles,
                }
           ;
        }
    }
}
