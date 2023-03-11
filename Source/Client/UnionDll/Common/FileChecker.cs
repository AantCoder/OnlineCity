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

        public static readonly List<string> IgnoredModFiles = new List<string>() 
            { ".cs", ".csproj", ".sln", ".gitignore", ".gitattributes" };

        public static readonly List<string> IgnoredModFolders = new List<string>()
            { "bin", "obj", ".vs" };

        public static readonly List<string> IgnoredConfigFiles = new List<string>() 
            { "KeyPrefs.xml", "Knowledge.xml", "LastPlayedVersion.txt", "Prefs.xml" };

        private class FastComputeHash
        {
            public Task<byte[]> ReadFile;

            public Task GetHash;

            public void Wait()
            {
                if (GetHash != null) GetHash.Wait();
            }
        }

        public static string GetCheckSum(byte[] data)
        {
            var sha = SHA512.Create();
            return Convert.ToBase64String(sha.ComputeHash(data));
        }

        public static string GetCheckSum(string data)
        {
            return GetCheckSum(Encoding.UTF8.GetBytes(data));
        }

        private static void GetCheckSum(ModelFileInfo mfi, string fileName, FastComputeHash computeHash)
        {
            try
            {
                if (computeHash.ReadFile != null) computeHash.ReadFile.Wait();

                computeHash.ReadFile = Task.Run(() =>
                {
                    try
                    {
                        if (!File.Exists(fileName)) return null;
                        var fileData = File.ReadAllBytes(fileName);
                        mfi.Size = fileData.Length;
                        return fileData;
                    }
                    catch (Exception exp)
                    {
                        ExceptionUtil.ExceptionLog(exp, "GetCheckSum 2 " + fileName);
                    }
                    return null;
                });
                computeHash.GetHash = computeHash.ReadFile.ContinueWith((task) =>
                {
                    try
                    {
                        if (task.Result == null)
                        {
                            mfi.Hash = null;
                            return;
                        }
                        var sha = SHA512.Create();
                        mfi.Hash = sha.ComputeHash(task.Result);
                    }
                    catch(Exception exp)
                    {
                        ExceptionUtil.ExceptionLog(exp, "GetCheckSum 3 " + fileName);
                    }
                });

                /*
                var sha = SHA512.Create();
                using (var fs = new FileStream(fileName, FileMode.Open))
                {
                    return sha.ComputeHash(fileData);
                }
                */
            }
            catch(Exception exp)
            {
                ExceptionUtil.ExceptionLog(exp, "GetCheckSum 1 " + fileName);
            }
        }

        public static void FileSynchronization(string modsDir, ModelModsFilesResponse serverFiles)
        {
            restoreFolderTree(modsDir, serverFiles.FoldersTree);

            foreach (var serverFile in serverFiles.Files)
            {
                //запрет на синхронизацию, только сравнить
                if (!serverFile.NeedReplace) continue;

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

        private static int nnnn = 0;
        private static ModelFileInfo GenerateHashXMLString(string XML, List<string> ignoreTag)
        {
            for (int i = 0; i < ignoreTag.Count; i++)
            {
                var item = ignoreTag[i];
                if (item.StartsWith("{lineWith}", StringComparison.InvariantCultureIgnoreCase))
                {
                    // Удаляем всю строку, если в ней есть текст после ключевого слова {lineWith}
                    item = item.Substring("{lineWith}".Length);
                    if (item.Length > 0)
                    {
                        int pos;
                        while ((pos = XML.IndexOf(item)) >= 0)
                        {
                            var posB = XML.LastIndexOf("\n", pos);
                            if (posB < 0) posB = 0;
                            else posB++;

                            var posE = XML.IndexOf("\n", pos + item.Length);
                            if (posE >= 0)
                            {
                                //Удаляем перенос строки в конце найденной
                                posE++;
                            }
                            else
                            {
                                posE = XML.Length;
                                //Если строка последняя в файле удаляем перенос строки до найденной
                                if (posB > 0)
                                { 
                                    posB--;
                                    if (posB > 0 && XML[posB] == '\r')
                                        posB--;
                                }
                            }
                            XML = XML.Remove(posB, posE - posB);
                        }
                            XML = GameXMLUtils.ReplaceByTag(XML, ignoreTag[i], "");
                    }
                }
                else
                {
                    XML = GameXMLUtils.ReplaceByTag(XML, item, "");
                }
            }
            var xb = Encoding.UTF8.GetBytes(XML);
            //if (XML.StartsWith("п»ї")) XML = XML.Substring(3);
            if (xb.Length > 3 && xb[0] == 0xEF && xb[1] == 0xBB && xb[2] == 0xBF)
            {
                var xb0 = new byte[xb.Length - 3];
                Array.Copy(xb, 3, xb0, 0, xb0.Length);
                xb = xb0;
            }
            if (MainHelper.DebugMode) File.WriteAllText(Loger.PathLog + "GenerateHashXMLString" + nnnn++ + ".txt", XML);

            //Loger.Log("GenerateHashXMLString " + xb.Length);
            var result = new ModelFileInfo();
            var sha = SHA512.Create();
            result.Hash = sha.ComputeHash(xb);
            result.FileName = "";

            return result;
        }

        public static ModelFileInfo GenerateHashXML(byte[] XMLByte, List<string> ignoreTag)
        {
            var XML = Encoding.UTF8.GetString(XMLByte);

            return GenerateHashXMLString(XML, ignoreTag);
        }

        public static ModelFileInfo GenerateHashXML(string XMLFileName, List<string> ignoreTag)
        {
            if (!File.Exists(XMLFileName)) return null;

            var XML = File.ReadAllText(XMLFileName, Encoding.UTF8);

            return GenerateHashXMLString(XML, ignoreTag);
        }

        public static bool IsIgnoreFolder(string path, List<string> ignoreFolder)
        {
            if (ignoreFolder == null) return false;
            path = "\\" + path.ToLower() + "\\";
            return ignoreFolder.Any(f => path.Contains("\\" + f.ToLower() + "\\"));
        }

        /// <summary>
        /// Generate hash for all files contains in root folder 
        /// </summary>
        /// <param name="rootFolder">can be null, then calc hash for all folder </param>
        /// <param name="checkFolders"></param>
        /// <param name="onStartUpdateFolder"> string : </param>
        /// <returns></returns>
        public static List<ModelFileInfo> GenerateHashFiles(string rootFolder, Action<string, int> onStartUpdateFolder, List<string> ignoreFolder)
        {
            var result = new List<ModelFileInfo>();

            if (string.IsNullOrEmpty(rootFolder) || !Directory.Exists(rootFolder))
            {
                Loger.Log($"Directory not found {rootFolder}", Loger.LogLevel.ERROR);
                return result;
            }

            generateHashFiles(result, ref rootFolder, rootFolder, ignoreFolder, true);

            var checkFolders = Directory.GetDirectories(rootFolder);

            // Файл который есть у клиента не найден, проверяем Первую директорию    
            for (var i = 0; i < checkFolders.Length; i++)
            {
                var folder = checkFolders[i];
                if (IsIgnoreFolder(folder, ignoreFolder)) continue;
                onStartUpdateFolder?.Invoke(folder, i);
#if DEBUG
                var di = new DirectoryInfo(folder);
                if ("OnlineCity".Equals(di.Name))
                    continue;
#endif

                var checkFolder = Path.IsPathRooted(rootFolder) ? Path.Combine(rootFolder, folder) : folder;
                if (Directory.Exists(checkFolder))
                {
                    generateHashFiles(result, ref rootFolder, checkFolder, ignoreFolder);
                }
                else
                {
                    Loger.Log($"Directory not found {checkFolder}", Loger.LogLevel.ERROR);
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
        private static void generateHashFiles(List<ModelFileInfo> result, ref string rootFolder, string folder, List<string> ignoreFolder, bool level0 = false)
        {
            if (!level0)
            {
                var dirs = Directory.GetDirectories(folder);
                foreach (var subDir in dirs)
                {
                    if (IsIgnoreFolder(subDir, ignoreFolder)) continue;
                    generateHashFiles(result, ref rootFolder, subDir, ignoreFolder);
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
                GetCheckSum(mfi, file, computeHash);
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

                //if (MainHelper.DebugMode)  
                Loger.Log($"ReHashFile b {file} {(oldHash == null ? "" : Convert.ToBase64String(oldHash).Substring(0, 6))}" 
                    + $"->{(mfi.Hash == null ? "" : Convert.ToBase64String(mfi.Hash).Substring(0, 6))}");

                GetCheckSum(mfi, file, computeHash);

                //if (MainHelper.DebugMode)  
                Loger.Log($"ReHashFile e {file} {(oldHash == null ? "" : Convert.ToBase64String(oldHash).Substring(0, 6))}" 
                    + $"->{(mfi.Hash == null ? "" : Convert.ToBase64String(mfi.Hash).Substring(0, 6))}");
            }
            computeHash.Wait();

            for (int i = 0; i < rep.Count; i++)
            {
                if (rep[i].Hash == null)
                {
                    rep.RemoveAt(i--);
                }
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
        /*
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
        */
    }
}