using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Model;
using System.Reflection;
using System.Xml;
using HugsLib.Utils;
using OCUnion;
using RimWorld.Planet;
using UnityEngine;

namespace RimWorldOnlineCity
{
    public static class ScribeSaverHelper
    {
        public static XmlWriter GetWriter(this ScribeSaver scribeSaver)
        {
            FieldInfo fieldInfo = typeof(ScribeSaver).GetField("writer", BindingFlags.Instance | BindingFlags.NonPublic);
            XmlWriter result = (XmlWriter)fieldInfo.GetValue(scribeSaver);
            return result;
        }

        public static void SetWriter(this ScribeSaver scribeSaver, XmlWriter writer)
        {
            FieldInfo fieldInfo = typeof(ScribeSaver).GetField("writer", BindingFlags.Instance | BindingFlags.NonPublic);
            fieldInfo.SetValue(scribeSaver, writer);
        }
    }
    static class GameUtils
    {
        /// <summary>
        /// Иконка вещи, опционально "i". Запускается в произвольном Rect
        /// </summary>
        public static void DravLineThing(Rect rect, ThingTrade thing, bool withInfo)
        {
            DravLineThing(rect, thing, withInfo, Color.white);
        }
        /// <summary>
        /// Иконка вещи, опционально "i". Запускается в произвольном Rect
        /// </summary>
        public static void DravLineThing(Rect rect, ThingTrade thing, bool withInfo, Color labelColor)
        {
            Widgets.ThingIcon(rect, thing.Def);
            if (string.IsNullOrEmpty(thing.StuffName))
            {
                TooltipHandler.TipRegion(rect, thing.Def.LabelCap);
                GUI.color = labelColor;
                if (withInfo) Widgets.InfoCardButton(rect.x + 24f, rect.y, thing.Def);
                GUI.color = Color.white;
            }
            else
            {
                TooltipHandler.TipRegion(rect, thing.Def.LabelCap + " из ".NeedTranslate() + thing.StuffDef.LabelAsStuff);
                GUI.color = labelColor;
                if (withInfo) Widgets.InfoCardButton(rect.x + 24f, rect.y, thing.Def, thing.StuffDef);
                GUI.color = Color.white;
            }
            // GenLabel.ThingLabel(this.Def, this.StuffDef, 1)
        }
        public static void DravLineThing(Rect rect, Thing thing, bool withInfo)
        {
            Widgets.ThingIcon(rect, thing);
            if (withInfo) Widgets.InfoCardButton(rect.x + 24f, rect.y, thing);

            var localThing = thing;
            TooltipHandler.TipRegion(rect, new TipSignal(delegate
            {
                string text = localThing.LabelCapNoCount;
                string tipDescription = localThing.GetDescription();
                if (!tipDescription.NullOrEmpty())
                {
                    text = text + ": " + tipDescription;
                }
                return text;
            }, localThing.GetHashCode()));
        }

        /// <summary>
        /// Иконка вещи, "i". Запускается в произвольном Rect
        /// </summary>
        /// <param name="rectLine"></param>
        /// <param name="thing"></param>
        /// <param name="labelColor"></param>
        public static void DravLineThing(Rect rectLine, ThingDef thing, Color labelColor)
        {
            //Rect rect = new Rect(-1f, -1f, 27f, 27f);
            Rect rect = new Rect(0f, 0f, 24f, 24f);

            Widgets.ThingIcon(rect, thing);
            Widgets.InfoCardButton(30f, 0f, thing);
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect rect2 = new Rect(55f, 0f, rectLine.width - 55f, rectLine.height);
            Text.WordWrap = false;
            GUI.color = labelColor;
            Widgets.Label(rect2, thing.LabelCap);
            GUI.color = Color.white;
            Text.WordWrap = true;
        }

        /// <summary>
        /// Иконка вещи, "i" и название. Должно запускаться в относительных коор
        /// </summary>
        /// <param name="rectLine"></param>
        /// <param name="thing"></param>
        /// <param name="labelColor"></param>
        public static void DravLineThing(Rect rectLine, Thing thing, Color labelColor)
        {
            //Rect rect = new Rect(-1f, -1f, 27f, 27f);
            Rect rect = new Rect(0f, 0f, 24f, 24f);

            Widgets.ThingIcon(rect, thing, 1f);

            Widgets.InfoCardButton(30f, 0f, thing);

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Tiny;
            Rect rect2 = new Rect(55f, 0f, rectLine.width - 55f, rectLine.height);
            Text.WordWrap = false;
            GUI.color = labelColor;
            Widgets.Label(rect2, thing.LabelCapNoCount);
            Text.WordWrap = true;
            GenUI.ResetLabelAlign();
            GUI.color = Color.white;

            var localThing = thing;
            TooltipHandler.TipRegion(rectLine, new TipSignal(delegate
            {
                string text = localThing.LabelCapNoCount;
                string tipDescription = localThing.GetDescription();
                if (!tipDescription.NullOrEmpty())
                {
                    text = text + ": " + tipDescription;
                }
                return text;
            }, localThing.GetHashCode()));
        }

