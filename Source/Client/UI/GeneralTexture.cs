using OCUnion;
using OCUnion.Transfer.Model;
using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity
{
    [StaticConstructorOnStartup]
    public class GeneralTexture
    {
        public static readonly Texture2D IconAddTex;
        public static readonly Texture2D IconDelTex;
        public static readonly Texture2D IconSubMenuTex;
        public static readonly Texture2D IconForums;
        public static readonly Texture2D IconSkull;
        public static readonly Texture2D TradeButtonIcon;
        public static readonly Texture2D Waypoint;
        //public static readonly Texture2D OC_Coin;
        public static readonly Texture2D Null;
        public static readonly Texture2D OCInfo;
        public static readonly Texture2D OCSystem;
        public static readonly Texture2D OCToChat;
        public static readonly Texture2D OCBug;
        public static readonly Texture2D IconHuman;
        public static readonly Texture2D OCE_To;
        public static readonly Texture2D OCE_Del;
        public static readonly Texture2D OCE_Sell;
        public static readonly Texture2D OCE_Trans;
        public static readonly Texture2D OCE_Swap;
        public static readonly Texture2D OCE_Add;

        static GeneralTexture()
        {
            IconAddTex = ContentFinder<Texture2D>.Get("OCAdd");
            IconDelTex = ContentFinder<Texture2D>.Get("OCDel");
            IconSubMenuTex = ContentFinder<Texture2D>.Get("OCSubMenu");
            IconForums = ContentFinder<Texture2D>.Get("Forums");   //"UI/HeroArt/WebIcons/Forums", true);
            IconSkull = ContentFinder<Texture2D>.Get("Skull");
            TradeButtonIcon = ContentFinder<Texture2D>.Get("Trade"); //Trade.png (рукопожатие с $)
            Waypoint = ContentFinder<Texture2D>.Get("Waypoint");
            //OC_Coin = ContentFinder<Texture2D>.Get("OC_Coin");
            Null = ContentFinder<Texture2D>.Get("Null");
            OCInfo = ContentFinder<Texture2D>.Get("OCInfo");
            OCSystem = ContentFinder<Texture2D>.Get("OCSystem");
            OCToChat = ContentFinder<Texture2D>.Get("OCToChat");
            OCBug = ContentFinder<Texture2D>.Get("OCBug");
            IconHuman = ContentFinder<Texture2D>.Get("IconHuman");
            OCE_To = ContentFinder<Texture2D>.Get("OCE_To");
            OCE_Del = ContentFinder<Texture2D>.Get("OCE_Del");
            OCE_Sell = ContentFinder<Texture2D>.Get("OCE_Sell");
            OCE_Trans = ContentFinder<Texture2D>.Get("OCE_Trans");
            OCE_Swap = ContentFinder<Texture2D>.Get("OCE_Swap");
            OCE_Add = ContentFinder<Texture2D>.Get("OCE_Add");

            Clear();
        }


        private class TextureContainer
        {
            public string Hash;
            public byte[] Data;
            public Texture2D _Texture;
            public Texture2D Texture
            {
                get
                {
                    if (_Texture != null) return _Texture;
                    if (Data == null || Data.Length == 0) return _Texture = Null;
                    return _Texture = GameUtils.GetTextureFromSaveData(Data);
                }
            }

            public TextureContainer()
            {
            }
            public TextureContainer(Texture2D texture)
            {
                _Texture = texture;
            }
        }

        public static GeneralTexture Get { get; private set; }

        /// <summary>
        /// Актуальные текстуры по имени
        /// </summary>
        private ConcurrentDictionary<string, TextureContainer> LoadedTextures = new ConcurrentDictionary<string, TextureContainer>();
        /// <summary>
        /// Время, когда текстура устареет по имени
        /// </summary>
        private Dictionary<string, DateTime> LoadedAgings = new Dictionary<string, DateTime>();
        /// <summary>
        /// Архивные текстуры, которые уже устарели
        /// </summary>
        private ConcurrentDictionary<string, TextureContainer> LoadedOldTextures = new ConcurrentDictionary<string, TextureContainer>();
        /// <summary>
        /// Список к загрузке (они уже есть в LoadedTextures но с путой картинкой Null)
        /// </summary>
        private HashSet<string> Loading = new HashSet<string>();
        /// <summary>
        /// Тот же спиок Loading, но в виде очереди
        /// </summary>
        private Queue<string> LoadingQueue = new Queue<string>();

        /// <summary>
        /// Кэш для GetDef и GetDefTextures
        /// </summary>
        private ConcurrentDictionary<string, Def> GetDefs = new ConcurrentDictionary<string, Def>();

        /// <summary>
        /// Кэш для GetDef и GetDefTextures
        /// </summary>
        private ConcurrentDictionary<Def, Texture2D> GetDefTextures = new ConcurrentDictionary<Def, Texture2D>();

        public static void Clear()
        {
            Get = new GeneralTexture();
        }
        private static bool Inited = false;
        public static void Init()
        {
            Clear();
            if (Inited) return;
            Inited = true;

            //Загружаем смайлики
            /* todo
            ModBaseData.RunMainThreadSync(() =>
            {
                Loger.Log("GetAllInFolder TTT() ");
                foreach (var txt in ContentFinder<Texture2D>.GetAllInFolder("Emoji"))
                {
                    Loger.Log("GetAllInFolder Emoji: " + txt.name); // name = "Emoji_1st_place_medal"
                }
            });
            */
        }

        public Def GetDef(string defName) => defName == null ? null :
            GetDefs.GetOrAdd(defName, (n) =>
            {
                Def def = (ThingDef)GenDefDatabase.GetDefSilentFail(typeof(ThingDef), defName, false);
                if (def == null) def = (WorldObjectDef)GenDefDatabase.GetDefSilentFail(typeof(WorldObjectDef), defName, false);
                return def;
            });

        public Texture2D GetDefTexture(Def def) => def == null ? null :
            GetDefTextures.GetOrAdd(def, (n) =>
            {
                if (def == null) return null;
                Texture2D texture;
                if (def is ThingDef)
                {
                    texture = ((ThingDef)def).GetUIIconForStuff(null); // Widgets.GetIconFor(def);// 
                }
                else if (def is WorldObjectDef)
                {
                    var defWO = def as WorldObjectDef;
                    texture = defWO.ExpandingIconTexture ?? (Texture2D)(defWO.Material?.mainTexture);
                }
                else
                    texture = null;

                return texture;
            });

        public Texture2D GetDefTexture(string defName) => GetDefTexture(GetDef(defName));

        /// <summary>
        /// Возвращает текстуру по кодовому имени. Варианты:
        /// pl_логинИкгрок - возвращает его иконку через FileSharingCategory.PlayerIcon
        /// cs_логинИкгрок@serverIdколонии - возвращает последний скриншот колонии
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Texture2D ByName(string name) => LoadedTextures.GetOrAdd(name, n =>
            {
                //добавляем в очередь на скачивание
                lock (Loading)
                {
                    if (!Loading.Contains(n))
                    {
                        Loading.Add(n);
                        LoadingQueue.Enqueue(n);
                    }
                }
                //пытаемся пока предоставить старое значение, либо прозарчную картинку
                return LoadedOldTextures.TryGetValue(n, out var res) ? res : new TextureContainer(Null);
            }).Texture;

        /// <summary>
        /// Событие обновления с сервера, должно вызываться значительно реже FPS
        /// </summary>
        /// <param name="connect"></param>
        public void Update(SessionClient connect)
        {
            int CountUpdateInRun = 1;
            int UpdateSecondColonyScreen = 60;

            lock (Loading)
            {
                //удаляем устаревшее
                var now = DateTime.UtcNow;
                foreach (var aging in LoadedAgings.Keys.ToList())
                {
                    if (LoadedAgings[aging] < now)
                    {
                        //удаляем из массива устаревших
                        LoadedAgings.Remove(aging);
                        //удаляем из загруженных, чтобы при следующем обращении поставить в очередь на загрузку
                        if (LoadedTextures.TryRemove(aging, out var old))
                        {
                            //добавляем в очередь устаревших, чтобы отдать картинку, пока не загрузилось
                            LoadedOldTextures.TryAdd(aging, old);
                        }
                    }
                }

                //загружаем
                for (int i = 0; i < CountUpdateInRun; i++)
                {
                    if (LoadingQueue.Count == 0) return;

                    TextureContainer texture;
                    TextureContainer oldTexture;

                    var name = LoadingQueue.Dequeue();
                    Loading.Remove(name);
                    if (!LoadedOldTextures.TryRemove(name, out oldTexture)) oldTexture = new TextureContainer(Null);

                    if (name.Length < 4) continue;
                    var sendName = name.Substring(3);

                    FileSharingCategory category;
                    if (name.StartsWith("pl_")) category = FileSharingCategory.PlayerIcon;
                    else if (name.StartsWith("cs_")) category = FileSharingCategory.ColonyScreen;
                    else continue;

                    var packet = connect.FileSharingDownload(new ModelFileSharing()
                    {
                        Category = category,
                        Name = sendName,
                        Hash = oldTexture.Hash
                    });

                    //если с сервера пришли данные, значит обновляем значения
                    if (packet?.Data != null && packet.Data.Length > 0)
                    {
                        texture = new TextureContainer()
                        {
                            Hash = packet.Hash, 
                            Data = packet.Data
                        };
                    }
                    else //иначе это признак, что данные не поменялись или что произошла ошибка, тогда восстанавливаем старые значения
                    {
                        if (packet?.Hash == null || oldTexture.Hash != packet?.Hash)
                        {
                            Loger.Log("Client GeneralTexture Error load: " + name + " " + oldTexture.Hash + "!=" + packet?.Hash, Loger.LogLevel.ERROR);
                        }

                        texture = oldTexture;
                    }

                    //задаем время устаревания, для кого нужно
                    if (name.StartsWith("cs_"))
                    {
                        LoadedAgings[name] = DateTime.UtcNow.AddSeconds(UpdateSecondColonyScreen);
                    }

                    LoadedTextures[name] = texture;
                }
            }
        }

    }
}
