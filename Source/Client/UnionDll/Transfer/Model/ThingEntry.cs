using OCUnion;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using Verse;

namespace Model
{
    public enum PawnAffiliation
    {
        Thing,
        /// <summary>
        /// Колонист
        /// </summary>
        Colonist,
        /// <summary>
        /// Пленник, передается пират под наркозом
        /// </summary>
        Prisoner,
        /// <summary>
        /// Раб
        /// </summary>
        Slave,
        /// <summary>
        /// Пленник, передается пират без наркоза
        /// </summary>
        Enemy
    }
    public enum PawnIdeo
    {
        Empty,
        /// <summary>
        /// Идеология игрока
        /// </summary>
        Colonist,
        /// <summary>
        /// Любая другая идеология
        /// </summary>
        Enemy
    }

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
        [XmlIgnore]
        public string Data { get; set; }
        /// <summary>
        /// Хеш поля Data для передачи его через AnyLoad / UploadService
        /// </summary>
        public long DataHash { get; set; }
        /// <summary>
        /// Оригинальный ID
        /// </summary>
        public int OriginalID { get; set; }
        /// <summary>
        /// ID объекта до передачи
        /// </summary>
        public int TransportID { get; set; }

        public PawnAffiliation Affiliation { get; set; }
        public PawnIdeo Ideo { get; set; }

        public bool IsCorpse { get; set; }

        public string LabelTextShort => Name + (Count > 1 ? " x" + Count.ToString() : "")
            + (IsCorpse ? " (corpse)": "")
            + (Affiliation == PawnAffiliation.Prisoner ? " (prisoner)" /* ⚯ ↀ "TabPrisoner"*/ //нельзя локализовать из-за использования на сервере
                : Affiliation == PawnAffiliation.Slave ? " (slave)" /* ☹ ꃢ  "Slave"*/: "");

        public override string ToString()
        {
            return LabelTextShort;
        }

        protected ThingEntry()
        { }

        public static ThingEntry CreateEntry(Thing thing, int count)
        {
            var that = new ThingEntry();
            if (thing is Corpse)
            {
                thing = (thing as Corpse).InnerPawn;
                that.IsCorpse = true;
            }
            that.SetBaseInfo(thing, count);
            that.SetData(thing);
            return that;
        }

        protected void SetBaseInfo(Thing thing, int count)
        {
            Name = thing.LabelCapNoCount;
            Count = count;
            OriginalID = thing.thingIDNumber;
            var pawn = thing as Pawn;
            if (pawn != null)
            {
                Affiliation = pawn.IsSlave ? PawnAffiliation.Slave
                    : pawn.IsPrisoner ? PawnAffiliation.Prisoner
                    : pawn.Faction == Faction.OfPlayer ? PawnAffiliation.Colonist
                    : PawnAffiliation.Enemy;
                Ideo = pawn.Ideo == null ? PawnIdeo.Empty
                    : Faction.OfPlayer.ideos.PrimaryIdeo == pawn.Ideo ? PawnIdeo.Colonist : PawnIdeo.Enemy;
            }
            else
            {
                Affiliation = PawnAffiliation.Thing;
                Ideo = PawnIdeo.Empty;
            }
        }

        protected void SetData(Thing thing)
        {
            var gx = new GameXMLUtils();
            Data = gx.ToXml(thing);
        }

