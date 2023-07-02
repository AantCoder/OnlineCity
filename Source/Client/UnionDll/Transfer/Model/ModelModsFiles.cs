using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class ModelModsFilesRequest : ISendable
    {
        [Serializable]
        public class FileQuery
        {
            public FolderType SourceDirectory { get; set; }
            public ulong ModId { get; set; }
            public string RelativePath { get; set; }

        }
        public enum RequestType
        {
            HashInfo = 0,
            FileData,
        }
        public PackageType PackageType => PackageType.Request35ListFiles;

        public RequestType Type { get; set; }
        public List<FileQuery> FileQueries { get; set; }
    }

    [Serializable]
    public class ModelModsFilesResponse : ISendable
    {
        [Serializable]
        public class HashInfo
        {
            /// <summary>
            /// Файлы которые находятся в этих директориях
            /// </summary>
            public List<ModelFileInfo> Files { get; set; }

            public List<IgnorePattern> FolderIgnores { get; set; }
            public List<IgnorePattern> ExtensionIgnores { get; set; }
        }

        [Serializable]
        public class FileEntry
        {
            public FolderType SourceDirectory { get; set; }
            public ulong ModId { get; set; }
            public string RelativePath { get; set; }
            public byte[] GZippedData { get; set; }
        }

        [Serializable]
        public class FileData
        {
            public List<FileEntry> Entries { get; set; }
        }

        public PackageType PackageType => PackageType.Response36ListFiles;

        public HashInfo Hashes { get; set; }
        public FileData Contents { get; set; }
    }

    [Serializable]
    public class IgnorePattern
    {
        public FolderType FolderType { get; set; }
        public string Pattern { get; set; }
    }


    [Serializable]
    public class FolderCheck
    {
        public FolderType FolderType { get; set; }

        /// <summary>
        /// Полный путь на сервере
        /// </summary>
        public string ServerPath { get; set; }

        /// <summary>
        /// Можно ли заменить файл по содержимому с сервера
        /// </summary>
        public bool NeedReplace { get; set; }

        /// <summary>
        /// Если задано, то в Path путь к XML файлу, у которого содержимое заданых тэгов не сравнивается
        /// </summary>
        public List<string> IgnoreTag { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string XMLFileName { get; set; }

        /// <summary>
        /// Игнорировать файлы и подпапки с указанным именем
        /// </summary>
        public List<string> IgnoreFile { get; set; }


        public List<string> IgnoreFolder { get; set; }

    }

    [Serializable]
    public enum FolderType
    {
        [Description("Configs folder")]
        ModsConfigPath,
        [Description("Game folder")]
        GamePath,
        [Description("Mods folder")]
        ModsFolder,
    }
}
