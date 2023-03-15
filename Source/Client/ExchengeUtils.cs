using Model;
using OCUnion;
using RimWorld;
using RimWorld.Planet;
using RimWorldOnlineCity.GameClasses.Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Transfer;
using Verse;

namespace RimWorldOnlineCity
{
    public static class ExchengeUtils
    {
        /// <summary>
        /// Получить все объекты на которых открыты сделки
        /// </summary>
        public static IEnumerable<WorldObject> GetWorldObjectsForTrade()
        {
            return UpdateWorldController.WorldObject_TradeOrdersOnline;
        }

        /// <summary>
        /// Получить все объекты на данной точке
        /// </summary>
        /// <param name="tileID"></param>
        /// <returns></returns>
        public static List<WorldObject> WorldObjectsByTile(int tileID)
        {
            return Find.WorldObjects.ObjectsAt(tileID).ToList();
        }

        /// <summary>
        /// Получить все игровые объекты игрока
        /// </summary>
        public static List<WorldObject> WorldObjectsPlayer()
        {
            return UpdateWorldController.allWorldObjects
                        .Where(o => (o.Faction?.IsPlayer ?? false)
                            && (o is Settlement || o is Caravan))
                        .ToList();
        }

        public static string CargoDeliveryCalc(WorldObject fromWorldObject, WorldObject toWorldObject, List<ThingTrade> things, out int cost, out int dist)
        {
            dist = GameUtils.DistanceBetweenTile(fromWorldObject.Tile, toWorldObject.Tile);
            cost = (int)(things.Sum(t => t.GameCost * t.Count) * SessionClientController.Data.GeneralSettings.ExchengeCostCargoDelivery / 1000f * dist / 100f);
            if (dist > 0 && cost <= 0) cost = 1;
            return $"{fromWorldObject.LabelShortCap} -> {toWorldObject?.LabelShortCap} "
                + "OCity_ExchengeUtils_Distance".Translate() + " " + dist + ", "
                + "OCity_ExchengeUtils_Cost".Translate() + " " + cost;
        }

        public static bool CargoDelivery(WorldObject fromWorldObject, WorldObject toWorldObject, List<ThingTrade> things, Action finish = null)
        {
            try
            {
                Loger.Log($"Client CargoDelivery: " + CargoDeliveryCalc(fromWorldObject, toWorldObject, things, out int cost, out int dist), Loger.LogLevel.EXCHANGE);

                return CargoDelivery(fromWorldObject.Tile, toWorldObject.Tile, things, cost, dist);
            }
            finally
            {
                if (finish != null) finish();
            }
        }

        private static bool CargoDelivery(int tileFrom, int tileTo, List<ThingTrade> things, int cost, int dist)
        {
            if (tileFrom == tileTo || tileFrom == 0 || tileTo == 0 || cost == 0 || dist == 0) return false;

            bool result = false;
            SessionClientController.Command((connect) =>
            {
                result = connect.ExchengeStorage(null, things, tileFrom, tileTo, cost, dist);
            });

            return result;
        }