        public virtual Thing CreateThing(bool useOriginalID = false, int stackCount = 0)
        {
            //Loger.Log($"CreateThing " + Name);
            var data = Data;
            if (OriginalID <= 0 || !useOriginalID)
            {
                data = PrepareID(() => Find.UniqueIDsManager.GetNextThingID());
            }
            else
            {
                data = PrepareID(() => OriginalID);
            }

            //Loger.Log($"CreateThing 1");
            if (Affiliation != PawnAffiliation.Thing) data = PrepareReferencing(data);

            var gx = new GameXMLUtils();
            //Loger.Log($"CreateThing 2");
            Thing thing = gx.FromXml<Thing>(data);
            //Loger.Log($"CreateThing 3");
            thing.stackCount = stackCount == 0 ? Count : stackCount;
            
            //if (MainHelper.DebugMode && thing.LabelCap == "Lighter, Служанка") Log.Message($" ==0 {thing.LabelCap} hc{thing.GetHashCode()} id{thing.ThingID}");
            //if (OriginalID <= 0 || !useOriginalID)
            //{
            //    if (MainHelper.DebugMode && thing.LabelCap == "Lighter, Служанка") Log.Message($" ==0 {thing.LabelCap} hc{thing.GetHashCode()} id{thing.ThingID}");
            //    thing.thingIDNumber = -1;
            //    ThingIDMaker.GiveIDTo(thing);
            //    if (MainHelper.DebugMode && thing.LabelCap == "Lighter, Служанка") Log.Message($" ==1 {thing.LabelCap} hc{thing.GetHashCode()} id{thing.ThingID}");
            //}
            //else
            //{
            //    thing.thingIDNumber = OriginalID;
            //}

            SetFaction(thing, Affiliation == PawnAffiliation.Colonist || Affiliation == PawnAffiliation.Thing);

            //Loger.Log($"CreateThing 4");
            if (thing is Pawn)
            {
                var p = thing as Pawn;

                if (Affiliation == PawnAffiliation.Prisoner)
                {
                    p.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
                    //p.health.AddHediff(HediffDefOf.Anesthetic);
                }

                if (Affiliation == PawnAffiliation.Slave)
                {
                    p.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Slave);
                }
            }
            //Loger.Log($"CreateThing 5");
            return thing;
        }

        protected void SetFaction(Thing thing, bool isColonist)
        {
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
        }

        public string PrepareID(Func<int> ID)
        {
            if (MainHelper.DebugMode) File.WriteAllText(Loger.PathLog + "MailIDB_" + (++nnnn).ToString() + ".xml", Data);
            var defName = GameXMLUtils.GetByTag(Data, "def") ?? "";
            var id = GameXMLUtils.GetByTag(Data, "id") ?? "";
            var data = Data;
            if (defName != "" && id.StartsWith(defName))
            {
                var newid = defName + ID() + "<";
                id += "<";
                if (id != newid)
                {
                    while (data.Contains(id))
                    {
                        data = data.Replace(id, newid);
                    }
                }
            }
            if (MainHelper.DebugMode) File.WriteAllText(Loger.PathLog + "MailIDA_" + (++nnnn).ToString() + ".xml", data);
            return data;
        }
        private string PrepareReferencing(string data)
        {
            var factionColonistLoadID = Find.FactionManager.OfPlayer.GetUniqueLoadID();
            //var fractionRoyaltyLoadID = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Empire)?.GetUniqueLoadID();
            var fractionRoyalty = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Empire);
            var ideo = Find.FactionManager.OfPlayer.ideos?.PrimaryIdeo;

            //if (string.IsNullOrEmpty(fractionRoyaltyLoadID)) Loger.Log("Client fractionRoyaltyLoadID is not find");

            //var factionPirateLoadID = factionPirate.GetUniqueLoadID();

            //меняем фракцию на игрока для всех

            if (string.IsNullOrEmpty(data) || !data.Contains(" Class=\"Pawn\"")) return data;
            if (MainHelper.DebugMode) File.WriteAllText(Loger.PathLog + "MailPawnB" + (++nnnn).ToString() + ".xml", data);

            RegisterReferencing(Find.FactionManager.OfPlayer);

            //логика коррекции основывается на 3х группах:
            //колонист, человек не колонист (пират или пленник), животное

            bool col = data.Contains("<kindDef>Colonist</kindDef>");
            if (!col) col = data.Contains("ColonistGeneral</kindDef>"); //для мода с андроидами

            bool isAnimal = GameXMLUtils.GetByTag(data, "def") == GameXMLUtils.GetByTag(data, "kindDef");

            //для всех людей устанавливаем фракцию игрока (у животных не меняем)

            if (!isAnimal)
            {
                string fraction = factionColonistLoadID; //col ? fractionColonist : fractionPirate;
                data = GameXMLUtils.ReplaceByTag(data, "faction", fraction);
                if (!col) data = GameXMLUtils.ReplaceByTag(data, "kindDef", "Colonist"); //or "Pirate"
                //if (MainHelper.DebugMode) Loger.Log(" Replace faction=>" + fraction);
            }

            //если это гости, то убираем у них это свойство - оно должно выставиться потом
            /*
            data = GameXMLUtils.ReplaceByTag(data, "guest", @"
    <hostFaction>null</hostFaction>
    <interactionMode>NoInteraction</interactionMode>
    <spotToWaitInsteadOfEscaping>(-1000, -1000, -1000)</spotToWaitInsteadOfEscaping>
    <lastPrisonBreakTicks>-1</lastPrisonBreakTicks>
  ");
            */
            data = GameXMLUtils.ReplaceByTag(data, "guest", @"
    <hostFaction>null</hostFaction>
    <slaveFaction>null</slaveFaction>
    <joinStatus>JoinAsSlave</joinStatus>
    <interactionMode>NoInteraction</interactionMode>
    <slaveInteractionMode>NoInteraction</slaveInteractionMode>
    <spotToWaitInsteadOfEscaping>(-1000, -1000, -1000)</spotToWaitInsteadOfEscaping>
    <lastPrisonBreakTicks>-1</lastPrisonBreakTicks>
    <ideoForConversion>null</ideoForConversion>
  ");

            //локализуем фракцию роялти
            var tagRoyalty = GameXMLUtils.GetByTag(data, "royalty");
            if (tagRoyalty != null)
            {
                if (MainHelper.DebugMode) Loger.Log("Client PrepareSpawnThingEntry fractionRoyalty RegisterReferencing");
                RegisterReferencing(fractionRoyalty);
                var fractionRoyaltyLoadID = fractionRoyalty.GetUniqueLoadID();

                string oldTR;
                do
                {
                    oldTR = tagRoyalty;
                    tagRoyalty = GameXMLUtils.ReplaceByTag(tagRoyalty, "faction", fractionRoyaltyLoadID);
                } while (oldTR != tagRoyalty);
                data = GameXMLUtils.ReplaceByTag(data, "royalty", tagRoyalty);
            }

            //идеология
            var ideoIndex = data.IndexOf("<ideo>");
            if (ideoIndex > 0)
            {
                /* Пример:
						<ideo>
							<ideo>Ideo_9</ideo>
							<previousIdeos />
							<certainty>0.6544925</certainty>
						</ideo>
                 */

                if (Ideo == PawnIdeo.Empty)
                {
                    //удаляем у пешки идеологию
                    if (MainHelper.DebugMode) Loger.Log("Client PrepareSpawnThingEntry ideo Clear");
                    var iiClose1 = data.IndexOf("</ideo>", ideoIndex);
                    if (iiClose1 >= 0)
                    {
                        var iiClose2 = data.IndexOf("</ideo>", iiClose1 + 1);
                        if (iiClose2 >= 0) iiClose1 = iiClose2;
                        iiClose1 += "</ideo>".Length;
                        data = data.Remove(ideoIndex, iiClose1 - ideoIndex);
                    }
                }
                else
                {
                    var iiClose1 = data.IndexOf("</ideo>", ideoIndex);
                    if (iiClose1 >= 0)
                    {
                        var iiClose2 = data.IndexOf("</ideo>", iiClose1 + 1);
                        if (iiClose2 >= 0)
                        {
                            string ideoLoadID;
                            if (Ideo == PawnIdeo.Colonist)
                            {
                                if (MainHelper.DebugMode) Loger.Log("Client PrepareSpawnThingEntry ideo Colonist RegisterReferencing");
                                RegisterReferencing(ideo);
                                ideoLoadID = ideo.GetUniqueLoadID();
                            }
                            else
                            {
                                if (MainHelper.DebugMode) Loger.Log("Client PrepareSpawnThingEntry ideo Enemy RegisterReferencing");
                                //Для определенной пешки, у игрока будет всегда одна и так же случайная идеология из общего пула
                                var listIdeo = Find.IdeoManager.IdeosListForReading.Where(i => i != ideo).ToList();
                                var ideoEnemy = listIdeo.Count == 0 ? null : listIdeo[new Random(data.GetHashCode()).Next(listIdeo.Count)];
                                if (ideoEnemy != null)
                                {
                                    RegisterReferencing(ideoEnemy);
                                    ideoLoadID = ideoEnemy.GetUniqueLoadID();
                                }
                                else
                                {
                                    ideoLoadID = ideo.GetUniqueLoadID();
                                    Loger.Log("Client PrepareSpawnThingEntry ideo Enemy RegisterReferencing Error: not find", Loger.LogLevel.ERROR);
                                }
                            }

                            // если есть идиология и поле <ideo>Ideo_9</ideo> не пустое
                            iiClose1 = iiClose2;
                            var ideoL = "<ideo>".Length;
                            //var ideoEL = "</ideo>".Length;
                            var innerTag = data.Substring(ideoIndex + ideoL, iiClose1 - ideoIndex - ideoL);

                            innerTag = GameXMLUtils.ReplaceByTag(innerTag, "ideo", ideoLoadID);

                            data = data.Substring(0, ideoIndex + ideoL) + innerTag + data.Substring(iiClose1);
                        }
                    }
                }
            }

            //возвращаем true, если это человек и не колонист (пират или пленник)

            if (MainHelper.DebugMode) File.WriteAllText(Loger.PathLog + "MailPawnA" + nnnn.ToString() + ".xml", data);
            return data;
        }


        public static List<IExposable> crossReferencingExposables;
        public static void RegisterReferencing(IExposable obj)
        {
            /*  Не сработало, не понятно почему
            if (Scribe.loader?.crossRefs?.crossReferencingExposables != null
                && !Scribe.loader.crossRefs.crossReferencingExposables.Contains(obj))
            {
                Scribe.loader.crossRefs.RegisterForCrossRefResolve(obj);
            }
            */
            /*  Тоже не сработало, тоже не понятно почему
            try
            {
                Loger.Log("Client RegisterReferencing " + obj.GetUniqueLoadID());
                var that = Traverse.Create(Scribe.loader.crossRefs);
                LoadedObjectDirectory loadedObjectDirectory = that.Field("loadedObjectDirectory").GetValue<LoadedObjectDirectory>();

                loadedObjectDirectory.RegisterLoaded(obj);
            }
            catch (Exception exp)
            {
                ExceptionUtil.ExceptionLog(exp, "Client RegisterReferencing");
            }
            */
            if (crossReferencingExposables == null) crossReferencingExposables = new List<IExposable>();
            if (!crossReferencingExposables.Contains(obj))
                crossReferencingExposables.Add(obj);

            /* Попытка убрать ошибку. Не сработало, но похоже это не важно
                Could not get load ID. We're asking for something which was never added during LoadingVars. pathRelToParent=/leader, parent=PlayerColony

            if (obj is Faction
                && Scribe.loader?.crossRefs?.loadIDs != null)
            {
                Scribe.loader.crossRefs.loadIDs.RegisterLoadIDReadFromXml(
                    null // может сюда передать это? (obj as Faction).loadID
                    , obj.GetType()
                    , "/leader"
                    , obj);
                    //string targetLoadID, Type targetType, string pathRelToParent, IExposable parent)
                Loger.Log("Client RegisterReferencing RegisterLoadIDReadFromXml " + obj.ToString());
            }
            */
        }
        private static int nnnn = 0;

    }

    public static class ThingEntryHelper
    {
        public static string ToStringThing<T>(this IEnumerable<T> list)
            where T : ThingEntry
        {
            return list.Aggregate("", (r, i) => r + Environment.NewLine + i.ToString());
        }
        public static string ToStringLabel<T>(this IEnumerable<T> list)
            where T : ThingEntry
        {
            return list.Aggregate("", (r, i) => r + Environment.NewLine + i.LabelTextShort);
        }
    }
}
