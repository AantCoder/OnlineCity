using Model;
using ServerOnlineCity.Model;
using OCUnion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Transfer;
using Util;

namespace ServerOnlineCity
{
    public class Repository
    {
        private static Repository Current = new Repository();
        public static Repository Get { get { return Current; } }

        public static BaseContainer GetData { get { return Get.Data; } }
        public static RepositorySaveData GetSaveData { get { return Get.RepSaveData; } }

        public BaseContainer Data;
        public bool ChangeData;

        public readonly WorkTimer Timer;

        public string SaveFileName;
        public string SaveFolderDataPlayers => Path.Combine(Path.GetDirectoryName(SaveFileName), "DataPlayers");

        private RepositorySaveData RepSaveData;

        public static PlayerServer GetPlayerByLogin(string login)
        {
            // Подумать как сделать это быстрее, может быть использовать ConcurentDictonary, но надо переписать регистрацию, сохранение и загрузку, удаление поселения 
            // Think how make this faster
            return Repository.GetData.PlayersAll.FirstOrDefault(p => p.Public.Login == login);
        }

        public static void DropUserFromMap(string login)
        {
            var data = Repository.GetData;
            lock (data)
            {
                for (int i = 0; i < data.WorldObjects.Count; i++)
                {
                    var item = data.WorldObjects[i];
                    if (item.LoginOwner != login) continue;
                    //удаление из базы
                    item.UpdateTime = DateTime.UtcNow;
                    data.WorldObjects.Remove(item);
                    data.WorldObjectsDeleted.Add(item);
                }
            }
        }

        public Repository()
        {
            Timer = new WorkTimer();
            RepSaveData = new RepositorySaveData(this);
        }

        public void Load()
        {
            bool needResave = false;
            if (!Directory.Exists(SaveFolderDataPlayers))
                Directory.CreateDirectory(SaveFolderDataPlayers);
            if (!File.Exists(SaveFileName))
            {
                Data = new BaseContainer();
                Save();
                Loger.Log("Server Create Data");
            }
            else
            {
                using (var fs = File.OpenRead(SaveFileName))
                {
                    var bf = new BinaryFormatter() { Binder = new ServerCoreSerializationBinder() };
                    Loger.Log("Server Load...");
                    Data = (BaseContainer)bf.Deserialize(fs);
                    //var dataVersion = Data.VersionNum;
                    Loger.Log("Server Version data: " + Data.Version + " Current version: " + MainHelper.VersionInfo);

                    if (Data.Version != MainHelper.VersionInfo || Data.VersionNum < MainHelper.VersionNum + 1)
                    {
                        convertToLastVersion();
                        needResave = true;
                    }

                    PlayerServer.PublicPosts = Data.PlayersAll[0].PublicChat.Posts;
                    if (Data.Orders == null) Data.Orders = new List<OrderTrade>();

                    Loger.Log("Server Load done. Users " + Data.PlayersAll.Count.ToString() + ": "
                        + Data.PlayersAll.Select(p => p.Public.Login).Aggregate((string)null, (r, i) => (r == null ? "" : r + ", ") + i)
                        );
                }
            }
            if (needResave)
                Save();
            ChangeData = false;
        }

        private void convertToLastVersion()
        {
            if (Data.VersionNum <= 20033)
            {
                foreach (var player in Data.PlayersAll)
                {
                    player.Public.Grants = player.Public.Grants | Grants.UsualUser;
                    if (player.IsAdmin)
                        player.Public.Grants = player.Public.Grants | Grants.SuperAdmin;
                }
            }

            Data.Version = MainHelper.VersionInfo;
            Data.VersionNum = MainHelper.VersionNum;
        }

        public void Save(bool onlyChangeData = false)
        {
            if (onlyChangeData && !ChangeData) return;
            Loger.Log("Server Saving");

            try
            {
                if (File.Exists(SaveFileName))
                {
                    if (File.Exists(SaveFileName + ".bak")) File.Delete(SaveFileName + ".bak");
                    File.Move(SaveFileName, SaveFileName + ".bak");
                }
                using (var fs = File.OpenWrite(SaveFileName))
                {
                    var bf = new BinaryFormatter();
                    bf.Serialize(fs, Data);
                }

                ChangeData = false;
            }
            catch
            {
                if (File.Exists(SaveFileName + ".bak"))
                    File.Copy(SaveFileName + ".bak", SaveFileName, true);
                throw;
            }

            Loger.Log("Server Saved");
        }

        public static string NormalizeLogin(string login)
        {
            char[] invalidFileChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidFileChars) if (login.Contains(c)) login = login.Replace(c, '_');
            return login.ToLowerInvariant();
        }
    }
}
