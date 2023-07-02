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
using System.Text.RegularExpressions;

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

        public struct RemappedHashSignature : IEquatable<RemappedHashSignature>
        {
            private static Regex _regex = new Regex(@"^Mod_([a-zA-Z0-9._ -]+)(_[a-zA-Z0-9_.-]+\.xml)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

            public FolderType SourceFolder;
            public ulong ModId;
            public string RemoteRelativePath;
            public string LocalRelativePath;
            public byte[] Hash;

            public static IEnumerable<RemappedHashSignature> FromLocalHashes(IEnumerable<ModelFileInfo> localHashes)
            {
                return localHashes.Select(hash => new RemappedHashSignature()
                {
                    SourceFolder = hash.SourceFolder,
                    ModId = hash.ModId,
                    RemoteRelativePath = null,
                    LocalRelativePath = hash.RelativePath,
                    Hash = hash.Hash,
                });
            }

            public static IEnumerable<RemappedHashSignature> FromRemoteHashes(IEnumerable<ModelFileInfo> remoteHashes, Dictionary<ulong, ModSummary> modSummaries)
            {

                var remappings = modSummaries.Keys.ToDictionary(l => l.ToString(), l => Path.GetFileName(modSummaries[l].RootPath));

                Match m = null;
                foreach (var hash in remoteHashes)
                {
                    if (hash.SourceFolder != FolderType.ModsConfigPath)
                    {
                        yield return new RemappedHashSignature()
                        {
                            SourceFolder = hash.SourceFolder,
                            ModId = hash.ModId,
                            RemoteRelativePath = hash.RelativePath,
                            LocalRelativePath = hash.RelativePath,
                            Hash = hash.Hash,
                        };
                        continue;
                    }
                    m = _regex.Match(hash.RelativePath);
                    if (m.Success)
                    {
                        var modId = m.Groups[1].Value;
                        if (!remappings.ContainsKey(modId))
                        {
                            yield return new RemappedHashSignature()
                            {
                                SourceFolder = hash.SourceFolder,
                                ModId = hash.ModId,
                                RemoteRelativePath = hash.RelativePath,
                                LocalRelativePath = hash.RelativePath,
                                Hash = hash.Hash,
                            };
                            continue;
                        }
                        var suffix = m.Groups[2].Value;
                        var newFilename = "Mod_" + remappings[modId] + suffix;
                        Loger.Log(String.Format("Client Remapped remote config file {0} to {1}.", hash.RelativePath, newFilename));
                        yield return new RemappedHashSignature()
                        {
                            SourceFolder = FolderType.ModsConfigPath,
                            ModId = 0,
                            RemoteRelativePath = hash.RelativePath,
                            LocalRelativePath = newFilename,
                            Hash = hash.Hash,
                        };
                    }
                    else
                    {
                        yield return new RemappedHashSignature()
                        {
                            SourceFolder = hash.SourceFolder,
                            ModId = hash.ModId,
                            RemoteRelativePath = hash.RelativePath,
                            LocalRelativePath = hash.RelativePath,
                            Hash = hash.Hash,
                        };
                    }
                }
            }

            public bool Equals(RemappedHashSignature other)
            {
                return (SourceFolder, ModId, LocalRelativePath) == (other.SourceFolder, other.ModId, other.LocalRelativePath) && Hash.SequenceEqual(other.Hash);
            }

            public override int GetHashCode()
            {
                if (Hash == null || Hash.Length < 8)
                {
                    return 0;
                }

                // переводит первые 4 байта в int
                return BitConverter.ToInt32(Hash, 0);
            }
        }

        public bool IsInitialized { get; private set; }
        public IEnumerable<IgnorePattern> FolderIgnores { get; private set; }
        public IEnumerable<IgnorePattern> ExtensionIgnores { get; private set; }


        public FileChecker.FolderSummary GameFolderSummary { get; private set; }
        public FileChecker.FolderSummary ConfigFolderSummary { get; private set; }

        private IEnumerable<ModelFileInfo> _remoteHashes;
        public IEnumerable<ChangeSet> ChangeSets { get; private set; }

        public class ChangeSet
        {
            public FolderType FolderType { get; private set; }
            public ulong ModId { get; private set; }

            public List<string> DeletePaths { get; private set; }
            public List<Tuple<string, string>> DownloadPaths { get; private set; }

            public string RemoveRoot { get; private set; }
            public string DownloadRoot { get; private set; }
            

            public ChangeSet(FolderType folderType, ulong modId, IEnumerable<string> deletePaths, IEnumerable<Tuple<string, string>> downloadPaths, string removeRoot, string downloadRoot)
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
                        .Where(s => s.Item2.EndsWith(".dll") || s.Item2.EndsWith(".exe"))
                        .Any();
                }
            }

            public override string ToString()
            {
                string examples = string.Join(", ", DeletePaths.Select(p => "-" + p).Concat(DownloadPaths.Select(p => "+" + p)).Take(3));
                if (DeletePaths.Concat(DownloadPaths.Select(t => t.Item2)).Count() > 3)
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
            _remoteHashes = hashes.Where((i) => !i.RelativePath.Split(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Where(s => s == "..").Any());
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
            var modSummaries = new Dictionary<ulong, ModSummary>();
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
                modSummaries[summary.ModId] = new ModSummary() {
                    RootPath = Path.Combine(GenFilePaths.ModsFolderPath, tuple.Item1),
                    IsEditable = true,
                    Summary = summary,
                };
                localMods.Add(summary.ModId);
            }

            if (SteamManager.Active)
            {
                var workshopFolderTasks = WorkshopItems.AllSubscribedItems
                    .Select(item => new Tuple<string, Task<FileChecker.FolderSummary>, string>(item.Directory.Name,
                    FileChecker.FolderSummary.FromFolder(
                        item.Directory.FullName,
                        FolderType.ModsFolder,
                        (ulong)item.PublishedFileId,
                        FolderIgnores.
                        Where((p) => p.FolderType == FolderType.ModsFolder).
                        Select((p) => p.Pattern),
                        ExtensionIgnores.
                        Where((p) => p.FolderType == FolderType.ModsFolder).
                        Select((p) => p.Pattern)
                        ),
                    item.Directory.FullName
                    )).ToArray();

                foreach (var tuple in workshopFolderTasks)
                {
                    _ = ModBaseData.Scheduler.Schedule(() => OnChangeFolderAction(tuple.Item1, counter++));
                    var summary = await tuple.Item2;
                    // Don't override local mods from workshop
                    if (!modSummaries.ContainsKey(summary.ModId))
                        modSummaries[summary.ModId] = new ModSummary()
                        {
                            RootPath = tuple.Item3,
                            IsEditable = false,
                            Summary = summary,
                        };
                }
            }

            var remoteModIds = _remoteHashes.
                Where((h) => h.SourceFolder == FolderType.ModsFolder).
                Select((h) => h.ModId).
                ToHashSet();

            IEnumerable<ModelFileInfo> localHashes = ConfigFolderSummary.Files;
            foreach (var modId in remoteModIds)
                if (modSummaries.ContainsKey(modId))
                    localHashes = localHashes.Concat(modSummaries[modId].Summary.Files);

            var localRemappedHashes = RemappedHashSignature.FromLocalHashes(localHashes).ToHashSet();
            var remoteRemappedHashes = RemappedHashSignature.FromRemoteHashes(_remoteHashes, modSummaries).ToHashSet();

            var removals = localRemappedHashes.Except(remoteRemappedHashes).ToHashSet();
            var additions = remoteRemappedHashes.Except(localRemappedHashes).ToHashSet();

            var changes = new List<ChangeSet>();
            if (removals.Where(h => h.SourceFolder == FolderType.ModsConfigPath).Any() || additions.Where(h => h.SourceFolder == FolderType.ModsConfigPath).Any())
            {
                changes.Add(new ChangeSet(
                    FolderType.ModsConfigPath,
                    0,
                    removals.Where(h => h.SourceFolder == FolderType.ModsConfigPath).Select(h => h.LocalRelativePath),
                    additions.Where(h => h.SourceFolder == FolderType.ModsConfigPath).Select(h => new Tuple<string, string>(h.RemoteRelativePath, h.LocalRelativePath)),
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
                    var modSummary = modSummaries[modId];
                    if (localMods.Contains(modId))
                    {
                        changes.Add(new ChangeSet(
                            FolderType.ModsFolder,
                            modId,
                            modRemovals.Where(h => h.ModId == modId).Select(h => h.LocalRelativePath),
                            modAdditions.Where(h => h.ModId == modId).Select(h => new Tuple<string, string>(h.RemoteRelativePath, h.LocalRelativePath)),
                            modSummary.EditablePath,
                            modSummary.EditablePath
                        ));
                    } else
                    {
                        changes.Add(new ChangeSet(
                            FolderType.ModsFolder,
                            modId,
                            Enumerable.Empty<string>(),
                            remoteRemappedHashes.Where(h => h.ModId == modId).Select(h => new Tuple<string, string>(h.RemoteRelativePath, h.LocalRelativePath)),
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
