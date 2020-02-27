using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
using OCUnion;
using OCUnion.Transfer.Model;

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
        /// <summary>
        /// Качество изготовления
        /// </summary>
        public int Quality { get; set; }
        /// <summary>
        /// Снято с трупа, применимо только к одежде
        /// </summary>
        public bool WornByCorpse { get; set; }
        /// <summary>
        /// Ориентацияю. Используется только при переносе объектов (CreateThing CreateTrade)
        /// </summary>
        public int Rotation { get; set; }
        /// <summary>
        /// Прогресс роста. Используется только при переносе объектов (CreateThing CreateTrade)
        /// </summary>
        public float Growth { get; set; }

        public IntVec3S Position { get; set; }

        /// <summary>
        /// У нас этого нет, невозможно продать. Вычисляется функцией ExchengeUtils.ChechToSell
        /// </summary>
        [NonSerialized]
        public bool NotTrade;

        [NonSerialized]
        public Thing DataThing_p;
        /// <summary>
        /// Объект соответствующий Data, для показа информации
        /// </summary>
        public Thing DataThing
        {
            get
            {
                if (DataThing_p == null && Data != null)
                {
                    DataThing_p = CreateThing(false);
                }
                return DataThing_p;
            }
            set { DataThing_p = value; }
        }

        [NonSerialized]
        private ThingDef Def_p;
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
        private ThingDef StuffDef_p;
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
                var result = Name + (Count > 1 ? " x" + Count.ToString() : "") + Environment.NewLine;
                if (Concrete)
                {
                    result += "OCity_ThingTrade_Quality_Strength".Translate(((QualityCategory)Quality).GetLabel(), HitPoints, MaxHitPoints);
                    if (WornByCorpse)
                    {
                        result += "OCity_ThingTrade_Cut_Body_Off".Translate();
                    }

                }
                else
                {
                    result += "OCity_ThingTrade_QualityBetter_StrengthMore".Translate(((QualityCategory)Quality).GetLabel(), HitPoints);
                    if (WornByCorpse)
                    {
                        result += "OCity_ThingTrade_CouldTake_OffCorpse".Translate();
                    }
                }

                return result;
            }
        }

        protected ThingTrade()
        { }

        public bool MatchesThing(Thing thing)
        {
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

            //Проверка схожести средствами игры, для надёжности и идентификации индивидуальности пешек
            if (Concrete)
            {
                //Log.Message(DataThing.def.defName + " ? " + thing.def.defName + " " + TransferableUtility.TransferAsOne(thing, DataThing).ToString());
                return TransferableUtility.TransferAsOne(thing, DataThing, TransferAsOneMode.Normal);
            }
            else
                return true;
        }

        public static ThingTrade CreateTrade(ThingDef thingDef, float minHitPointsPercents, QualityCategory minQualities, int count)
        {
            var that = new ThingTrade();
            that.Name = thingDef.LabelCap;
            that.Count = count;

            that.DefName = thingDef.defName;
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

            return that;
        }

        public static ThingTrade CreateTrade(Thing thing, int count, bool withData = true)
        {
            var that = new ThingTrade();
            that.SetBaseInfo(thing, count);
            if (withData) that.SetData(thing);
            that.DataThing = thing;
            that.Concrete = true;

            that.DefName = thing.def.defName;
            that.StuffName = thing.Stuff == null ? null : thing.Stuff.defName;

            var pawn = thing as Pawn;
            if (pawn == null)
            {
                that.HitPoints = thing.HitPoints;
                that.MaxHitPoints = thing.MaxHitPoints;
            }
            else
            {
                that.HitPoints = (int)(pawn.health.summaryHealth.SummaryHealthPercent * 100f);
                that.MaxHitPoints = 100;
            }

            QualityCategory qq;
            if (QualityUtility.TryGetQuality(thing, out qq)) that.Quality = (int)qq;

            Apparel thingA = thing as Apparel;
            if (thingA != null) that.WornByCorpse = thingA.WornByCorpse;

            that.Rotation = thing.Rotation.AsInt;

            Plant thingP = thing as Plant;
            if (thingP != null) that.Growth = thingP.Growth;

            that.Position = new IntVec3S(thing.Position);

            return that;
        }

        public override Thing CreateThing(bool useOriginalID = false, int stackCount = 0)
        {
            //useOriginalID не используется.

            var def = (ThingDef)GenDefDatabase.GetDef(typeof(ThingDef), DefName);
            var stuffDef = !string.IsNullOrEmpty(StuffName) ? (ThingDef)GenDefDatabase.GetDef(typeof(ThingDef), StuffName) : null;
            Thing thing = !string.IsNullOrEmpty(StuffName)
                ? ThingMaker.MakeThing(def, stuffDef)
                : ThingMaker.MakeThing(def);
            thing.stackCount = stackCount > 0 ? stackCount : Count;

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

            thing.Rotation = new Rot4(Rotation);

            Plant thingP = thing as Plant;
            if (thingP != null) thingP.Growth = Growth;

            thing.Position = Position.Get();

            return thing;
        }

    }
}
