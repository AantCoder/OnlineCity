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
        /// <summary>
        /// ID объекта до передачи
        /// </summary>
        public int TransportID { get; set; }

        public bool isColonist { get; set; }

        protected ThingEntry()
        { }

        public static ThingEntry CreateEntry(Thing thing, int count)
        {
            var that = new ThingEntry();
            that.SetBaseInfo(thing, count);
            that.SetData(thing);
            return that;
        }

        protected void SetBaseInfo(Thing thing, int count)
        {
            Name = thing.LabelCapNoCount;
            Count = count;
            OriginalID = thing.thingIDNumber;
            isColonist = thing.Faction == Faction.OfPlayer ? true : false;
        }

        protected void SetData(Thing thing)
        {
            var gx = new GameXMLUtils();
            Data = gx.ToXml(thing);
        }

        public virtual Thing CreateThing(bool useOriginalID = false, int stackCount = 0)
        {
            var gx = new GameXMLUtils();
            Thing thing = gx.FromXml<Thing>(Data);
            thing.stackCount = stackCount == 0 ? Count : stackCount;
            if (OriginalID <= 0 || !useOriginalID)
            {
                thing.thingIDNumber = -1;
                ThingIDMaker.GiveIDTo(thing);
            }
            else
            {
                thing.thingIDNumber = OriginalID;
            }
            if (thing.def.CanHaveFaction)
            {
                if (isColonist)
                {
                    thing.SetFaction(Faction.OfPlayer);
                }
                else
                {
                    thing.SetFaction(Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.defName == "Pirate"));
                }
            }
            return thing;
        }

        private static int nnnn = 0;
        /// <summary>
        /// Меняет у людей (всех пешек не животных) фракцию на фракцию игрока (иначе посе спавна без косяков не получается поменять у пешек фракцию на фракцию игрока).
        /// Возвращает истину, когда это был пират или пленник
        /// </summary>
        /// <param name="fractionColonist"></param>
        /// <returns>Истина, если это особый вид - пленник</returns>
        public bool SetFaction(string fractionColonist)
        {
            if (string.IsNullOrEmpty(Data) || !Data.Contains(" Class=\"Pawn\"")) return false;
            if (MainHelper.DebugMode) File.WriteAllText(Loger.PathLog + "MailPawnB" + (++nnnn).ToString() + ".xml", Data);
            //логика коррекции основывается на 3х группыах:
            //колонист, человек не колонист (пират или пленник), животное
            bool col = Data.Contains("<kindDef>Colonist</kindDef>");
            if (!col) col = Data.Contains("ColonistGeneral</kindDef>"); //для мода с андроидами
            bool isAnimal = GameXMLUtils.GetByTag(Data, "def") == GameXMLUtils.GetByTag(Data, "kindDef");
            //для всех людей устанавливаем фракцию игрока (у животных не меняем)
            if (!isAnimal)
            {
                string fraction = fractionColonist; //col ? fractionColonist : fractionPirate;
                Data = GameXMLUtils.ReplaceByTag(Data, "faction", fraction);
                if (!col) Data = GameXMLUtils.ReplaceByTag(Data, "kindDef", "Colonist"); //"Pirate"
                //if (MainHelper.DebugMode) Loger.Log(" Replace faction=>" + fraction);
            }
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
            //возвращаем true, если это человек и не колонист (пират или пленник)
            if (MainHelper.DebugMode) File.WriteAllText(Loger.PathLog + "MailPawnA" + nnnn.ToString() + ".xml", Data);
            return !col && !isAnimal;
        }
    }
}
