using OCUnion;
using OCUnion.Common;
using OCUnion.Transfer.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RimWorldOnlineCity.ClientHashCheck
{
    public class ClientFileChecker
    {

        public FolderCheck Folder { get; }

        public string FolderPath { get; }

        public Action<string, int> OnChangeFolderAction { get; set; }

        public List<ModelFileInfo> FilesHash { get; private set; }

        public bool Complete { get; private set; }

        public ClientFileChecker(FolderCheck folder, string folderPath)
        {
            Folder = folder;
            FolderPath = folderPath.NormalizePath();
        }

        public void CalculateHash()
        {
            Loger.Log($"CalculateHash {FolderPath}");
            var filesHash = FileChecker.GenerateHashFiles(FolderPath, OnChangeFolderAction
                , Folder.FolderType != FolderType.GamePath ? new List<string>() : new List<string>() { "mods" });
            if (Folder.FolderType != FolderType.GamePath) FilesHash = filesHash;
            else
            {
                var ignorePath = "mods\\".NormalizePath();
                FilesHash = filesHash.Where(mfi => !mfi.FileName.StartsWith(ignorePath) && !mfi.FileName.Contains(("\\" + ignorePath).NormalizePath())).ToList();
            }
            Complete = true;
        }

        public void RecalculateHash(List<string> lists)
        {
            FileChecker.ReHashFiles(FilesHash, FolderPath, lists);
        }
    }
}
