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
        /*
        /// <summary>
        /// Тип вещи
        /// </summary>
        public string DefName { get; set; }
        /// <summary>
        /// Материал из чего изготовлено
        /// </summary>
        public string StuffName { get; set; }
        /// <summary>
        /// Текущая прочность, если 0 считается мсксимальной
        /// </summary>
        public int HitPoints { get; set; }
        /// <summary>
        /// Максимальная прочность (только информационно)
        /// </summary>
        public int MaxHitPoints { get; set; }
        /// <summary>
        /// Качество изготовления
        /// </summary>
        public int Quality { get; set; }
        /// <summary>
        /// Снято с трупа, применимо только к одежде
        /// </summary>
        public bool WornByCorpse { get; set; }
        */

        private ThingEntry()
        { }

        public ThingEntry(Thing thing, int count)
        {
            Name = thing.LabelCapNoCount;
            Count = count;
            OriginalID = thing.thingIDNumber;

            var gx = new GameXMLUtils();
            Data = gx.ToXml(thing);

            /*
            DefName = thing.def.defName;
            StuffName = thing.Stuff == null ? null : thing.Stuff.defName;
            HitPoints = thing.HitPoints;
            MaxHitPoints = thing.MaxHitPoints;
            
            QualityCategory qq;
            if (QualityUtility.TryGetQuality(thing, out qq)) Quality = (int)qq - 3;

            Apparel thingA = thing as Apparel;
            if (thingA != null) WornByCorpse = thingA.WornByCorpse;
            */
        }

        public Thing CreateThing(bool useOriginalID, int stackCount = 0)
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
                compQuality.SetQuality((QualityCategory)(Quality + 3), ArtGenerationContext.Outsider);
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
