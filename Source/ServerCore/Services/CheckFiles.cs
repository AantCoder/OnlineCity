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

        private const long MaxPacketSize = 5000000;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = checkFiles((ModelModsFiles)request.Packet, context);
            return result;
        }

        private ModelModsFiles checkFiles(ModelModsFiles packet, ServiceContext context)
        {
            var NoApproveWorld = ServerManager.FileHashChecker.ApproveWorldType[packet.FolderType];
            var filesDir = ServerManager.FileHashChecker.CheckedDirAndFiles[packet.FolderType].ServerDirectory;
            var foldersTree = ServerManager.FileHashChecker.CheckedDirAndFiles[packet.FolderType].FolderTree;
            var workDict = ServerManager.FileHashChecker.CheckedDirAndFiles[packet.FolderType].HashFiles;
            var ignoredFiles = ServerManager.FileHashChecker.CheckedDirAndFiles[packet.FolderType].IgrnoredFiles;

            var result = new List<ModelFileInfo>();
            var allServerFiles = new HashSet<string>(workDict.Keys);
            var packetFiles = packet.Files != null ? packet.Files : new List<ModelFileInfo>(0);

            long packetSize = 0;
            long totalSize = 0;

            foreach (var modelFile in packetFiles)
            {
                var modelFileFileName = modelFile.FileName.ToLower();
                if (FileHashChecker.FileNameContainsIgnored(modelFileFileName, ignoredFiles))
                {
                    continue;
                }

                if (workDict.TryGetValue(modelFileFileName, out ModelFileInfo fileInfo))
                {
                    allServerFiles.Remove(modelFileFileName); // 

                    if (!ModelFileInfo.UnsafeByteArraysEquale(modelFile.Hash, fileInfo.Hash))
                    {
                        // read file for send to Client      
                        // файл  найден, но хеши не совпадают, необходимо заменить файл
                        if (packetSize < MaxPacketSize)
                        {
                            var addFile = GetFile(filesDir, fileInfo.FileName);
                            result.Add(addFile);
                            packetSize += addFile.Size;
                            totalSize += addFile.Size;
                        }
                        else
                        {
                            var size = GetFileSize(filesDir, fileInfo.FileName);
                            totalSize += size;
                        }
                    }
                }
                else
                {
                    // mark file for delete 
                    // Если файл с таким именем не найден, помечаем файл на удаление
                    modelFile.Hash = null;
                    result.Add(modelFile);
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
                        result.Add(GetFile(filesDir, workDict[fileName].FileName)); //workDict[fileName].FileName вместо fileName для восстановления заглавных
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
                FoldersTree = foldersTree,
                TotalSize = totalSize, 
            };
        }

        private ModelFileInfo GetFile(string rootDir, string fileName)
        {
            var newFile = new ModelFileInfo() { FileName = fileName };
            var fullname = Path.Combine(rootDir, fileName);
            newFile.Hash = File.ReadAllBytes(fullname);
            newFile.Size = newFile.Hash.Length;
            return newFile;
        }

        private long GetFileSize(string rootDir, string fileName)
        {
            var fullname = Path.Combine(rootDir, fileName);
            return new FileInfo(fullname).Length;
        }
    }
}
