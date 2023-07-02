using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServerCore.Model;
using OCUnion.Transfer.Model;
using OCUnion.Common;
using OCUnion.Transfer;
using OCUnion.Transfer.Types;
using OCUnion;
using System.Threading.Tasks;

namespace ServerOnlineCity
{
    
    public class FileHashChecker
    {
        private FolderCheck GameFolderInfo;
        public FileChecker.FolderSummary GameFolderSummary { get; private set; }
        public FolderCheck ConfigFolderInfo { get; private set; }
        public FileChecker.FolderSummary ConfigFolderSummary { get; private set; }
        public Dictionary<ulong, Tuple<FolderCheck, string, FileChecker.FolderSummary>> ModSummaries { get; private set; }

        public IEnumerable<ModelFileInfo> Files
        {
            get
            {
                if (GameFolderSummary != null)
                    foreach (var f in GameFolderSummary.Files)
                        yield return f;
                if (ConfigFolderSummary != null)
                    foreach (var f in ConfigFolderSummary.Files)
                        yield return f;

                if (ModSummaries != null)
                {
                    foreach (var modid in ModSummaries.Keys)
                        foreach (var f in ModSummaries[modid].Item3.Files)
                            yield return f;
                }
            }
        }

        public IEnumerable<IgnorePattern> ExtensionIgnores { get; private set; }
        public IEnumerable<IgnorePattern> FolderIgnores { get; private set; }

        public static async Task<FileHashChecker> FromServerSettings(ServerSettings serverSettings)
        {
            var result = new FileHashChecker();
            var extensionIgnores = new List<IgnorePattern>();
            var folderIgnores = new List<IgnorePattern>();

            if (serverSettings.EqualFiles.Any((f) => f.FolderType == FolderType.GamePath))
            {
                if (serverSettings.EqualFiles.Where((f) => f.FolderType == FolderType.GamePath).Count() > 1)
                    throw new Exception("No support for having multiple GamePath folders.");

                var folderInfo = serverSettings.EqualFiles.Where((f) => f.FolderType == FolderType.GamePath).First();
                result.GameFolderInfo = folderInfo;
                result.GameFolderSummary = await FileChecker.FolderSummary.FromFolder(folderInfo.ServerPath.Replace("\\", "" + Path.DirectorySeparatorChar), FolderType.GamePath, 0, folderInfo.IgnoreFolder ?? Enumerable.Empty<string>(), folderInfo.IgnoreFile ?? Enumerable.Empty<string>());

                folderIgnores.AddRange((folderInfo.IgnoreFolder ?? Enumerable.Empty<string>()).Select((t) => new IgnorePattern() { FolderType = FolderType.GamePath, Pattern = t }));
                extensionIgnores.AddRange((folderInfo.IgnoreFile ?? Enumerable.Empty<string>()).Select((t) => new IgnorePattern() { FolderType = FolderType.GamePath, Pattern = t }));
            }
            if (serverSettings.EqualFiles.Any((f) => f.FolderType == FolderType.ModsConfigPath))
            {
                if (serverSettings.EqualFiles.Where((f) => f.FolderType == FolderType.ModsConfigPath).Count() > 1)
                    throw new Exception("No support for having multiple ModsConfigPath folders.");

                var folderInfo = serverSettings.EqualFiles.Where((f) => f.FolderType == FolderType.ModsConfigPath).First();
                result.ConfigFolderInfo = folderInfo;
                result.ConfigFolderSummary = await FileChecker.FolderSummary.FromFolder(folderInfo.ServerPath.Replace("\\", "" + Path.DirectorySeparatorChar), FolderType.ModsConfigPath, 0, folderInfo.IgnoreFolder ?? Enumerable.Empty<string>(), folderInfo.IgnoreFile ?? Enumerable.Empty<string>());

                folderIgnores.AddRange((folderInfo.IgnoreFolder ?? Enumerable.Empty<string>()).Select((t) => new IgnorePattern() { FolderType = FolderType.ModsConfigPath, Pattern = t }));
                extensionIgnores.AddRange((folderInfo.IgnoreFile ?? Enumerable.Empty<string>()).Select((t) => new IgnorePattern() { FolderType = FolderType.ModsConfigPath, Pattern = t }));
            }

            if (serverSettings.EqualFiles.Any((f) => f.FolderType == FolderType.ModsFolder))
            {
                result.ModSummaries = new Dictionary<ulong, Tuple<FolderCheck, string, FileChecker.FolderSummary>>();
                var modFolders = serverSettings.EqualFiles.Where((f) => f.FolderType == FolderType.ModsFolder);
                foreach (var modFolder in modFolders)
                {

                    var modTasks = FileChecker.FolderSummary.HashModFolder(modFolder.ServerPath.Replace("\\", "" + Path.DirectorySeparatorChar), modFolder.IgnoreFolder ?? Enumerable.Empty<string>(), modFolder.IgnoreFile ?? Enumerable.Empty<string>());
                    foreach (var t in modTasks)
                    {
                        Console.WriteLine("Hashing: {0}\\{1}", modFolder.ServerPath, t.Item1);
                        var summary = await t.Item2;

                        result.ModSummaries[summary.ModId] = new Tuple<FolderCheck, string, FileChecker.FolderSummary>(modFolder, Path.Combine(modFolder.ServerPath.Replace("\\", "" + Path.DirectorySeparatorChar), t.Item1), summary);
                    }

                    folderIgnores.AddRange((modFolder.IgnoreFolder ?? Enumerable.Empty<string>()).Select((t) => new IgnorePattern() { FolderType = FolderType.ModsFolder, Pattern = t }));
                    extensionIgnores.AddRange((modFolder.IgnoreFile ?? Enumerable.Empty<string>()).Select((t) => new IgnorePattern() { FolderType = FolderType.ModsFolder, Pattern = t }));
                }
            }
            result.ExtensionIgnores = extensionIgnores;
            result.FolderIgnores = folderIgnores;

            return result;
        }

    }
}
