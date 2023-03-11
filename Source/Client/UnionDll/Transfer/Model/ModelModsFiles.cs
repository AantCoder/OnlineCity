using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class ModelModsFilesRequest
    {
        /// <summary>
        /// Дерево каталогов которое требуется восстановить
        /// </summary>
        public FolderType FolderType { get; set; }

        /// <summary>
        /// После основного запроса на файлы из дериктории с 0, идут запросы на синхронизацию XML файлов
        /// </summary>
        public int NumberFileRequest { get; set; }

        public int CodeRequest => (int)FolderType * 1000 + NumberFileRequest;

        /// <summary>
        /// Файлы которые находятся в этих директориях
        /// </summary>
        public List<ModelFileInfo> Files { get; set; }
    }

    [Serializable]
    public class ModelModsFilesResponse
    {
        public FolderCheck Folder { get; set; }

        /// <summary>
        /// Дерево каталогов которое требуется восстановить
        /// </summary>
        public FoldersTree FoldersTree { get; set; }

        /// <summary>
        /// Файлы которые находятся в этих директориях
        /// </summary>
        public List<ModelFileInfo> Files { get; set; }

        /// <summary>
        /// Какой объем остался к отправке, без текущего пакета
        /// </summary>
        public long TotalSize { get; set; }

        /// <summary>
        /// Если задано, то в Path путь к XML файлу, у которого содержимое заданых тэгов не сравнивается
        /// </summary>
        public List<string> IgnoreTag { get; set; }
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
