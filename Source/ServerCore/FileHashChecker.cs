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
        public class CheckedDirAndFile
        {
            public string ServerDirectory { get; set; }
            public FoldersTree FolderTree { get; set; }
            public IReadOnlyDictionary<string, ModelFileInfo> HashFiles { get; set; }
            public string[] IgrnoredFiles { get; set; }
        }

        public IReadOnlyDictionary<FolderType, CheckedDirAndFile> CheckedDirAndFiles { get; private set; }

        public IReadOnlyDictionary<FolderType, ApproveLoadWorldReason> ApproveWorldType { get; private set; }

        public FileHashChecker(ServerSettings serverSettings)
        {
            if (!serverSettings.IsModsWhitelisted)
            {
                return;
            }

            setupApproveWorldType(serverSettings);

            var result = new Dictionary<FolderType, CheckedDirAndFile>();

            var folderTree = FoldersTree.GenerateTree(serverSettings.ModsDirectory);
            result.Add(FolderType.ModsFolder, new CheckedDirAndFile()
            {
                ServerDirectory = serverSettings.ModsDirectory,
                FolderTree = folderTree,
                HashFiles = getFiles(serverSettings.ModsDirectory, serverSettings.IgnoredLocalModFiles),
                IgrnoredFiles = serverSettings.IgnoredLocalModFiles,
            });

            folderTree = new FoldersTree();
            result.Add(FolderType.ModsConfigPath, new CheckedDirAndFile()
            {
                ServerDirectory = serverSettings.ModsConfigsDirectoryPath, 
                FolderTree = folderTree,
                HashFiles = getFiles(serverSettings.ModsConfigsDirectoryPath, serverSettings.IgnoredLocalConfigFiles),
                IgrnoredFiles = serverSettings.IgnoredLocalConfigFiles,
            });

            CheckedDirAndFiles = result;
        }

        private void writeFolderNameToServerLog(string folderName, int folderindex)
        {
            Loger.Log($"Check {folderName}");
        }

        private IReadOnlyDictionary<string, ModelFileInfo> getFiles(string directory, string[] ignores)
        {
            var checkedFiles = FileChecker.GenerateHashFiles(directory, writeFolderNameToServerLog);
            var files = checkedFiles
                .Where(x => !FileNameContainsIgnored(new FileInfo(x.FileName).Name, ignores));

            var res = files.ToDictionary(f => f.FileName.ToLower());
            Loger.Log($"Hashed {res.Count} files from ModsDirectory={directory}");
            return res;
        }

        public static bool FileNameContainsIgnored(string fileName, string[] ignored)
        {
            foreach (var ignoredFName in ignored)
            {
                if (fileName.ToLower().EndsWith(ignoredFName.ToLower()))
                {
                    return true;
                }
            }

            return false;
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
