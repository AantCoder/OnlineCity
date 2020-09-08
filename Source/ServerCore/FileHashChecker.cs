using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServerCore.Model;
using OCUnion.Transfer.Model;
using OCUnion.Common;
using OCUnion.Transfer;
using OCUnion.Transfer.Types;
using OCUnion;

namespace ServerOnlineCity

{
    public class FileHashChecker
    {
        /// <summary>
        /// Item1 Server Directory 
        /// Item2 Folder Tree
        /// hash Files in this dir
        /// </summary>
        public IReadOnlyDictionary<FolderType, Tuple<string, FoldersTree, IReadOnlyDictionary<string, ModelFileInfo>>> CheckedDirAndFiles { get; private set; }

        public IReadOnlyDictionary<FolderType, ApproveLoadWorldReason> ApproveWorldType { get; private set; }

        public FileHashChecker(ServerSettings serverSettings)
        {
            if (!serverSettings.IsModsWhitelisted)
            {
                return;
            }

            serverSettings.SteamWorkShopModsDir = Environment.CurrentDirectory;

            // 2. Создаем файлы со списком разрешенных папок, которые отправим клиенту
            var modsFolders = new ModelFileInfo() // 0 
            {
                FileName = "ApprovedMods.txt",
                Hash = FileChecker.CreateListFolder(serverSettings.ModsDirectory)
            };
            var steamFolders = new ModelFileInfo() // 1 
            {
                FileName = "ApprovedSteamWorkShop.txt",
                Hash = FileChecker.CreateListFolder(serverSettings.SteamWorkShopModsDir)
            };

            // index: 0 - list Folders in Mods dir, 1 -list Folders in Steam dir          

            serverSettings.AppovedFolderAndConfig = new ModelModsFiles()
            {
                Files = new List<ModelFileInfo>()
                {
                    modsFolders,
                    steamFolders,
                }
            };

            setupApproveWorldType(serverSettings);

            var result = new Dictionary<FolderType, Tuple<string, FoldersTree, IReadOnlyDictionary<string, ModelFileInfo>>>();

            var folderTree = FoldersTree.GenerateTree(serverSettings.ModsDirectory);
            result.Add(FolderType.ModsFolder, Tuple.Create(serverSettings.ModsDirectory, folderTree, getModFiles(serverSettings)));

            folderTree = FoldersTree.GenerateTree(serverSettings.SteamWorkShopModsDir);
            result.Add(FolderType.SteamFolder, Tuple.Create(serverSettings.SteamWorkShopModsDir, folderTree, getSteamFiles(serverSettings)));

            folderTree = new FoldersTree();
            result.Add(FolderType.ModsConfigPath, Tuple.Create(serverSettings.ModsConfigsDirectoryPath, folderTree, getModsConfigFiles(serverSettings)));

            CheckedDirAndFiles = result;
        }

        private IReadOnlyDictionary<string, ModelFileInfo> getModFiles(ServerSettings serverSettings)
        {
            var files = FileChecker.GenerateHashFiles(serverSettings.ModsDirectory, Directory.GetDirectories(serverSettings.ModsDirectory));
            Loger.Log($"Hashed {files.Count} files from ModsDirectory={serverSettings.ModsDirectory}");
            return files.ToDictionary(f => f.FileName);
        }

        private IReadOnlyDictionary<string, ModelFileInfo> getSteamFiles(ServerSettings serverSettings)
        {
            // Steam folder not check
            var files = FileChecker.GenerateHashFiles(serverSettings.SteamWorkShopModsDir, new string[0]); //Directory.GetDirectories(serverSettings.SteamWorkShopModsDir)
            return files.ToDictionary(f => f.FileName);
        }

        private IReadOnlyDictionary<string, ModelFileInfo> getModsConfigFiles(ServerSettings serverSettings)
        {
            var dir = new DirectoryInfo(serverSettings.ModsConfigsDirectoryPath);

            var parent = dir.Parent.FullName;
            var name = dir.Name;

            var files = FileChecker.GenerateHashFiles(parent, new string[] { name })
                .Where(x => !serverSettings.IgnoredLocalConfigFiles.Contains(new FileInfo(x.FileName).Name));

            foreach (var f in files)
            {
                f.FileName = new FileInfo(f.FileName).Name;
            }

            var res = files.ToDictionary(f => f.FileName);
            Loger.Log($"Hashed {res.Count} files from ModsConfigsDirectoryPath={serverSettings.ModsConfigsDirectoryPath}");

            return res;
        }

        private void setupApproveWorldType(ServerSettings serverSettings)
        {
            var approveWorldType = new Dictionary<FolderType, ApproveLoadWorldReason>()
            {
                { FolderType.ModsFolder, ApproveLoadWorldReason.ModsFilesFail  },
                { FolderType.SteamFolder, ApproveLoadWorldReason.ModsSteamWorkShopFail },
                { FolderType.ModsConfigPath, ApproveLoadWorldReason.NotAllConfigsOnClient},
            };

            ApproveWorldType = approveWorldType;
        }
    }
}