        /// <summary>
        /// Перемещаем выделенные вещи из fromWorldObject в toWorldObject
        /// </summary>
        /// <param name="toWorldObject"></param>
        public static bool MoveSelectThings(WorldObject fromWorldObject, WorldObject toWorldObject, List<TransferableOneWay> selectTow, Action finish = null)
        {
            var select = selectTow.TransferableOneWaysToDictionary();
            return MoveSelectThings(fromWorldObject, toWorldObject, select, finish);
        }
        /// <summary>
        /// Перемещаем выделенные вещи из fromWorldObject в toWorldObject
        /// </summary>
        /// <param name="toWorldObject"></param>
        public static bool MoveSelectThings(WorldObject fromWorldObject, WorldObject toWorldObject, Dictionary<Thing, int> select, Action finish = null)
        {
            //LongEventHandler.QueueLongEvent(() =>
            //{
            try
            {
                if (SessionClientController.Data.BackgroundSaveGameOff)
                {
                    Loger.Log($"Client ExchengeEdit Cancel BackgroundSaveGameOff", Loger.LogLevel.EXCHANGE);
                    return false;
                }
                Loger.Log($"Client ExchengeEdit MoveSelectThings: {fromWorldObject.LabelShortCap} -> {toWorldObject?.LabelShortCap} ", Loger.LogLevel.EXCHANGE);
                //Thread.Sleep(5000);

                //Изымаем вещи из источника fromWorldObject
                var toTargetThing = new List<Thing>();
                var toTargetEntry = new List<ThingTrade>();
                if (fromWorldObject is TradeThingsOnline)
                {
                    //Мы будем куда-то в игру забирать с сервера, и toTargetEntry не нужен
                    //В источнике AllThings недавно созданные ещё свободные вещи, нужно только отобрать выбраное количество
                    toTargetThing = select.Select(p =>
                    {
                        if (p.Key.stackCount != p.Value) p.Key.stackCount = p.Value;
                        return p.Key;
                    }).ToList();
                    //вещи для удаления из яблока
                    toTargetEntry = toTargetThing.Select(t => ThingTrade.CreateTrade(t, t.stackCount)).ToList();
                }
                else
                {
                    toTargetThing = fromWorldObject is Caravan ? ExchengeUtils.DeSpawnCaravan(select, fromWorldObject as Caravan) : ExchengeUtils.DeSpawnMap(select);

                    //Вещи нужно отправить на сервер (нужен список ThingEntry)
                    if (toWorldObject == null
                        || toWorldObject is TradeThingsOnline
                        || toWorldObject is CaravanOnline) toTargetEntry = ExchengeUtils.CreateTradeAndDestroy(toTargetThing);
                }

                //Помещаем вещи в назначение toWorldObject, если это игровой объект
                if (toWorldObject is Caravan || toWorldObject is Settlement)
                {
                    Loger.Log($"Client ExchengeEdit MoveSelectThings SpawnThings", Loger.LogLevel.EXCHANGE);
                    if (toWorldObject is Caravan)
                        ExchengeUtils.SpawnThings(toTargetThing, toWorldObject as Caravan);
                    else
                        ExchengeUtils.SpawnThings(toTargetThing, (toWorldObject as Settlement).Map);
                }

                //В этой позиции мы отредактировали данные игры и ниже посылаем изменения на сервер

                //Если один из объектов красное яблоко TradeThingsOnline, то посылаем изменения на сервер
                //А также если назначение другой игрок (toWorldObject is CaravanOnline), то дополнительно отправляем письмо (ExchengeUtils.SendThingsAndSave)
                if (toWorldObject is TradeThingsOnline)
                {
                    //1 вариант завершения MoveSelectThings: передача в торговый склад

                    Loger.Log($"Client ExchengeEdit MoveSelectThings SaveGame and ExchengeStorage", Loger.LogLevel.EXCHANGE);
                    //отправляем вещи toTargetEntry в красное яблоко
                    //После передачи сохраняем, чтобы нельзя было обузить
                    SessionClientController.SaveGameNowSingleAndCommandSafely(
                        (connect) =>
                        {
                            return connect.ExchengeStorage(toTargetEntry, null, fromWorldObject.Tile);
                        },
                        () =>
                        {
                            if (finish != null) finish();
                        },
                        null,
                        false); //если не удалось отправить письмо, то жопа так как сейв уже прошел

                }
                else if (fromWorldObject is TradeThingsOnline)
                {
                    //2 вариант завершения MoveSelectThings: передача из торгового склада

                    Loger.Log($"Client ExchengeEdit MoveSelectThings ExchengeStorage", Loger.LogLevel.EXCHANGE);

                    //удаляем вещи toTargetEntry в красном яблоке (в игре уже их разместили)
                    var errorMessage = SessionClientController.CommandSafely((connect) =>
                    {
                        return connect.ExchengeStorage(null, toTargetEntry, fromWorldObject.Tile);
                    });

                    if (toWorldObject is CaravanOnline)
                    {
                        //дополнительно передаем игроку
                        if (errorMessage == null)
                        {
                            //передача другому игроку 
                            ExchengeUtils.SendThingsAndSave(toTargetEntry.Cast<ThingEntry>().ToList(),
                                toWorldObject as CaravanOnline,
                                () => { if (finish != null) finish(); },
                                () => { SessionClientController.Disconnected("OCity_SessionCC_Disconnected".TranslateCache()); }
                                );
                            return true;
                        }
                    }
                    else if (toWorldObject == null)
                    {
                        if (finish != null) finish();
                        return true;
                    }
                    //если была ошибка, то отменяем операции с игрой откатывая до сейва
                    if (errorMessage != null) SessionClientController.Disconnected("OCity_SessionCC_Disconnected".TranslateCache() + " " + errorMessage);
                    else
                    {
                        //если всё хорошо сохраняем, чтобы при возможном будущем сбое вещи взятые из яблока не пропали
                        SessionClientController.SaveGameNow(false,
                            () =>
                            {
                                if (finish != null) finish();
                            });
                    }
                }
                else if (toWorldObject is CaravanOnline)
                {
                    //3 вариант завершения MoveSelectThings: передача из игры другому игроку

                    //передача другому игроку 
                    ExchengeUtils.SendThingsAndSave(toTargetEntry.Cast<ThingEntry>().ToList(),
                        toWorldObject as CaravanOnline,
                        () => { if (finish != null) finish(); },
                        () => { SessionClientController.Disconnected("OCity_SessionCC_Disconnected".TranslateCache()); }
                        );
                    return true;
                }
                else
                {
                    //4 вариант завершения MoveSelectThings: из игры в игру, или просто удалить из игры, при toWorldObject = null

                    Loger.Log($"Client ExchengeEdit MoveSelectThings noob", Loger.LogLevel.EXCHANGE);
                    if (finish != null) finish();
                }
                Loger.Log($"Client ExchengeEdit MoveSelectThings end", Loger.LogLevel.EXCHANGE);
            }
            catch (Exception exp)
            {
                ExceptionUtil.ExceptionLog(exp, "MoveSelectThings");
                return false;
            }
            return true;

            //}, "OCity_UpdateWorld_Trade".TranslateCache(), true, null);
        }

