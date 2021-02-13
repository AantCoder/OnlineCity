using OCUnion;
using OCUnion.Common;
using OCUnion.Transfer.Model;
using OCUnion.Transfer.Types;
using RimWorldOnlineCity.ClientHashCheck;
using RimWorldOnlineCity.UI;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

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

            ApproveLoadWorldReason result = ApproveLoadWorldReason.LoginOk;
            bool downloading = true;
            long totalSize = 0;
            long downloadSize = 0;
            while (downloading)
            {
                var clientFileChecker = (ClientFileChecker)context;
                var model = new ModelModsFiles()
                {
                    Files = clientFileChecker.FilesHash,
                    FolderType = clientFileChecker.FolderType,
                };

                UpdateModsWindow.Title = "OC_Hash_Downloading";
                UpdateModsWindow.HashStatus = "";
                UpdateModsWindow.SummaryList = null;
                Loger.Log($"Send hash {clientFileChecker.Folder}");

                var res = _sessionClient.TransObject2<ModelModsFiles>(model, RequestTypePackage, ResponseTypePackage);

                if (res.Files.Count > 0)
                {
                    if (totalSize == 0) totalSize = res.TotalSize;
                    downloadSize += res.Files.Sum(f => f.Size);
                    Loger.Log($"Files that need for a change:");
                    UpdateModsWindow.HashStatus = "OC_Hash_Downloading_Finish"
                        + (downloadSize * 100 / totalSize).ToString() + "%";

                    result = result | ApproveLoadWorldReason.ModsFilesFail;
                    FileChecker.FileSynchronization(clientFileChecker.Folder, res);
                    clientFileChecker.RecalculateHash(res.Files.Select(f => f.FileName).ToList());

                    var addList = res.Files
                        .Select(f => f.FileName)
                        .Where(f => f.Contains("\\"))
                        .Select(f => f.Substring(0, f.IndexOf("\\")))
                        //.Distinct() //вместо дистинкта группируем без разницы заглавных букв, но сохраняем оригинальное название
                        .Select(f => new { orig = f, comp = f.ToLower() })
                        .GroupBy(p => p.comp)
                        .Select(g => g.Max(p => p.orig))
                        .Where(f => UpdateModsWindow.SummaryList == null || !UpdateModsWindow.SummaryList.Any(sl => sl == f))
                        .ToList();
                    if (UpdateModsWindow.SummaryList == null)
                        UpdateModsWindow.SummaryList = addList;
                    else
                        UpdateModsWindow.SummaryList.AddRange(addList);
                }
                downloading = res.TotalSize != 0;
            }
            return result;
        }
    }
}

