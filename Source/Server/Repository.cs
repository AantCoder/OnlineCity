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
using ServerOnlineCity.Mechanics;
using OCUnion.Transfer.Model;

namespace ServerOnlineCity
{
    public class Repository
    {
        private static Repository Current = new Repository();
        public static Repository Get { get { return Current; } }

        public static BaseContainer GetData { get { return Get.Data; } }
        public static RepositorySaveData GetSaveData { get { return Get.RepSaveData; } }
        public static RepositoryFileSharing GetFileSharing { get { return Get.RepFileSharing; } }

        public BaseContainer Data;
        public bool ChangeData;

        public readonly WorkTimer Timer;

        public string SaveFileName;
        public string SaveFolderDataPlayers => Path.Combine(Path.GetDirectoryName(SaveFileName.NormalizePath()), "DataPlayers");


        private RepositorySaveData RepSaveData;

        private RepositoryFileSharing RepFileSharing;

        public static PlayerServer GetPlayerByLogin(string login, bool withNotApprove = false)
        {
            if (string.IsNullOrEmpty(login))
            {
                return null;
            }

            PlayerServer res;
            if (withNotApprove) Repository.GetData.PlayersAllDicWithNotApprove.TryGetValue(login, out res);
            else Repository.GetData.PlayersAllDic.TryGetValue(login, out res);

            return res;
        }

