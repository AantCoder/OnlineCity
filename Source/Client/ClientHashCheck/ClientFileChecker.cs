using OCUnion;
using OCUnion.Common;
using OCUnion.Transfer.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Verse;
using Verse.Steam;
using RimWorldOnlineCity;

namespace RimWorldOnlineCity.ClientHashCheck
{
    
    public class ClientFileChecker
    {
        public struct ModSummary
        {
            public string RootPath { get; set; }
            public bool IsEditable { get; set; }
            public FileChecker.FolderSummary Summary { get; set; }

            public string EditablePath
            {
                get
                {
                    if (IsEditable)
                        return RootPath;
                    return Path.Combine(GenFilePaths.ModsFolderPath, Summary.ModId.ToString());
                }
            }
        }

        public bool IsInitialized { get; private set; }
        public IEnumerable<IgnorePattern> FolderIgnores { get; private set; }
        public IEnumerable<IgnorePattern> ExtensionIgnores { get; private set; }


        public FileChecker.FolderSummary GameFolderSummary { get; private set; }
        public FileChecker.FolderSummary ConfigFolderSummary { get; private set; }
        private Dictionary<ulong, ModSummary> ModSummaries { get; set; }

        private HashSet<ModelFileInfo> _remoteHashes;
        private HashSet<ModelFileInfo> _localHashes;
        public IEnumerable<ChangeSet> ChangeSets { get; private set; }

        public class ChangeSet
        {
            public FolderType FolderType { get; private set; }
            public ulong ModId { get; private set; }

            public List<string> DeletePaths { get; private set; }
            public List<string> DownloadPaths { get; private set; }

            public string RemoveRoot { get; private set; }
            public string DownloadRoot { get; private set; }
            

            public ChangeSet(FolderType folderType, ulong modId, IEnumerable<string> deletePaths, IEnumerable<string> downloadPaths, string removeRoot, string downloadRoot)
            {
                FolderType = folderType;
                ModId = modId;
                DeletePaths = deletePaths.ToList();
                DownloadPaths = downloadPaths.ToList();
                RemoveRoot = removeRoot;
                DownloadRoot = downloadRoot;
            }

            public bool IsDangerous
            {
                get
                {
                    return DownloadPaths
                        .Where(s => s.EndsWith(".dll") || s.EndsWith(".exe"))
                        .Any();
                }
            }

            public override string ToString()
            {
                string examples = string.Join(", ", DeletePaths.Select(p => "-" + p).Concat(DownloadPaths.Select(p => "+" + p)).Take(3));
                if (DeletePaths.Concat(DownloadPaths).Count() > 3)
                    examples += ", ...";
                string line1 = string.Format("{0}: -{1} +{2}: {3}", SourceDescription, DeletePaths.Count, DownloadPaths.Count, examples);
                if (!IsDangerous)
                    return line1;

                return line1 + "\n" + "    ^ This changeset contains executable files.";
            }

            public string SourceDescription
            {
                get
                {
                    string source = "<unknown>";
                    switch (FolderType)
                    {
                        case FolderType.ModsConfigPath:
                            source = "config";
                            break;
                        case FolderType.GamePath:
                            source = "game files";
                            break;
                        case FolderType.ModsFolder:
                            source = "mod[" + ModId + "]";
                            break;
                    }

                    return source;
                }
            }
        }

