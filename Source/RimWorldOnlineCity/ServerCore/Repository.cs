using Model;
using OCServer.Model;
using OCUnion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Transfer;
using Util;

namespace OCServer
{
    public class Repository
    {
        private static Repository Current = new Repository();
        public static Repository Get { get { return Current; } }

        public const string DISCORD = "Discord";

        public static BaseContainer GetData {  get { return Get.Data; } }

        public BaseContainer Data;
        public bool ChangeData;

        public readonly WorkTimer Timer;

        public string SaveFileName;
        public string SaveFolderDataPlayers => Path.Combine(Path.GetDirectoryName(SaveFileName), "DataPlayers");

        private Repository()
        {
            Timer = new WorkTimer();
        }
        
        public void Load()
        {
            bool needResave = false;
            if (!Directory.Exists(SaveFolderDataPlayers))
            {
                Directory.CreateDirectory(SaveFolderDataPlayers);
            }

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
                    var bf = new BinaryFormatter();
                    Loger.Log("Server Load...");
                    Data = (BaseContainer)bf.Deserialize(fs);
                    //var dataVersion = Data.VersionNum;
                    Loger.Log("Server Version data: " + Data.Version + " Current version: " + MainHelper.VersionInfo);

                    if (Data.Version != MainHelper.VersionInfo)
                    {
                        Data.Version = MainHelper.VersionInfo;
                        needResave = true;
                    }

                    PlayerServer.PublicPosts = Data.PlayersAll[0].PublicChat.Posts;
                    if (Data.Orders == null) Data.Orders = new List<OrderTrade>();

                    /*
                    try ////////////////////////////////
                    {
                        Loger.Log("Server test " + Data.PlayersAll[1].Chats[0].Posts.Count + "=" + Data.PlayersAll[Data.PlayersAll.Count - 1].Chats[0].Posts.Count);
                        Data.PlayersAll[1].Chats[0].Posts.Add(new ChatPost());
                        Loger.Log("Server test " + Data.PlayersAll[1].Chats[0].Posts.Count + "=" + Data.PlayersAll[Data.PlayersAll.Count - 1].Chats[0].Posts.Count);
                    }
                    catch { }
                    */
                    
                    Loger.Log("Server Load done. Users " + Data.PlayersAll.Count.ToString() + ": "
                        + Data.PlayersAll.Select(p => p.Public.Login).Aggregate((string)null, (r, i) => (r == null ? "" : r + ", ") + i)
                        );
                }
            }
            if (needResave)
                Save();
            ChangeData = false;
        }

        public byte[] LoadPlayerData(string login)
        {
            var fileName = Path.Combine(SaveFolderDataPlayers, NormalizeLogin(login) + ".dat");
            
            var info = new FileInfo(fileName);
            if (!info.Exists || info.Length < 10) return null;
            
            //читаем содержимое
            bool readAsXml;
            using (var file = File.OpenRead(fileName))
            {
                var buff = new byte[10];
                file.Read(buff, 0, 10);
                readAsXml = Encoding.ASCII.GetString(buff, 0, 10).Contains("<?xml");
            }

            //считываем текст как xml сейва или как сжатого zip'а
            var saveFileData = File.ReadAllBytes(fileName);
            if (readAsXml)
            {
                return saveFileData;
            }
            else
            {
                return GZip.UnzipByteByte(saveFileData);
            }
        }

        public void Save(bool onlyChangeData = false)
        {
            if (onlyChangeData && !ChangeData) return;
            Loger.Log("Server Saving");
            /*
            try ///////////////////////////////
            {
                Data.PlayersAll[1].Chats[0].Posts.Add(new ChatPost());
                Loger.Log("Server test " + Data.PlayersAll[1].Chats[0].Posts.Count + "=" + Data.PlayersAll[2].Chats[0].Posts.Count);
            }
            catch { }
            */
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
        
        public void SavePlayerData(string login, byte[] data)
        {
            var fileName = Path.Combine(SaveFolderDataPlayers, NormalizeLogin(login) + ".dat");
            if (File.Exists(fileName))
            {
                var info = new FileInfo(fileName);
                if (File.Exists(fileName + ".bak")) File.Delete(fileName + ".bak");
                File.Move(fileName, fileName + ".bak");
            }

            if (data == null || data.Length < 10) return;

            File.WriteAllBytes(fileName, data);
            Loger.Log("Server User " + Path.GetFileNameWithoutExtension(fileName) + " saved.");
        }
        
        public static string NormalizeLogin(string login)
        {
            char[] invalidFileChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidFileChars) if (login.Contains(c)) login = login.Replace(c, '_');
            return login.ToLowerInvariant();
        }
    }
}
