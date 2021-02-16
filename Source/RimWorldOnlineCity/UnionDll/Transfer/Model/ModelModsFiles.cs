using System;
using System.Collections.Generic;
using System.ComponentModel;

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

        /// <summary>
        /// Какой объем остался к отправке, без текущего пакета
        /// </summary>
        public long TotalSize { get; set; }
    }

    [Serializable]
    public enum FolderType
    {
        [Description("Mods folder")]
        ModsFolder,
        [Description("Configs folder")]
        ModsConfigPath,
    }
}