        public ClientFileChecker(IEnumerable<ModelFileInfo> hashes, IEnumerable<IgnorePattern> folderIgnores, IEnumerable<IgnorePattern> extensionIgnores)
        {
            FolderIgnores = folderIgnores;
            ExtensionIgnores = extensionIgnores;
            // Don't allow escaping from rootdir
            _remoteHashes = hashes.Where((i) => !i.RelativePath.Split(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Where(s => s == "..").Any()).ToHashSet();
        }

        public async Task Initialize(Action<string, int> OnChangeFolderAction)
        {
            if (IsInitialized) return;
            IsInitialized = true;

            int counter = 0;
            _ = ModBaseData.Scheduler.Schedule(() => OnChangeFolderAction("<ModConfig>", counter++));
            ConfigFolderSummary = await FileChecker.FolderSummary.FromFolder(GenFilePaths.ConfigFolderPath, FolderType.ModsConfigPath, 0,
                FolderIgnores.
                Where((p) => p.FolderType == FolderType.ModsConfigPath).
                Select((p) => p.Pattern),
                ExtensionIgnores.
                Where((p) => p.FolderType == FolderType.ModsConfigPath).
                Select((p) => p.Pattern)
            );
            _ = ModBaseData.Scheduler.Schedule(() => OnChangeFolderAction("<GameData>", counter++));
            /*
            Don't check gamedata right now, should be covered by mod sync

            GameFolderSummary = await FileChecker.FolderSummary.FromFolder(Path.GetDirectoryName(GenFilePaths.ModsFolderPath), FolderType.GamePath, 0,
                FolderIgnores.
                Where((p) => p.FolderType == FolderType.GamePath).
                Select((p) => p.Pattern),
                ExtensionIgnores.
                Where((p) => p.FolderType == FolderType.GamePath).
                Select((p) => p.Pattern)
            );
            */

            var localMods = new List<ulong>();
            ModSummaries = new Dictionary<ulong, ModSummary>();
            var folderTasks = FileChecker.FolderSummary.HashModFolder(GenFilePaths.ModsFolderPath,
                FolderIgnores.
                Where((p) => p.FolderType == FolderType.ModsFolder).
                Select((p) => p.Pattern),
                ExtensionIgnores.
                Where((p) => p.FolderType == FolderType.ModsFolder).
                Select((p) => p.Pattern));
            foreach (var tuple in folderTasks)
            {
                _ = ModBaseData.Scheduler.Schedule(() => OnChangeFolderAction(tuple.Item1, counter++));
                var summary = await tuple.Item2;
                ModSummaries[summary.ModId] = new ModSummary() {
                    RootPath = Path.Combine(GenFilePaths.ModsFolderPath, tuple.Item1),
                    IsEditable = true,
                    Summary = summary,
                };
                localMods.Add(summary.ModId);
            }

            if (SteamManager.Active)
            {
                // Check workshop after local files
                var workshopPaths = new string[] {
                    Path.Combine(GenFilePaths.ModsFolderPath, "..", "..", "..", "workshop", "content", "294100"),
                    "/Program Files (x86)/Steam/steamapps/workshop/content/294100",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam", "steam", "steamapps", "workshop", "content", "294100"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Steam", "steamapps", "workshop", "content", "294100"),
                }.Where((p) => Directory.Exists(p));
                if (workshopPaths.Any())
                {
                    var path = workshopPaths.First();
                    var workshopFolderTasks = FileChecker.FolderSummary.HashModFolder(path,
                        FolderIgnores.
                        Where((p) => p.FolderType == FolderType.ModsFolder).
                        Select((p) => p.Pattern),
                        ExtensionIgnores.
                        Where((p) => p.FolderType == FolderType.ModsFolder).
                        Select((p) => p.Pattern));
                    foreach (var tuple in workshopFolderTasks)
                    {
                        _ = ModBaseData.Scheduler.Schedule(() => OnChangeFolderAction(tuple.Item1, counter++));
                        var summary = await tuple.Item2;
                        // Don't override local mods from workshop
                        if (!ModSummaries.ContainsKey(summary.ModId))
                            ModSummaries[summary.ModId] = new ModSummary() {
                                RootPath = Path.Combine(GenFilePaths.ModsFolderPath, tuple.Item1),
                                IsEditable = false,
                                Summary = summary,
                            };
                    }
                }
            }

            var remoteModIds = _remoteHashes.
                Where((h) => h.SourceFolder == FolderType.ModsFolder).
                Select((h) => h.ModId).
                ToHashSet();

            IEnumerable<ModelFileInfo> localHashes = ConfigFolderSummary.Files;
            foreach (var modId in remoteModIds)
                if (ModSummaries.ContainsKey(modId))
                    localHashes = localHashes.Concat(ModSummaries[modId].Summary.Files);

            _localHashes = localHashes.ToHashSet();

            var removals = _localHashes.Except(_remoteHashes).ToHashSet();
            var additions = _remoteHashes.Except(_localHashes).ToHashSet();

            var changes = new List<ChangeSet>();
            if (removals.Where(h => h.SourceFolder == FolderType.ModsConfigPath).Any() || additions.Where(h => h.SourceFolder == FolderType.ModsConfigPath).Any())
            {
                changes.Add(new ChangeSet(
                    FolderType.ModsConfigPath,
                    0,
                    removals.Where(h => h.SourceFolder == FolderType.ModsConfigPath).Select(h => h.RelativePath),
                    additions.Where(h => h.SourceFolder == FolderType.ModsConfigPath).Select(h => h.RelativePath),
                    GenFilePaths.ConfigFolderPath,
                    GenFilePaths.ConfigFolderPath
                ));
            }

            var serverMods = _remoteHashes
                .Where(h => h.SourceFolder == FolderType.ModsFolder)
                .Select(h => h.ModId)
                .ToHashSet();

            var modAdditions = additions.Where(h => h.SourceFolder == FolderType.ModsFolder);
            var modRemovals = removals.Where(h => h.SourceFolder == FolderType.ModsFolder && remoteModIds.Contains(h.ModId));

            if (modAdditions.Any() || modRemovals.Any())
            {
                var dirtyMods = modAdditions.Select(h => h.ModId).Concat(modRemovals.Select(h => h.ModId)).ToHashSet();
                foreach(var modId in dirtyMods)
                {
                    var modSummary = ModSummaries[modId];
                    if (localMods.Contains(modId))
                    {
                        changes.Add(new ChangeSet(
                            FolderType.ModsFolder,
                            modId,
                            modRemovals.Where(h => h.ModId == modId).Select(h => h.RelativePath),
                            modAdditions.Where(h => h.ModId == modId).Select(h => h.RelativePath),
                            modSummary.EditablePath,
                            modSummary.EditablePath
                        ));
                    } else
                    {
                        changes.Add(new ChangeSet(
                            FolderType.ModsFolder,
                            modId,
                            Enumerable.Empty<string>(),
                            _remoteHashes.Where(h => h.ModId == modId).Select(h => h.RelativePath),
                            modSummary.EditablePath,
                            modSummary.EditablePath
                        ));
                    }
                }
            }

            ChangeSets = changes;
        }

        public static async Task<ClientFileChecker> FromServer(SessionClient client)
        {
            ModelModsFilesResponse response = await client.TransAsync<ModelModsFilesRequest, ModelModsFilesResponse>(new ModelModsFilesRequest() { Type = ModelModsFilesRequest.RequestType.HashInfo });
            if (response.Hashes == null)
                throw new InvalidOperationException("Hashes not supplied by server.");
            return new ClientFileChecker(response.Hashes.Files, response.Hashes.FolderIgnores, response.Hashes.ExtensionIgnores);
        }
}
}
