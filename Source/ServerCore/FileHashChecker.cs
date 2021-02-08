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
        //TO DO: refatoring : Tuple содержит очень много элементов, сложно читать, разбить на класс с полями

        /// <summary>
        /// Item1 Server Directory 
        /// Item2 Folder Tree
        /// Item3 hash Files in this dir
        /// Item4 string [] IgrnoredFiles
        /// </summary>
        public IReadOnlyDictionary<FolderType,
            Tuple<string, FoldersTree, IReadOnlyDictionary<string, ModelFileInfo>, string[]>> CheckedDirAndFiles
        { get; private set; }

        public IReadOnlyDictionary<FolderType, ApproveLoadWorldReason> ApproveWorldType { get; private set; }

        public FileHashChecker(ServerSettings serverSettings)
        {
            if (!serverSettings.IsModsWhitelisted)
            {
                return;
            }

            setupApproveWorldType(serverSettings);

            var result = new Dictionary<FolderType, Tuple<string, FoldersTree, IReadOnlyDictionary<string, ModelFileInfo>, string[]>>();

            var folderTree = FoldersTree.GenerateTree(serverSettings.ModsDirectory);
            result.Add(FolderType.ModsFolder, Tuple.Create(serverSettings.ModsDirectory, folderTree, getModFiles(serverSettings), serverSettings.IgnoredLocalModFiles));

            folderTree = new FoldersTree();
            result.Add(FolderType.ModsConfigPath, Tuple.Create(serverSettings.ModsConfigsDirectoryPath, folderTree, getModsConfigFiles(serverSettings), serverSettings.IgnoredLocalConfigFiles));

            CheckedDirAndFiles = result;
        }

        private void writeFolderNameToServerLog(string folderName, int folderindex)
        {
            Loger.Log($"Check {folderName}");
        }

        private IReadOnlyDictionary<string, ModelFileInfo> getModsConfigFiles(ServerSettings serverSettings)
        {
            var checkedFiles = FileChecker.GenerateHashFiles(serverSettings.ModsConfigsDirectoryPath, writeFolderNameToServerLog);

            // TO DO : refactoring not caclulate hash for ignored files ( for faster start )
            // TO DO : refactoring: Объеденить методы getModsConfigFiles   getModFiles в один с передачей параметров
            var files = checkedFiles
                .Where(x => !FileNameContainsIgnored(new FileInfo(x.FileName).Name, serverSettings.IgnoredLocalConfigFiles));

            var res = files.ToDictionary(f => f.FileName);
            Loger.Log($"Hashed {res.Count} files from ModsConfigsDirectoryPath={serverSettings.ModsConfigsDirectoryPath}");

            return res;
        }

        public static bool FileNameContainsIgnored(string fileName, string[] ignored)
        {
            foreach (var ignoredFName in ignored)
            {
                if (fileName.EndsWith(ignoredFName))
                {
                    return true;
                }
            }

            return false;
        }

        private IReadOnlyDictionary<string, ModelFileInfo> getModFiles(ServerSettings serverSettings)
        {
            var checkedFiles = FileChecker.GenerateHashFiles(serverSettings.ModsDirectory, writeFolderNameToServerLog);
            var files = checkedFiles
                .Where(x => !FileNameContainsIgnored(new FileInfo(x.FileName).Name, serverSettings.IgnoredLocalModFiles));

            var res = files.ToDictionary(f => f.FileName);
            Loger.Log($"Hashed {res.Count} files from ModsDirectory={serverSettings.ModsDirectory}");
            return res;
        }

        private void setupApproveWorldType(ServerSettings serverSettings)
        {
            var approveWorldType = new Dictionary<FolderType, ApproveLoadWorldReason>()
            {
                { FolderType.ModsFolder, ApproveLoadWorldReason.ModsFilesFail  },
                { FolderType.ModsConfigPath, ApproveLoadWorldReason.NotAllConfigsOnClient},
            };

            ApproveWorldType = approveWorldType;
        }
    }
}