        /// <summary>
        /// Указываем реальные привязанные игровые вещи с кол-во, которое нужно взять и сделать свободными.
        /// Для вещей с карты игрока
        /// </summary>
        public static List<Thing> DeSpawnMap(Dictionary<Thing, int> select)
        {
            var freeThings = new List<Thing>();
            foreach (var pair in select)
            {
                var thing = pair.Key;
                var numToTake = pair.Value;
                if (thing is Pawn)
                {
                    var pawn = thing as Pawn;
                    pawn.DeSpawn();
                    freeThings.Add(pawn);
                }
                else
                {
                    Thing freeThing = thing.SplitOff(numToTake);
                    freeThings.Add(freeThing);
                }
            }
            return freeThings;
        }

        /// <summary>
        /// Указываем реальные привязанные игровые вещи с кол-во, которое нужно взять и сделать свободными.
        /// Для вещей с каравана игрока
        /// </summary>
        /// <returns></returns>
        public static List<Thing> DeSpawnCaravan(Dictionary<Thing, int> select, Caravan caravan)
        {
            if (MainHelper.DebugMode) Loger.Log("OCity debug");
            var freeThings = new List<Thing>();
            bool selectAllCaravan = caravan.PawnsListForReading.Count == select.Count(s => s.Key is Pawn);
            if (selectAllCaravan)
            {
                Loger.Log("DeSpawnCaravan. Select all Caravan");
                select = new Dictionary<Thing, int>();
                foreach (var pawn in caravan.PawnsListForReading)
                {
                    foreach (var item in pawn.inventory.innerContainer.ToDictionary(t => t, t => t.stackCount))
                        select.Add(item.Key, item.Value);
                    select.Add(pawn, 1);
                    pawn.inventory.innerContainer.Clear();
                }
            }
            if (MainHelper.DebugMode) Loger.Log("OCity debug 1");
            //передаем выбранные товары из caravan к другому игроку в сaravanOnline
            ThingOwner<Thing> сontainer = new ThingOwner<Thing>();
            foreach (var pair in select.OrderBy(s => s.Key is Pawn ? 0 : 1)) //сначала все пешки
            {
                var thing = pair.Key;
                var numToTake = pair.Value;
                if (thing is Pawn)
                {
                    if (MainHelper.DebugMode) Loger.Log("OCity debug 2 " + thing.Label);
                    var pawn = thing as Pawn;
                    //если отдали пешку, то выкладываем все другим и удаляемся из каравана
                    var things = pawn.inventory.innerContainer.ToList();
                    pawn.inventory.innerContainer.Clear();
                    if (MainHelper.DebugMode) Loger.Log("OCity debug 3");
                    GameUtils.DeSpawnSetupOnCaravan(caravan, pawn);
                    foreach (var thin in things)
                    {
                        var p = CaravanInventoryUtility.FindPawnToMoveInventoryTo(thin, caravan.PawnsListForReading, null);
                        if (p != null) p.inventory.innerContainer.TryAdd(thin, true);
                    }
                    if (MainHelper.DebugMode) Loger.Log("OCity debug 4 ");
                    freeThings.Add(pawn);
                }
                else
                {
                    if (MainHelper.DebugMode) Loger.Log("OCity debug 5 " + thing.Label);
                    ////если отдали вещь, то находим кто её тащит и убираем с него
                    //Pawn ownerOf = CaravanInventoryUtility.GetOwnerOf(caravan, thing);
                    ////Loger.Log("OCity debug 6");
                    ////if (ownerOf == null) Loger.Log("OCity debug: ownerOf is null");
                    ////else if (ownerOf.inventory == null) Loger.Log("OCity debug: ownerOf.inventory is null");
                    ////else if (ownerOf.inventory.innerContainer == null) Loger.Log("OCity debug: ownerOf.inventory.innerContainer is null");
                    ////Loger.Log("OCity debug: " + thing.ToString() + " " + thing.LabelCap);
                    //if (ownerOf != null)
                    //{
                    //    Loger.Log("OCity debug 6 " + ownerOf.Label + $" inInvent={ownerOf.inventory.innerContainer.Contains(thing)} d={thing.Destroyed} cnt={thing.stackCount}");

                    //    //вариант из игры
                    //    //ownerOf.inventory.innerContainer.TryTransferToContainer(thing, сontainer, numToTake, out Thing freeThing);
                    //    //руками (numToTake не может быть больше thing.stackCount)
                    //    var allStac = thing.stackCount <= numToTake;
                    //    Thing freeThing = thing.SplitOff(numToTake);
                    //    if (allStac && ownerOf.inventory.innerContainer.Contains(thing))
                    //    {
                    //        ownerOf.inventory.innerContainer.Remove(thing);
                    //    }

                    //    //var allStac = thing.stackCount <= numToTake;
                    //    //Thing freeThing = ThingMaker.MakeThing(thing.def, thing.Stuff);
                    //    //freeThing.stackCount = numToTake;

                    //    //Loger.Log("OCity debug 6 " + ownerOf.Label
                    //    //    + $" inInvent={ownerOf.inventory.innerContainer.Contains(thing)} d={thing.Destroyed} oldCnt={thing?.stackCount} eqv={freeThing==thing} selCnt={freeThing?.stackCount}");
                    //    freeThings.Add(freeThing);
                    //}
                    //else
                    //{
                    //    Loger.Log("OCity debug 7 null");
                    //    Thing freeThing = thing.SplitOff(numToTake);
                    //    freeThings.Add(freeThing);
                    //}

                    Thing freeThing = thing.SplitOff(numToTake);
                    freeThings.Add(freeThing);
                    if (MainHelper.DebugMode) Loger.Log("OCity debug 7");
                }

                if (thing.holdingOwner != null)
                {
                    Loger.Log("Error DeSpawnCaravan holdingOwner != null");
                    //thing.holdingOwner = null;
                }
                //Loger.Log("OCity debug 9");
            }
            if (MainHelper.DebugMode) Loger.Log("OCity debug 10");
            if (selectAllCaravan)
            {
                Find.WorldObjects.Remove(caravan);
            }
            return freeThings;
        }

