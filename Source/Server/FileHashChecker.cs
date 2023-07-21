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
            public FolderCheck Settings { get; set; }
            public FoldersTree FolderTree { get; set; }
            public IReadOnlyDictionary<string, ModelFileInfo> HashFiles { get; set; }
            public List<string> IgnoredFiles { get; set; }
            public List<string> IgnoredFolder { get; set; }
        }

        /// <summary>
        /// key int: CodeRequest => (int)FolderType * 1000 + NumberFileRequest;
        /// </summary>
        public IReadOnlyDictionary<int, CheckedDirAndFile> CheckedDirAndFiles { get; private set; }
        public IReadOnlyDictionary<FolderType, List<CheckedDirAndFile>> CheckedXMLByFolderType { get; private set; }

        public FileHashChecker(ServerSettings serverSettings)
        {
            if (!serverSettings.IsModsWhitelisted)
            {
                return;
            }

            var result = new Dictionary<int, CheckedDirAndFile>();
            var resultXML = new Dictionary<FolderType, List<CheckedDirAndFile>>();

            foreach (var setting in serverSettings.EqualFiles)
            {
                var folderTree = FoldersTree.GenerateTree(setting.ServerPath.NormalizePath());

                if (setting.IgnoreTag != null && setting.IgnoreTag.Count > 0)
                {
                    continue;
                }
                else
                {
                    result.Add((int)setting.FolderType * 1000, new CheckedDirAndFile()
                    {
                        Settings = setting,
                        FolderTree = folderTree,
                        HashFiles = getFiles(setting.ServerPath.NormalizePath(), setting.IgnoreFile.NormalizePath(), setting.IgnoreFolder.NormalizePath()),
                        IgnoredFiles = setting.IgnoreFile.NormalizePath(),
                        IgnoredFolder = setting.IgnoreFolder.NormalizePath(),
                    });
                }
            }

            foreach (var setting in serverSettings.EqualFiles)
            {
                if (setting.IgnoreTag != null && setting.IgnoreTag.Count > 0)
                {
                    //Это не дериктория, а один XML файл
                    var ni = new CheckedDirAndFile()
                    {
                        Settings = setting,
                        //больше ничего не устанавилвается, т.к. брем отдельно из настроек
                    };
                    if (!resultXML.ContainsKey(setting.FolderType)) resultXML.Add(setting.FolderType, new List<CheckedDirAndFile>());
                    resultXML[setting.FolderType].Add(ni);
                    result.Add((int)setting.FolderType * 1000 + resultXML[setting.FolderType].Count, ni);
                    //убираем файл из списка стандартной синхронизации
                    if (result.ContainsKey((int)setting.FolderType * 1000))
                    {
                        result[(int)setting.FolderType * 1000].IgnoredFiles.Add(setting.XMLFileName.NormalizePath());
                    }
                }
                else
                {
                    continue;
                }
            }
            CheckedDirAndFiles = result;
            CheckedXMLByFolderType = resultXML;
        }

        private void writeFolderNameToServerLog(string folderName, int folderindex)
        {
            Loger.Log($"Check {folderName}");
        }

        private IReadOnlyDictionary<string, ModelFileInfo> getFiles(string directory, List<string> ignores, List<string> ignoreFolder)
        {
            var checkedFiles = FileChecker.GenerateHashFiles(directory, writeFolderNameToServerLog, ignoreFolder);
            var files = checkedFiles
                .Where(x => !FileNameContainsIgnored(new FileInfo(x.FileName).Name, ignores, ignoreFolder));

            var res = files.ToDictionary(f => f.FileName.ToLower());
            Loger.Log($"Hashed {res.Count} files from ModsDirectory={directory}");
            return res;
        }

        public static bool FileNameContainsIgnored(string fileName, List<string> ignored, List<string> ignoreFolder)
        {
            if (FileChecker.IsIgnoreFolder(fileName, ignoreFolder))
            {
                return true;
            }
            foreach (var ignoredFName in ignored)
            {
                if (fileName.ToLower().EndsWith(ignoredFName.ToLower()))
                {
                    return true;
                }
            }

            return false;
        }

    }
}
