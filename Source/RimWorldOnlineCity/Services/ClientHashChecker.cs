using OCUnion;
using OCUnion.Common;
using OCUnion.Transfer.Model;
using OCUnion.Transfer.Types;
using RimWorldOnlineCity.ClientHashCheck;
using RimWorldOnlineCity.UI;

namespace RimWorldOnlineCity.Services
{
    sealed class ClientHashChecker : IOnlineCityClientService<ApproveLoadWorldReason>
    {
        public PackageType RequestTypePackage => PackageType.Request35ListFiles;
        public PackageType ResponseTypePackage => PackageType.Response36ListFiles;

        private readonly Transfer.SessionClient _sessionClient;

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
            // each request - response Client-Server-Client ~100-200 ms, get all check hash in one request             
            // каждый запрос-отклик к серверу ~100-200 мс, получаем за один запрос все файлы
            // ~40 000 files *512 SHA key ~ size of package ~ 2,5 Mb 

            var clientFileChecker = (ClientFileChecker)context;
            var model = new ModelModsFiles()
            {
                Files = clientFileChecker.FilesHash,
                FolderType = clientFileChecker.FolderType,
            };

            UpdateModsWindow.WindowTitle = $"Send files {clientFileChecker.FolderType.ToString()} to server";
            Loger.Log($"Send hash {clientFileChecker.Folder}");

            var res = _sessionClient.TransObject2<ModelModsFiles>(model, RequestTypePackage, ResponseTypePackage);

            ApproveLoadWorldReason result = ApproveLoadWorldReason.LoginOk;
            if (res.Files.Count > 0)
            {
                Loger.Log($"Files that need for a change:");
                UpdateModsWindow.HashStatus = string.Join("\n", res.Files);
                    result = result | ApproveLoadWorldReason.ModsFilesFail;
                FileChecker.FileSynchronization(clientFileChecker.Folder, res);
            }

            return result;
        }
    }
}

