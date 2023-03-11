using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Transfer
{
    [Serializable]
    public class TradeOrder : TradeOrderShort
    {
        // Id на сервере. 
        // Если 0 - то добавить новый.
        // Если больше 0 - отредактировать
        // Если меньше 0 - удалить c Id без минуса

        /// <summary>
        /// Создатель сделки, его имя в LoginOwner
        /// </summary>
        [XmlIgnore]
        public Player Owner { get; set; }

        /// <summary>
        /// Где находиться товар
        /// </summary>
        public Place Place { get; set; }

        /// <summary>
        /// Время размещения на сервере
        /// </summary>
        public DateTime Created  { get; set; }

        /// <summary>
        /// Вещи (тип и кол-во) которые отдаст Owner. Все должны быть Concrete = true
        /// </summary>
        public List<ThingTrade> SellThings { get; set; }

        /// <summary>
        /// Вещи которые получит Owner. Могут быть с любым Concrete
        /// Может быть использовать только как фильтр: BuyThings[i].MatchesThing()
        /// </summary>
        public List<ThingTrade> BuyThings { get; set; }

        /// <summary>
        /// Кол-во сделок успешно реализованнх
        /// </summary>
        public int CountFnished { get; set; }

        /// <summary>
        /// Кол-во доступных сделок. Определяет кол-во заблокированных вещей (SellThings*CountReady)
        /// </summary>
        public int CountReady { get; set; }

        /// <summary>
        /// Если список не пустой, то это приватная сделка доступная только перечисленным игрокам
        /// </summary>
        public List<Player> PrivatPlayers { get; set; }

        /// <summary>
        /// Требует ручного сброса в null при изменении SellThings
        /// </summary>
        public int SellThingsHash
        {
            get
            {
                if (_SellThingsHash == 0) _SellThingsHash = GetThingsHash(SellThings);
                return _SellThingsHash;
            }
            set => _SellThingsHash = value;
        }

        [NonSerialized]
        [XmlIgnore]
        private int _SellThingsHash;

        /// <summary>
        /// Требует ручного сброса в null при изменении SellThings
        /// </summary>
        public int BuyThingsHash
        {
            get
            {
                if (_BuyThingsHash == 0) _BuyThingsHash = GetThingsHash(BuyThings);
                return _BuyThingsHash;
            }
            set => _BuyThingsHash = value;
        }

        [NonSerialized]
        [XmlIgnore]
        private int _BuyThingsHash;

        /// <summary>
        /// Хеш, для предварительно проверки простых условий.
        /// Соответствует ли одни список вещей другому, по типу вещей (точнее то, что будет проверяться на строгое равенство)
        /// </summary>
        private int GetThingsHash(List<ThingTrade> things)
        {
            return GetThingsHashString(things)
                .GetHashCode();
        }

        private string GetThingsHashString(List<ThingTrade> things)
        {
            return things.Count == 0 ? "empty" 
                : things.Select(t => t.DefName + "(" + t.PawnParam + ")" + (t.Count == 1 ? "" : "*" + t.Count) + "#")
                .OrderBy(t => t)
                .Distinct()
                .Aggregate((r, i) => r + i);
        }

        public override string ToString()
        {
            return "Order " + Id + " " + Owner.Login + " tile=" + Tile + " cnt=" + this.CountReady
                + " " + GetThingsHashString(BuyThings) + " ==buyToSell==> " + GetThingsHashString(SellThings)
                + (PrivatPlayers != null && PrivatPlayers.Count > 0 ? " privat=" + string.Join(",", PrivatPlayers.Select(p => p.Login)) : "");
        }

        public TradeOrder Clone()
        {
            var clone =  (TradeOrder)this.MemberwiseClone();
            clone.SellThings = this.SellThings.Select(t => (ThingTrade)t.Clone()).ToList();
            clone.BuyThings = this.BuyThings.Select(t => (ThingTrade)t.Clone()).ToList();
            clone.PrivatPlayers = new List<Player>(this.PrivatPlayers);
            return clone;
        }

    }
}
