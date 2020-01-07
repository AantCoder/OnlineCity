using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using System.IO;
using OCUnion.Transfer.Model;
using OCUnion.Transfer;
using System.Text;
using System;

namespace OCUnion.Common
{
    public static class FileChecker
    {
        // 1. Проверяет все моды в папке Mods 
        // 2. Проверяет все моды в папке SteamWorkShop
        // формирует таблицу вида: Имя файла | контрольная сумма.
        // на серверной и клиентской стороне
        // затем на серверной стороне формируется в ответ список: файл , массив байт. если файл подлежит замене и HashSize = 0  - файл подлежит удалению
        // также в ответ отправляется список модов и порядок включения 

        /// <summary>
        /// Расширения файлов исключаемые из проверки
        /// </summary>
        public static readonly string[] ExcludedExternals = new string[] { ".cs", ".csproj", ".sln", ".md", ".gitignore", ".gitattributes" }; // , ".png"

        public static byte[] GetCheckSum(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return new byte[0];
            }

            return getCheckSum(fileName);
        }

        private static byte[] getCheckSum(string fileName)
        {
            using (var fs = new FileStream(fileName, FileMode.Open))
            {
                var sha = SHA512.Create();
                return sha.ComputeHash(fs);
            }
        }

        public static byte[] GetCheckSum(byte[] file)
        {
            var sha = SHA512.Create();
            return sha.ComputeHash(file);
        }

        public static void FileSynchronization(string modsDir, ModelModsFiles serverFiles)
        {
            restoreFolderTree(modsDir, serverFiles.FoldersTree);

            foreach (var serverFile in serverFiles.Files)
            {
                var fullName = Path.Combine(modsDir, serverFile.FileName);
                // Имя присутствует в списке, файл необходимо будет удалить ( или заменить)
                if (File.Exists(fullName))
                {
                    File.Delete(fullName);
                }

                if (serverFile.Hash == null)
                {
                    continue;
                }


                // Create the file, or overwrite if the file must exist.
                using (FileStream fs = File.Create(fullName))
                {
                    Loger.Log("Restore: " + fullName);

                    if (serverFile.Hash.Length > 0)
                    {
                        fs.Write(serverFile.Hash, 0, serverFile.Hash.Length);
                    }
                }
            }
        }

        public static List<ModelFileInfo> GenerateHashFiles(string rootFolder, string[] checkFolders)
        {
            var result = new List<ModelFileInfo>();

            if (string.IsNullOrEmpty(rootFolder) || !Directory.Exists(rootFolder))
            {
                Loger.Log($"Directory not found {rootFolder}");
                return result;
            }

            // Файл который есть у клиента не найден, проверяем Первую директорию    
            foreach (var folder in checkFolders)
            {

#if DEBUG
                var di = new DirectoryInfo(folder);
                if ("OnlineCity".Equals(di.Name))
                    continue;
#endif
                var checkFolder = Path.Combine(rootFolder, folder);
                if (Directory.Exists(checkFolder))
                {
                    result.AddRange(generateHashFiles(ref rootFolder, checkFolder));

                }
                else
                {
                    Loger.Log($"Directory not found {checkFolder}");
                }
            }

            return result;
        }

        private static void restoreFolderTree(string modsDir, FoldersTree foldersTree)
        {
            if (foldersTree.SubDirs == null)
            {
                return;
            }

            foreach (var folder in foldersTree.SubDirs)
            {
                var dirName = Path.Combine(modsDir, folder.directoryName);
                if (!Directory.Exists(dirName))
                {
                    Loger.Log($"Create directory: {dirName}");
                    Directory.CreateDirectory(dirName);
                }

                // check and create subdirs 
                restoreFolderTree(dirName, folder);
            }
        }

        /// <summary>
        /// Check hash mods 
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="isSteam"></param>
        /// <returns></returns>
        private static List<ModelFileInfo> generateHashFiles(ref string rootFolder, string folder)
        {
            var result = new List<ModelFileInfo>();
            var dirs = Directory.GetDirectories(folder);
            var files = Directory.GetFiles(folder);
            int fileNamePos = rootFolder.Length + 1;

            foreach (var subDir in dirs)
            {
                result.AddRange(generateHashFiles(ref rootFolder, subDir));
            }

            foreach (var file in files.Where(x => ApproveExt(x)))
            {
                result.Add(new ModelFileInfo()
                {
                    FileName = file.Substring(fileNamePos),
                    Hash = getCheckSum(file)
                }
                );
            }

            return result;
        }

        private static bool ApproveExt(string fileName)
        {
            foreach (var ext in ExcludedExternals)
            {
                if (fileName.EndsWith(ext))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Create txt file contains directory list as byte array 
        /// Создает текстовый файл содержащий список разрешенных директорий в виде массива байт для отправки
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static byte[] CreateListFolder(string directory)
        {
            var dirs = Directory.GetDirectories(directory).OrderBy(x => x);
            var sb = new StringBuilder();
            foreach (var dir in dirs)
            {
                // только для дебага, а то папка Online city каждый раз обновляется

                var di = new DirectoryInfo(dir);
#if DEBUG
                if (di.Name.Equals("OnlineCity"))
                    continue;
#endif
                sb.AppendLine(di.Name);
            }

            var txt = sb.ToString();
            var diRoot = new DirectoryInfo(directory);
            File.WriteAllText(Path.Combine(Loger.PathLog, diRoot.Name + ".txt"), txt);
            return Encoding.ASCII.GetBytes(txt);
        }
    }
}