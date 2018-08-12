using Model;
using OCServer.Model;
using OCUnion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace OCServer
{
    public class Repository
    {
        private static Repository Current = new Repository();
        public static Repository Get { get { return Current; } }

        public static BaseContainer GetData {  get { return Get.Data; } }

        public BaseContainer Data;
        public bool ChangeData;

        public readonly WorkTimer Timer;

        public string SaveFileName;

        public Repository()
        {
            Timer = new WorkTimer();
        }
        
        public void Load()
        {
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
                    Data = (BaseContainer)bf.Deserialize(fs);
                    Loger.Log("Server Load Data");
                    Loger.Log("Server Users " + Data.PlayersAll.Count.ToString() + ": "
                        + Data.PlayersAll.Select(p => p.Public.Login).Aggregate((string)null, (r, i) => (r == null ? "" : r + ", ") + i)
                        );
                    PlayerServer.PublicPosts = Data.PlayersAll[0].PublicChat.Posts;
                    /*
                    try ////////////////////////////////
                    {
                        Loger.Log("Server test " + Data.PlayersAll[1].Chats[0].Posts.Count + "=" + Data.PlayersAll[Data.PlayersAll.Count - 1].Chats[0].Posts.Count);
                        Data.PlayersAll[1].Chats[0].Posts.Add(new ChatPost());
                        Loger.Log("Server test " + Data.PlayersAll[1].Chats[0].Posts.Count + "=" + Data.PlayersAll[Data.PlayersAll.Count - 1].Chats[0].Posts.Count);
                    }
                    catch { }
                    */
                }
            }
            ChangeData = false;
        }

        public void Save(bool onlyChangeData = false)
        {
            if (onlyChangeData && !ChangeData) return;
            Loger.Log("Server Saved");
            /*
            try ///////////////////////////////
            {
                Data.PlayersAll[1].Chats[0].Posts.Add(new ChatPost());
                Loger.Log("Server test " + Data.PlayersAll[1].Chats[0].Posts.Count + "=" + Data.PlayersAll[2].Chats[0].Posts.Count);
            }
            catch { }
            */
            if (File.Exists(SaveFileName))
            {
                File.Copy(SaveFileName, SaveFileName + ".bak", true);
            }
            try
            {
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
        }
        
    }
}
