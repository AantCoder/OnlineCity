using System;
using System.Collections.Generic;
using System.IO;

namespace OCUnion.Transfer
{
    [Serializable]
    public class FoldersTree
    {
        public string directoryName;
        public List<FoldersTree> SubDirs;

        public static FoldersTree GenerateTree(string rootFolder)
        {
            // time work on my SSD ~ 350 мс
            return generateSubTree(rootFolder);
        }

        private static FoldersTree generateSubTree(string rootFolder)
        {
            var di = new DirectoryInfo(rootFolder);

            var result = new FoldersTree() { directoryName = di.Name, SubDirs = new List<FoldersTree>() };
            foreach (var dir in Directory.GetDirectories(rootFolder))
            {
                // var dirName = Path.Combine(rootFolder, dir);
                var subTree = generateSubTree(dir);
                result.SubDirs.Add(subTree);
            }

            return result;
        }

        public static void ReCreateeTree(string rootFolder, FoldersTree folderTree)
        {
            foreach (var subFolder in folderTree.SubDirs)
            {
                // создаем под директорию 
                var fullNameSubDir = Path.Combine(rootFolder, subFolder.directoryName);
                if (!Directory.Exists(fullNameSubDir.Replace("\\", "" + Path.DirectorySeparatorChar)))
                {
                    Directory.CreateDirectory(fullNameSubDir.Replace("\\", "" + Path.DirectorySeparatorChar));
                }

                // для каждой созданной диретории создаем её поддиреторию 
                foreach (var subTree in subFolder.SubDirs)
                {
                    ReCreateeTree(fullNameSubDir, subTree);
                }
            }
        }
    }
}
