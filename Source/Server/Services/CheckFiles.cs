using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using OCUnion;
using OCUnion.Transfer;
using OCUnion.Transfer.Model;
using OCUnion.Transfer.Types;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class CheckFiles : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request35ListFiles;

        public int ResponseTypePackage => (int)PackageType.Response36ListFiles;

        private const long MaxPacketSize = 5000000;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = checkFiles((ModelModsFilesRequest)request.Packet, context).Result;
            return result;
        }

        private async Task<ModelModsFilesResponse> checkFiles(ModelModsFilesRequest packet, ServiceContext context)
        {
            // Hash fetch
            if (packet.Type == ModelModsFilesRequest.RequestType.HashInfo)
            {
                return new ModelModsFilesResponse()
                {
                    Hashes = new ModelModsFilesResponse.HashInfo()
                    {
                        Files = ServerManager.HashChecker.Files.ToList(),
                        ExtensionIgnores = ServerManager.HashChecker.ExtensionIgnores.ToList(),
                        FolderIgnores = ServerManager.HashChecker.FolderIgnores.ToList(),
                    },
                };
            }
            if (packet.FileQueries == null)
                return new ModelModsFilesResponse();

            // Client wants downloads
            long sizeLeft = MaxPacketSize;
            var response = new ModelModsFilesResponse()
            {
                Contents = new ModelModsFilesResponse.FileData(),
            };
            var configRequests = packet.FileQueries.Where((q) => q.SourceDirectory != FolderType.ModsFolder);
            var modRequests = packet.FileQueries.Where((q) => q.SourceDirectory == FolderType.ModsFolder);
            var toSend = new List<ModelFileInfo>();

            foreach (var configRequest in configRequests)
            {
                var hash = ServerManager.HashChecker.ConfigFolderSummary.Files.FirstOrDefault((f) => f.SourceFolder == configRequest.SourceDirectory && f.RelativePath == configRequest.RelativePath);
                if (hash == null)
                    return new ModelModsFilesResponse();

                if (hash.Size > sizeLeft && toSend.Count != 0)
                    continue;

                sizeLeft -= hash.Size;
                toSend.Add(hash);
            }
            foreach (var modRequest in modRequests)
            {
                if (!ServerManager.HashChecker.ModSummaries.ContainsKey(modRequest.ModId))
                    return new ModelModsFilesResponse();

                var modSummary = ServerManager.HashChecker.ModSummaries[modRequest.ModId];
                var hash = modSummary.Item3.Files.FirstOrDefault(f => f.RelativePath == modRequest.RelativePath);
                if (hash == null)
                    return new ModelModsFilesResponse();

                if (hash.Size > sizeLeft && toSend.Count != 0)
                    continue;

                sizeLeft -= hash.Size;
                toSend.Add(hash);
            }

            // Read and compress files for sending
            var reads = toSend.Select((todo) =>
            {
                string path = null;
                if (todo.SourceFolder == FolderType.ModsConfigPath)
                    path = Path.Combine(ServerManager.HashChecker.ConfigFolderInfo.ServerPath.Replace("\\", "" + Path.DirectorySeparatorChar), todo.RelativePath);
                else if (todo.SourceFolder == FolderType.ModsFolder)
                {
                    var modSummary = ServerManager.HashChecker.ModSummaries[todo.ModId];
                    path = Path.Combine(modSummary.Item2, todo.RelativePath);
                }
                else
                    throw new NotImplementedException();

                return Task.Factory.StartNew(() =>
                {
                    var dest = new MemoryStream();
                    using (var f = File.OpenRead(path))
                    {
                        var gzipper = new GZipStream(dest, CompressionLevel.Fastest);
                        f.CopyTo(gzipper);
                        gzipper.Close();
                    }
                    var buffer = dest.ToArray();
                    return new ModelModsFilesResponse.FileEntry()
                    {
                        GZippedData = buffer,
                        ModId = todo.ModId,
                        RelativePath = todo.RelativePath,
                        SourceDirectory = todo.SourceFolder,
                    };
                }, TaskCreationOptions.LongRunning);

            }).ToArray();
            response.Contents.Entries = (await Task.WhenAll(reads)).ToList();

            return response;

            
        }
        
    }
}
