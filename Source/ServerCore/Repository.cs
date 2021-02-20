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
using ServerOnlineCity.Services;

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
            if (string.IsNullOrEmpty(login))
            {
                return null;
            }

            Repository.GetData.PlayersAllDic.TryGetValue(login, out var res);
            return res;
        }

        /// <summary>
        /// Проверка на блокировку по оборудованию. 
        /// Функция подверждает если ключь есть. 
        /// Вешает ограничение, если ключа не пришло (чтобы можно было обновить клиент и только).
        /// Дисконнект, если ключь в блокировке
        /// </summary>
        /// <param name="context"></param>
        /// <param name="key">Поле с ключами между @@@. До первого разделителя игнорируется</param>
        /// <returns>Часть key до первого разделителя @@@</returns>
        public static string CheckIsIntruder(ServiceContext context, string key, string login)
        {
            if (key == null
                || !key.Contains("@@@"))
            {
                context.PossiblyIntruder = true;
                Loger.Log($"Is possibly intruder or not update {login} (empty key)");
                return key;
            }
            else
            {
                context.IntruderKeys = key;
                var keys = key.Split(new string[] { "@@@" }, StringSplitOptions.None);
                for (int i = 1; i < keys.Length; i++)
                {
                    if (string.IsNullOrEmpty(keys[i])) continue;
                    if (Repository.CheckIsIntruder(keys[i]))
                    {
                        Loger.Log($"Is intruder {login} key={keys[i]}");
                        context.Disconnect("intruder");
                        break;
                    }
                }

                Loger.Log($"Checked {login} key={key.Substring(key.IndexOf("@@@") + 3)}");
                return keys[0];
            }
        }

        private static HashSet<string> Blockkey = null;
        private static DateTime BlockkeyUpdate = DateTime.MinValue;
        public static bool CheckIsIntruder(string key)
        {
            if ((DateTime.UtcNow - BlockkeyUpdate).TotalSeconds > 30)
            {
                var fileName = Loger.PathLog + "blockkey.txt";

                BlockkeyUpdate = DateTime.UtcNow;
                if (!File.Exists(fileName)) Blockkey = new HashSet<string>();
                else
                    try
                    {
                        var listBlock = File.ReadAllLines(fileName, Encoding.UTF8)
                            .Select(b => b.Replace("@@@", "").Trim())
                            .Select(b => b.Contains(" ") ? b.Substring(0, b.IndexOf(" ")) : b)
                            .Where(b => b != String.Empty)
                            .ToArray();
                        Blockkey = new HashSet<string>(listBlock);
                    }
                    catch (Exception exp)
                    {
                        Loger.Log("CheckIsIntruder error " + exp.Message);
                        Blockkey = new HashSet<string>();
                        return false;
                    }
            }
            var k = key.Replace("@@@", "").Trim();
            return Blockkey.Contains(k);
        }

        private static HashSet<string> Blockip = null;
        private static DateTime BlockipUpdate = DateTime.MinValue;
        public static bool CheckIsBanIP(string IP)
        {
            if ((DateTime.UtcNow - BlockipUpdate).TotalSeconds > 30)
            {
                var fileName = Loger.PathLog + "blockip.txt";

                BlockipUpdate = DateTime.UtcNow;
                if (!File.Exists(fileName)) Blockip = new HashSet<string>();
                else
                    try
                    {
                        var listBlock = File.ReadAllLines(fileName, Encoding.UTF8);
                        if (listBlock.Any(b => b.Contains("/")))
                        {
                            listBlock = listBlock
                                .SelectMany(b =>
                                {
                                    var bb = b.Trim();
                                    var comment = "";
                                    var ic = bb.IndexOf(" ");
                                    if (ic > 0)
                                    {
                                        comment = bb.Substring(ic);
                                        bb = bb.Substring(0, ic);
                                    }
                                    if (bb.Any(c => !char.IsDigit(c) && c != '.' && c != '/')) return new List<string>();
                                    var ls = bb.LastIndexOf("/");
                                    if (ls < 0) return new List<string>() { bb + comment };
                                    var lp = bb.LastIndexOf(".");
                                    if (lp <= 0) return new List<string>();
                                    var ib = int.Parse(bb.Substring(lp + 1, ls - (lp + 1)));
                                    var ie = int.Parse(bb.Substring(ls + 1));
                                    var res = new List<string>();
                                    var s = bb.Substring(0, lp + 1);
                                    for (int i = ib; i <= ie; i++)
                                        res.Add(s + i.ToString() + comment);
                                    return res;
                                })
                                .ToArray();
                            File.WriteAllLines(fileName, listBlock, Encoding.Default);
                        }
                        Blockip = new HashSet<string>(listBlock
                            .Select(b => b.Contains(" ") ? b.Substring(0, b.IndexOf(" ")) : b));
                    }
                    catch (Exception exp)
                    {
                        Loger.Log("CheckIsBanIP error " + exp.Message);
                        Blockip = new HashSet<string>();
                        return false;
                    }

            }

            return Blockip.Contains(IP.Trim());
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
                    i--;
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

                    if (Data.Orders == null) Data.Orders = new List<OrderTrade>();

                    Data.UpdatePlayersAllDic();

                    Loger.Log("Server Load done. Users " + Data.PlayersAll.Count.ToString() + ": "
                        + Data.PlayersAll.Select(p => p.Public.Login).Aggregate((string)null, (r, i) => (r == null ? "" : r + ", ") + i)
                        );

                    ChatManager.Instance.NewChatManager(Data.MaxIdChat, Data.PlayerSystem.Chats.Keys.First());
                }
            }


            if (needResave)
                Save();
            ChangeData = false;


        }

        private void convertToLastVersion()
        {
            if (Data.VersionNum < 30033)
            {
                ChatManager.Instance.PublicChat.PartyLogin.Clear();

                foreach (var player in Data.PlayersAll)
                {
                    player.Public.Grants = player.Public.Grants | Grants.UsualUser;
                    if (player.IsAdmin)
                        player.Public.Grants = player.Public.Grants | Grants.SuperAdmin;

                    //if (player.IdChats == null || !player.IdChats.Any())
                    //{
                    //    player.IdChats = new HashSet<int>() { ChatManager.PublicChatId };
                    //}

                    ChatManager.Instance.PublicChat.PartyLogin.Add(player.Public.Login);
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

                Data.MaxIdChat = ChatManager.Instance.MaxChatId;

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
