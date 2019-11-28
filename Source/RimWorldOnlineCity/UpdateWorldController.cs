using Model;
using OCUnion;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Transfer;
using Verse;

namespace RimWorldOnlineCity
{
    static class UpdateWorldController
    {
        public static void SendToServer(ModelPlayToServer toServ)
        {
            toServ.LastTick = (long)Find.TickManager.TicksGame;

            //отправка всех новых и измененных объектов игрока
            toServ.WObjects = Find.WorldObjects.AllWorldObjects
                .Where(o => o.Faction != null && o.Faction.IsPlayer //&& !(o is CaravanOnline) && !(o is BaseOnline)
                    && (o is Settlement || o is Caravan)) //Чтобы отсеч разные карты событий
                .Select(o => GetWorldObjectEntry(o))
                .ToList();

            //свои объекты которые удалил пользователь с последнего обновления
            if (ToDelete != null)
            {
                var toDeleteNewNow = MyWorldObjectEntry
                    .Where(p => !Find.WorldObjects.AllWorldObjects.Any(wo => wo.ID == p.Key))
                    .Select(p => p.Value)
                    .ToList();
                ToDelete.AddRange(toDeleteNewNow);
            }

            toServ.WObjectsToDelete = ToDelete;
        }

        public static void LoadFromServer(ModelPlayToClient fromServ, bool removeMissing)
        {
            if (removeMissing)
            {
                //запускается только при первом получении данных от сервера после загрузки или создания карты
                //удаляем все объекты других игроков (на всякий случай, т.к. в сейв они не сохраняются)

                var missingWObjects = Find.WorldObjects.AllWorldObjects
                    .Select(o => o as CaravanOnline)
                    .Where(o => o != null)
                    //.Where(o => !fromServ.WObjects.Any(wo => wo.ServerId == o.OnlineWObject.ServerId))
                    .ToList();
                for (int i = 0; i < missingWObjects.Count; i++)
                {
                    Find.WorldObjects.Remove(missingWObjects[i]);
                }
                Loger.Log("RemoveMissing " + missingWObjects.Count);
            }

            //обновление всех объектов
            ToDelete = new List<WorldObjectEntry>();
            if (fromServ.WObjects != null && fromServ.WObjects.Count > 0)
            {
                for (int i = 0; i < fromServ.WObjects.Count; i++)
                    ApplyWorldObject(fromServ.WObjects[i]);
            }
            if (fromServ.WObjectsToDelete != null && fromServ.WObjectsToDelete.Count > 0)
            {
                for (int i = 0; i < fromServ.WObjectsToDelete.Count; i++)
                    DeleteWorldObject(fromServ.WObjectsToDelete[i]);
            }

            //пришла посылка от каравана другого игрока
            if (fromServ.Mails != null && fromServ.Mails.Count > 0)
            {
                LongEventHandler.QueueLongEvent(delegate
                //LongEventHandler.ExecuteWhenFinished(delegate
                {
                    foreach (var mail in fromServ.Mails)
                    {
                        if (mail.To == null
                            || mail.To.Login != SessionClientController.My.Login
                            || mail.Things == null
                            || mail.Things.Count == 0
                            || mail.PlaceServerId <= 0) continue;
                        //находим наш объект, кому пришла передача
                        var placeId = MyWorldObjectEntry
                            .Where(p => p.Value.ServerId == mail.PlaceServerId)
                            .Select(p => p.Key)
                            .FirstOrDefault();

                        Loger.Log("Mail " + placeId + " "
                            + (mail.From == null ? "-" : mail.From.Login) + "->"
                            + (mail.To == null ? "-" : mail.To.Login) + ":"
                            + mail.ContentString());
                        WorldObject place;
                        if (placeId == 0)
                        {
                            //если нет, и какой-то сбой, посылаем в первый поселек
                            place = Find.WorldObjects.Settlements
                                .FirstOrDefault(f => f.Faction == Faction.OfPlayer && f is MapParent && ((MapParent)f).Map.IsPlayerHome);
                        }
                        else
                        {
                            place = Find.WorldObjects.AllWorldObjects
                                .FirstOrDefault(o => o.ID == placeId && o.Faction == Faction.OfPlayer);
                        }
                        //создаем объекты
                        if (place != null)
                        {
                            DropToWorldObject(place, mail.Things, (mail.From == null ? "-" : mail.From.Login));
                        }
                    }
                }, "", false, null);
            }
        }

