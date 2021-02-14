using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using System.IO;
using OCUnion.Transfer.Model;
using OCUnion.Transfer;
using System.Text;
using System;
using System.Threading.Tasks;

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

        public static readonly string[] IgnoredModFiles = new string[] { ".cs", ".csproj", ".sln", ".gitignore", ".gitattributes" };

        public static readonly string[] IgnoredConfigFiles = { "KeyPrefs.xml", "Knowledge.xml", "LastPlayedVersion.txt", "Prefs.xml" };

        private class FastComputeHash
        {
            public Task<byte[]> ReadFile;

            public Task GetHash;

            public void Wait()
            {
                if (GetHash != null) GetHash.Wait();
            }
        }

        private static void getCheckSum(ModelFileInfo mfi, string fileName, FastComputeHash computeHash)
        {
            if (computeHash.ReadFile != null) computeHash.ReadFile.Wait();

            computeHash.ReadFile = Task.Run(() =>
            {
                var fileData = File.ReadAllBytes(fileName);
                mfi.Size = fileData.Length;
                return fileData;
            });
            computeHash.GetHash = computeHash.ReadFile.ContinueWith((task) =>
            {
                var sha = SHA512.Create();
                mfi.Hash = sha.ComputeHash(task.Result);
            });

            /*
            var sha = SHA512.Create();
            using (var fs = new FileStream(fileName, FileMode.Open))
            {
                return sha.ComputeHash(fileData);
            }
            */
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

        /// <summary>
        /// Generate hash for all files contains in root folder 
        /// </summary>
        /// <param name="rootFolder">can be null, then calc hash for all folder </param>
        /// <param name="checkFolders"></param>
        /// <param name="onStartUpdateFolder"> string : </param>
        /// <returns></returns>
        public static List<ModelFileInfo> GenerateHashFiles(string rootFolder, Action<string, int> onStartUpdateFolder)
        {
            var result = new List<ModelFileInfo>();

            if (string.IsNullOrEmpty(rootFolder) || !Directory.Exists(rootFolder))
            {
                Loger.Log($"Directory not found {rootFolder}");
                return result;
            }

            generateHashFiles(result, ref rootFolder, rootFolder, true);

            var checkFolders = Directory.GetDirectories(rootFolder);

            // Файл который есть у клиента не найден, проверяем Первую директорию    
            for (var i = 0; i < checkFolders.Length; i++)
            {
                var folder = checkFolders[i];
                onStartUpdateFolder?.Invoke(folder, i);
#if DEBUG
                var di = new DirectoryInfo(folder);
                if ("OnlineCity".Equals(di.Name))
                    continue;
#endif

                var checkFolder = Path.Combine(rootFolder, folder);
                if (Directory.Exists(checkFolder))
                {
                    generateHashFiles(result, ref rootFolder, checkFolder);
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
        private static void generateHashFiles(List<ModelFileInfo> result, ref string rootFolder, string folder, bool level0 = false)
        {
            if (!level0)
            {
                var dirs = Directory.GetDirectories(folder);
                foreach (var subDir in dirs)
                {
                    generateHashFiles(result, ref rootFolder, subDir);
                }
            }

            var files = Directory.GetFiles(folder);
            int fileNamePos = rootFolder.Length + 1;
            var computeHash = new FastComputeHash();
            foreach (var file in files.Where(x => ApproveExt(x)))
            {
                var mfi = new ModelFileInfo()
                {
                    FileName = file.Substring(fileNamePos)
                };
                getCheckSum(mfi, file, computeHash);
#if DEBUG
                /*
                if (mfi.FileName.Contains("armony"))
                {
                    Loger.Log($"generateHashFiles Harmony: {FileName} {Hash}");
                }*/
#endif
                result.Add(mfi);
            }
            computeHash.Wait();
        }

        public static void ReHashFiles(List<ModelFileInfo> rep, string folder, List<string> fileNames)
        {
            var dir = rep.ToDictionary(f => f.FileName);
            var computeHash = new FastComputeHash();
            foreach (var fileName in fileNames)
            {
                ModelFileInfo mfi;
                if (!dir.TryGetValue(fileName, out mfi))
                {
                    mfi = new ModelFileInfo()
                    {
                        FileName = fileName,
                    };
                    rep.Add(mfi);
                }
                var file = Path.Combine(folder, fileName);
                var oldHash = mfi.Hash;

                //if (MainHelper.DebugMode) Loger.Log($"ReHashFile b {file} {(oldHash == null ? "" : Convert.ToBase64String(oldHash))}" + $"->{(mfi.Hash == null ? "" : Convert.ToBase64String(mfi.Hash))}");

                getCheckSum(mfi, file, computeHash);

                //if (MainHelper.DebugMode) Loger.Log($"ReHashFile e {file} {(oldHash == null ? "" : Convert.ToBase64String(oldHash))}" + $"->{(mfi.Hash == null ? "" : Convert.ToBase64String(mfi.Hash))}");
            }
            computeHash.Wait();
        }

        private static bool ApproveExt(string fileName)
        {
            foreach (var ext in IgnoredModFiles)
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