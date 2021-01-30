using Model;
using OCUnion;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Transfer; 
using Verse;

namespace RimWorldOnlineCity
{
    static class UpdateWorldController
    {
        /// <summary>
        /// Для поиска объектов, уже созданных в прошлые разы
        /// </summary>
        private static Dictionary<long, int> ConverterServerId { get; set; }
        public static Dictionary<int, WorldObjectEntry> WorldObjectEntrys { get; private set; }
        private static List<WorldObjectEntry> ToDelete { get; set; }

        private static List<WorldObjectEntry> LastSendMyWorldObjects { get; set; }
        private static List<WorldObjectOnline> LastWorldObjectOnline { get; set; }

        public static void SendToServer(ModelPlayToServer toServ, bool firstRun, ModelGameServerInfo modelWorldObjectOnline)
        {
            toServ.LastTick = (long)Find.TickManager.TicksGame;

            var allWorldObjectsArr = new WorldObject[Find.WorldObjects.AllWorldObjects.Count];
            Find.WorldObjects.AllWorldObjects.CopyTo(allWorldObjectsArr);

            var allWorldObjects = allWorldObjectsArr.Where(wo => wo != null).ToList();

            if (SessionClientController.Data.GeneralSettings.EquableWorldObjects)
            {

                try
                {
                    // Game on init
                    if (firstRun && modelWorldObjectOnline != null)
                    {
                        if (modelWorldObjectOnline.WObjectOnlineList.Count > 0)
                        {
                            toServ.WObjectOnlineList = allWorldObjectsArr.Where(wo => wo is Settlement)
                                                                 .Where(wo => wo.HasName && !wo.Faction.IsPlayer).Select(obj => GetWorldObjects(obj)).ToList();
                        }
                        return;
                    }
                }
                catch (Exception e)
                {
                    Loger.Log("Exception >> " + e);
                    Log.Error("SendToServer FirstRun error");
                    return;
                }
            }

            if (!firstRun)
            {
                //Loger.Log("Client TestBagSD 035");
                Dictionary<Map, List<Pawn>> cacheColonists = new Dictionary<Map, List<Pawn>>();
                //отправка всех новых и измененных объектов игрока
                toServ.WObjects = allWorldObjects
                    .Where(o => o.Faction?.IsPlayer == true //o.Faction != null && o.Faction.IsPlayer
                        && (o is Settlement || o is Caravan)) //Чтобы отсеч разные карты событий
                    .Select(o => GetWorldObjectEntry(o, cacheColonists))
                    .ToList();
                LastSendMyWorldObjects = toServ.WObjects;

                //Loger.Log("Client TestBagSD 036");
                //свои объекты которые удалил пользователь с последнего обновления
                if (ToDelete != null)
                {
                    var toDeleteNewNow = WorldObjectEntrys
                        .Where(p => !allWorldObjects.Any(wo => wo.ID == p.Key))
                        .Select(p => p.Value)
                        .ToList();
                    ToDelete.AddRange(toDeleteNewNow);
                }

                toServ.WObjectsToDelete = ToDelete;
            }

            if (SessionClientController.Data.GeneralSettings.EquableWorldObjects)
            {
                //  Non-Player World Objects
                try
                {
                    var OnlineWObjArr = allWorldObjectsArr.Where(wo => wo is Settlement)
                                          .Where(wo => wo.HasName && !wo.Faction.IsPlayer);
                    if (!firstRun)
                    {
                        if (LastWorldObjectOnline != null && LastWorldObjectOnline.Count > 0)
                        {
                            toServ.WObjectOnlineToDelete = LastWorldObjectOnline.Where(WOnline => !OnlineWObjArr.Any(wo => ValidateOnlineWorldObject(WOnline, wo))).ToList();

                            toServ.WObjectOnlineToAdd = OnlineWObjArr.Where(wo => !LastWorldObjectOnline.Any(WOnline => ValidateOnlineWorldObject(WOnline, wo)))
                                                                        .Select(obj => GetWorldObjects(obj)).ToList();
                        }
                    }

                    toServ.WObjectOnlineList = OnlineWObjArr.Select(obj => GetWorldObjects(obj)).ToList();
                    LastWorldObjectOnline = toServ.WObjectOnlineList;
                }
                catch (Exception e)
                {
                    Loger.Log("Exception >> " + e);
                    Log.Error("ERROR SendToServer WorldObject Online");
                }
            }
        }

