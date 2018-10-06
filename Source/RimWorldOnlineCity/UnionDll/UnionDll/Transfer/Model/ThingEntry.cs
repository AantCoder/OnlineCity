using OCUnion;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace Model
{
    /// <summary>
    /// Модель хранящая игровой объект с для сериализации и восстановления.
    /// </summary>
    [Serializable]
    public class ThingEntry
    {
        /// <summary>
        /// Имя как то, которое выводится в интерфейсе игры
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Количество
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// XML с данными
        /// </summary>
        public string Data { get; set; }
        /// <summary>
        /// Оригинальный ID
        /// </summary>
        public int OriginalID { get; set; }

        protected ThingEntry()
        { }

        public static ThingEntry CreateEntry(Thing thing, int count)
        {
            var that = new ThingEntry();
            that.SetBaseInfo(thing, count);         
            return that;
        }

        protected void SetBaseInfo(Thing thing, int count)
        {
            Name = thing.LabelCapNoCount;
            Count = count;
            OriginalID = thing.thingIDNumber;

            var gx = new GameXMLUtils();
            Data = gx.ToXml(thing);
        }

        public virtual Thing CreateThing(bool useOriginalID, int stackCount = 0)
        {
            var gx = new GameXMLUtils();
            Thing thing = gx.FromXml<Thing>(Data);
            thing.stackCount = stackCount == 0 ? Count : stackCount;
            if (OriginalID <= 0 || useOriginalID)
            {
                thing.thingIDNumber = -1;
                ThingIDMaker.GiveIDTo(thing);
            }
            else
                thing.thingIDNumber = OriginalID;
            return thing;
            /*
            var def = (ThingDef)GenDefDatabase.GetDef(typeof(ThingDef), DefName);
            var stuffDef = !string.IsNullOrEmpty(StuffName) ? (ThingDef)GenDefDatabase.GetDef(typeof(ThingDef), StuffName) : GenStuff.DefaultStuffFor(def);
            Thing thing = ThingMaker.MakeThing(def, stuffDef);
            thing.stackCount = stackCount == 0 ? Count : stackCount;

            if (HitPoints > 0) thing.HitPoints = HitPoints;
            
            CompQuality compQuality = thing.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                compQuality.SetQuality((QualityCategory)Quality, ArtGenerationContext.Outsider);
            }

            if (WornByCorpse)
            {
                Apparel thingA = thing as Apparel;
                if (thingA != null)
                {
                    typeof(Apparel)
                       .GetField("wornByCorpseInt", BindingFlags.Instance | BindingFlags.NonPublic)
                       .SetValue(thingA, true);
                }
            }
            return thing;
            */
        }

    }
}