        /// <summary>
        /// Объединяет одинаковые вещи в список внутри одного контейнера TransferableOneWay
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public static List<TransferableOneWay> DistinctThings(IEnumerable<Thing> things)
        {
            var transferables = new List<TransferableOneWay>();
            foreach (var item in things)
            {
                TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching<TransferableOneWay>(item, transferables);
                if (transferableOneWay == null)
                {
                    transferableOneWay = new TransferableOneWay();
                    transferables.Add(transferableOneWay);
                }
                transferableOneWay.things.Add(item);
            }
            return transferables;
        }

        /// <summary>
        /// Возвращает набор вещей из allThings выбранных в targets или null, если чего то не хватает.
        /// Выставляет target.NotTrade
        /// </summary>
        /// <param name="targets">Искомые веши или фильтры из ордера</param>
        /// <param name="allThings">Все доступные вещи</param>s
        /// <returns></returns>
        public static List<TransferableOneWay> ChechToTrade(IEnumerable<ThingTrade> targets, IEnumerable<Thing> allThings)
        {
            //сортируем цели для выборки более плохих вначале
            var rate = 1;//Максимальное кол-во повторов, уменьшается для каждой следующей вещи. Расчет на то, что вещи не конкурируют между собой или конкурируют незначительно
            //первый запуск для выяснения rate
            return ChechToTradeDo(targets, allThings, ref rate, false);

        }
        /// <summary>
        /// Возвращает набор вещей из allThings выбранных в targets или null, если чего то не хватает.
        /// Выставляет target.NotTrade
        /// </summary>
        /// <param name="targets">Искомые веши или фильтры из ордера</param>
        /// <param name="allThings">Все доступные вещи</param>
        /// <param name="rate">Кол-во повтором этой сделки</param>
        /// <returns></returns>
        public static List<TransferableOneWay> ChechToTrade(IEnumerable<ThingTrade> targets, IEnumerable<Thing> allThings, out int rate)
        {
            //сортируем цели для выборки более хорошие вначале, а вещи сначала более плохие
            var trs = targets
                .Where(t => t.Count > 0)
                .OrderBy(t => t.DefName + "#" + (9 - t.Quality).ToString() + t.HitPoints.ToString().PadLeft(5) + t.Count.ToString().PadLeft(6))
                .ToList();
            rate = 100000000;//Максимальное кол-во повторов, уменьшается для каждой следующей вещи. Расчет на то, что вещи не конкурируют между собой или конкурируют незначительно
            //первый запуск для выяснения rate
            var res = ChechToTradeDo(trs, allThings, ref rate, true);
            if (res == null) return null;
            //повторно запускаем для корректного заполнения CountToTransfer (т.к. при уменьшении rate не перерасчитываются вещи отобранные ранее)
            return ChechToTradeDo(trs, allThings, ref rate, false);
        }
        private static List<TransferableOneWay> ChechToTradeDo(IEnumerable<ThingTrade> targets, IEnumerable<Thing> allThings, ref int rate, bool setRect)
        {
            bool result = true;
            var selects = new List<TransferableOneWay>();
            var source = allThings.ToDictionary(i => i, i => i.stackCount);
            //сортируем вещи сначала более плохие
            var sourceKeys = source.Keys
                .Select(t =>
                {
                    QualityCategory qq;
                    QualityUtility.TryGetQuality(t, out qq);
                    return new { thing = t, q = qq };
                })
                .OrderBy(t => t.thing.def.defName + "#" + ((int)t.q).ToString() + (10000 - t.thing.HitPoints).ToString() + t.thing.stackCount.ToString().PadLeft(6)) 
                .Select(t => t.thing)
                .ToList();
            foreach (var target in targets)
            {
                if (target.Count == 0)
                {
                    target.NotTrade = false;
                    continue;
                }
                if (setRect && target.Count > 100 && rate > 1000000) rate = 1000000; //от переполнения
                var select = new TransferableOneWay();
                //Log.Message(target.DefName);
                foreach (var thing in sourceKeys)
                {
                    if (target.Count * rate <= select.CountToTransfer) break;
                    if (source[thing] == 0) continue;
                    if (target.MatchesThing(thing))
                    {
                        //нам подходит выбираем нужное кол-во
                        select.things.Add(thing);
                        var count = target.Count * rate - select.CountToTransfer > source[thing]
                            ? source[thing]
                            : target.Count * rate - select.CountToTransfer;
                        select.ForceTo(select.CountToTransfer + count);
                        source[thing] -= count;
                        //Log.Message(target.DefName + " == " + thing.def.defName + " o:" + source[thing].ToString() + " g:" + select.CountToTransfer.ToString() + " rate:" + rate.ToString());
                    }
                    //else Log.Message(target.DefName + " != " + thing.def.defName + " " + select.CountToTransfer.ToString());
                }
                if (setRect && target.Count > select.CountToTransfer
                    || !setRect && target.Count * rate > select.CountToTransfer)
                {
                    result = false;
                    target.NotTrade = true;
                    //Log.Message("NotTrade " + target.Count.ToString() + " > " + select.CountToTransfer.ToString());
                }
                else 
                {
                    if (setRect && target.Count * rate > select.CountToTransfer)
                    {
                        rate = select.CountToTransfer / target.Count;
                        //Log.Message(" rate:" + rate.ToString());
                    }
                    selects.Add(select);
                    target.NotTrade = false;
                }
            }
            return result ? selects : null;
        }