        public static void LoadFromServer(ModelPlayToClient fromServ, bool removeMissing)
        {

            /*var testF = Find.FactionManager.AllFactions.ToList();
            Loger.Log("---------------------------------------------------------------");
            foreach (var f in testF)
            {
                Loger.Log("Faction group >> " + f.Name);
                Loger.Log("Faction LabelCap >> " + f.def.LabelCap);
                Loger.Log("Faction defName >> " + f.def.defName);
                Loger.Log(" ");
            }
            Loger.Log("---------------------------------------------------------------");
            Loger.Log("FactionDef def in DefDatabase<FactionDef>.AllDefs >>>");
            foreach (FactionDef def in DefDatabase<FactionDef>.AllDefs)
            {
                Loger.Log("FactionDef isPlayer >>> " + def.isPlayer);
                Loger.Log("FactionDef LabelCap >> " + def.LabelCap);
                Loger.Log("FactionDef defName >> " + def.defName);
                Loger.Log(" ");
            }
            Loger.Log("---------------------------------------------------------------");
            */

            if (SessionClientController.Data.GeneralSettings.EquableWorldObjects)
            	ApplyNonPlayerWorldObject(fromServ);

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
            //свои поселения заполняем отдельно теми, что последний раз отправляли, но на всякий случай не первый раз
            //we fill our settlements separately with those that were last sent, but just in case not for the first time
            if (!removeMissing && SessionClientController.Data.Players.ContainsKey(SessionClientController.My.Login))
            {
                SessionClientController.Data.Players[SessionClientController.My.Login].WObjects = LastSendMyWorldObjects
                    .Select(wo => wo.Type == WorldObjectEntryType.Base
                        ? (CaravanOnline)new BaseOnline() { Tile = wo.Tile, OnlineWObject = wo,  }
                        : new CaravanOnline() { Tile = wo.Tile, OnlineWObject = wo })
                    .ToList();
                    /*
                    UpdateWorldController.WorldObjectEntrys.Values
                    .Where(wo => wo.LoginOwner == SessionClientController.My.Login)
                    .Select(wo => wo.Type == WorldObjectEntryType.Base
                        ? (CaravanOnline)new BaseOnline() { Tile = wo.Tile, OnlineWObject = wo }
                        : new CaravanOnline() { Tile = wo.Tile, OnlineWObject = wo })
                    .ToList();
                    */
                //todo test it (Нет цены своих колоний)
            }

            //пришла посылка от каравана другого игрока
            if (fromServ.Mails != null && fromServ.Mails.Count > 0)
            {
                LongEventHandler.QueueLongEvent(delegate
                //LongEventHandler.ExecuteWhenFinished(delegate
                {
                    foreach (var mail in fromServ.Mails)
                    {
                        MailController.MailArrived(mail);
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


        #region WorldObject


        public static int GetLocalIdByServerId(long serverId)
        {
            int objId;
            if (ConverterServerId == null
                || !ConverterServerId.TryGetValue(serverId, out objId))
            {
                return 0;
            }
            return objId;
        }

        public static WorldObjectEntry GetMyByServerId(long serverId)
        {
            WorldObjectEntry storeWO;
            int objId;
            if (ConverterServerId == null
                || !ConverterServerId.TryGetValue(serverId, out objId)
                || WorldObjectEntrys == null
                || !WorldObjectEntrys.TryGetValue(objId, out storeWO))
            {
                return null;
            }
            return storeWO;
        }

        public static WorldObjectEntry GetMyByLocalId(int id)
        {
            WorldObjectEntry storeWO;
            if (WorldObjectEntrys == null
                || !WorldObjectEntrys.TryGetValue(id, out storeWO))
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

        public static string GetTestText()
        {
            var text = "ConverterServerId.";
            foreach (var item in ConverterServerId)
            {
                text += Environment.NewLine + item.Key + ", " + item.Value;
            }

            text += Environment.NewLine + Environment.NewLine + "MyWorldObjectEntry.";
            foreach (var item in WorldObjectEntrys)
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

        public static WorldObjectEntry GetServerInfo(WorldObject myWorldObject)
        {
            WorldObjectEntry storeWO;
            if (WorldObjectEntrys == null
                || !WorldObjectEntrys.TryGetValue(myWorldObject.ID, out storeWO))
            {
                return null;
            }
            return storeWO;
        }

        /// <summary>
        /// Только для своих объетков
        /// </summary>
        public static WorldObjectEntry GetWorldObjectEntry(WorldObject worldObject, Dictionary<Map, List<Pawn>> cacheColonists)
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
                //Loger.Log("Client TestBagSD 002");
                var transferables = CalculateTransferables(caravan);
                //Loger.Log("Client TestBagSD 003");

                List<ThingCount> stackParts = new List<ThingCount>();
                for (int i = 0; i < transferables.Count; i++)
                {
                    TransferableUtility.TransferNoSplit(transferables[i].things, transferables[i].MaxCount/*CountToTransfer*/, delegate (Thing originalThing, int toTake)
                    {
                        stackParts.Add(new ThingCount(originalThing, toTake));
                    }, false, false);
                }
                //Loger.Log("Client TestBagSD 004");
                worldObjectEntry.FreeWeight = CollectionsMassCalculator.Capacity(stackParts)
                    - CollectionsMassCalculator.MassUsage(stackParts, IgnorePawnsInventoryMode.Ignore, false, false);
                //Loger.Log("Client TestBagSD 005");

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
                //Loger.Log("Client TestBagSD 006");
            }
            else if (worldObject is Settlement)
            {
                //Loger.Log("Client TestBagSD 007");
                var map = (worldObject as Settlement).Map;
                if (map != null)
                {
                    //Loger.Log("Client TestBagSD 008");
                    try
                    {
                        worldObjectEntry.MarketValue = map.wealthWatcher.WealthTotal;
                    }
                    catch
                    {
                        Thread.Sleep(100);
                        worldObjectEntry.MarketValue = map.wealthWatcher.WealthTotal;
                    }

                    worldObjectEntry.MarketValuePawn = 0;

                    //Loger.Log("Client TestBagSD 015");
                    List<Pawn> ps;
                    if (!cacheColonists.TryGetValue(map, out ps))
                    {
                        var mapPawnsA = new Pawn[map.mapPawns.AllPawnsSpawned.Count];
                        map.mapPawns.AllPawnsSpawned.CopyTo(mapPawnsA);

                        ps = mapPawnsA.Where(p => p.Faction == Faction.OfPlayer && p.RaceProps.Humanlike).ToList();
                        cacheColonists[map] = ps;
                    }

                    //Loger.Log("Client TestBagSD 016");
                    foreach (Pawn current in ps)
                    {
                        worldObjectEntry.MarketValuePawn += current.MarketValue;
                    }
                    //Loger.Log("Client TestBagSD 017");
                    //Loger.Log("Map things "+ worldObjectEntry.MarketValue + " pawns " + worldObjectEntry.MarketValuePawn);
                }
            }
            //Loger.Log("Client TestBagSD 018");

            WorldObjectEntry storeWO;
            if (WorldObjectEntrys.TryGetValue(worldObject.ID, out storeWO))
            {
                //если серверу приходит объект без данного ServerId, значит это наш новый объект (кроме первого запроса, т.к. не было ещё загрузки)
                worldObjectEntry.ServerId = storeWO.ServerId;
            }
            //Loger.Log("Client TestBagSD 019");

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
                    if (!WorldObjectEntrys.Any(wo => wo.Value.ServerId == worldObjectEntry.ServerId))
                    {
                        err += "2 ";

                        for (int i = 0; i < allWorldObjects.Count; i++)
                        {
                            err += "3 ";
                            if (!WorldObjectEntrys.ContainsKey(allWorldObjects[i].ID)
                                && allWorldObjects[i].Tile == worldObjectEntry.Tile
                                && (allWorldObjects[i] is Caravan && worldObjectEntry.Type == WorldObjectEntryType.Caravan
                                    || allWorldObjects[i] is MapParent && worldObjectEntry.Type == WorldObjectEntryType.Base))
                            {
                                err += "4 ";
                                var id = allWorldObjects[i].ID;
                                Loger.Log("SetMyID " + id + " ServerId " + worldObjectEntry.ServerId + " " + worldObjectEntry.Name);
                                WorldObjectEntrys.Add(id, worldObjectEntry);

                                ConverterServerId[worldObjectEntry.ServerId] = id;
                                err += "5 ";
                                return;
                            }
                        }

                        err += "6 ";
                        Loger.Log("ToDel " + worldObjectEntry.ServerId + " " + worldObjectEntry.Name);

                        //объект нужно удалить на сервере - его нету у самого игрока (не заполняется при самом первом обновлении после загрузки)
                        if (ToDelete != null) ToDelete.Add(worldObjectEntry);
                        err += "7 ";
                    }
                    else
                    {
                        //если такой есть, то обновляем информацию
                        var pair = WorldObjectEntrys.First(wo => wo.Value.ServerId == worldObjectEntry.ServerId);
                        WorldObjectEntrys[pair.Key] = worldObjectEntry;
                    }
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

        #endregion

        public static void InitGame()
        {
            WorldObjectEntrys = new Dictionary<int, WorldObjectEntry>();
            ConverterServerId = new Dictionary<long, int>();
            ToDelete = null;
        }

        #region Non-Player World Objects
        private static void ApplyNonPlayerWorldObject(ModelPlayToClient fromServ)
        {
            try
            {
                if (fromServ.WObjectOnlineToDelete != null && fromServ.WObjectOnlineToDelete.Count > 0)
                {
                    var objectToDelete = Find.WorldObjects.AllWorldObjects.Where(wo => wo is Settlement)
                                                     .Where(wo => wo.HasName && !wo.Faction.IsPlayer)
                                                     .Where(o => fromServ.WObjectOnlineToDelete.Any(fs => ValidateOnlineWorldObject(fs, o))).ToList();
                    objectToDelete.ForEach(o => Find.WorldObjects.Remove(o));
                    if (LastWorldObjectOnline != null && LastWorldObjectOnline.Count > 0)
                    {
                        LastWorldObjectOnline.RemoveAll(WOnline => objectToDelete.Any(o => ValidateOnlineWorldObject(WOnline, o)));
                    }
                }

                if (fromServ.WObjectOnlineToAdd != null && fromServ.WObjectOnlineToAdd.Count > 0)
                {
                    for (var i = 0; i < fromServ.WObjectOnlineToAdd.Count; i++)
                    {
                        if (!Find.WorldObjects.AnySettlementAt(fromServ.WObjectOnlineToAdd[i].Tile))
                        {
                            Faction faction = Find.FactionManager.AllFactions.FirstOrDefault(fm => fm.def.LabelCap == fromServ.WObjectOnlineToAdd[i].FactionGroup);
                            if (faction != null)
                            {
                                Loger.Log("fromServ.WObjectOnlineToAdd[i].Name >> " + fromServ.WObjectOnlineToAdd[i].Name);
                                Loger.Log("fromServ.WObjectOnlineToAdd[i].FactionDef >> " + fromServ.WObjectOnlineToAdd[i].FactionDef);
                                Loger.Log("fromServ.WObjectOnlineToAdd[i].FactionGroup >> " + fromServ.WObjectOnlineToAdd[i].FactionGroup);
                                var npcBase = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                                npcBase.SetFaction(faction);
                                npcBase.Tile = fromServ.WObjectOnlineToAdd[i].Tile;
                                npcBase.Name = fromServ.WObjectOnlineToAdd[i].Name;
                                Find.WorldObjects.Add(npcBase);
                                //LastWorldObjectOnline.Add(fromServ.OnlineWObjectToAdd[i]);
                            }
                            else
                            {
                                Loger.Log("Faction is missing or not found : " + fromServ.WObjectOnlineToAdd[i].FactionGroup);
                                Loger.Log("Skipping ToAdd Settlement : " + fromServ.WObjectOnlineToAdd[i].Name);
                            }

                        }
                        else
                        {
                            Loger.Log("Can't Add Settlement. Tile is already occupied " + Find.WorldObjects.SettlementAt(fromServ.WObjectOnlineToAdd[i].Tile));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Exception LoadFromServer >> " + e);
            }
        }

        public static WorldObjectOnline GetWorldObjects(WorldObject obj)
        {
            var worldObject = new WorldObjectOnline();
            worldObject.Name = obj.LabelCap;
            worldObject.Tile = obj.Tile;
            worldObject.FactionGroup = obj?.Faction?.def?.LabelCap;
            worldObject.FactionDef = obj?.Faction?.def?.defName;
            return worldObject;
        }

        private static bool ValidateOnlineWorldObject(WorldObjectOnline WObjectOnline1, WorldObject WObjectOnline2)
        {
            if (WObjectOnline1.Name == WObjectOnline2.LabelCap
                && WObjectOnline1.Tile == WObjectOnline2.Tile)
            {
                return true;
            }
            return false;
        }
        #endregion

        #region
        public static void HandleWObjectFaction()
        {
            //! WIP Handling of World Objects faction missing in game client
        }
        #endregion
    }
}
