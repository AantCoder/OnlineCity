using OCUnion;
using OCUnion.Transfer.Model;
using RimWorld;
using RimWorldOnlineCity.UI;
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
        public const int UpdateSecondColonyScreen = 60;

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
        public static readonly Texture2D Pawns;
        public static readonly Texture2D PawnsDown;
        public static readonly Texture2D PawnsBleed;
        public static readonly Texture2D PawnsNeedingTend;
        public static readonly Texture2D PawnsAnimal;
        public static readonly Texture2D ItemStash;
        public static readonly Texture2D AttackSettlement;
        public static readonly Texture2D OpenBox;
        public static readonly Texture2D Caravan;
        public static readonly Texture2D HomeAreaOn;
        public static readonly Texture2D RankingUp;
        public static readonly Texture2D RankingDown;
        public static readonly Texture2D BaseOnlineButtonShowMap;
        public static readonly Texture2D IncidentViewIcon;

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
            Pawns = ContentFinder<Texture2D>.Get("Pawns");
            PawnsDown = ContentFinder<Texture2D>.Get("PawnsDown");
            PawnsBleed = ContentFinder<Texture2D>.Get("PawnsBleed");
            PawnsNeedingTend = ContentFinder<Texture2D>.Get("PawnsNeedingTend");
            PawnsAnimal = ContentFinder<Texture2D>.Get("PawnsAnimal");
            ItemStash = ContentFinder<Texture2D>.Get("ItemStash");
            AttackSettlement = ContentFinder<Texture2D>.Get("AttackSettlement");
            OpenBox = ContentFinder<Texture2D>.Get("OpenBox");
            Caravan = ContentFinder<Texture2D>.Get("Caravan");
            HomeAreaOn = ContentFinder<Texture2D>.Get("HomeAreaOn");
            RankingUp = ContentFinder<Texture2D>.Get("RankingUp");
            RankingDown = ContentFinder<Texture2D>.Get("RankingDown");
            BaseOnlineButtonShowMap = ContentFinder<Texture2D>.Get("ShowMap");
            IncidentViewIcon = ContentFinder<Texture2D>.Get("IncidentViewIcon");

            Clear();
        }


        private class TextureContainer
        {
            public DateTime LoadTime;
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
        private List<string> LoadingQueue = new List<string>();
        /// <summary>
        /// Загружается непостредственно сейчас
        /// </summary>
        private HashSet<string> LoadingNow = new HashSet<string>();

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

        public Texture2D GetEmoji(string name)
        {
            name = name.Trim();
            if (name.StartsWith(":")) name = name.Substring(1, name.Length - 1);
            if (name.EndsWith(":")) name = name.Substring(0, name.Length - 1);
            name = $"Emoji/Emoji_" + name;

            Texture2D icon;
            if (!PanelText.GlobalImgs.TryGetValue(name, out icon))
            {
                try
                {
                    icon = ContentFinder<Texture2D>.Get(name, false);
                }
                catch
                {
                    icon = null;
                }
                if (icon != null) PanelText.GlobalImgs.Add(name, icon);
            }
            return icon;
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
                        LoadingQueue.Add(n);
                    }
                }
                //пытаемся пока предоставить старое значение, либо прозарчную картинку
                return LoadedOldTextures.TryGetValue(n, out var res) ? res : new TextureContainer(Null);
            }).Texture;

        /// <summary>
        /// Для изображений загруженных с сервера выдает время с момента загрузки, либо больше года, если нет данных (по разным причинам)
        /// </summary>
        public TimeSpan GetLoadTimeByName(string name) => LoadedTextures.TryGetValue(name, out var res)
            ? DateTime.UtcNow - res.LoadTime
            : LoadedOldTextures.TryGetValue(name, out res)
            ? DateTime.UtcNow - res.LoadTime
            : TimeSpan.MaxValue;

        /// <summary>
        /// Была попытка загрузки, но данных на сервере нет
        /// </summary>
        public bool IsNotDataByName(string name) => IsNotDataByLoadTime(GetLoadTimeByName(name));

        /// <summary>
        /// Была попытка загрузки, но данных на сервере нет
        /// </summary>
        public bool IsNotDataByLoadTime(TimeSpan time) => time < TimeSpan.MaxValue && time > new TimeSpan(1, 0, 0, 0);

        /// <summary>
        /// Не было завершенных попыток загрузить данные
        /// </summary>
        public bool IsNotCheckByName(string name) => IsNotCheckByLoadTime(GetLoadTimeByName(name));

        /// <summary>
        /// Не было завершенных попыток загрузить данные
        /// </summary>
        public bool IsNotCheckByLoadTime(TimeSpan time) => time == TimeSpan.MaxValue;

        /// <summary>
        /// Загрузка ещё не завершена, по ByName возвращается старое значение, если оно есть
        /// </summary>
        public bool IsLoadingByName(string name)
        {
            lock (LoadingNow) 
            { 
                if (LoadingQueue.Contains(name)) return true;
                return LoadingNow.Contains(name);
            } 
        }

        /// <summary>
        /// Событие обновления с сервера, должно вызываться значительно реже FPS
        /// </summary>
        /// <param name="connect"></param>
        public void Update(SessionClient connect)
        {
            int CountUpdateInRun = 1;
            int CountCheckInRun = 100;

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

                //предварительно проверяем хэш количеством по CountCheckInRun
                if (LoadingQueue.Count > 3)
                {
                    //получаем данные из системы кэша файлового или LoadedOldTextures
                    var loadingWork = new List<Tuple<string, ModelFileSharing>>(); //тут name и hash из кэша для проверки на сервере
                    for (int i = 0; i < CountCheckInRun && i < LoadingQueue.Count; i++)
                    {
                        var name = LoadingQueue[i];

                        LoadedOldTextures.TryGetValue(name, out var oldTexture);
                        var hash = oldTexture?.Hash ?? CacheResource.GetHash(name);

                        var mfs = GetModelFileSharing(name, hash);
                        if (mfs == null) break;

                        loadingWork.Add(new Tuple<string, ModelFileSharing>(name, mfs));
                    }

                    //делаем быстрый запрос на получених хеша
                    var checkResult = connect.FileSharingDownloadOnlyCheck(loadingWork.Select(item => item.Item2).ToList());

                    if (checkResult != null)
                    {
                        for (int i = loadingWork.Count - 1; i >= 0 ; i--)
                        {
                            var item = loadingWork[i];
                            var name = item.Item1;
                            var res = checkResult[i];

                            if (res?.Hash == null  || item.Item2.Hash == res?.Hash)
                            {
                                //ответ пришел и на сервере нет данного файла, либо у нас ровно то же содержимое

                                if (!LoadedOldTextures.TryGetValue(name, out var texture))
                                {
                                    if (res?.Hash == null) texture = new TextureContainer(Null);
                                    else
                                    {
                                        texture = new TextureContainer()
                                        {
                                            Hash = item.Item2.Hash,
                                            Data = CacheResource.GetData(name),
                                        };
                                    }
                                }

                                //применяем
                                LoadingQueue.RemoveAt(i);
                                Loading.Remove(name);

                                SetLoadedTextures(name, texture);
                            }
                        }
                    }
                }

                //загружаем количеством по CountUpdateInRun
                for (int i = 0; i < CountUpdateInRun; i++)
                {
                    if (LoadingQueue.Count == 0) return;

                    TextureContainer texture;
                    TextureContainer oldTexture;

                    string name = null;
                    try
                    {
                        lock (LoadingNow)
                        {
                            name = LoadingQueue[0];
                            LoadingQueue.RemoveAt(0);
                            LoadingNow.Add(name);
                            Loading.Remove(name);
                        }
                        if (!LoadedOldTextures.TryRemove(name, out oldTexture)) oldTexture = new TextureContainer(Null);

                        var mfs = GetModelFileSharing(name, oldTexture.Hash);
                        if (mfs == null) continue;

                        var packet = connect.FileSharingDownload(mfs);

                        //если с сервера пришли данные, значит обновляем значения
                        if (packet?.Data != null && packet.Data.Length > 0)
                        {
                            texture = new TextureContainer()
                            {
                                Hash = packet.Hash,
                                Data = packet.Data,
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
                        SetLoadedTextures(name, texture);
                    }
                    finally
                    {
                        if (name != null) LoadingNow.Remove(name);
                    }
                }
            }
        }

        private void SetLoadedTextures(string name, TextureContainer texture)
        { 
            texture.LoadTime = DateTime.UtcNow;

            //задаем время устаревания, для кого нужно
            if (name.StartsWith("cs_"))
            {
                LoadedAgings[name] = DateTime.UtcNow.AddSeconds(UpdateSecondColonyScreen);
            }

            LoadedTextures[name] = texture;

            if (texture.Data != null) CacheResource.SetData(name, texture.Data);
        }

        private ModelFileSharing GetModelFileSharing(string name, string hash)
        {
            if (name.Length < 4) return null;
            var sendName = name.Substring(3);

            FileSharingCategory category;
            if (name.StartsWith("pl_")) category = FileSharingCategory.PlayerIcon;
            else if (name.StartsWith("cs_")) category = FileSharingCategory.ColonyScreen;
            else return null;

            return new ModelFileSharing()
            {
                Category = category,
                Name = sendName,
                Hash = hash
            };
        }

    }
}
