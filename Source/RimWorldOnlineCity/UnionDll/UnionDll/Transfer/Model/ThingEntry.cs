using OCUnion;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fractionColonist"></param>
        /// <param name="fractionPirate"></param>
        /// <returns>Истина, если это особый вид - пленник</returns>
        public bool SetFaction(string fractionColonist, string fractionPirate)
        {
            if (string.IsNullOrEmpty(Data) || !Data.Contains(" Class=\"Pawn\"")) return false;
            if (MainHelper.DebugMode) File.WriteAllText(Loger.PathLog + "MailPawnB.xml", Data);

            bool col = Data.Contains("<kindDef>Colonist</kindDef>");
            string fraction = fractionColonist; //col ? fractionColonist : fractionPirate;
            Data = GameXMLUtils.ReplaceByTag(Data, "faction", fraction);
            Data = GameXMLUtils.ReplaceByTag(Data, "kindDef", "Colonist" /*"Pirate"*/);
            //if (MainHelper.DebugMode) Loger.Log(" Replace faction=>" + fraction);

            //если это гости, то убираем у них это свойство - оно должно выставиться потом

            Data = GameXMLUtils.ReplaceByTag(Data, "guest", @"
    <hostFaction>null</hostFaction>
    <interactionMode>NoInteraction</interactionMode>
    <spotToWaitInsteadOfEscaping>(-1000, -1000, -1000)</spotToWaitInsteadOfEscaping>
    <lastPrisonBreakTicks>-1</lastPrisonBreakTicks>
  ");
            /*
            Data = GameXMLUtils.ReplaceByTag(Data, "hostFaction", "null", "<guest>");
            Data = GameXMLUtils.ReplaceByTag(Data, "prisoner", "False", "<guest>");*/

            /* попытка передавать заключенных не получилась
            Data = GameXMLUtils.ReplaceByTag(Data, "hostFaction", 
                (val) =>
                {
                    if (MainHelper.DebugMode) Loger.Log(" Replace hostFaction "+ val + "=>" + fractionColonist);
                    return val == "null" ? null : fractionColonist;

                });
                */

            if (MainHelper.DebugMode) File.WriteAllText(Loger.PathLog + "MailPawnA.xml", Data);
            return !col;
        }
    }
}
