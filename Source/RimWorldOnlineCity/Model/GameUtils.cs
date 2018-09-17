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
        public static void DravLineThing(Rect rect, ThingTrade thing, bool withInfo)
        {
            Widgets.ThingIcon(rect, thing.Def);
            if (string.IsNullOrEmpty(thing.StuffName))
            {
                TooltipHandler.TipRegion(rect, thing.Def.LabelCap);
                if (withInfo) Widgets.InfoCardButton(rect.x + 24f, rect.y, thing.Def);
            }
            else
            {
                TooltipHandler.TipRegion(rect, thing.Def.LabelCap + " из ".NeedTranslate() + thing.StuffDef.LabelAsStuff);
                if (withInfo) Widgets.InfoCardButton(rect.x + 24f, rect.y, thing.Def, thing.StuffDef);
            }
            // GenLabel.ThingLabel(this.Def, this.StuffDef, 1)
        }

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

        public static List<TransferableOneWay> ChechToTrade(IEnumerable<ThingTrade> targets, IEnumerable<Thing> allThings)
        {
            var select = new List<TransferableOneWay>();
            foreach (var target in targets)
            {
                //todo
            }
            return select;
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