        public static void DestroyThings(List<Thing> things)
        {
            foreach (var thing in things.OrderBy(t => t is Pawn ? 1 : 0)) //сначала все вещи
            {
                if (MainHelper.DebugMode) Loger.Log("OCity debug DestroyThings " + thing.Label + $" d={thing.Destroyed} cnt={thing?.stackCount} ");

                if (thing is Pawn)
                {
                    if (MainHelper.DebugMode)
                    {
                        foreach (var t in (thing as Pawn).inventory.innerContainer)
                            Loger.Log("OCity debug DestroyThings " + t.Label + $" d={t.Destroyed} cnt={t?.stackCount} ho={t.holdingOwner == (thing as Pawn).inventory.innerContainer} ");
                    }
                    GameUtils.PawnDestroy(thing as Pawn);
                }
                else
                {
                    thing.Destroy();
                }
            }
        }

        /// <summary>
        /// Перевод свободных (новосозданнных или отвязанных от прошлого метоположения) вещей в объекты сервере и удаление их из игры
        /// </summary>
        public static List<ThingEntry> CreateEntryAndDestroy(List<Thing> things)
        {
            var sendThings = things.Select(t => ThingEntry.CreateEntry(t, t.stackCount)).ToList();
            DestroyThings(things);
            return sendThings;
        }
        /// <summary>
        /// Перевод свободных (новосозданнных или отвязанных от прошлого метоположения) вещей в объекты сервере и удаление их из игры
        /// </summary>
        public static List<ThingTrade> CreateTradeAndDestroy(List<Thing> things)
        {
            var sendThings = things.Select(t => ThingTrade.CreateTrade(t, t.stackCount)).ToList();
            DestroyThings(things);
            return sendThings;
        }

