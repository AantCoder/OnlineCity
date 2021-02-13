using OCUnion;
using OCUnion.Common;
using OCUnion.Transfer.Model;
using System;
using System.Collections.Generic;

namespace RimWorldOnlineCity.ClientHashCheck
{
    public interface IClientFileChecker
    {
        FolderType FolderType { get; }
        List<ModelFileInfo> FilesHash { get; }
        string Folder { get; }
        void CalculateHash();        
    }

    public class ClientFileChecker : IClientFileChecker
    {

        public ClientFileChecker(FolderType folderType, string folderName)
        {
            FolderType = folderType;
            Folder = folderName;
        }

        public FolderType FolderType { get; }

        public Action<string, int> OnChangeFolderAction { get; set; }

        public List<ModelFileInfo> FilesHash { get; private set; }
        public string Folder { get; private set; }

        public void CalculateHash()
        {
            Loger.Log($"{FolderType.ToString()} {Folder}");
            FilesHash = FileChecker.GenerateHashFiles(Folder, OnChangeFolderAction);
        }

        public void RecalculateHash(List<string> lists)
        {
            FileChecker.ReHashFiles(FilesHash, Folder, lists);
        }
    }
}
