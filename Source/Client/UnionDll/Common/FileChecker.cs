using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using System.IO;
using OCUnion.Transfer.Model;
using OCUnion.Transfer;
using System.Text;
using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

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
            { ".cs", ".csproj", ".sln", ".gitignore", ".gitattributes", ".DS_Store"};

        public static readonly List<string> IgnoredModFolders = new List<string>()
            { "bin", "obj", ".vs" };

        public static readonly List<string> IgnoredConfigFiles = new List<string>() 
            { "KeyPrefs.xml", "Knowledge.xml", "LastPlayedVersion.txt", "Prefs.xml", ".DS_Store"};

        public class FolderSummary
        {
            public IEnumerable<ModelFileInfo> Files { get; private set; }
            public FolderType SourceDirectory { get; private set; }
            public ulong ModId { get; private set; }
            public byte[] Hash { get; private set; }

            public FolderSummary(FolderType sourceDirectory, ulong modId, IEnumerable<ModelFileInfo> files)
            {
                if (sourceDirectory != FolderType.ModsFolder && modId != 0)
                    throw new ArgumentException("ModID must be 0 when summarizing a non-mod folder.");

                SourceDirectory = sourceDirectory;
                Files = files.OrderBy(s => s).ToArray();
                ModId = modId;

                var mem = new MemoryStream(files.Count() * 64); // SHA512 hash is 64 bytes
                foreach (var f in Files)
                    mem.Write(f.Hash, 0, f.Hash.Length);
                var sha = SHA512.Create();
                Hash = sha.ComputeHash(mem);
            }

            public static async Task<FolderSummary> FromFolder(string path, FolderType folderType, ulong modId, IEnumerable<string> ignoredFolders, IEnumerable<string> ignoredExtensions)
            {
                var hashTasks = ComputeHashRecursive(path, path, folderType, modId, ignoredFolders, ignoredExtensions);

                var infos = await Task.WhenAll(hashTasks.ToArray());
                return new FolderSummary(folderType, modId, infos);
            }

            public static IEnumerable<Tuple<string, Task<FolderSummary>>> HashModFolder(string modPath, IEnumerable<string> ignoredFolders, IEnumerable<string> ignoredExtensions)
            {
                if (!Directory.Exists(modPath))
                    throw new ArgumentException("modPath does not exist", nameof(modPath));

                foreach (var dir in Directory.GetDirectories(modPath))
                {
                    var aboutPath = Path.Combine(dir, "About", "PublishedFileId.txt");
                    if (!File.Exists(aboutPath)) continue;

                    string content = "";
                    using (var f = new StreamReader(File.OpenRead(aboutPath)))
                        content = f.ReadLine();

                    ulong id = 0;
                    if (!ulong.TryParse(content, out id))
                        throw new Exception("Mod at " + dir + " does not have a PublishedFileId.txt.");

                    var subfolder = Path.GetFileName(dir);

                    yield return new Tuple<string, Task<FolderSummary>>(subfolder, FromFolder(dir, FolderType.ModsFolder, id, ignoredFolders, ignoredExtensions));
                }
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

        public static Task<ModelFileInfo> ComputeHash(string path, string rootPath, FolderType sourceDirectory, ulong modId)
        {
            if (path is null)
                throw new ArgumentNullException(nameof(path));
            if (!rootPath.EndsWith("" + Path.DirectorySeparatorChar))
                rootPath += Path.DirectorySeparatorChar;
            if (!path.StartsWith(rootPath))
                throw new ArgumentException("path is not in the root path provided.", nameof(path));

            var sha = SHA512.Create();
            return Task.Factory.StartNew<ModelFileInfo>(() =>
            {
                using (var f = File.OpenRead(path))
                {
                    var result = new ModelFileInfo();
                    var relativePath = path.Substring(rootPath.Length);
                    result.RelativePath = relativePath.Replace("" + Path.DirectorySeparatorChar, "/"); // Use normal slash over network to allow linux and mac clients to work as well
                    result.Hash = sha.ComputeHash(f);
                    result.Size = f.Length;
                    result.ModId = modId;
                    result.SourceFolder = sourceDirectory;
                    return result;
                }
            }, TaskCreationOptions.LongRunning);
        }

        public static IEnumerable<Task<ModelFileInfo>> ComputeHashRecursive(string directory, string rootPath, FolderType sourceDirectory, ulong modId, IEnumerable<string> ignoredFolders, IEnumerable<string> ignoredExtensions)
        {
            if (directory is null)
                throw new ArgumentNullException(nameof(directory));
            if (!Directory.Exists(directory))
                throw new ArgumentException("Directory does not exist", nameof(directory));

            foreach (var file in Directory.GetFiles(directory).Where((f) => !ignoredExtensions.Any(e => f.EndsWith(e))))
                yield return ComputeHash(file, rootPath, sourceDirectory, modId);

            foreach (var childDir in Directory.GetDirectories(directory).Where((d) => !ignoredFolders.Contains(d)))
                foreach (var task in ComputeHashRecursive(childDir, rootPath, sourceDirectory, modId, ignoredFolders, ignoredExtensions))
                    yield return task;
        }
        
    }
}
