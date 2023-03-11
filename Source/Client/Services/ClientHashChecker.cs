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
using Verse;
using RimWorldOnlineCity.Model;
using System.Text;

namespace RimWorldOnlineCity.Services
{
    sealed class ClientHashChecker : IOnlineCityClientService<bool>
    {
        public PackageType RequestTypePackage => PackageType.Request35ListFiles;
        public PackageType ResponseTypePackage => PackageType.Response36ListFiles;

        private readonly Transfer.SessionClient _sessionClient;

        public ClientHashCheckerResult Report { get; set; }

        public ClientHashChecker(Transfer.SessionClient sessionClient)
        {
            _sessionClient = sessionClient;
        }

        /// <summary>
        /// Генерируем запрос серверу в зависимости от контекста
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Файлы соответствуют образцу</returns>
        public bool GenerateRequestAndDoJob(object context)
        {
            // each request - response Client-Server-Client ~100-200 ms, get all check hash in one request             
            // каждый запрос-отклик к серверу ~100-200 мс, получаем за один запрос все файлы
            // ~40 000 files *512 SHA key ~ size of package ~ 2,5 Mb 

            bool result = true;
            UpdateModsWindow.Title = "OC_Hash_Downloading".Translate();
            UpdateModsWindow.HashStatus = "";
            UpdateModsWindow.SummaryList = null;

            var clientFileChecker = (ClientFileChecker)context;
            var model = new ModelModsFilesRequest()
            {
                FolderType = clientFileChecker.Folder.FolderType,
                Files = clientFileChecker.FilesHash,
                NumberFileRequest = 0
            };
            long totalSize = 0;
            long downloadSize = 0;
            try
            {
                while (true)
                {
                    Loger.Log($"Send hash {clientFileChecker.Folder.FolderType} N{model.NumberFileRequest}");

                    var res = _sessionClient.TransObject2<ModelModsFilesResponse>(model, RequestTypePackage, ResponseTypePackage);

                    if (res.Files == null || res.Files.Count == 0)
                    {
                        if (model.NumberFileRequest == 0)
                        {
                            model.NumberFileRequest = 1;
                            continue;
                        }
                        break;
                    }

                    if (res.IgnoreTag != null && res.IgnoreTag.Count > 0)
                    {
                        //Файлы настроек присылаются каждый раз и сравнивается с текущим после удаления тэгов
                        var XMLFileName = Path.Combine(clientFileChecker.FolderPath, res.Files[0].FileName);
                        var xmlServer = FileChecker.GenerateHashXML(res.Files[0].Hash, res.IgnoreTag);
                        var xmlClient = FileChecker.GenerateHashXML(XMLFileName, res.IgnoreTag);

                        //Если хеши не равны, то продолжаем как с обычным файлом присланым для замены
                        if (xmlClient != null && xmlServer.Equals(xmlClient))
                        {
                            Loger.Log("File XML good: " + res.Files[0].FileName);
                            res.Files.RemoveAt(0);
                        }
                        else
                        {
                            Loger.Log("File XML need for a change: " + res.Files[0].FileName
                                + $" {(xmlClient?.Hash == null ? "" : Convert.ToBase64String(xmlClient.Hash).Substring(0, 6))} "
                                + $"-> {(xmlServer?.Hash == null ? "" : Convert.ToBase64String(xmlServer.Hash).Substring(0, 6))} "
                                + " withoutTag: " + res.IgnoreTag[0], Loger.LogLevel.WARNING);
                        }
                    }

                    if (res.Files.Count > 0)
                    {
                        if (totalSize == 0) totalSize = res.TotalSize;
                        downloadSize += res.Files.Sum(f => f.Size);
                        Loger.Log($"Files that need for a change: {downloadSize}/{totalSize} count={res.Files.Count}", Loger.LogLevel.WARNING);
                        var pr = downloadSize > totalSize || totalSize == 0 ? 100 : downloadSize * 100 / totalSize;
                        UpdateModsWindow.HashStatus = "OC_Hash_Downloading_Finish".Translate()
                            + pr.ToString() + "%";

                        result = false;
                        if (res.Files.Any(f => f.NeedReplace)) FileChecker.FileSynchronization(clientFileChecker.FolderPath, res);
                        clientFileChecker.RecalculateHash(res.Files.Select(f => f.FileName).ToList());

                        Report.FileSynchronization(res.Files);

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

                    if (res.TotalSize == 0 //проверили весь объем
                        || (res.IgnoreTag != null && res.IgnoreTag.Count > 0) //это XML файл, они идут по одному
                        || res.Files.Any(f => !f.NeedReplace) //это файлы без права замены, а значит проблема не может быть решена
                        ) model.NumberFileRequest++;
                }
                return result;
            }
            catch (Exception ex)
            {
                Loger.Log(ex.ToString());
                SessionClientController.Disconnected("Error " + ex.Message);
                return false;
            }
        }
    }
}
