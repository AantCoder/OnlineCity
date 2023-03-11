using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
using OCUnion;
using OCUnion.Transfer.Model;
using System.Xml.Serialization;

namespace Model
{
    /// <summary>
    /// Модель хранящая расширенную информацию полученную с игрового объека (при Concrete == true),
    /// либо только некоторую информацию о том, какой объект должен быть (при Concrete == false)
    /// </summary>
    [Serializable]
    public class ThingTrade : ThingEntry
    {
        /// <summary>
        /// Содержиться информация о конкретном объекте
        /// </summary>
        public bool Concrete { get; set; }

        /// <summary>
        /// Тип вещи
        /// </summary>
        public string DefName { get; set; }
        /// <summary>
        /// Материал из чего изготовлено
        /// </summary>
        public string StuffName { get; set; }
        /// <summary>
        /// Текущая прочность, если 0 считается масксимальной
        /// Либо минимально требуемая прочность (при Concrete == false)
        /// </summary>
        public int HitPoints { get; set; }
        /// <summary>
        /// Максимальная прочность
        /// Либо всегда 100 (при Concrete == false)
        /// </summary>
        public int MaxHitPoints { get; set; }
        public int HitPointsPrecent => Concrete ? HitPoints * 100 / MaxHitPoints : HitPoints;
        /// <summary>
        /// Качество изготовления
        /// </summary>
        public int Quality { get; set; }
        /// <summary>
        /// Снято с трупа, применимо только к одежде
        /// </summary>
        public bool WornByCorpse { get; set; }
        /// <summary>
        /// Не замороженное портится. Информационное (не учавствует в фильтрах и сравнениях)
        /// </summary>
        public bool Rottable { get; set; }
        /// <summary>
        /// Ориентацияю. Используется только при переносе объектов (CreateThing CreateTrade)
        /// </summary>
        public int Rotation { get; set; }
        /// <summary>
        /// Прогресс роста. Используется только при переносе объектов (CreateThing CreateTrade)
        /// </summary>
        public float Growth { get; set; }

        [XmlIgnore]
        public IntVec3S Position { get; set; }
        /// <summary>
        /// Несколько параметров пешки текстом, для сравнения в MatchesThing(ThingTrade)
        /// </summary>
        public string PawnParam { get; set; }
        /// <summary>
        /// Цена за 1 единицу в серебре по стандартной расценки игры. Заполняется только при Concrete (при создании из игрово вещи)
        /// </summary>
        public float GameCost { get; set; }
        /// <summary>
        /// У нас этого нет, невозможно продать. Вычисляется функцией ExchengeUtils.ChechToSell
        /// </summary>
        [NonSerialized]
        public bool NotTrade;
        /// <summary>
        /// Аналогично NotTrade,. указывает доступное кол-во
        /// </summary>
        [NonSerialized]
        public int TradeCount;

        [NonSerialized]
        [XmlIgnore]
        private Thing DataThing_p;

        [XmlIgnore]
        public bool IsPawn => !string.IsNullOrEmpty(PawnParam);

        [XmlIgnore]
        public bool IsPawnHuman => !string.IsNullOrEmpty(PawnParam) && (PawnParam.StartsWith("Human") || PawnParam.StartsWith("Colonist"));

        /// <summary>
        /// Объект соответствующий Data, для показа информации
        /// </summary>
        [XmlIgnore]
        public Thing DataThing
        {
            get
            {
                if (DataThing_p == null && Data != null)
                {
                    DataThing_p = CreateThing();
                }
                return DataThing_p;
            }
            set { DataThing_p = value; }
        }

        [XmlIgnore]
        public bool DataThingNeedCreateThing => DataThing_p == null && Data != null;

        [NonSerialized]
        [XmlIgnore]
        private ThingDef Def_p;
        [XmlIgnore]
        public ThingDef Def
        {
            get
            {
                if (Def_p == null)
                {
                    Def_p = (ThingDef)GenDefDatabase.GetDef(typeof(ThingDef), DefName);
                }
                return Def_p;
            }
            set { Def_p = value; }
        }

        [NonSerialized]
        [XmlIgnore]
        private ThingDef StuffDef_p;
        [XmlIgnore]
        public ThingDef StuffDef
        {
            get
            {
                if (StuffDef_p == null)
                {
                    StuffDef_p = !string.IsNullOrEmpty(StuffName) ? (ThingDef)GenDefDatabase.GetDef(typeof(ThingDef), StuffName) : GenStuff.DefaultStuffFor(Def);
                }
                return StuffDef_p;
            }
            set { StuffDef_p = value; }
        }