        public static WorldObject GetPlace(IModelPlace modelPlace, bool softSettlement = true, bool softNewCaravan = false)
        {
            if (modelPlace.PlaceServerId <= 0)
            {
                Loger.Log("GetPlace fail: no data", Loger.LogLevel.ERROR);
                return null;
            }
            //находим наш объект, кому пришла передача
            var placeId = UpdateWorldController.GetLocalIdByServerId(modelPlace.PlaceServerId);

            WorldObject place = null;
            if (placeId == 0)
            {
                if (softNewCaravan)
                {
                    Loger.Log($"GetPlace softNewCaravan Tile={modelPlace.Tile}");
                    //если нет, и нужен караван, то возможно он был пересоздан: ищим в той же точке новый
                    place = Find.WorldObjects.AllWorldObjects
                        .Where(o => o is Caravan && o.Tile == modelPlace.Tile && o.Faction == Faction.OfPlayer)
                        .OrderByDescending(o => o.ID)
                        .FirstOrDefault();
                }
                else if (softSettlement)
                {
                    Loger.Log("GetPlace softSettlement");
                    //если нет, и какой-то сбой, посылаем в первый поселек
                    place = Find.WorldObjects.Settlements
                        .FirstOrDefault(f => f.Faction == Faction.OfPlayer && f is MapParent && ((MapParent)f).Map.IsPlayerHome);
                }
            }
            else
            {
                place = Find.WorldObjects.AllWorldObjects
                    .FirstOrDefault(o => o.ID == placeId && o.Faction == Faction.OfPlayer);
            }
            //создаем объекты
            if (place == null)
            {
                Loger.Log("GetPlace fail: place is null", Loger.LogLevel.ERROR);
                return null;
            }

            return place;
        }

        /// <summary>
        /// Спавн свободных (новосозданнных или отвязанных от прошлого метоположения) вещей на карту игрока
        /// </summary>
        public static void SpawnThings(List<Thing> things, Map map, IntVec3 cell = default)
        {
            if (cell == default) cell = GameUtils.GetTradeCell(map);
            foreach (var thing in things)
            {
                var pawn = thing as Pawn;
                if (pawn != null)
                {
                    GenSpawn.Spawn(pawn, cell, map);
                    if (pawn.Dead
                        && !Find.WorldPawns.AllPawnsDead.Contains(pawn))
                    {
                        Find.WorldPawns.AllPawnsDead.Add(pawn);
                    }
                }
                else
                    GenDrop.TryDropSpawn(thing, cell, map, ThingPlaceMode.Near, out _, null);
            }
        }