        public static State GetStateByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            Repository.GetData.StatesDic.TryGetValue(name, out var res);
            return res;
        }
        public static StatePosition GetStatePosition(Player player) => GetStatePositionByName(player.StateName, player.StatePositionName);
        public static StatePosition GetStatePositionByName(string nameState, string namePosition)
        {
            if (string.IsNullOrEmpty(nameState) || string.IsNullOrEmpty(namePosition))
            {
                return null;
            }

            if (Repository.GetData.StatePositionsDic.TryGetValue(nameState, out var resState)
                && resState.TryGetValue(namePosition, out var res))
            {
                return res;
            }
            else
            {
                return null;
            }
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
                Loger.Log($"Is possibly intruder or not update {login} (empty key)", Loger.LogLevel.WARNING);
                return key;
            }
            else
            {

                var isIntruder = "";
                context.IntruderKeys = "";
                var keys = key.Split(new string[] { "@@@" }, StringSplitOptions.None);
                var keysСleaned = new List<string>();
                for (int i = 1; i < keys.Length; i++)
                {
                    if (string.IsNullOrEmpty(keys[i]) || keys[i].Trim().Length <= 3) continue;
                    keysСleaned.Add(keys[i]);
                    context.IntruderKeys += "@@@" + keys[i];
                    if (string.IsNullOrEmpty(isIntruder) && Repository.CheckIsIntruder(keys[i]))
                    {
                        Loger.Log($"Is intruder {login} key={keys[i]}", Loger.LogLevel.WARNING);
                        isIntruder = keys[i];
                    }
                }

                if (context.IntruderKeys == "")
                {
                    //копия блока выше
                    context.PossiblyIntruder = true;
                    Loger.Log($"Is possibly intruder or not update {login} (empty keys)", Loger.LogLevel.WARNING);
                    return key;
                }

                if (string.IsNullOrEmpty(isIntruder) && !string.IsNullOrEmpty(login) && Repository.CheckIsIntruder(login))
                {
                    Loger.Log($"Is intruder {login} by login", Loger.LogLevel.WARNING);
                    isIntruder = login;
                }

                if (!string.IsNullOrEmpty(isIntruder))
                {
                    if (!string.IsNullOrEmpty(login)) keysСleaned.Add(login);
                    //добавляем новые ключи в блокировку, связанные с найденным
                    Repository.AddIntruder(keysСleaned, " auto add by " + isIntruder + " (login " + login + " IP " + context.AddrIP + ")");

                    context.Disconnect("intruder");
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

                        var addPl = listBlock.Where(b => b.Length != 20)
                            .Select(b => GetPlayerByLogin(b))
                            .Where(p => p != null)
                            .ToList();
                        foreach (var pl in addPl)
                        {
                            var add = pl.IntruderKeys.Split(new string[] { "@@@" }, StringSplitOptions.None)
                                .Where(k => k.Length > 3)
                                .Where(k => !Blockkey.Contains(k))
                                .ToList();
                            if (add.Count > 0) AddIntruder(add, $" auto add by login {pl.Public.Login}");
                        }
                    }
                    catch (Exception exp)
                    {
                        Loger.Log("CheckIsIntruder error " + exp.Message, Loger.LogLevel.ERROR);
                        Blockkey = new HashSet<string>();
                        return false;
                    }
            }
            var kk = key.Replace("@@@", "").Trim();
            return Blockkey.Contains(kk);
        }
        public static void AddIntruder(List<string> keys, string comment)
        {
            if (Blockkey == null) CheckIsIntruder("");
            var add = "";
            foreach (var key in keys)
            { 
                var k = key.Replace("@@@", "").Trim();
                if (k != "" && !Blockkey.Contains(k))
                {
                    add += Environment.NewLine + k + " //" + comment.Replace("\r", "").Replace("\n", " ");
                }
            }

            if (add != "")
            {
                var fileName = Loger.PathLog + "blockkey.txt";

                File.AppendAllText(fileName, add, Encoding.UTF8);

                //немедленно обновить из файла
                BlockkeyUpdate = DateTime.MinValue;
                CheckIsIntruder("");
            }
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
                        Loger.Log("CheckIsBanIP error " + exp.Message, Loger.LogLevel.ERROR);
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
                //удаление объектов игрока
                for (int i = 0; i < data.WorldObjects.Count; i++)
                {
                    var item = data.WorldObjects[i];
                    if (item.LoginOwner != login) continue;
                    i--;
                    item.UpdateTime = DateTime.UtcNow;
                    data.WorldObjects.Remove(item);
                    if (data.WorldObjectsDeleted == null) data.WorldObjectsDeleted = new List<WorldObjectEntry>();
                    data.WorldObjectsDeleted.Add(item);
                }
                
                //удаление сделок и складов
                for (int i = data.Orders.Count - 1; i >= 0; i--)
                {
                    var item = data.Orders[i];
                    if (item.Owner.Login != login) continue;
                    data.OrderOperator.OrderRemove(item);
                }

                //удаление всех скринов колоний с сервера
                //пример World\DataPlayers\colonyscreen\login_6_157.png 
                var folderName = Get.RepFileSharing.GetFolderName(FileSharingCategory.ColonyScreen);
                if (Directory.Exists(folderName))
                {
                    var files = Directory.GetFiles(folderName, login + "_*_*.png");
                    foreach (var file in files)
                    {
                        try
                        {
                            if (File.Exists(file)) File.Delete(file);
                        }
                        catch { }
                    }
                }

                //удаление всех рейдов, которые заказал игрок
                for (int i = 0; i < data.PlayersAll.Count; i++)
                {
                    if (data.PlayersAll[i].FunctionMails == null) continue;

                    for (int ii = 0; ii < data.PlayersAll[i].FunctionMails.Count; ii++)
                    {
                        var fmail = data.PlayersAll[i].FunctionMails[ii] as FMailIncident;
                        if (fmail != null
                            && fmail.Mail?.From?.Login == login)
                        {
                            data.PlayersAll[i].FunctionMails.RemoveAt(ii--);
                        }
                    }
                }
            }
        }

        public Repository()
        {
            Timer = new WorkTimer();
            RepSaveData = new RepositorySaveData(this);
            RepFileSharing = new RepositoryFileSharing(this);
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
                Data.PostLoad();
                Loger.Log("Server Create Data");
            }
            else
            {
                using (var fs = File.OpenRead(SaveFileName))
                {
                    var bf = new BinaryFormatter() { Binder = new ServerCoreSerializationBinder() };
                    Loger.Log("Server Load... " + (new FileInfo(SaveFileName).FullName));
                    Data = (BaseContainer)bf.Deserialize(fs);
                    //var dataVersion = Data.VersionNum;
                    Loger.Log("Server Version data: " + Data.Version + " Current version: " + MainHelper.VersionInfo);
                    Loger.Log($"Server unified version {MainHelper.VersionNum}");
                    if (Data.Version != MainHelper.VersionInfo || Data.VersionNum < MainHelper.VersionNum + 1)
                    {
                        //convertToLastVersion(); To delete??
                        needResave = true;
                    }

                    Data.PostLoad();

                    Loger.Log("Server Load done. Users " + Data.GetPlayersAll.Count.ToString() + ": "
                        + Data.GetPlayerLoginsAll.Aggregate((string)null, (r, i) => (r == null ? "" : r + ", ") + i)
                        );

                    ChatManager.Instance.NewChatManager(Data.MaxIdChat, Data.PlayerSystem.Chats.Keys.First());
                }
                Loger.Log($"Server local time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}");
                Loger.Log($"The difference between time zones: {(DateTime.UtcNow - DateTime.Now).ToString("g")}");
            }

            if (needResave)
                Save();
            ChangeData = false;
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
