using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Model;
using System.Reflection;
using System.Xml;
using OCUnion;
using RimWorld.Planet;
using UnityEngine;
using OCUnion.Transfer.Model;
using System.Threading;

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

    [StaticConstructorOnStartup]
    static class GameUtils
    {
        internal static readonly Texture2D CircleFill = ContentFinder<Texture2D>.Get("circle-fill");

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
                TooltipHandler.TipRegion(rect, thing.Def.LabelCap + "OCity_GameUtils_From".Translate() + thing.StuffDef.LabelAsStuff);
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
                string tipDescription = localThing.DescriptionFlavor;
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
                string tipDescription = localThing.DescriptionFlavor;
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
                TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching<TransferableOneWay>(item, transferables, TransferAsOneMode.Normal);
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
            Current.Game.storyteller = new Storyteller(StorytellerDefOf.Cassandra, DifficultyDefOf.Rough);
            Current.Game.World = WorldGenerator.GenerateWorld(
                0.05f,
                GenText.RandomSeedString(),
                OverallRainfall.Normal,
                OverallTemperature.Normal,
                OverallPopulation.Little
                );
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

        public static Func<IntVec3> GetAttackCells(Map map)
        {
            IntVec3 enterCell = FindNearEdgeCell(map, null);
            return () => CellFinder.RandomSpawnCellForPawnNear(enterCell, map, 4);
        }

        /// <summary>
        /// CaravanEnterMapUtility.FindNearEdgeCell
        /// </summary>
        private static IntVec3 FindNearEdgeCell(Map map, Predicate<IntVec3> extraCellValidator)
        {
            Predicate<IntVec3> baseValidator = (IntVec3 x) => x.Standable(map) && !x.Fogged(map);
            Faction hostFaction = map.ParentFaction;
            IntVec3 root;
            if (CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => baseValidator(x) && (extraCellValidator == null || extraCellValidator(x)) && ((hostFaction != null && map.reachability.CanReachFactionBase(x, hostFaction)) || (hostFaction == null && map.reachability.CanReachBiggestMapEdgeRoom(x))), map, CellFinder.EdgeRoadChance_Neutral, out root))
            {
                return CellFinder.RandomClosewalkCellNear(root, map, 5, null);
            }
            if (extraCellValidator != null && CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => baseValidator(x) && extraCellValidator(x), map, CellFinder.EdgeRoadChance_Neutral, out root))
            {
                return CellFinder.RandomClosewalkCellNear(root, map, 5, null);
            }
            if (CellFinder.TryFindRandomEdgeCellWith(baseValidator, map, CellFinder.EdgeRoadChance_Neutral, out root))
            {
                return CellFinder.RandomClosewalkCellNear(root, map, 5, null);
            }
            Log.Warning("Could not find any valid edge cell.", false);
            return CellFinder.RandomCell(map);
        }

        public static IntVec3 SpawnCaravanPirate(Map map, List<ThingEntry> pawns, Action<Thing, ThingEntry> spawn = null)
        {
            var nextCell = GameUtils.GetAttackCells(map);
            return SpawnList(map, pawns, true, (p) => true, spawn, (p) => nextCell());
        }

        public static IntVec3 SpawnList<TE>(Map map, List<TE> pawns, bool attackCell
            , Func<TE, bool> getPirate
            , Action<Thing, TE> spawn = null
            , Func<Thing, IntVec3> getCell = null)
            where TE : ThingEntry
        {
            if (MainHelper.DebugMode) Loger.Log("SpawnList...");

            //на основе UpdateWorldController.DropToWorldObjectDo
            var factionPirate = Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.defName == "Pirate")
                    ?? Find.FactionManager.OfAncientsHostile; //SessionClientController.Data.FactionPirate;

            IntVec3 ret = new IntVec3();
            ModBaseData.RunMainThreadSync(() =>
            {
                Thing thinXZ;
                for (int i = 0; i < pawns.Count; i++)
                {
                    var thing = pawns[i];
                    //GenSpawn.Spawn(pawn, cell, map, Rot4.Random, WipeMode.Vanish, false);

                    if (MainHelper.DebugMode) Loger.Log("Prepare...");
                    var thin = PrepareSpawnThingEntry(thing, factionPirate, getPirate(thing));

                    var cell = getCell != null ? getCell(thin) : thin.Position;
                    if (i == 0) ret = cell;

                    //if (MainHelper.DebugMode) 
                    try 
                    { 
                        Loger.Log("Spawn... " + thin.Label); 
                    } 
                    catch 
                    { 
                        Loger.Log("Spawn... "); 
                    }
                    if (thin is Pawn)
                    {
                        if (MainHelper.DebugMode) Loger.Log("Pawn... " + thin.Position.x + " " + thin.Position.y);
                        try
                        {
                            GenSpawn.Spawn((Pawn)thin, cell, map);
                        }
                        catch(Exception exp)
                        {
                            Loger.Log("SpawnList Exception " + thing.Name + ": " + exp.ToString());
                            Thread.Sleep(5);
                            GenSpawn.Spawn((Pawn)thin, cell, map);
                        }
                    }
                    else
                        GenDrop.TryDropSpawn(thin, cell, map, ThingPlaceMode.Near, out thinXZ, null);
                    if (spawn != null) spawn(thin, thing);
                    if (MainHelper.DebugMode) Loger.Log("Spawn...OK");
                }

            });
            return ret;
        }

        /// <summary>
        /// Создает игровой объект. 
        /// Если это пешка не колонист, то делаем его пиратом заключённым.
        /// Если freePirate=true, то делаем враждебными пиратами всех из фракции игрока.
        /// revertPirate=true включает freePirate, если это бы не пират, и делает игроком, если был пиратом
        /// </summary>
        public static Thing PrepareSpawnThingEntry(ThingEntry thing, Faction factionPirate, bool freePirate = false/*, bool revertPirate = false*/)
        {
            var factionColonistLoadID = Find.FactionManager.OfPlayer.GetUniqueLoadID();
            var factionPirateLoadID = factionPirate.GetUniqueLoadID();

            var prisoner = thing.SetFaction(factionColonistLoadID, factionPirateLoadID);
            Thing thin;
            thin = thing.CreateThing(false);
            if (MainHelper.DebugMode) Loger.Log("SetFaction...");
            if (thin.def.CanHaveFaction)
            {
                if (MainHelper.DebugMode) Loger.Log("SetFaction...1");
                /*
                if (revertPirate)
                {
                    if (thin is Pawn && thin.Faction == Find.FactionManager.OfPlayer)
                    {
                        thin.SetFaction(factionPirate);
                        var p = thin as Pawn;
                        if (!freePirate) p.guest.SetGuestStatus(factionPirate, true);
                    }
                    else
                        thin.SetFaction(Find.FactionManager.OfPlayer);
                }
                else*/
                {
                    if (thin is Pawn && (prisoner || freePirate && (/*((Pawn)thin).RaceProps.Humanlike ||*/ thin.Faction == Find.FactionManager.OfPlayer)))
                    {
                        if (MainHelper.DebugMode) Loger.Log("SetFaction...2");
                        thin.SetFaction(factionPirate);
                        if (MainHelper.DebugMode) Loger.Log("SetFaction...3");
                        var p = thin as Pawn;
                        if (MainHelper.DebugMode) Loger.Log("SetFaction...4");
                        if (!freePirate && p.guest != null) p.guest.SetGuestStatus(factionPirate, true);
                        if (MainHelper.DebugMode) Loger.Log("SetFaction...5");
                    }
                    else
                    {
                        if (MainHelper.DebugMode) Loger.Log("SetFaction...6");
                        thin.SetFaction(Find.FactionManager.OfPlayer);
                        if (MainHelper.DebugMode) Loger.Log("SetFaction...7");
                    }
                }
            }
            return thin;
        }


        public static void PawnDestroy(Pawn pawn)
        {
            pawn.Destroy(DestroyMode.Vanish);
            Find.WorldPawns.RemovePawn(pawn); //не проверенное полное удаление, чтобы не появлялось клонов пешки после возврата её назад
        }

        public static void ApplyState(Thing thing, AttackThingState state, bool pawnHealthStateDead = false)
        {
            //полезное из игры: RecoverFromUnwalkablePositionOrKill
            if (state.StackCount > 0 && thing.stackCount != state.StackCount)
            {
                Loger.Log("Client ApplyState Set StackCount " + thing.stackCount.ToString() + " -> " + state.StackCount.ToString());
                thing.stackCount = state.StackCount;
            }

            if (thing.Position.x != state.Position.x || thing.Position.z != state.Position.z)
            {
                thing.Position = state.Position.Get();
                if (thing is Pawn)
                {
                    var pawn = (Pawn)thing;
                    try
                    {
                        pawn.Notify_Teleported(true, true);
                    }
                    catch (Exception ext)
                    {
                        Loger.Log("Client ApplyState Exception " + ext.ToString());
                    }
                    pawn.Drawer.DrawTrackerTick();
                }
            }

            if (thing is Fire)
            {
                (thing as Fire).fireSize = (float)state.HitPoints / 10000f;
            }
            else
            {
                if (thing.def.useHitPoints)
                {
                    Loger.Log("Client ApplyState Set HitPoints " + thing.HitPoints.ToString() + " -> " + state.HitPoints.ToString());
                    thing.HitPoints = state.HitPoints;
                }
            }
            
            if (thing is Pawn)
            {
                var pawn = thing as Pawn;
                if ((int)pawn.health.State != (int)state.DownState)
                {
                    if (pawn.health.State == PawnHealthState.Dead)
                    {
                        Loger.Log("Client ApplyState Set pawn state is Dead! Error to change on " + state.DownState.ToString());
                    }
                    else if (state.DownState == AttackThingState.PawnHealthState.Dead)
                    {
                        if (pawnHealthStateDead)
                        {
                            Loger.Log("Client ApplyState Set pawn state (1): " + pawn.health.State.ToString() + " -> " + state.DownState.ToString());
                            HealthUtility.DamageUntilDead(pawn);
                            //PawnKill(pawn);
                        }
                    }
                    else if (state.DownState == AttackThingState.PawnHealthState.Down)
                    {
                        Loger.Log("Client ApplyState Set pawn state (2): " + pawn.health.State.ToString() + " -> " + state.DownState.ToString());
                        //todo! Применяем наркоз?
                        HealthUtility.DamageUntilDowned(pawn, false);
                    }
                    else
                    {
                        Loger.Log("Client ApplyState Set pawn state (3): " + pawn.health.State.ToString() + " -> " + state.DownState.ToString());
                        //полное лечение
                        pawn.health.Notify_Resurrected();
                    }
                }
            }

        }
        /*
        public static void PawnKill(Pawn pawn)
        {
            //заменено на HealthUtility.DamageUntilDead(p);
            DamageDef crush = DamageDefOf.Crush;
            float amount = 99999f;
            float armorPenetration = 999f;
            BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
            DamageInfo damageInfo = new DamageInfo(crush, amount, armorPenetration, -1f, null, brain, null, DamageInfo.SourceCategory.Collapse, null);
            pawn.TakeDamage(damageInfo);
            if (!pawn.Dead)
            {
                pawn.Kill(new DamageInfo?(damageInfo), null);
            }
        }
        */

        private static bool DialodShowing = false;
        private static Queue<Action> DialodQueue = new Queue<Action>();

        public static void ShowDialodOKCancel(string title
            , string text
            , Action ActOK
            , Action ActCancel
            , GlobalTargetInfo? target = null
            , string AltText = null
            , Action ActAlt = null)
        {
            DiaNode diaNode = new DiaNode(text);

            if (target != null)
            {
                var diaOptionT = new DiaOption("JumpToLocation".Translate()); //"Перейти к месту"
                diaOptionT.action = () =>
                {
                    CameraJumper.TryJumpAndSelect(target.Value);
                };
                diaNode.options.Add(diaOptionT);
            }

            DiaOption diaOption = new DiaOption("OCity_GameUtils_Ok".Translate()); //OK -> Принять передачу
            diaOption.action = () => { ActOK(); DialodQueueGoNext(); };
            /*{ спавн пешки бегущей "на помощь"
                GenSpawn.Spawn(refugee, spawnSpot, map, WipeMode.Vanish);
                refugee.SetFaction(Faction.OfPlayer, null);
                CameraJumper.TryJump(refugee);
                QueuedIncident qi = new QueuedIncident(new FiringIncident(IncidentDefOf.RaidEnemy, null, raidParms), Find.TickManager.TicksGame + IncidentWorker_RefugeeChased.RaidDelay.RandomInRange, 0);
                Find.Storyteller.incidentQueue.Add(qi);

            };*/
            diaOption.resolveTree = true;
            diaNode.options.Add(diaOption);

            if (!string.IsNullOrEmpty(AltText) && ActAlt != null)
            {
                diaOption = new DiaOption(AltText);
                diaOption.action = () => { ActAlt(); DialodQueueGoNext(); };
                diaOption.resolveTree = true;
                diaNode.options.Add(diaOption);
            }

            if (ActCancel != null)
            {
                diaOption = new DiaOption("RejectLetter".Translate());
                //RansomDemand_Reject это "Отказаться"
                //RejectLetter это Отклонить
                diaOption.action = () => { ActCancel(); DialodQueueGoNext(); };
                diaOption.resolveTree = true;
                diaNode.options.Add(diaOption);
            }

            Action show = () => Find.WindowStack.Add(new Dialog_NodeTreeWithFactionInfo(diaNode, null, true, true, title));

            //если окно одно, то запускаем, если это окно создается при уже открытом другом, то ставим в очередь
            lock (DialodQueue)
            {
                if (!DialodShowing)
                {
                    DialodShowing = true;
                    show();
                }
                else
                {
                    DialodQueue.Enqueue(show);
                }
            }
        }

        private static void DialodQueueGoNext()
        {
            lock (DialodQueue)
            {
                if (DialodQueue.Count > 0)
                {
                    DialodQueue.Dequeue()();
                }
                else
                {
                    DialodShowing = false;
                }
            }
        }

        /// <summary>
        /// Рисуем кружок с цифрой. Код из мода ResearchTree (MIT license)
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="main"></param>
        /// <param name="background"></param>
        /// <param name="label"></param>
        public static void DrawLabel(Rect canvas, Color main, Color background, int label)
        {
            // draw coloured tag
            GUI.color = main;
            GUI.DrawTexture(canvas, CircleFill);

            // if this is not first in line, grey out centre of tag
            if (background != main)
            {
                GUI.color = background;
                GUI.DrawTexture(canvas.ContractedBy(2f), CircleFill);
            }

            // draw queue number
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(canvas, label.ToString());
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
