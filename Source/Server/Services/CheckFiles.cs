using System.Collections.Generic;
using System.IO;
using System.Linq;
using OCUnion;
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
            result.Packet = checkFiles((ModelModsFilesRequest)request.Packet, context);
            return result;
        }

        private ModelModsFilesResponse checkFiles(ModelModsFilesRequest packet, ServiceContext context)
        {
            if (!ServerManager.FileHashChecker.CheckedDirAndFiles.TryGetValue(
                packet.CodeRequest,
                out var checkedDirAndFile))
            {
                //отвечаем всё ок ничего не нужно синхронить
                return new ModelModsFilesResponse()
                {
                    Folder = new FolderCheck() { FolderType = packet.FolderType },
                    Files = new List<ModelFileInfo>(),
                    FoldersTree = new FoldersTree(),
                    TotalSize = 0
                };
            }

            var result = new List<ModelFileInfo>();

            if (packet.CodeRequest % 1000 == 0)
            {
                var allServerFiles = new HashSet<string>(checkedDirAndFile.HashFiles.Keys);
                var packetFiles = packet.Files != null ? packet.Files : new List<ModelFileInfo>(0);
                long packetSize = 0;
                long totalSize = 0;

                foreach (var modelFile in packetFiles)
                {
                    var modelFileFileName = modelFile.FileName.ToLower();
                    if (FileHashChecker.FileNameContainsIgnored(modelFileFileName, checkedDirAndFile.IgnoredFiles
                        , checkedDirAndFile.IgnoredFolder))
                    {
                        continue;
                    }

                    if (checkedDirAndFile.HashFiles.TryGetValue(modelFileFileName, out ModelFileInfo fileInfo))
                    {
                        allServerFiles.Remove(modelFileFileName); // 

                        if (!ModelFileInfo.UnsafeByteArraysEquale(modelFile.Hash, fileInfo.Hash))
                        {
                            // read file for send to Client      
                            // файл  найден, но хеши не совпадают, необходимо заменить файл
                            if (packetSize < MaxPacketSize)
                            {
                                var addFile = GetFile(checkedDirAndFile.Settings.ServerPath, fileInfo.FileName, checkedDirAndFile.Settings.NeedReplace);
                                result.Add(addFile);
                                packetSize += addFile.Size;
                                totalSize += addFile.Size;
                                //Loger.Log($"packetSize={packetSize} totalSize={totalSize}");
                            }
                            else
                            {
                                var size = GetFileSize(checkedDirAndFile.Settings.ServerPath, fileInfo.FileName);
                                totalSize += size;
                            }
                        }
                    }
                    else
                    {
                        // mark file for delete 
                        // Если файл с таким именем не найден, помечаем файл на удаление
                        modelFile.Hash = null;
                        modelFile.NeedReplace = checkedDirAndFile.Settings.NeedReplace;
                        result.Add(modelFile);
                    }
                }

                lock (context.Player)
                {
                    // проверяем в обратном порядке: что бы у клиенты были все файлы
                    if (allServerFiles.Any())
                    {
                        foreach (var fileName in allServerFiles)
                        {
                            if (FileHashChecker.FileNameContainsIgnored(fileName, checkedDirAndFile.IgnoredFiles
                                , checkedDirAndFile.IgnoredFolder))
                            {
                                continue;
                            }

                            context.Player.ApproveLoadWorldReason = false;

                            if (packetSize < MaxPacketSize)
                            {
                                var addFile = GetFile(checkedDirAndFile.Settings.ServerPath
                                    , checkedDirAndFile.HashFiles[fileName].FileName
                                    , checkedDirAndFile.Settings.NeedReplace); //workDict[fileName].FileName вместо fileName для восстановления заглавных
                                result.Add(addFile);
                                packetSize += addFile.Size;
                                totalSize += addFile.Size;
                                //Loger.Log($"packetSize={packetSize} totalSize={totalSize}");
                            }
                            else
                            {
                                var size = GetFileSize(checkedDirAndFile.Settings.ServerPath, checkedDirAndFile.HashFiles[fileName].FileName);
                                totalSize += size;
                            }
                        }
                    }

                    // Если файлы не прошли проверку, помечаем флагом, запрет загрузки мира
                    if (result.Any())
                    {
                        context.Player.ApproveLoadWorldReason = false;
                    }
                }

                return new ModelModsFilesResponse()
                {
                    Folder = checkedDirAndFile.Settings,
                    Files = result,
                    // микроптимизация: если файлы не будут восстанавливаться, не отправляем обратно список папок
                    // на восстановление ( десериализацию папок также тратится время)
                    FoldersTree = result.Any() ? checkedDirAndFile.FolderTree : new FoldersTree(),
                    TotalSize = totalSize,
                };
            }
            else
            {
                var addFile = GetFile(checkedDirAndFile.Settings.ServerPath, checkedDirAndFile.Settings.XMLFileName, true);
                addFile.NeedReplace = checkedDirAndFile.Settings.NeedReplace;
                result.Add(addFile);

                return new ModelModsFilesResponse()
                {
                    Folder = checkedDirAndFile.Settings,
                    Files = result,
                    FoldersTree = new FoldersTree(),
                    TotalSize = 0,
                    IgnoreTag = checkedDirAndFile.Settings.IgnoreTag
                };
            }
        }

        private ModelFileInfo GetFile(string rootDir, string fileName, bool needReplace)
        {
            var newFile = new ModelFileInfo() { FileName = fileName, NeedReplace = needReplace };
            if (needReplace)
            {
                var fullname = Path.Combine(rootDir, fileName);
                newFile.Hash = File.ReadAllBytes(fullname);
                newFile.Size = newFile.Hash.Length;
            }
            else
            {
                newFile.Size = GetFileSize(rootDir, fileName);
            }
            return newFile;
        }

        private long GetFileSize(string rootDir, string fileName)
        {
            var fullname = Path.Combine(rootDir, fileName);
            return new FileInfo(fullname).Length;
        }
    }
}