        public static List<Thing> GetAllThings(Caravan caravan)
        {
            var goods = CaravanInventoryUtility.AllInventoryItems(caravan).ToList().Concat(
                caravan.PawnsListForReading
                .Cast<Thing>()
                ).ToList();
            return goods;
        }

        public static List<Thing> GetAllThings(Map map)
        {
            var goods = CaravanFormingUtility.AllReachableColonyItems(map).ToList().Concat(
                map.mapPawns.AllPawnsSpawned
                .Cast<Thing>()
                ).ToList();
            return goods;
        }

        public static string PlayerTextInfo(Player player)
        {
            //todo
            return " ";
        }

        public static void ShortSetupForQuickTestPlay()
        {
            //частичная копия 
            Current.Game = new Game();
            Current.Game.InitData = new GameInitData();
            Current.Game.Scenario = ScenarioDefOf.Crashlanded.scenario;
            Find.Scenario.PreConfigure();
            Current.Game.storyteller = new Storyteller(StorytellerDefOf.Cassandra, DifficultyDefOf.Hard);
            Current.Game.World = WorldGenerator.GenerateWorld(0.05f, GenText.RandomSeedString(), OverallRainfall.Normal, OverallTemperature.Normal);
        }

        /// <summary>
        /// Куски с SpawnSetup на карту, не относящиеся к карте
        /// </summary>
        /// <param name="pawn"></param>
        public static void SpawnSetupOnCaravan(Pawn pawn)
        {
            /*
            if (Find.TickManager != null)
            {
                Find.TickManager.RegisterAllTickabilityFor(pawn);
            }
            StealAIDebugDrawer.Notify_ThingChanged(pawn);
            if (pawn is IThingHolder && Find.ColonistBar != null)
            {
                Find.ColonistBar.MarkColonistsDirty();
            }
            if (pawn.def.receivesSignals)
            {
                Find.SignalManager.RegisterReceiver(pawn);
            }
            */
            if (pawn.Faction != Faction.OfPlayer)
                pawn.SetFaction(Faction.OfPlayer, null);
            if (!pawn.IsWorldPawn())
            {
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
            }
        }

        public static void DeSpawnSetupOnCaravan(Caravan caravan, Pawn pawn)
        {
            caravan.PawnsListForReading.Remove(pawn);
            if (pawn is IThingHolder && Find.ColonistBar != null)
            {
                Find.ColonistBar.MarkColonistsDirty();
            }
            /*
            if (Find.TickManager != null)
            {
                Find.TickManager.RegisterAllTickabilityFor(pawn);
            }
            StealAIDebugDrawer.Notify_ThingChanged(pawn);
            if (pawn is IThingHolder && Find.ColonistBar != null)
            {
                Find.ColonistBar.MarkColonistsDirty();
            }
            if (pawn.def.receivesSignals)
            {
                Find.SignalManager.RegisterReceiver(pawn);
            }
            if (pawn.IsWorldPawn())
            {
                Find.WorldPawns.RemovePawn(pawn);
            }
            */
        }

        /// <summary>
        /// Получаем координаты ячейки куда сгружать груз для указанной карты.
        /// Ячейка - центр склада. Выбирается склад как лучший по: 
        /// имеет в названии слово "торг" или "trad", не свалка с именем поумолчанию, самый большой, по названию
        /// </summary>
        public static IntVec3 GetTradeCell(Map map)
        {
            //название свалки по умолчанию
            var labelDumping = "DumpingStockpile".Translate();
            var labelDumping2 = "DumpingStockpileLabel".Translate();

            Zone zone = map.zoneManager.AllZones
                .OrderBy(z =>
                    (z.label.IndexOf("торг", StringComparison.OrdinalIgnoreCase) >= 0
                    || z.label.IndexOf("trad", StringComparison.OrdinalIgnoreCase) >= 0
                    ? "0" : "1")
                    //пробуем выбирать не свалку
                    + (z.label.IndexOf(labelDumping, StringComparison.OrdinalIgnoreCase) == 0
                    || z.label.IndexOf(labelDumping2, StringComparison.OrdinalIgnoreCase) == 0
                    ? "1" : "0")
                    + (100000000 - z.Cells.Count).ToString().PadLeft(10)
                    + z.label)
                .FirstOrDefault();
            if (zone == null) return map.Center;

            var res = zone.Cells.Aggregate(new IntVec3(), (a, i) => { a.x += i.x; a.z += i.z; return a; });
            res.x /= zone.Cells.Count;
            res.z /= zone.Cells.Count;
            return res;
        }
    }
}
