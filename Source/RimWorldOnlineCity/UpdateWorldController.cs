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
                var toDeleteNewNow = WorldObjectEntrys
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
            MyWorldObjectEntrys = new List<WorldObjectEntry>();
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

        /// <summary>
        /// Для поиска объектов, уже созданных в прошлые разы
        /// </summary>
        private static Dictionary<long, int> ConverterServerId { get; set; }
        private static Dictionary<int, WorldObjectEntry> WorldObjectEntrys { get; set; }
        private static List<WorldObjectEntry> ToDelete { get; set; }
        public static List<WorldObjectEntry> MyWorldObjectEntrys { get; private set; }

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
            if (WorldObjectEntrys.TryGetValue(worldObject.ID, out storeWO))
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
                    MyWorldObjectEntrys.Add(worldObjectEntry);
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
        public static void InitGame()
        {
            WorldObjectEntrys = new Dictionary<int, WorldObjectEntry>();
            ConverterServerId = new Dictionary<long, int>();
            ToDelete = null;
        }

        #endregion
    }
}
