using OCUnion;
using OCUnion.Common;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ServerOnlineCity
{
    public class RepositoryFileSharing
    {
        private Repository MainRepository;

        private ConcurrentDictionary<string, ModelFileSharing> CacheFileDataByFileName = new ConcurrentDictionary<string, ModelFileSharing>();

        private long CacheSize;

        private const long CacheSizeMax = 10 * 1024 * 1024;

        private DateTime CacheClear;

        private const int CacheClearMaxMinute = 30;

        private ConcurrentDictionary<FileSharingCategory, IFileSharingWorker> Workers = null;
        private IFileSharingWorker WorkersDefault = new WorkerDefault();

        public RepositoryFileSharing(Repository repository)
        {
            MainRepository = repository;
        }

        /// <summary>
        /// Сохраняем данные в хранилище и записываем в fileSharing значение Hash
        /// </summary>
        public bool SaveFileSharing(PlayerServer player, ModelFileSharing fileSharing)
        {
            var worker = GetWorker(fileSharing.Category);
            var fileName = worker.CheckAndGetFileNameUpload(player, fileSharing);
            if (string.IsNullOrEmpty(fileName)) return false;
            fileName = Path.Combine(GetFolderName(fileSharing.Category), fileName);
            if (!CheckFileName(fileName)) return false;

            //записываем файл на диск и заполняем в fileSharing хеш файла
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));

                File.WriteAllBytes(fileName, fileSharing.Data);

                fileSharing.Hash = GetHash(fileSharing.Data);

                CacheFileDataByFileName.TryRemove(fileName, out _);

                return true;
            }
            catch (Exception ext)
            {
                Loger.Log("FileSharing Exception " + ext.ToString());
                return false;
            }
        }

        /// <summary>
        /// Читаем из хранилища. Если fileSharing.Hash равен значению хеша в хранилище, то ничего не делаем.
        /// Иначе заполняем Hash и Data
        /// </summary>
        public void LoadFileSharing(PlayerServer player, ModelFileSharing fileSharing)
        {
            var worker = GetWorker(fileSharing.Category);
            var fileName = worker.CheckAndGetFileNameDownload(player, fileSharing);
            if (string.IsNullOrEmpty(fileName)) return;
            fileName = Path.Combine(GetFolderName(fileSharing.Category), fileName);

            if (!CheckFileName(fileName)) return;

            if ((DateTime.UtcNow - CacheClear).TotalMinutes > CacheClearMaxMinute
                || CacheSize > CacheSizeMax)
            {
                CacheClear = DateTime.UtcNow;
                CacheSize = 0;
                CacheFileDataByFileName = new ConcurrentDictionary<string, ModelFileSharing>();
            }
            var fileData = CacheFileDataByFileName.GetOrAdd(fileName, _ =>
            {
                //читаем файл
                var fd = GetDataFileSharing(fileName);
                if (fd == null)
                {
                    CacheSize += 1024;
                    return fd;
                }
                fd.Name = fileSharing.Name;
                fd.Category = fileSharing.Category;

                CacheSize += fd.Data.Length + 1024;
                return fd;
            });
            if (fileData == null) return;

            if (!string.IsNullOrEmpty(fileSharing.Hash))
            {
                //сверяем хеш файла, если он такой же, то в пакете ничего не меняем, иначе продолжаем
                if (fileSharing.Hash == fileData.Hash) return;
            }
            //заполняем хеш и содержимое файла
            fileSharing.Hash = fileData.Hash;
            fileSharing.Data = fileData.Data;
        }

        private bool CheckFileName(string fileName)
        {
            if (!string.IsNullOrWhiteSpace(fileName)
                && !fileName.Contains("..") 
                && !fileName.Contains(@"\\") 
                && !fileName.Contains(@"//")
                ) return true;
            Loger.Log("FileSharing corruption file name: " + fileName);
            return false;
        }

        private ModelFileSharing GetDataFileSharing(string fileName)
        {
            try
            {
                if (!File.Exists(fileName)) return null;

                var data = File.ReadAllBytes(fileName);

                return new ModelFileSharing()
                {
                    Data = data,
                    Hash = GetHash(data),
                };
            }
            catch(Exception ext)
            {
                Loger.Log("FileSharing Exception " + ext.ToString());
                return null;
            }
        }

        private string GetHash(byte[] data)
        {
            return FileChecker.GetCheckSum(data);
        }

        internal string GetFolderName(FileSharingCategory category)
        {
            var categoryName = Repository.NormalizeLogin(Enum.GetName(typeof(FileSharingCategory), category));
            return Path.Combine(MainRepository.SaveFolderDataPlayers, categoryName);
        }

        private IFileSharingWorker GetWorker(FileSharingCategory category)
        {
            if (Workers == null)
            {
                var categorys = Enum.GetValues(typeof(FileSharingCategory))
                    .Cast<FileSharingCategory>()
                    .ToDictionary(c => Enum.GetName(typeof(FileSharingCategory), c));

                var workers = new ConcurrentDictionary<FileSharingCategory, IFileSharingWorker>();
                foreach (var type in Assembly.GetAssembly(typeof(RepositoryFileSharing)).GetTypes())
                {
                    if (!type.IsClass)
                    {
                        continue;
                    }

                    if (type.GetInterfaces().Any(x => x == typeof(IFileSharingWorker)))
                    {
                        var worker = (IFileSharingWorker)Activator.CreateInstance(type);
                        var workerName = worker.GetType().Name;
                        var workerCategory = categorys.Keys.FirstOrDefault(cName => workerName.EndsWith(cName));
                        if (workerCategory != null)
                        {
                            worker.Category = categorys[workerCategory];
                            worker.FileSharing = this;
                            workers[categorys[workerCategory]] = worker;
                        }
                    }
                }
                Workers = workers;
            }

            if (Workers.TryGetValue(category, out var res))
                return res;
            else
                return WorkersDefault;
        }

        private interface IFileSharingWorker
        {
            FileSharingCategory Category { get; set; }
            RepositoryFileSharing FileSharing { get; set; }
            string CheckAndGetFileNameDownload(PlayerServer player, ModelFileSharing info);
            string CheckAndGetFileNameUpload(PlayerServer player, ModelFileSharing info);
        }

        #region workers
        private class WorkerDefault : IFileSharingWorker
        {
            public FileSharingCategory Category { get; set; }
            public RepositoryFileSharing FileSharing { get; set; }
            public virtual string CheckAndGetFileNameDownload(PlayerServer player, ModelFileSharing info)
            {
                return Repository.NormalizeLogin(info.Name);
            }
            public virtual string CheckAndGetFileNameUpload(PlayerServer player, ModelFileSharing info)
            {
                Loger.Log($"Server FileSharing Save WorkerDefault {player.Public.Login} size={info.Data?.Length}b name={info.Name}");
                return Repository.NormalizeLogin(info.Name);
            }
        }

        private class WorkerPlayerIcon : WorkerDefault
        {
            /// <summary>
            /// Размер стандартной квадратной иконки
            /// </summary>
            private int NeedSize = 256;

            public override string CheckAndGetFileNameDownload(PlayerServer player, ModelFileSharing info)
            {
                return base.CheckAndGetFileNameDownload(player, info) + ".png";
            }
            public override string CheckAndGetFileNameUpload(PlayerServer player, ModelFileSharing info)
            {
                Loger.Log($"Server FileSharing Save PlayerIcon {player.Public.Login} size={info.Data?.Length}b");
                //больше 2 мб исходник не пропускаем
                if (info.Data == null || info.Data.Length == 0 || info.Data.Length > 2 * 1024 * 1024) return null;

                //перекодирование изображения под размер NeedSize
                try
                {
                    var imageEnd = new Bitmap(NeedSize, NeedSize);
                    Bitmap imageData;
                    using (var data = new MemoryStream(info.Data))
                    {
                        imageData = new Bitmap(data);
                    }
                    Loger.Log($"Server FileSharing Save PlayerIcon {player.Public.Login} orig={imageData.Width}*{imageData.Height}");

                    if (imageData.Width < 10 || imageData.Height < 10 || imageData.Width > 2000 || imageData.Height > 2000) return null;

                    var rectData = imageData.Width >= imageData.Height
                        ? new Rectangle((imageData.Width - imageData.Height) / 2, 0, imageData.Height - 1, imageData.Height - 1)
                        : new Rectangle(0, (imageData.Height - imageData.Width) / 2, imageData.Width - 1, imageData.Width - 1);

                    using (var graphics = Graphics.FromImage(imageEnd))
                    {
                        graphics.DrawImage(imageData
                            , new Rectangle(0, 0, NeedSize, NeedSize)
                            , rectData
                            , GraphicsUnit.Pixel);
                    }
                    using (var data = new MemoryStream())
                    {
                        imageEnd.Save(data, ImageFormat.Png);
                        info.Data = data.ToArray();
                    }
                    Loger.Log($"Server FileSharing Save PlayerIcon {player.Public.Login} end={imageEnd.Width}*{imageEnd.Height}");
                }
                catch (Exception ext)
                {
                    Loger.Log($"Server FileSharing Save PlayerIcon {player.Public.Login} Exception " + ext.ToString());
                    return null;
                }

                return base.CheckAndGetFileNameUpload(player, info) + ".png";
            }
        }

        private class WorkerColonyScreen : IFileSharingWorker
        {
            public FileSharingCategory Category { get; set; }
            public RepositoryFileSharing FileSharing { get; set; }

            //В name передается логин@serverId колонии скрин которой нужен
            //при записи дописывать текущий тик игрока, при чтении искать файл с максимальным тиком (если в имени не указан тик)
            public string CheckAndGetFileNameDownload(PlayerServer player, ModelFileSharing info)
            {
                try
                {
                    string name = info.Name;
                    var namePart = name.Split('@');
                    var serverId = int.Parse(namePart[1]);
                    var login = Repository.NormalizeLogin(namePart[0]);

                    var mask = login + "_" + serverId.ToString() + "_*.png";
                    var folderName = FileSharing.GetFolderName(Category);
                    Directory.CreateDirectory(folderName);
                    var files = Directory.GetFiles(folderName, mask);
                    if (files.Length == 0) return null;
                    var file = files.Select(f =>
                        {
                            try
                            {
                                var i0 = f.LastIndexOf("_");
                                var i1 = f.LastIndexOf(".");
                                var tick = int.Parse(f.Substring(i0 + 1, i1 - i0 - 1));
                                return new { tick, f };
                            }
                            catch
                            {
                                return null;
                            }
                        })
                        .Where(a => a != null)
                        .OrderByDescending(a => a.tick)
                        .Select(a => a.f)
                        .FirstOrDefault();
                    return file == null ? null : Path.GetFileName(file);
                }
                catch (Exception ext)
                {
                    Loger.Log("FileSharing Exception " + ext.ToString());
                    return null;
                }
            }
            public string CheckAndGetFileNameUpload(PlayerServer player, ModelFileSharing info)
            {
                try
                {
                    Loger.Log($"Server FileSharing Save ColonyScreen {player.Public.Login} size={info.Data?.Length}b name={info.Name}");
                    //больше 2 мб исходник не пропускаем
                    if (info.Data == null || info.Data.Length == 0 || info.Data.Length > 4 * 1024 * 1024) return null; 

                    string name = info.Name;
                    var namePart = name.Split('@');
                    var serverId = int.Parse(namePart[1]);
                    var login = Repository.NormalizeLogin(namePart[0]); 
                    if (login != Repository.NormalizeLogin(player.Public.Login)) return null;

                    ControlSizeFolder();
                    var tick = player.Public.LastTick;
                    //проверяем, что этот id принадлежит игроку
                    var data = Repository.GetData;
                    var wo = data.WorldObjects.FirstOrDefault(w => w.PlaceServerId == serverId);
                    //if (wo == null || wo.LoginOwner != player.Public.Login) return null; //todo!

                    return login + "_" + serverId.ToString() + "_" + tick.ToString() + ".png";
                }
                catch (Exception ext)
                {
                    Loger.Log("FileSharing Exception " + ext.ToString());
                    return null;
                }
            }
            private void ControlSizeFolder()
            {
                if (ServerManager.ServerSettings.ColonyScreenFolderMaxMb == 0) return;

                var fileNames = Directory.GetFiles(FileSharing.GetFolderName(Category), "*_*_*.png");
                if (fileNames.Length < 2) return;

                var files = fileNames.Select(fn => new FileInfo(fn)).ToList();
                var needClear = files.Sum(f => f.Length) - ServerManager.ServerSettings.ColonyScreenFolderMaxMb * 1024 * 1024;
                if (needClear > 0) return;

                var list = files.Select(fi =>
                    {
                        try
                        {
                            var f = fi.Name;
                            var i0 = f.LastIndexOf("_");
                            var i1 = f.LastIndexOf(".");
                            var tick = int.Parse(f.Substring(i0 + 1, i1 - i0 - 1));
                            var loginServId = f.Substring(0, i0);
                            return new { tick, loginServId, fi };
                        }
                        catch
                        {
                            return null;
                        }
                    })
                    .Where(a => a != null)
                    .GroupBy(a => a.loginServId)
                    .Where(g => g.Count() > 1)
                    .SelectMany(g =>
                    {
                        var max = g.Max(a => a.tick);
                        return g.Where(a => a.tick < max);
                    })
                    .OrderBy(a => a.fi.LastWriteTimeUtc)
                    .Select(a => a.fi)
                    .ToList();

                for (int i = 0; i < list.Count; i++)
                {
                    if (needClear <= 0) return;
                    needClear -= list[i].Length;
                    list[i].Delete();
                }
            }

        }
        #endregion

    }
}