        public string LabelText
        {
            get
            {
                var result = LabelTextShort + Environment.NewLine;
                if (Concrete)
                {
                    result += "OCity_ThingTrade_Quality_Strength".Translate(Quality >= 0 ? ((QualityCategory)Quality).GetLabel() : "n/a", HitPoints, MaxHitPoints);
                    if (WornByCorpse)
                    {
                        result += "OCity_ThingTrade_Cut_Body_Off".Translate();
                    }

                }
                else
                {
                    result += "OCity_ThingTrade_QualityBetter_StrengthMore".Translate(Quality >= 0 ? ((QualityCategory)Quality).GetLabel() : "n/a", HitPoints);
                    if (WornByCorpse)
                    {
                        result += "OCity_ThingTrade_CouldTake_OffCorpse".Translate();
                    }
                }

                return result;
            }
        }

        public override string ToString()
        {
            return $"{DefName} {(Concrete ? "conc" : "")} " + base.ToString() + " cost=" + this.GameCost + " " + PawnParam;
        }

        protected ThingTrade()
        { }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        /// <summary>
        /// Проверяет, что вещь thing, не хуже, чем текущая (если пешка, то должна быть равна)
        /// При equal вещь должна быть точно такой же
        /// </summary>
        public bool MatchesThingTrade(ThingTrade thing, bool equal = false)
        {
            //быстрая проверка
            if (thing == null)
            {
                //Log.Message(" ? thing == null");
                return false;
            }
            if (DefName != thing.DefName)
            {
                //Log.Message(" ? DefName != thing.def.defName" + (DefName ?? "null") + " " + (thing.def.defName ?? "null"));
                return false;
            }

            //общая проверка
            if (thing.StuffName != StuffName)
            {
                //Log.Message(" ? testThing.StuffName != StuffName " + (StuffName ?? "null") + " " + (testThing.StuffName ?? "null"));
                return false;
            }
            if (thing.Quality >= 0 && Quality >= 0
                && (!equal && thing.Quality > Quality
                || equal && thing.Quality == Quality))
            {
                //Log.Message(" ? testThing.Quality < Quality " + Quality + " " + testThing.Quality);
                return false;
            }
            if (!equal && thing.WornByCorpse && !WornByCorpse
                || equal && thing.WornByCorpse == WornByCorpse)
            {
                //Log.Message(" ? testThing.WornByCorpse && !WornByCorpse " + WornByCorpse + " " + testThing.WornByCorpse);
                return false;
            }
            if (!equal && HitPointsPrecent > thing.HitPointsPrecent && thing.HitPointsPrecent >= 0
                || equal && HitPointsPrecent == thing.HitPointsPrecent)
            {
                //Log.Message(" ? hitPrecent < HitPoints * 100 / MaxHitPoints");
                return false;
            }

            if (PawnParam != thing.PawnParam) //если это пешка, то должно быть заполнено у обоих, если нет, то у обоих null
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Проверяет, что вещь thing, не хуже, чем текущая (если пешка, то должна быть равна)
        /// </summary>
        /// <returns></returns>
        public bool MatchesThing(Thing thing)
        {
            if (thing == null) return false;
            //быстрая проверка
            if (DefName != thing.def.defName) return false;

            var testThing = CreateTrade(thing, 1, false);
            return MatchesThingTrade(testThing);
            /*
            //быстрая проверка
            if (thing == null)
            {
                //Log.Message(" ? thing == null");
                return false;
            }
            if (DefName != thing.def.defName)
            {
                //Log.Message(" ? DefName != thing.def.defName" + (DefName ?? "null") + " " + (thing.def.defName ?? "null"));
                return false;
            }

            var testThing = CreateTrade(thing, 1);

            //общая проверка
            if (testThing.StuffName != StuffName
                )
            {
                //Log.Message(" ? testThing.StuffName != StuffName " + (StuffName ?? "null") + " " + (testThing.StuffName ?? "null"));
                return false;
            }
            if (testThing.Quality < Quality
                )
            {
                //Log.Message(" ? testThing.Quality < Quality " + Quality + " " + testThing.Quality);
                return false;
            }
            if (testThing.WornByCorpse && !WornByCorpse
                )
            {
                //Log.Message(" ? testThing.WornByCorpse && !WornByCorpse " + WornByCorpse + " " + testThing.WornByCorpse);
                return false;
            }

            //в зависимости от Concrete
            int hitPrecent = (testThing.HitPoints + 1) * 100 / testThing.MaxHitPoints;
            if (Concrete)
            {
                if (hitPrecent < HitPoints * 100 / MaxHitPoints)
                {
                    //Log.Message(" ? hitPrecent < HitPoints * 100 / MaxHitPoints");
                    return false;
                }
            }
            else
                if (hitPrecent < HitPoints)
                {
                    //Log.Message(" ? hitPrecent < HitPoints");
                    return false;
                }

            if (Concrete && thing is Pawn && (thing as Pawn).RaceProps.Humanlike)
            {
                if (thing.LabelShort != DataThing.LabelShort) return false;
            }

            //Проверка схожести средствами игры, для надёжности и идентификации индивидуальности пешек
            if (Concrete)
            {
                //Log.Message(DataThing.def.defName + " ? " + thing.def.defName + " " + TransferableUtility.TransferAsOne(thing, DataThing).ToString());
                return TransferableUtility.TransferAsOne(thing, DataThing, TransferAsOneMode.Normal);
            }
            else
                return true;
            */
        }

        /// <summary>
        /// Минимальные данные для создания простых вещей в том числе на сервере
        /// </summary>
        /// <param name="thingDefName"></param>
        /// <param name="count"></param>
        /// <param name="minHitPointsPercents"></param>
        /// <param name="minQualities"></param>
        /// <returns></returns>
        public static ThingTrade CreateTradeServer(string thingDefName, int count, float minHitPointsPercents = 100, int minQualities = -1)
        {
            var that = new ThingTrade();
            that.Name = thingDefName;
            that.Count = count;

            that.DefName = thingDefName;
            that.HitPoints = (int)(minHitPointsPercents * 100);
            that.MaxHitPoints = 100;

            that.Quality = (int)minQualities;

            // Не заполняются:
            //Data
            //OriginalID
            //StuffName
            //WornByCorpse
            //Rotation
            //Growth
            //Position
            //PawnParam

            return that;
        }

        /// <summary>
        /// Заполнить только базовые свойства для требования к вещи, не пешки для BuyThings и потом MatchesThing(ThingTrade)
        /// После создания в фильты ещё редактируются значениия WornByCorpse
        /// </summary>
        public static ThingTrade CreateTrade(ThingDef thingDef, float minHitPointsPercents, QualityCategory minQualities, int count)
        {
            var that = new ThingTrade();
            that.Name = thingDef.LabelCap;
            that.Count = count;

            that.DefName = thingDef.defName;
            that.HitPoints = (int)(minHitPointsPercents * 100);
            that.MaxHitPoints = 100;

            that.Quality = thingDef.FollowQualityThingFilter() ? (int)minQualities : -1;

            that.GameCost = thingDef.BaseMarketValue;
            if (that.GameCost < 0.01f) that.GameCost = 0.01f;

            // Не заполняются:
            //Data
            //OriginalID
            //StuffName
            //WornByCorpse
            //Rotation
            //Growth
            //Position
            //PawnParam

            return that;
        }

        protected void SetFromThing(Thing thing, int count, bool withData = true)
        {
            this.SetBaseInfo(thing, count);
            if (withData) this.SetData(thing);
            this.DataThing = thing;
            this.Concrete = true;

            this.DefName = thing.def.defName;
            this.StuffName = thing.Stuff == null ? null : thing.Stuff.defName;

            var pawn = thing as Pawn;
            if (pawn == null)
            {
                this.HitPoints = thing.HitPoints;
                this.MaxHitPoints = thing.MaxHitPoints;
            }
            else
            {
                this.HitPoints = (int)(pawn.health.summaryHealth.SummaryHealthPercent * 100f);
                this.MaxHitPoints = 100;

                this.PawnParam =
                    $"{/*pawn.kindDef*/(pawn.RaceProps.Humanlike ? pawn.IsColonist ? "Colonist" : "Humanlike" : "")} gender: {pawn.gender}, lifeStage: {pawn.ageTracker.CurLifeStageIndex}"
                    //+ $" years:{(int)pawn.ageTracker.AgeChronologicalYears}"
                    + (pawn.RaceProps.Humanlike ? " " + pawn.LabelShort /*LabelCap ?*/ : ""); // для людей (не животных), проверяем имена, для идентификации
            }
            QualityCategory qq;
            if (QualityUtility.TryGetQuality(thing, out qq)) this.Quality = (int)qq;
            else this.Quality = -1;

            Apparel thingA = thing as Apparel;
            if (thingA != null) this.WornByCorpse = thingA.WornByCorpse;

            if (thing is ThingWithComps)
            {
                var compRottable = (thing as ThingWithComps).GetComp<CompRottable>();
                this.Rottable = compRottable != null;
            }

            this.Rotation = thing.Rotation.AsInt;

            Plant thingP = thing as Plant;
            if (thingP != null) this.Growth = thingP.Growth;

            this.Position = new IntVec3S(thing.Position);

            this.GameCost = thing.MarketValue;
            if (this.GameCost < 0.01f) this.GameCost = 0.01f;

        }

        public static ThingTrade CreateTrade(Thing thing, int count, bool withData = true)
        {
            var that = new ThingTrade();
            that.SetFromThing(thing, count, withData);
            return that;
        }

        public override Thing CreateThing(bool useOriginalID = false, int stackCount = 0)
        {
            if (Data != null) return base.CreateThing(useOriginalID, stackCount);

            //useOriginalID не используется.

            var def = (ThingDef)GenDefDatabase.GetDef(typeof(ThingDef), DefName);
            var stuffDef = !string.IsNullOrEmpty(StuffName) ? (ThingDef)GenDefDatabase.GetDef(typeof(ThingDef), StuffName) : null;
            Thing thing = !string.IsNullOrEmpty(StuffName)
                ? ThingMaker.MakeThing(def, stuffDef)
                : ThingMaker.MakeThing(def);
            thing.stackCount = stackCount > 0 ? stackCount : Count;

            if (HitPoints > 0) thing.HitPoints = HitPoints;

            SetFaction(thing, Affiliation == PawnAffiliation.Colonist || Affiliation == PawnAffiliation.Thing);

            if (Quality >= 0)
            {
                CompQuality compQuality = thing.TryGetComp<CompQuality>();
                if (compQuality != null)
                {
                    compQuality.SetQuality((QualityCategory)Quality, ArtGenerationContext.Outsider);
                }
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

            thing.Rotation = new Rot4(Rotation);

            Plant thingP = thing as Plant;
            if (thingP != null) thingP.Growth = Growth;

            thing.Position = Position.Get();

            return thing;
        }


        /// <summary>
        /// Информация достатоная для отображения.
        /// defName, кол-во, цена, качество, параметры пешки (PawnParam), скилы (SkillsToString)
        /// </summary>
        /// <returns></returns>
        public virtual string PackToString()
        {
            return $"{DefName},{Count},{(int)(GameCost*1000)},{Quality},{PawnParam.Replace(",", "@")},{(int)Affiliation},{Name}";
        }

        public virtual ThingTradeInfoParam UnpackFromString(string str)
        {
            var comps = str.Split(new char[] { ',' }, 7);

            DefName = comps[0];
            Count = int.Parse(comps[1]);
            GameCost = int.Parse(comps[2]) / 1000f;
            Quality = int.Parse(comps[3]);
            PawnParam = comps[4].Replace("@", ",");
            Affiliation = (PawnAffiliation)int.Parse(comps[5]);
            Name = comps[6];

            return new ThingTradeInfoParam(PawnParam);
        }
    }

    public class ThingTradeInfoParam
    {
        public string KindDef { get; set; }
        public bool GenderMale { get; set; }
        public int LifeStage { get; set; }

        //$"{pawn.kindDef} gender: {pawn.gender}, lifeStage: {pawn.ageTracker.CurLifeStageIndex}"
        public ThingTradeInfoParam(string pawnParam)
        {
            try
            {
                var i1 = pawnParam.IndexOf(" ");
                KindDef = pawnParam.Substring(0, i1);

                i1 = pawnParam.IndexOf("gender:");
                var i2 = pawnParam.IndexOf(",", i1);
                GenderMale = pawnParam.Substring(i1 + "gender:".Length, i2 - i1 - "gender:".Length).Trim() == "1";

                i1 = pawnParam.IndexOf("lifeStage: ");
                i2 = pawnParam.IndexOf(" ", i1 + "lifeStage: ".Length);
                if (i2 < 0) i2 = pawnParam.Length;
                LifeStage = int.Parse(pawnParam.Substring(i1 + "lifeStage: ".Length, i2 - i1 - "lifeStage: ".Length).Trim());
            }
            catch { }
        }
    }

    public static class ThingTradeHelper
    {
        /// <summary>
        /// Отсортировать: сначала плохие потом хорошие
        /// </summary>
        /// <param name="targets"></param>
        /// <returns></returns>
        public static List<ThingTrade> OrderByCost(this IEnumerable<ThingTrade> targets)
        {
            return targets
                .Where(t => t.Count > 0)
                .OrderBy(t => t.DefName + "#" + (8 - t.Quality).ToString() + t.HitPoints.ToString().PadLeft(5) + t.Count.ToString().PadLeft(6))
                .ToList();
        }

        /// <summary>
        /// Отсортировать: сначала хорошие потом плохие
        /// </summary>
        /// <param name="targets"></param>
        /// <returns></returns>
        public static List<ThingTrade> OrderByDescendingCost(this IEnumerable<ThingTrade> targets)
        {
            return targets
                .Where(t => t.Count > 0)
                .OrderBy(t => t.DefName + "#" + (t.Quality + 1).ToString() + (99999 - t.HitPoints).ToString().PadLeft(5) + (999999 - t.Count).ToString().PadLeft(6))
                .ToList();
        }
    }
}