        /// <summary>
        /// Спавн свободных (новосозданнных или отвязанных от прошлого метоположения) вещей в караван игрока
        /// </summary>
        public static void SpawnThings(List<Thing> things, Caravan caravan)
        {
            var pawns = caravan.PawnsListForReading;
            foreach (var thing in things)
            {
                var pawn = thing as Pawn;
                if (pawn != null)
                {
                    caravan.AddPawn(pawn, true);
                    GameUtils.SpawnSetupOnCaravan(pawn);
                    if (pawn.Dead
                        && !Find.WorldPawns.AllPawnsDead.Contains(pawn))
                    {
                        Find.WorldPawns.AllPawnsDead.Add(pawn);
                    }
                }
                else
                {
                    var p = CaravanInventoryUtility.FindPawnToMoveInventoryTo(thing, pawns, null);
                    if (p != null)
                        p.inventory.innerContainer.TryAdd(thing, true);
                }
            }
        }

        public static void SpawnToWorldObject(WorldObject place, List<ThingEntry> things, string text = null)
        {
            GlobalTargetInfo ti = new GlobalTargetInfo(place);
            //var factionPirate = SessionClientController.Data.FactionPirate;

            if (MainHelper.DebugMode) Loger.Log("Spawn================================================= {");

            //как в Dialog_Exchenge.UpdateOrdersList
            //игнорировать автоисправляющуся ошибку 
            //Could not get load ID. We're asking for something which was never added during LoadingVars. pathRelToParent=/leader, parent=PlayerTribe
            using (var gameError = GameUtils.NormalGameError())
            {
                if (place is Settlement && ((Settlement)place).Map != null)
                {
                    var map = ((Settlement)place).Map;
                    var cell = GameUtils.GetTradeCell(map);
                    ti = new GlobalTargetInfo(cell, map);
                    var toSpawn = new List<Thing>();
                    if (MainHelper.DebugMode) Loger.Log("CreateThing...");
                    foreach (var thing in things)
                    {
                        //if (MainHelper.DebugMode) Loger.Log("Spawn------------------------------------------------- {" + Environment.NewLine
                        //    + thing.Data + Environment.NewLine
                        //    + "Spawn------------------------------------------------- }" + Environment.NewLine);
                        var thin = thing.CreateThing();
                        toSpawn.Add(thin);
                    }
                    if (MainHelper.DebugMode) Loger.Log("Spawn map...");
                    SpawnThings(toSpawn, map, cell);
                    if (MainHelper.DebugMode) Loger.Log("Spawn map...OK");
                }
                else if (place is Caravan)
                {
                    var caravan = place as Caravan;
                    var toSpawn = new List<Thing>();
                    if (MainHelper.DebugMode) Loger.Log("CreateThing...");
                    foreach (var thing in things)
                    {
                        var thin = thing.CreateThing();
                        toSpawn.Add(thin);
                    }
                    if (MainHelper.DebugMode) Loger.Log("Spawn caravan...");
                    SpawnThings(toSpawn, caravan);
                    if (MainHelper.DebugMode) Loger.Log("Spawn caravan...OK");
                }
            }
            if (MainHelper.DebugMode) Loger.Log("Spawn================================================= }");

            if (text != null)
            {
                Find.LetterStack.ReceiveLetter("OCity_UpdateWorld_Trade".Translate()
                    , text
                    , LetterDefOf.PositiveEvent
                    , ti
                    , null);
            }
        }

