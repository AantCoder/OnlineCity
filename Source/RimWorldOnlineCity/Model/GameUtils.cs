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