        public static void ClearWorld()
        {
            //Loger.Log("ClearWorld");
            var deleteWObjects = Find.WorldObjects.AllWorldObjects
                .Where(o => o is CaravanOnline)
                .ToList();

            for (int i = 0; i < deleteWObjects.Count; i++)
                Find.WorldObjects.Remove(deleteWObjects[i]);
        }

        private static void DropToWorldObject(WorldObject place, List<ThingEntry> things, string from)
        {
            var text = string.Format("OCity_UpdateWorld_TradeDetails".Translate()
                    , from
                    , place.LabelCap
                    , things.Aggregate("", (r, i) => r + Environment.NewLine + i.Name + " x" + i.Count));
            /*
            GlobalTargetInfo ti = new GlobalTargetInfo(place);
            if (place is Settlement && ((Settlement)place).Map != null)
            {
                var cell = GameUtils.GetTradeCell(((Settlement)place).Map);
                ti = new GlobalTargetInfo(cell, ((Settlement)place).Map);
            }
            */
            Find.TickManager.Pause();
            GameUtils.ShowDialodOKCancel("OCity_UpdateWorld_Trade".Translate()
                , text
                , () => DropToWorldObjectDo(place, things, from, text)
                , () => Log.Message("Drop Mail from " + from + ": " + text)
            );
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

        private static void DropToWorldObjectDo(WorldObject place, List<ThingEntry> things, string from, string text)
        {
            GlobalTargetInfo ti = new GlobalTargetInfo(place);
            var factionPirate = SessionClientController.Data.FactionPirate;

            if (MainHelper.DebugMode) Loger.Log("Mail================================================= {");

            if (place is Settlement && ((Settlement)place).Map != null)
            {
                var map = ((Settlement)place).Map;
                var cell = GameUtils.GetTradeCell(map);
                ti = new GlobalTargetInfo(cell, map);
                Thing thinXZ;
                foreach (var thing in things)
                {
                    if (MainHelper.DebugMode) Loger.Log("Mail------------------------------------------------- {"  + Environment.NewLine
                        + thing.Data + Environment.NewLine
                        + "Mail------------------------------------------------- }" + Environment.NewLine);
                    var thin = PrepareSpawnThingEntry(thing, factionPirate);
                    
                    if (MainHelper.DebugMode) Loger.Log("Spawn...");
                    if (thin is Pawn)
                    {
                        GenSpawn.Spawn((Pawn)thin, cell, map);
                    }
                    else
                        GenDrop.TryDropSpawn(thin, cell, map, ThingPlaceMode.Near, out thinXZ, null);
                    if (MainHelper.DebugMode) Loger.Log("Spawn...OK");
                }
            }
            else if (place is Caravan)
            {
                var pawns = (place as Caravan).PawnsListForReading;
                foreach (var thing in things)
                {
                    /*
                    thing.SetFaction(factionColonistLoadID, factionPirateLoadID);
                    var thin = thing.CreateThing(false);
                    */
                    var thin = PrepareSpawnThingEntry(thing, factionPirate);

                    if (thin is Pawn)
                    {
                        (place as Caravan).AddPawn(thin as Pawn, true);
                        GameUtils.SpawnSetupOnCaravan(thin as Pawn);
                    }
                    else
                    {
                        var p = CaravanInventoryUtility.FindPawnToMoveInventoryTo(thin, pawns, null);
                        if (p != null)
                            p.inventory.innerContainer.TryAdd(thin, true);
                    }
                }
            }

            if (MainHelper.DebugMode) Loger.Log("Mail================================================= }");
            
            Find.LetterStack.ReceiveLetter("OCity_UpdateWorld_Trade".Translate()
                , text
                , LetterDefOf.PositiveEvent
                , ti
                , null);
        }

        #region WorldObject

        /// <summary>
        /// Для поиска объектов, уже созданных в прошлые разы
        /// </summary>
        private static Dictionary<long, int> ConverterServerId { get; set; }
        private static Dictionary<int, WorldObjectEntry> MyWorldObjectEntry { get; set; }
        private static List<WorldObjectEntry> ToDelete { get; set; }

        public static string GetTestText()
        {
            var text = "ConverterServerId.";
            foreach (var item in ConverterServerId)
            {
                text += Environment.NewLine + item.Key + ", " + item.Value;
            }

            text += Environment.NewLine + Environment.NewLine + "MyWorldObjectEntry.";
            foreach (var item in MyWorldObjectEntry)
            {
                text += Environment.NewLine + item.Key + ", " + item.Value.ServerId + " " + item.Value.Name;
            }

            text += Environment.NewLine + Environment.NewLine + "ToDelete.";
            foreach (var item in ToDelete)
            {
                text += Environment.NewLine + item.ServerId + " " + item.Name;
            }
            return text;
        }

        public static WorldObjectEntry GetMyByServerId(long serverId)
        {
            WorldObjectEntry storeWO;
            int objId;
            if (ConverterServerId == null
                || !ConverterServerId.TryGetValue(serverId, out objId)
                || MyWorldObjectEntry == null
                || !MyWorldObjectEntry.TryGetValue(objId, out storeWO))
            {
                return null;
            }
            return storeWO;
        }

        public static CaravanOnline GetOtherByServerId(long serverId, List<WorldObject> allWorldObjects = null)
        {
            int objId;
            if (ConverterServerId == null
                || !ConverterServerId.TryGetValue(serverId, out objId))
            {
                return null;
            }
            
            if (allWorldObjects == null) allWorldObjects = Find.WorldObjects.AllWorldObjects;

            CaravanOnline worldObject = null;
            for (int i = 0; i < allWorldObjects.Count; i++)
            {
                if (allWorldObjects[i].ID == objId && allWorldObjects[i] is CaravanOnline)
                {
                    worldObject = allWorldObjects[i] as CaravanOnline;
                    break;
                }
            }
            return worldObject;
        }

        public static WorldObject GetWOByServerId(long serverId, List<WorldObject> allWorldObjects = null)
        {
            int objId;
            if (ConverterServerId == null
                || !ConverterServerId.TryGetValue(serverId, out objId))
            {
                return null;
            }

            if (allWorldObjects == null) allWorldObjects = Find.WorldObjects.AllWorldObjects;
            
            for (int i = 0; i < allWorldObjects.Count; i++)
            {
                if (allWorldObjects[i].ID == objId)
                {
                    return allWorldObjects[i];
                }
            }
            return null;
        }

        public static WorldObjectEntry GetServerInfo(WorldObject myWorldObject)
        {
            WorldObjectEntry storeWO;
            if (MyWorldObjectEntry == null
                || !MyWorldObjectEntry.TryGetValue(myWorldObject.ID, out storeWO))
            {
                return null;
            }
            return storeWO;
        }

        /// <summary>
        /// Только для своих объетков
        /// </summary>
        public static WorldObjectEntry GetWorldObjectEntry(WorldObject worldObject)
        {
            var worldObjectEntry = new WorldObjectEntry();
            worldObjectEntry.Type = worldObject is Caravan ? WorldObjectEntryType.Caravan : WorldObjectEntryType.Base;
            worldObjectEntry.Tile = worldObject.Tile;
            worldObjectEntry.Name = worldObject.LabelCap;
            worldObjectEntry.LoginOwner = SessionClientController.My.Login;
            worldObjectEntry.FreeWeight = 999999;

            //определяем цену и вес 
            var caravan = worldObject as Caravan;
            if (caravan != null)
            {
                var transferables = CalculateTransferables(caravan);

                List<ThingCount> stackParts = new List<ThingCount>();
                for (int i = 0; i < transferables.Count; i++)
                {
                    TransferableUtility.TransferNoSplit(transferables[i].things, transferables[i].MaxCount/*CountToTransfer*/, delegate (Thing originalThing, int toTake)
                    {
                        stackParts.Add(new ThingCount(originalThing, toTake));
                    }, false, false);
                }
                worldObjectEntry.FreeWeight = CollectionsMassCalculator.Capacity(stackParts)
                    - CollectionsMassCalculator.MassUsage(stackParts, IgnorePawnsInventoryMode.Ignore, false, false);

                worldObjectEntry.MarketValue = 0f;
                worldObjectEntry.MarketValuePawn = 0f;
                for (int i = 0; i < stackParts.Count; i++)
                {
                    int count = stackParts[i].Count;

                    if (count > 0)
                    {
                        Thing thing = stackParts[i].Thing;
                        if (thing is Pawn)
                        {
                            worldObjectEntry.MarketValuePawn += thing.MarketValue
                                + WealthWatcher.GetEquipmentApparelAndInventoryWealth(thing as Pawn);
                        }
                        else
                            worldObjectEntry.MarketValue += thing.MarketValue * (float)count;
                    }
                }
            }
            else if (worldObject is Settlement)
            {
                var map = (worldObject as Settlement).Map;
                if (map != null)
                {
                    worldObjectEntry.MarketValue = map.wealthWatcher.WealthTotal;

                    worldObjectEntry.MarketValuePawn = 0;
                    foreach (Pawn current in map.mapPawns.FreeColonists)
                    {
                        worldObjectEntry.MarketValuePawn += current.MarketValue;
                    }
                    //Loger.Log("Map things "+ worldObjectEntry.MarketValue + " pawns " + worldObjectEntry.MarketValuePawn);
                }
            }
            
            WorldObjectEntry storeWO;
            if (MyWorldObjectEntry.TryGetValue(worldObject.ID, out storeWO))
            {
                //если серверу приходит объект без данного ServerId, значит это наш новый объект (кроме первого запроса, т.к. не было ещё загрузки)
                worldObjectEntry.ServerId = storeWO.ServerId;
            }

            return worldObjectEntry;
        }


        #region Вычисление массы, сдернуто с Dialog_SplitCaravan

        private static List<TransferableOneWay> CalculateTransferables(Caravan caravan)
        {
            var transferables = new List<TransferableOneWay>();
            AddPawnsToTransferables(caravan, transferables);
            AddItemsToTransferables(caravan, transferables);
            return transferables;
        }

        private static void AddPawnsToTransferables(Caravan caravan, List<TransferableOneWay> transferables)
        {
            List<Pawn> pawnsListForReading = caravan.PawnsListForReading;
            for (int i = 0; i < pawnsListForReading.Count; i++)
            {
                AddToTransferables(pawnsListForReading[i], transferables);
            }
        }

        private static void AddItemsToTransferables(Caravan caravan, List<TransferableOneWay> transferables)
        {
            List<Thing> list = CaravanInventoryUtility.AllInventoryItems(caravan);
            for (int i = 0; i < list.Count; i++)
            {
                AddToTransferables(list[i], transferables);
            }
        }

        private static void AddToTransferables(Thing t, List<TransferableOneWay> transferables)
        {
            TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching<TransferableOneWay>(t, transferables, TransferAsOneMode.Normal);
            if (transferableOneWay == null)
            {
                transferableOneWay = new TransferableOneWay();
                transferables.Add(transferableOneWay);
            }
            transferableOneWay.things.Add(t);
        }
        #endregion 
        
        /// <summary>
        /// Для всех объектов с сервера, в т.ч. и для наших.
        /// Для своих объектов заполняем данные в словарь MyWorldObjectEntry
        /// </summary>
        /// <param name="worldObjectEntry"></param>
        /// <returns></returns>
        public static void ApplyWorldObject(WorldObjectEntry worldObjectEntry)
        {
            var err = "";
            try
            {
                List<WorldObject> allWorldObjects = Find.WorldObjects.AllWorldObjects;
                err += "1 ";
                if (worldObjectEntry.LoginOwner == SessionClientController.My.Login)
                {
                    //для своих нужно только занести в MyWorldObjectEntry (чтобы запомнить ServerId)
                    if (MyWorldObjectEntry.Any(wo => wo.Value.ServerId == worldObjectEntry.ServerId))
                        return;
                    err += "2 ";

                    for (int i = 0; i < allWorldObjects.Count; i++)
                    {
                        err += "3 ";
                        if (!MyWorldObjectEntry.ContainsKey(allWorldObjects[i].ID)
                            && allWorldObjects[i].Tile == worldObjectEntry.Tile
                            && (allWorldObjects[i] is Caravan && worldObjectEntry.Type == WorldObjectEntryType.Caravan
                                || allWorldObjects[i] is MapParent && worldObjectEntry.Type == WorldObjectEntryType.Base))
                        {
                            err += "4 ";
                            var id = allWorldObjects[i].ID;
                            Loger.Log("SetMyID " + id + " ServerId " + worldObjectEntry.ServerId + " " + worldObjectEntry.Name);
                            MyWorldObjectEntry.Add(id, worldObjectEntry);

                            if (!ConverterServerId.ContainsKey(worldObjectEntry.ServerId))
                                ConverterServerId.Add(worldObjectEntry.ServerId, id);
                            err += "5 ";
                            return;
                        }
                    }

                    err += "6 ";
                    Loger.Log("ToDel " + worldObjectEntry.ServerId + " " + worldObjectEntry.Name);

                    //объект нужно удалить на сервере - его нету у самого игрока (не заполняется при самом первом обновлении после загрузки)
                    if (ToDelete != null) ToDelete.Add(worldObjectEntry);
                    err += "7 ";
                    return;
                }

                //поиск уже существующих
                CaravanOnline worldObject = null;
                /*
                int existId;
                if (ConverterServerId.TryGetValue(worldObjectEntry.ServerId, out existId))
                {
                    for (int i = 0; i < allWorldObjects.Count; i++)
                    {
                        if (allWorldObjects[i].ID == existId && allWorldObjects[i] is CaravanOnline)
                        {
                            worldObject = allWorldObjects[i] as CaravanOnline;
                            break;
                        }
                    }
                }
                */
                err += "8 ";
                worldObject = GetOtherByServerId(worldObjectEntry.ServerId, allWorldObjects);

                err += "9 ";
                //если тут база другого игрока, то удаление всех кто занимает этот тайл, кроме караванов (удаление новых НПЦ и событий с занятых тайлов)
                if (worldObjectEntry.Type == WorldObjectEntryType.Base)
                {
                    err += "10 ";
                    for (int i = 0; i < allWorldObjects.Count; i++)
                    {
                        err += "11 ";
                        if (allWorldObjects[i].Tile == worldObjectEntry.Tile && allWorldObjects[i] != worldObject
                            && !(allWorldObjects[i] is Caravan) && !(allWorldObjects[i] is CaravanOnline)
                            && (allWorldObjects[i].Faction == null || !allWorldObjects[i].Faction.IsPlayer))
                        {
                            err += "12 ";
                            Loger.Log("Remove " + worldObjectEntry.ServerId + " " + worldObjectEntry.Name);
                            Find.WorldObjects.Remove(allWorldObjects[i]);
                        }
                    }
                }

                err += "13 ";
                //создание
                if (worldObject == null)
                {
                    err += "14 ";
                    worldObject = worldObjectEntry.Type == WorldObjectEntryType.Base
                        ? (CaravanOnline)WorldObjectMaker.MakeWorldObject(ModDefOf.BaseOnline)
                        : (CaravanOnline)WorldObjectMaker.MakeWorldObject(ModDefOf.CaravanOnline);
                    err += "15 ";
                    worldObject.SetFaction(Faction.OfPlayer);
                    worldObject.Tile = worldObjectEntry.Tile;
                    Find.WorldObjects.Add(worldObject);
                    err += "16 ";
                    ConverterServerId.Add(worldObjectEntry.ServerId, worldObject.ID);
                    Loger.Log("Add " + worldObjectEntry.ServerId + " " + worldObjectEntry.Name + " " + worldObjectEntry.LoginOwner);
                    err += "17 ";
                }
                else
                {
                    err += "18 ";
                    ConverterServerId[worldObjectEntry.ServerId] = worldObject.ID; //на всякий случай
                    err += "19 ";
                    Loger.Log("SetID " + worldObjectEntry.ServerId + " " + worldObjectEntry.Name);
                }
                err += "20 ";
                //обновление
                worldObject.Tile = worldObjectEntry.Tile;
                err += "21 ";
                worldObject.OnlineWObject = worldObjectEntry;
            }
            catch
            {
                Loger.Log("ApplyWorldObject ErrorLog: " + err);
                throw;
            }
        }

        public static void DeleteWorldObject(WorldObjectEntry worldObjectEntry)
        {
            List<WorldObject> allWorldObjects = Find.WorldObjects.AllWorldObjects;
            
            //поиск уже существующих
            CaravanOnline worldObject = null;
            /*
            int existId;
            if (ConverterServerId.TryGetValue(worldObjectEntry.ServerId, out existId))
            {
                for (int i = 0; i < allWorldObjects.Count; i++)
                {
                    if (allWorldObjects[i].ID == existId && allWorldObjects[i] is CaravanOnline)
                    {
                        worldObject = allWorldObjects[i] as CaravanOnline;
                        break;
                    }
                }
            }
            */
            worldObject = GetOtherByServerId(worldObjectEntry.ServerId);

            if (worldObject != null)
            {
                Loger.Log("DeleteWorldObject " + DevelopTest.TextObj(worldObjectEntry) + " " 
                    + (worldObject == null ? "null" : worldObject.ID.ToString()));
                Find.WorldObjects.Remove(worldObject);
            }
        }
        public static void InitGame()
        {
            MyWorldObjectEntry = new Dictionary<int, WorldObjectEntry>();
            ConverterServerId = new Dictionary<long, int>();
            ToDelete = null;
        }

        #endregion
    }
}