        public static void SendThingsAndSave(List<ThingEntry> sendThings, 
            CaravanOnline destination,
            Action finishGood = null,
            Action finishBad = null)
        {
            if ((sendThings?.Count ?? 0) == 0)
            {
                Loger.Log("Client SendThings Not SendThings");
                return;
            }

            //После передачи сохраняем, чтобы нельзя было обузить, после чего передаем вещи
            SessionClientController.SaveGameNowSingleAndCommandSafely(
                (connect) =>
                {
                    return connect.SendThings(sendThings
                        , SessionClientController.My.Login
                        , destination.OnlinePlayerLogin
                        , destination.OnlineWObject.PlaceServerId
                        , destination.Tile);
                },
                finishGood,
                finishBad); //если не удалось отправить письмо, то жопа так как сейв уже прошел
        }
        public static void SendThingsWithDestroy(Dictionary<Thing, int> select
            , Caravan caravan //отправить null, если отправка с карты, а не с каравана
            , CaravanOnline destination)
        {
            if (!SessionClientController.Data.BackgroundSaveGameOff)
            {
                List<ThingEntry> sendThings;
                using (var gameError = new CatchGameError())
                {
                    var freeThing = caravan == null ? DeSpawnMap(select) : DeSpawnCaravan(select, caravan);

                    if (gameError.GameError != null) Loger.Log("Client SendThingsWithDestroy GameError DeSpawn");
                    gameError.GameError = null;

                    if (MainHelper.DebugMode) Loger.Log("OCity SendThingsWithDestroy 2 " + freeThing.Count);
                    sendThings = CreateEntryAndDestroy(freeThing);

                    if (gameError.GameError != null) Loger.Log("Client SendThingsWithDestroy GameError CreateEntryAndDestroy");
                }

                SendThingsAndSave(sendThings, destination);
            }
        }

        /// <summary>
        /// Пункт меню передачи товаров в CaravanOnline that (караван или поселение другого игрока). Откуда не важно.
        /// </summary>
        public static FloatMenuOption ExchangeOfGoods_GetFloatMenu(CaravanOnline that, Action actionFloatMenu)
        {
            FloatMenuOption fmoTrade;
            // Передача товара
            bool disTrade = GameUtils.IsProtectingNovice();
            fmoTrade = new FloatMenuOption("OCity_Caravan_Trade".Translate(that.OnlinePlayerLogin + " " + that.OnlineName)
                + (disTrade ? "OCity_Caravan_Abort".Translate().ToString() + " " + MainHelper.MinCostForTrade.ToString() : "") // "Вам нет года или стоимость меньше" You are under a year old or cost less than
                , actionFloatMenu, MenuOptionPriority.Default, null, null, 0f, null, that);
            if (disTrade)
            {
                fmoTrade.Disabled = true;
            }
            return fmoTrade;
        }

        /// <summary>
        /// Совершение действия передачи товаров другому игроку
        /// </summary>
        public static void ExchangeOfGoods_DoAction(CaravanOnline destination, Caravan source)
        {
            Dialog_TradeOnline form = null;
            if (destination.OnlineWObject == null)
            {
                Log.Error("OCity_Caravan_LOGNoData".Translate());
                return;
            }

            var goods = GameUtils.GetAllThings(source);

            form = new Dialog_TradeOnline(goods
                , destination.OnlinePlayerLogin
                , destination.OnlineWObject.FreeWeight
                , () =>
                {
                    ExchangeOfGoods_DoAction(destination, source, form.GetSelect());
                });
            Find.WindowStack.Add(form);
        }

        /// <summary>
        /// Совершение действия передачи товаров другому игроку 
        /// </summary>
        /// <param name="source">Отправить null если не караван</param>
        public static void ExchangeOfGoods_DoAction(CaravanOnline destination, Caravan source, List<TransferableOneWay> goods) =>
            ExchangeOfGoods_DoAction(destination, source, goods.TransferableOneWaysToDictionary());

        /// <summary>
        /// Совершение действия передачи товаров другому игроку 
        /// </summary>
        /// <param name="source">Отправить null если не караван</param>
        public static void ExchangeOfGoods_DoAction(CaravanOnline destination, Caravan source, Dictionary<Thing, int> goods)
        {
            if (destination.OnlineWObject == null)
            {
                Log.Error("OCity_Caravan_LOGNoData".Translate());
                return;
            }
            ExchengeUtils.SendThingsWithDestroy(goods
                , source
                , destination);
        }

    }
}
