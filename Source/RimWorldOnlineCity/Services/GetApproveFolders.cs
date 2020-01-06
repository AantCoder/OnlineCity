using OCUnion;
using OCUnion.Common;
using OCUnion.Transfer.Model;
using System.IO;
using Transfer;
using Verse;

namespace RimWorldOnlineCity.Services
{
    public sealed class GetApproveFolders : IOnlineCityClientService<bool>
    {
        public PackageType RequestTypePackage => PackageType.Request37GetApproveFolders;

        public PackageType ResponseTypePackage => PackageType.Response38GetApproveFolders;

        private readonly Transfer.SessionClient _sessionClient;
        public static string ConfigPath => Path.Combine(GenFilePaths.ConfigFolderPath, "OnlineCity");

        public GetApproveFolders(Transfer.SessionClient sessionClient)
        {
            _sessionClient = sessionClient;
            if (!Directory.Exists(ConfigPath))
            {
                Directory.CreateDirectory(ConfigPath);
            }
        }

        public static string GetModsApprovedFoldersFileName(string ip)
        {
            return Path.Combine(ConfigPath, ip + "_mods.txt");
        }

        public static string GetSteamApprovedFoldersFileName(string ip)
        {
            return Path.Combine(ConfigPath, ip + "_steam.txt");
        }

        /// <summary>
        /// Request from server file contains approved folders
        /// if file exist and matched then return true else false
        /// Запрос у сервера списка разрешенных папок 
        /// после получения этого файла мы сравниваем с тем что был ранее, если не было или не совпадает возвращаем false иначе true
        /// </summary>
        /// <param name="context">File Name</param> // имя сервера для которого проверяем вида 10.10.10.10.PortNumber
        /// <returns>Ответ</returns>
        public bool GenerateRequestAndDoJob(object context)
        {
            var ip = context as string;
            if (string.IsNullOrEmpty(ip))
            {
                return false;
            }

            // first row of ModelModsFiles second contains Steam 
            var approvedFolders = _sessionClient.TransObject2<ModelModsFiles>(new ModelInt(), RequestTypePackage, ResponseTypePackage);
            // каждый запрос-отклик к серверу ~100-200 мс, получаем за один запрос все файлы
            //var steamApprovedFolders = _sessionClient.TransObject2<ModelModsFiles>(new ModelInt() { Value = 1 }, RequestTypePackage, ResponseTypePackage);

            //Если на сервере настройка не задана, то считаем что проверка пройдена
            if (approvedFolders == null) return true;

            var modsFileName = GetModsApprovedFoldersFileName(ip);
            var steamFileName = GetSteamApprovedFoldersFileName(ip);
            var modsConfigName = Path.Combine(GenFilePaths.ConfigFolderPath, "ModsConfig.xml"); //%appdata%\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config

            var modsListFile = approvedFolders.Files[0].Hash;
            var steamListFile = approvedFolders.Files[1].Hash;
            var modsConfig = approvedFolders.Files[2].Hash;

            var result = checkAndCreateFile(modsFileName, modsListFile) & checkAndCreateFile(steamFileName, steamListFile) & checkAndCreateFile(modsConfigName, modsConfig);

            return result;
        }

        private bool checkAndCreateFile(string fileName, byte[] orign)
        {
            var hashFile = FileChecker.GetCheckSum(fileName);
            var hashServer = FileChecker.GetCheckSum(orign);

            if (!ModelFileInfo.UnsafeByteArraysEquale(hashFile, hashServer))
            {
                Loger.Log($"Create file {fileName}");
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                File.WriteAllBytes(fileName, orign);

                return false;
            }

            Loger.Log($"Hash for the file {fileName} equal on the server");
            return true;
        }
    }
}
