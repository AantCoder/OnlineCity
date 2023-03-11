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
using GameClasses;
using OCUnion.Transfer.Model;
using UnityEngine;
using Random = System.Random;
using RimWorldOnlineCity.GameClasses.Harmony;

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
        /// <summary>
        /// Список объектов TradeOrdersOnline для быстрого доступа (можно получить и из Find.WorldObjects)
        /// </summary>
        public static HashSet<TradeOrdersOnline> WorldObject_TradeOrdersOnline { get; set; }

        private static List<WorldObjectEntry> LastSendMyWorldObjects { get; set; }
        private static List<WorldObjectOnline> LastWorldObjectOnline { get; set; }
        private static List<FactionOnline> LastFactionOnline { get; set; }

        private static Dictionary<int, WorldObjectBaseOnline> LastCatchAllWorldObjectsByID;

        #region PrepareInMainThread

        public static List<WorldObject> allWorldObjects;
        public static List<WorldObjectEntry> WObjects;
        public static PlayerGameProgress gameProgress;
        public static void PrepareInMainThread()
        {
            try
            {
                //DateTime debugtime = DateTime.UtcNow;
                gameProgress = new PlayerGameProgress() { Pawns = new List<PawnStat>() };

                allWorldObjects = GameUtils.GetAllWorldObjects();
                Dictionary<Map, List<Pawn>> cacheColonists = new Dictionary<Map, List<Pawn>>();
                Dictionary<WorldObjectEntry, Map> tmpMap = new Dictionary<WorldObjectEntry, Map>();
                WObjects = allWorldObjects
                        .Where(o => (o.Faction?.IsPlayer ?? false) //o.Faction != null && o.Faction.IsPlayer
                            && (o is Settlement || o is Caravan)) //Чтобы отсеч разные карты событий
                        .Select(o =>
                        {
                            var oo = GetWorldObjectEntry(o, gameProgress, cacheColonists);
                            if (o is MapParent) tmpMap.Add(oo, ((MapParent)o).Map);
                            return oo;
                        })
                        .ToList();

                //обновляем информацию по своим поселениям
                //безналичные средства, которые принадлежит игроку в целом раскидываем порпоционально стоимости его объектов
                var totalMarketValue = WObjects.Sum(wo => wo.MarketValue + wo.MarketValuePawn);
                if (totalMarketValue > 0)
                {
                    var cashlessBalance = Math.Abs(SessionClientController.Data.CashlessBalance);
                    var storageBalance = Math.Abs(SessionClientController.Data.StorageBalance);
                    foreach (var wo in WObjects)
                    {
                        wo.MarketValueBalance = cashlessBalance * (wo.MarketValue + wo.MarketValuePawn) / totalMarketValue;
                        wo.MarketValueStorage = storageBalance * (wo.MarketValue + wo.MarketValuePawn) / totalMarketValue;
                    }
                }
                else
                {
                    foreach (var wo in WObjects)
                    {
                        wo.MarketValueBalance = 0;
                        wo.MarketValueStorage = 0;
                    }
                }

                //устанавливаем доп стоимость в карты
                MainTabWindow_DoStatisticsPage_Patch.PatchColonyWealth = WObjects
                    .Where(o => o.Type == WorldObjectEntryType.Base)
                    .ToDictionary(o => tmpMap[o], o => (o.MarketValueBalance + o.MarketValueStorage) * (float)SessionClientController.Data.GeneralSettings.ExchengePrecentWealthForIncident / 1000f);

                //Loger.Log("PrepareInMainThread " + ModBaseData.GlobalData.ActionNumReady + " debugtime " + (DateTime.UtcNow - debugtime).TotalMilliseconds); 
            }
            catch (Exception ex)
            {
                Loger.Log("Exception PrepareInMainThread " + ex.ToString());
            }
        }
        #endregion PrepareInMainThread

        public static void SendToServer(ModelPlayToServer toServ, bool firstRun, ModelGameServerInfo modelGameServerInfo)
        {
            //Перед запуском должна отработать PrepareInMainThread()

            //Loger.Log("Empire=" + Find.FactionManager.FirstFactionOfDef(FactionDefOf.Empire)?.GetUniqueLoadID());

            toServ.LastTick = (long)Find.TickManager.TicksGame;

            //var allWorldObjectsArr = new WorldObject[Find.WorldObjects.AllWorldObjects.Count];
            //Find.WorldObjects.AllWorldObjects.CopyTo(allWorldObjectsArr);

            //List<WorldObject> allWorldObjects = allWorldObjectsArr.Where(wo => wo != null).ToList();
            List<Faction> factionList = Find.FactionManager.AllFactionsListForReading;

            if (SessionClientController.Data.GeneralSettings.EquableWorldObjects)
            {
                #region Send to Server: firstRun EquableWorldObjects
                try
                {
                    // Game on init
                    if (firstRun && modelGameServerInfo != null)
                    {
                        if (modelGameServerInfo.WObjectOnlineList.Count > 0)
                        {
                            toServ.WObjectOnlineList = allWorldObjects.Where(wo => wo is Settlement)
                                                                 .Where(wo => wo.HasName && !wo.Faction.IsPlayer).Select(obj => GetWorldObjects(obj)).ToList();
                        }

                        if(modelGameServerInfo.FactionOnlineList.Count > 0)
                        {
                            List <Faction> factions = Find.FactionManager.AllFactionsListForReading;
                            toServ.FactionOnlineList = factions.Select(obj => GetFactions(obj)).ToList();
                        }
                        return;
                    }
                }
                catch (Exception e)
                {
                    Loger.Log("Exception >> " + e, Loger.LogLevel.ERROR);
                    Log.Error("SendToServer FirstRun error");
                    return;
                }
                #endregion
            }

            if (!firstRun)
            {
                //Loger.Log("Client TestBagSD 035");
                //отправка всех новых и измененных объектов игрока
                toServ.WObjects = WObjects;
                //Dictionary<Map, List<Pawn>> cacheColonists = new Dictionary<Map, List<Pawn>>();
                //toServ.WObjects = allWorldObjects
                //    .Where(o => (o.Faction?.IsPlayer ?? false) //o.Faction != null && o.Faction.IsPlayer
                //        && (o is Settlement || o is Caravan)) //Чтобы отсеч разные карты событий
                //    .Select(o => GetWorldObjectEntry(o, gameProgress, cacheColonists))
                //    .ToList();
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
            gameProgress.TransLog = Loger.GetTransLog();
            toServ.GameProgress = gameProgress;

            if (SessionClientController.Data.GeneralSettings.EquableWorldObjects)
            {
                #region Send to Server: Non-Player World Objects
                //  Non-Player World Objects
                try
                {
                    var OnlineWObjArr = allWorldObjects.Where(wo => wo is Settlement)
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
                #endregion

                #region Send to Server: Non-Player Factions
                // Non-Player Factions
                try
                {
                    if (!firstRun)
                    {
                        if (LastFactionOnline != null && LastFactionOnline.Count > 0)
                        {
                            toServ.FactionOnlineToDelete = LastFactionOnline.Where(FOnline => !factionList.Any(f => ValidateFaction(FOnline, f))).ToList();

                            toServ.FactionOnlineToAdd = factionList.Where(f => !LastFactionOnline.Any(FOnline => ValidateFaction(FOnline, f)))
                                                                        .Select(obj => GetFactions(obj)).ToList();
                        }
                    }

                    toServ.FactionOnlineList = factionList.Select(obj => GetFactions(obj)).ToList();
                    LastFactionOnline = toServ.FactionOnlineList;
                }
                catch (Exception e)
                {
                    Loger.Log("Exception >> " + e);
                    Log.Error("ERROR SendToServer Faction Online");
                }
                #endregion
            }
        }

        public static void LoadFromServer(ModelPlayToClient fromServ, bool removeMissing)
        {
            if (SessionClientController.Data.GeneralSettings.EquableWorldObjects)
            {
                ApplyFactionsToWorld(fromServ);
                // ---------------------------------------------------------------------------------- // 
                ApplyNonPlayerWorldObject(fromServ);
            }

            if (removeMissing)
            {
                //запускается только при первом получении данных от сервера после загрузки или создания карты
                //удаляем все объекты других игроков (на всякий случай, т.к. в сейв они не сохраняются)

                var missingWObjects = Find.WorldObjects.AllWorldObjects
                    .Where(o => o is CaravanOnline || o is WorldObjectBaseOnline)
                    .ToList();
                for (int i = 0; i < missingWObjects.Count; i++)
                {
                    Find.WorldObjects.Remove(missingWObjects[i]);
                }
                Loger.Log("RemoveMissing " + missingWObjects.Count);
            }

            //обновление всех объектов
            ToDelete = new List<WorldObjectEntry>();
            List<WorldObject> catchAllWorldObjects = Find.WorldObjects.AllWorldObjects.ToList();
            Dictionary<int, WorldObjectBaseOnline> catchAllWorldObjectsByID = catchAllWorldObjects
                .Select(wo => wo as WorldObjectBaseOnline)
                .Where(wo => (wo?.ID ?? 0) != 0)
                .ToDictionary(wo => wo.ID);
            if (fromServ.WObjects != null && fromServ.WObjects.Count > 0)
            {
                for (int i = 0; i < fromServ.WObjects.Count; i++)
                    ApplyWorldObject(fromServ.WObjects[i], ref catchAllWorldObjects, ref catchAllWorldObjectsByID);
            }
            if (fromServ.WObjectsToDelete != null && fromServ.WObjectsToDelete.Count > 0)
            {
                for (int i = 0; i < fromServ.WObjectsToDelete.Count; i++)
                    DeleteWorldObject(fromServ.WObjectsToDelete[i], ref catchAllWorldObjects, ref catchAllWorldObjectsByID);
            }
            
            if (fromServ.WTObjects != null && fromServ.WTObjects.Count > 0)
            {
                for (int i = 0; i < fromServ.WTObjects.Count; i++)
                    ApplyTradeWorldObject(fromServ.WTObjects[i], ref catchAllWorldObjects, ref catchAllWorldObjectsByID);
            }
            if (fromServ.WTObjectsToDelete != null && fromServ.WTObjectsToDelete.Count > 0)
            {
                for (int i = 0; i < fromServ.WTObjectsToDelete.Count; i++)
                    DeleteTradeWorldObject(fromServ.WTObjectsToDelete[i], ref catchAllWorldObjects, ref catchAllWorldObjectsByID);
            }
            LastCatchAllWorldObjectsByID = catchAllWorldObjectsByID;

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
        
        public static WorldObjectBaseOnline GetOtherByServerIdDirtyRead(long serverId) => 
            GetOtherByServerId(serverId, LastCatchAllWorldObjectsByID);
        public static WorldObjectBaseOnline GetOtherByServerId(long serverId
            , Dictionary<int, WorldObjectBaseOnline> allWorldObjectsByID = null
            )
        {
            int objId;
            if (ConverterServerId == null
                || !ConverterServerId.TryGetValue(serverId, out objId))
            {
                return null;
            }

            WorldObjectBaseOnline worldObject = null;

            if (allWorldObjectsByID == null)
            {
                var allWorldObjects = Find.WorldObjects.AllWorldObjects;

                for (int i = 0; i < allWorldObjects.Count; i++)
                {
                    if (allWorldObjects[i].ID == objId && allWorldObjects[i] is WorldObjectBaseOnline)
                    {
                        worldObject = allWorldObjects[i] as WorldObjectBaseOnline;
                        break;
                    }
                }
                return worldObject;
            }
            else
                return allWorldObjectsByID.TryGetValue(objId, out worldObject) ? worldObject : null;
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
                text += Environment.NewLine + item.Key + ", " + item.Value.PlaceServerId + " " + item.Value.Name;
            }

            text += Environment.NewLine + Environment.NewLine + "ToDelete.";
            foreach (var item in ToDelete)
            {
                text += Environment.NewLine + item.PlaceServerId + " " + item.Name;
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

        private static void GameProgressAdd(PlayerGameProgress gameProgress, Pawn pawn)
        {
            if (pawn.Dead) return;
            if (pawn.IsFreeColonist && !pawn.IsPrisoner && !pawn.IsPrisonerOfColony && pawn.RaceProps.Humanlike)
            {
                gameProgress.ColonistsCount++;
                gameProgress.Pawns.Add(PawnStat.CreateTrade(pawn));

                if (pawn.Downed) gameProgress.ColonistsDownCount++;
                if (pawn.health.hediffSet.BleedRateTotal > 0) gameProgress.ColonistsBleedCount++;
                //pawn.health.hediffset.pain_total    // уровень боли

                int maxSkill = 0;
                for (int i = 0; i < pawn.skills.skills.Count; i++)
                {
                    if (pawn.skills.skills[i].Level == 20) maxSkill++;
                }
                if (maxSkill >= 8) gameProgress.PawnMaxSkill++;

                var kh = pawn.records.GetAsInt(RecordDefOf.KillsHumanlikes);
                var km = pawn.records.GetAsInt(RecordDefOf.KillsMechanoids);

                gameProgress.KillsHumanlikes += kh;
                gameProgress.KillsMechanoids += km;
                if (gameProgress.KillsBestHumanlikesPawnName == null
                    || kh > gameProgress.KillsBestHumanlikes)
                {
                    gameProgress.KillsBestHumanlikesPawnName = pawn.LabelCapNoCount;
                    gameProgress.KillsBestHumanlikes = kh;
                }
                if (gameProgress.KillsBestMechanoidsPawnName == null
                    || km > gameProgress.KillsBestMechanoids)
                {
                    gameProgress.KillsBestMechanoidsPawnName = pawn.LabelCapNoCount;
                    gameProgress.KillsBestMechanoids = km;
                }
            }
        }

        public static Dictionary<int, DateTime> LastForceRecount = new Dictionary<int, DateTime>();
        /// <summary>
        /// Только для своих объетков
        /// </summary>
        public static WorldObjectEntry GetWorldObjectEntry(WorldObject worldObject
            , PlayerGameProgress gameProgress
            , Dictionary<Map, List<Pawn>> cacheColonists)
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

                var transferables = GameUtils.GetAllThings(caravan, true, false).DistinctToTransferableOneWays();

                //Loger.Log("Client TestBagSD 003");

                List<ThingCount> stackParts = new List<ThingCount>();
                for (int i = 0; i < transferables.Count; i++)
                {
                    var allCount = transferables[i].MaxCount;
                    for (int ti = 0; ti < transferables[i].things.Count; ti++)
                    {
                        int cnt = Mathf.Min(transferables[i].things[ti].stackCount, allCount);
                        allCount -= cnt;

                        stackParts.Add(new ThingCount(transferables[i].things[ti], cnt));

                        if (allCount <= 0) break;
                    }
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
                            worldObjectEntry.MarketValuePawn += thing.MarketValue;
                                //убрано из-за того, что эти вещи должны уже получаться здесь: GameUtils.GetAllThings(caravan, *true*, false)
                                //+ WealthWatcher.GetEquipmentApparelAndInventoryWealth(thing as Pawn);
                            GameProgressAdd(gameProgress, thing as Pawn);
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
                        DateTime lastForceRecount;
                        if (!LastForceRecount.TryGetValue(map.uniqueID, out lastForceRecount))
                            LastForceRecount.Add(map.uniqueID, DateTime.UtcNow.AddSeconds(new Random(map.uniqueID * 7).Next(0, 10)));
                        else if ((DateTime.UtcNow - lastForceRecount).TotalSeconds> 30)
                        {
                            LastForceRecount[map.uniqueID] = DateTime.UtcNow;
                            ModBaseData.RunMainThread(() =>
                            {
                                map.wealthWatcher.ForceRecount();
                            });
                        }
                        worldObjectEntry.MarketValue = map.wealthWatcher.WealthTotal;
                    }
                    catch
                    {
                        Thread.Sleep(100);
                        try
                        {
                            worldObjectEntry.MarketValue = map.wealthWatcher.WealthTotal;
                        }
                        catch
                        {
                        }
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

                        GameProgressAdd(gameProgress, current);
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
                worldObjectEntry.PlaceServerId = storeWO.PlaceServerId;
            }
            //Loger.Log("Client TestBagSD 019");

            return worldObjectEntry;
        }


        /// <summary>
        /// Для всех объектов с сервера, в т.ч. и для наших.
        /// Для своих объектов заполняем данные в словарь MyWorldObjectEntry
        /// </summary>
        /// <param name="worldObjectEntry"></param>
        /// <returns></returns>
        public static void ApplyWorldObject(WorldObjectEntry worldObjectEntry
            , ref List<WorldObject> allWorldObjects
            , ref Dictionary<int, WorldObjectBaseOnline> allWorldObjectsByID)
        {
            var err = "";
            try
            {
                err += "1 ";
                if (worldObjectEntry.LoginOwner == SessionClientController.My.Login)
                {
                    //для своих нужно только занести в MyWorldObjectEntry (чтобы запомнить ServerId)
                    if (!WorldObjectEntrys.Any(wo => wo.Value.PlaceServerId == worldObjectEntry.PlaceServerId))
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
                                Loger.Log("SetMyID " + id + " ServerId " + worldObjectEntry.PlaceServerId + " " + worldObjectEntry.Name);
                                WorldObjectEntrys.Add(id, worldObjectEntry);

                                ConverterServerId[worldObjectEntry.PlaceServerId] = id;
                                err += "5 ";
                                return;
                            }
                        }

                        err += "6 ";
                        Loger.Log("ToDel " + worldObjectEntry.PlaceServerId + " " + worldObjectEntry.Name);

                        //объект нужно удалить на сервере - его нету у самого игрока (не заполняется при самом первом обновлении после загрузки)
                        if (ToDelete != null) ToDelete.Add(worldObjectEntry);
                        err += "7 ";
                    }
                    else
                    {
                        //если такой есть, то обновляем информацию
                        var pair = WorldObjectEntrys.First(wo => wo.Value.PlaceServerId == worldObjectEntry.PlaceServerId);
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
                worldObject = GetOtherByServerId(worldObjectEntry.PlaceServerId, allWorldObjectsByID) as CaravanOnline;

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
                            Loger.Log("Remove " + worldObjectEntry.PlaceServerId + " " + worldObjectEntry.Name);
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
                    //DrawTerritory(worldObject.Tile);
                    Find.WorldObjects.Add(worldObject);
                    err += "16 ";
                    ConverterServerId.Add(worldObjectEntry.PlaceServerId, worldObject.ID);
                    allWorldObjectsByID.Add(worldObject.ID, worldObject);
                    allWorldObjects.Add(worldObject);
                    Loger.Log("Add " + worldObjectEntry.PlaceServerId + " " + worldObjectEntry.Name + " " + worldObjectEntry.LoginOwner);
                    err += "17 ";
                }
                else
                {
                    err += "18 ";
                    ConverterServerId[worldObjectEntry.PlaceServerId] = worldObject.ID; //на всякий случай
                    err += "19 ";
                    //Loger.Log("SetID " + worldObjectEntry.ServerId + " " + worldObjectEntry.Name);
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

        public static void DrawTerritory(int centralTile)
        {
            List<int> neighbors = new List<int>(0);
            Find.WorldGrid.GetTileNeighbors(centralTile, neighbors);
            foreach (var tile in neighbors)
            {
                Find.WorldDebugDrawer.FlashTile(tile, WorldMaterials.DebugTileRenderQueue, null, 999999);
            }
            Find.WorldDebugDrawer.FlashTile(centralTile, WorldMaterials.DebugTileRenderQueue, null, 999999);
        }
        public static void DeleteWorldObject(WorldObjectEntry worldObjectEntry
            , ref List<WorldObject> allWorldObjects
            , ref Dictionary<int, WorldObjectBaseOnline> allWorldObjectsByID
            )
        {
            
            //поиск уже существующих
            CaravanOnline worldObject = null;
            worldObject = GetOtherByServerId(worldObjectEntry.PlaceServerId, allWorldObjectsByID) as CaravanOnline;

            if (worldObject != null)
            {
                //Loger.Log("DeleteWorldObject " + DevelopTest.TextObj(worldObjectEntry) + " " 
                //    + (worldObject == null ? "null" : worldObject.ID.ToString()));
                allWorldObjectsByID.Remove(worldObject.ID);
                allWorldObjects.Remove(worldObject);
                ConverterServerId.Remove(worldObjectEntry.PlaceServerId);
                Find.WorldObjects.Remove(worldObject);
            }
        }

        public static void ApplyTradeWorldObject(TradeWorldObjectEntry worldObjectEntry
            , ref List<WorldObject> allWorldObjects
            , ref Dictionary<int, WorldObjectBaseOnline> allWorldObjectsByID
            )
        {
            var err = "";
            try
            {
                err += "1 ";

                //поиск уже существующих
                WorldObjectBaseOnline worldObject = null;
                worldObject = GetOtherByServerId(worldObjectEntry.PlaceServerId, allWorldObjectsByID) as WorldObjectBaseOnline;

                err += "2 ";
                //создание
                if (worldObject == null)
                {
                    err += "3 ";
                    if (worldObjectEntry.Type == TradeWorldObjectEntryType.TradeOrder)
                    {
                        worldObject = (WorldObjectBaseOnline)WorldObjectMaker.MakeWorldObject(ModDefOf.TradeOrdersOnline);
                        //передаем только заголовок, TradeOrders нужно обновить до TradeOrder отдельными запросами
                        ((TradeOrdersOnline)worldObject).TradeOrders = new List<TradeOrderShort>() { (TradeOrderShort)worldObjectEntry };
                        WorldObject_TradeOrdersOnline.Add((TradeOrdersOnline)worldObject);
                        if (MainHelper.DebugMode) Loger.Log($"Client WorldObject_TradeOrdersOnline.Add Tile={worldObject.Tile} load={worldObjectEntry.Tile}");
                    }
                    else
                    {
                        worldObject = (WorldObjectBaseOnline)WorldObjectMaker.MakeWorldObject(ModDefOf.TradeThingsOnline);
                        ((TradeThingsOnline)worldObject).TradeThings = (TradeThingStorage)worldObjectEntry;
                    }
                    err += "4 ";
                    worldObject.SetFaction(Faction.OfPlayer);
                    worldObject.Tile = worldObjectEntry.Tile;
                    Find.WorldObjects.Add(worldObject);
                    if (MainHelper.DebugMode) Loger.Log($"Client WorldObject_TradeOrdersOnline Set0 Tile={worldObject.Tile} load={worldObjectEntry.Tile}");
                    err += "5 ";
                    ConverterServerId.Add(worldObjectEntry.PlaceServerId, worldObject.ID);
                    allWorldObjectsByID.Add(worldObject.ID, worldObject);
                    allWorldObjects.Add(worldObject);
                    Loger.Log("Add " + (worldObjectEntry.Type == TradeWorldObjectEntryType.TradeOrder ? "TradeOrderShort greenApp " : "TradeThingStorage redApp")
                        + worldObjectEntry.PlaceServerId + " " + worldObjectEntry.Name + " " + worldObjectEntry.LoginOwner);
                    err += "6 ";
                }
                else
                {
                    err += "7 ";
                    ConverterServerId[worldObjectEntry.PlaceServerId] = worldObject.ID; //на всякий случай
                    if (worldObjectEntry.Type == TradeWorldObjectEntryType.TradeOrder)
                    {
                        var worldObjectTO = worldObject as TradeOrdersOnline;
                        err += "8 ";
                        //передаем только заголовок, TradeOrders нужно обновить до TradeOrder отдельными запросами
                        int i = 0;
                        for (; i < worldObjectTO.TradeOrders.Count; i++)
                        {
                            if (worldObjectTO.TradeOrders[i].Id == worldObjectEntry.Id)
                            {
                                worldObjectTO.TradeOrders[i] = (TradeOrderShort)worldObjectEntry;
                                break;
                            }
                        }
                        if (i == worldObjectTO.TradeOrders.Count)
                            worldObjectTO.TradeOrders.Add((TradeOrderShort)worldObjectEntry);
                    }
                    else
                    {
                        err += "9 ";
                        ((TradeThingsOnline)worldObject).TradeThings = (TradeThingStorage)worldObjectEntry;
                    }
                    //Loger.Log("SetID " + worldObjectEntry.ServerId + " " + worldObjectEntry.Name);
                }
                err += "10 ";
                //обновление
                worldObject.Tile = worldObjectEntry.Tile;
                if (MainHelper.DebugMode) Loger.Log($"Client WorldObject_TradeOrdersOnline Set Tile={worldObject.Tile} load={worldObjectEntry.Tile}");
                err += "11 ";
            }
            catch
            {
                Loger.Log("ApplyTradeWorldObject ErrorLog: " + err, Loger.LogLevel.ERROR);
                throw;
            }
        }

        public static void DeleteTradeWorldObject(TradeWorldObjectEntry worldObjectEntry
            , ref List<WorldObject> allWorldObjects
            , ref Dictionary<int, WorldObjectBaseOnline> allWorldObjectsByID
            )
        {
            //поиск уже существующих
            var worldObject = GetOtherByServerId(worldObjectEntry.PlaceServerId, allWorldObjectsByID) as WorldObjectBaseOnline;

            if (worldObject != null)
            {
                //удаляем ордер из списка этого места
                if (worldObjectEntry.Type == TradeWorldObjectEntryType.TradeOrder)
                {
                    var worldObjectTO = worldObject as TradeOrdersOnline;
                    for (int i = 0; i < worldObjectTO.TradeOrders.Count; i++)
                        if (worldObjectTO.TradeOrders[i].Id == worldObjectEntry.Id)
                        {
                            worldObjectTO.TradeOrders.RemoveAt(i--);
                            break;
                        }
                    if (worldObjectTO.TradeOrders.Count == 0)
                    {
                        allWorldObjectsByID.Remove(worldObject.ID);
                        allWorldObjects.Remove(worldObject);
                        ConverterServerId.Remove(worldObjectEntry.PlaceServerId);
                        Find.WorldObjects.Remove(worldObject);
                        WorldObject_TradeOrdersOnline.Remove(worldObjectTO);
                    };
                }
                else
                {
                    allWorldObjectsByID.Remove(worldObject.ID);
                    allWorldObjects.Remove(worldObject);
                    ConverterServerId.Remove(worldObjectEntry.PlaceServerId);
                    Find.WorldObjects.Remove(worldObject);
                }
                //Loger.Log("DeleteWorldObject " + DevelopTest.TextObj(worldObjectEntry) + " " 
                //    + (worldObject == null ? "null" : worldObject.ID.ToString()));
            }
        }

        #endregion

        public static void InitGame()
        {
            WorldObjectEntrys = new Dictionary<int, WorldObjectEntry>();
            ConverterServerId = new Dictionary<long, int>();
            WorldObject_TradeOrdersOnline = new HashSet<TradeOrdersOnline>();
            ToDelete = null;
            LastCatchAllWorldObjectsByID = null;
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
                    objectToDelete.ForEach(o => {
                        Find.WorldObjects.SettlementAt(o.Tile).Destroy();
                        Find.World.WorldUpdate();
                    });
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
                            Faction faction = Find.FactionManager.AllFactionsListForReading.FirstOrDefault(fm => 
                            fm.def.LabelCap == fromServ.WObjectOnlineToAdd[i].FactionGroup &&
                            fm.loadID == fromServ.WObjectOnlineToAdd[i].loadID);
                            if (faction != null)
                            {
                                var npcBase = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                                npcBase.SetFaction(faction);
                                npcBase.Tile = fromServ.WObjectOnlineToAdd[i].Tile;
                                npcBase.Name = fromServ.WObjectOnlineToAdd[i].Name;
                                Find.WorldObjects.Add(npcBase);
                                //LastWorldObjectOnline.Add(fromServ.OnlineWObjectToAdd[i]);
                            }
                            else
                            {
                                Log.Warning("Faction is missing or not found : " + fromServ.WObjectOnlineToAdd[i].FactionGroup);
                                Loger.Log("Skipping ToAdd Settlement : " + fromServ.WObjectOnlineToAdd[i].Name);
                            }

                        }
                        else
                        {
                            Loger.Log("Can't Add Settlement. Tile is already occupied " + Find.WorldObjects.SettlementAt(fromServ.WObjectOnlineToAdd[i].Tile), Loger.LogLevel.WARNING);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Exception LoadFromServer ApplyNonPlayerWorldObject >> " + e);
            }
        }

        public static WorldObjectOnline GetWorldObjects(WorldObject obj)
        {
            var worldObject = new WorldObjectOnline();
            worldObject.Name = obj.LabelCap;
            worldObject.Tile = obj.Tile;
            worldObject.FactionGroup = obj?.Faction?.def?.LabelCap;
            worldObject.FactionDef = obj?.Faction?.def?.defName;
            worldObject.loadID = obj.Faction.loadID;
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

        #region Factions
        private static void ApplyFactionsToWorld(ModelPlayToClient fromServ)
        {
            try
            {
                // ! WIP Factions
                if (fromServ.FactionOnlineToDelete != null && fromServ.FactionOnlineToDelete.Count > 0)
                {
                    var factionToDelete = Find.FactionManager.AllFactionsListForReading.Where(f => !f.IsPlayer)
                        .Where(obj => fromServ.FactionOnlineToDelete.Any(fs => ValidateFaction(fs, obj))).ToList();

                    OCFactionManager.UpdateFactionIDS(fromServ.FactionOnlineList);
                    for (var i = 0; i < factionToDelete.Count; i++)
                    {
                        OCFactionManager.DeleteFaction(factionToDelete[i]);
                    }

                    if (LastFactionOnline != null && LastFactionOnline.Count > 0)
                    {
                        LastFactionOnline.RemoveAll(FOnline => factionToDelete.Any(obj => ValidateFaction(FOnline, obj)));
                    }
                }

                if (fromServ.FactionOnlineToAdd != null && fromServ.FactionOnlineToAdd.Count > 0)
                {
                    for (var i = 0; i < fromServ.FactionOnlineToAdd.Count; i++)
                    {
                        try
                        {
                            var existingFaction = Find.FactionManager.AllFactionsListForReading.Where(f => ValidateFaction(fromServ.FactionOnlineToAdd[i], f)).ToList();
                            if (existingFaction.Count == 0)
                            {
                                OCFactionManager.UpdateFactionIDS(fromServ.FactionOnlineList);
                                OCFactionManager.AddNewFaction(fromServ.FactionOnlineToAdd[i]);
                            }
                            else
                            {
                                Loger.Log("Failed to add faction. Faction already exists. > " + fromServ.FactionOnlineToAdd[i].LabelCap, Loger.LogLevel.ERROR);
                            }

                        }
                        catch
                        {
                            Loger.Log("Error faction to add LabelCap >> " + fromServ.FactionOnlineToAdd[i].LabelCap, Loger.LogLevel.ERROR);
                            Loger.Log("Error faction to add DefName >> " + fromServ.FactionOnlineToAdd[i].DefName, Loger.LogLevel.ERROR);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("OnlineCity: Error Apply new faction to world >> " + e);
            }
        }

        public static FactionOnline GetFactions(Faction obj)
        {
            var faction = new FactionOnline();
            faction.Name = obj.Name;
            faction.LabelCap = obj.def.LabelCap;
            faction.DefName = obj.def.defName;
            faction.loadID = obj.loadID;
            return faction;
        }

        private static bool ValidateFaction(FactionOnline fOnline1, Faction fOnline2)
        {
            if (fOnline1.LabelCap == fOnline2.def.LabelCap &&
                fOnline1.DefName == fOnline2.def.defName && 
                fOnline1.loadID == fOnline2.loadID)
            {
                return true;
            }
            return false;
        }
        #endregion
    }
}
