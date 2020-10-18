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
            var filesDir = ServerManager.FileHashChecker.CheckedDirAndFiles[packet.FolderType].Item1;
            var NoApproveWorld = ServerManager.FileHashChecker.ApproveWorldType[packet.FolderType];
            var workDict = ServerManager.FileHashChecker.CheckedDirAndFiles[packet.FolderType].Item3;
            var foldersTree = ServerManager.FileHashChecker.CheckedDirAndFiles[packet.FolderType].Item2;

            var result = new List<ModelFileInfo>();
            var allServerFiles = new HashSet<string>(workDict.Keys);
            var packetFiles = packet.Files != null ? packet.Files : new List<ModelFileInfo>(0);

            foreach (var file in packetFiles)
            {
                if (workDict.TryGetValue(file.FileName, out ModelFileInfo fileInfo))
                {
                    allServerFiles.Remove(file.FileName); // 

                    if (!ModelFileInfo.UnsafeByteArraysEquale(file.Hash, fileInfo.Hash))
                    {
                        // read file for send to Client      
                        // файл  найден, но хеши не совпадают, необходимо заменить файл
                        result.Add(GetFile(filesDir, fileInfo.FileName));
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
                        result.Add(GetFile(filesDir, fileName));
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
                FolderType = packet.FolderType,
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
