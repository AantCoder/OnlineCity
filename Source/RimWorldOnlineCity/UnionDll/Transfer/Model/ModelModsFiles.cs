using System;
using System.Collections.Generic;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class ModelModsFiles
    {
        public FolderType FolderType { get; set; }
        /// <summary>
        /// Дерево каталогов которое требуется восстановить
        /// </summary>
        public FoldersTree FoldersTree { get; set; }
        /// <summary>
        /// Файлы которые находятся в этих директориях
        /// </summary>
        public List<ModelFileInfo> Files { get; set; }
    }

    public enum FolderType
    {
        ModsFolder,
        SteamFolder,
        ModsConfigPath,
    }
}
