using System.Collections.Generic;
using System.IO;
using System.Linq;
using OCUnion.Transfer;
using OCUnion.Transfer.Model;
using OCUnion.Transfer.Types;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class CheckFiles : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request35ListFiles;

        public int ResponseTypePackage => (int)PackageType.Response36ListFiles;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = checkFiles((ModelModsFiles)request.Packet, context);
            return result;
        }

        private ModelModsFiles checkFiles(ModelModsFiles packet, ServiceContext context)
        {
            var modsDir = packet.IsSteam ? ServerManager.ServerSettings.SteamWorkShopModsDir : ServerManager.ServerSettings.ModsDirectory;
            var NoApproveWorld = packet.IsSteam ? ApproveLoadWorldReason.ModsSteamWorkShopFail : ApproveLoadWorldReason.ModsFilesFail;
            var workDict = packet.IsSteam ? ServerManager.SteamFilesDict : ServerManager.ModFilesDict;
            var foldersTree = packet.IsSteam ? ServerManager.ServerSettings.SteamDirConfig.FoldersTree : ServerManager.ServerSettings.ModsDirConfig.FoldersTree;

            var result = new List<ModelFileInfo>();

            var allServerFiles = new HashSet<string>(workDict.Keys);
            foreach (var file in packet.Files)
            {
                if (workDict.TryGetValue(file.FileName, out ModelFileInfo fileInfo))
                {
                    allServerFiles.Remove(file.FileName); // 

                    if (!ModelFileInfo.UnsafeByteArraysEquale(file.Hash, fileInfo.Hash))
                    {
                        // read file for send to Client      
                        // файл  найден, но хеши не совпадают, необходимо заменить файл
                        result.Add(GetFile(modsDir, fileInfo.FileName));
                    }
                }
                else
                {
                    // mark file for delete 
                    // Если файл с таким именем не найден, помечаем файл на удаление
                    file.Hash = null;
                    result.Add(file);
                }
            }

            lock (context.Player)
            {
                // проверяем в обратном порядке: что бы у клиенты были все файлы
                if (allServerFiles.Any())
                {
                    context.Player.ApproveLoadWorldReason = context.Player.ApproveLoadWorldReason | ApproveLoadWorldReason.NotAllFilesOnClient;
                    foreach (var fileName in allServerFiles)
                    {
                        result.Add(GetFile(modsDir, fileName));
                    }
                }

                // Если файлы не прошли проверку, помечаем флагом, запрет загрузки мира
                if (result.Any())
                {
                    context.Player.ApproveLoadWorldReason = context.Player.ApproveLoadWorldReason | NoApproveWorld;
                }
            }

            if (!result.Any())
            {
                // микроптимизация: если файлы не будут восстанавливаться, не отправляем обратно список папок
                // на восстановление ( десериализацию папок также тратится время)
                foldersTree = new FoldersTree();
            }

            return new ModelModsFiles()
            {
                Files = result,
                IsSteam = packet.IsSteam,
                FoldersTree = foldersTree
            };
        }

        private ModelFileInfo GetFile(string rootDir, string fileName)
        {
            var newFile = new ModelFileInfo() { FileName = fileName };
            var fullname = Path.Combine(rootDir, fileName);
            newFile.Hash = File.ReadAllBytes(fullname);
            return newFile;
        }
    }
}
