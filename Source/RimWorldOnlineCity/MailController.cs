using Model;
using OCUnion;
using RimWorld;
using RimWorld.Planet;
using RimWorldOnlineCity.GameClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Transfer;
using Verse;

namespace RimWorldOnlineCity
{
    static class MailController
    {
        private static Dictionary<ModelMailTradeType, Action<ModelMailTrade>> TypeMailProcessing = new Dictionary<ModelMailTradeType, Action<ModelMailTrade>>()
        {
            { ModelMailTradeType.CreateThings, MailProcessCreateThings},
            { ModelMailTradeType.DeleteByServerId, MailProcessDeleteByServerId},
            { ModelMailTradeType.AttackCancel, MailProcessAttackCancel},
            { ModelMailTradeType.AttackTechnicalVictory, MailProcessAttackTechnicalVictory},
        };

        public static void MailArrived(ModelMailTrade mail)
        {
            try
            {
                if (mail.To == null
                    || mail.To.Login != SessionClientController.My.Login) return;

                Action<ModelMailTrade> action;
                if (TypeMailProcessing.TryGetValue(mail.Type, out action))
                {
                    action(mail);
                }
                else
                {
                    Loger.Log("Mail fail: error type " + mail.Type);
                }
            }
            catch(Exception e)
            {
                Loger.Log("Mail Exception: " + e.ToString());
            }
        }

        private static WorldObject GetPlace(ModelMailTrade mail, bool softSettlement = true, bool softNewCaravan = false)
        {
            if (mail.PlaceServerId <= 0)
            {
                Loger.Log("Mail fail: no data");
                return null;
            }
            //находим наш объект, кому пришла передача
            var placeId = UpdateWorldController.GetLocalIdByServerId(mail.PlaceServerId);

            Loger.Log("Mail " + placeId + " "
                + (mail.From == null ? "-" : mail.From.Login) + "->"
                + (mail.To == null ? "-" : mail.To.Login) + ":"
                + mail.ContentString());
            WorldObject place = null;
            if (placeId == 0)
            {
                if (softNewCaravan)
                {
                    Loger.Log($"Mail softNewCaravan Tile={mail.Tile}");
                    //если нет, и нужен караван, то возможно он был пересоздан: ищим в той же точке новый
                    place = Find.WorldObjects.AllWorldObjects
                        .Where(o => o is Caravan && o.Tile == mail.Tile && o.Faction == Faction.OfPlayer)
                        .OrderByDescending(o => o.ID)
                        .FirstOrDefault();
                }
                else if (softSettlement)
                {
                    Loger.Log("Mail softSettlement");
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
                Loger.Log("Mail fail: place is null");
                return null;
            }

            return place;
        }

        #region CreateThings
        public static void MailProcessCreateThings(ModelMailTrade mail)
        { 
            if (mail.Things == null
                || mail.Things.Count == 0
                || mail.PlaceServerId <= 0)
            {
                Loger.Log("Mail fail: no data");
                return;
            }

            var place = GetPlace(mail);
            if (place != null)
            {
                DropToWorldObject(place, mail.Things, (mail.From == null ? "-" : mail.From.Login));
            }
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
                    if (MainHelper.DebugMode) Loger.Log("Mail------------------------------------------------- {" + Environment.NewLine
                        + thing.Data + Environment.NewLine
                        + "Mail------------------------------------------------- }" + Environment.NewLine);
                    var thin = GameUtils.PrepareSpawnThingEntry(thing, factionPirate);

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
                    var thin = GameUtils.PrepareSpawnThingEntry(thing, factionPirate);

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
        #endregion CreateThings

        #region DeleteByServerId
        public static void MailProcessDeleteByServerId(ModelMailTrade mail)
        {
            Loger.Log("Client MailProcessDeleteByServerId " + mail.PlaceServerId);

            if (mail.PlaceServerId <= 0)
            {
                Loger.Log("Mail fail: no data");
                return;
            }

            var place = GetPlace(mail, false, true);
            if (place != null)
            {
                Find.WorldObjects.Remove(place);

                //автосейв с единым сохранением
                SessionClientController.SaveGameNow(true);
            }
        }
        #endregion DeleteByServerId

        #region AttackCancel
        public static void MailProcessAttackCancel(ModelMailTrade mail)
        {
            Loger.Log("Client MailProcessAttackCancel");

            GameAttackTrigger_Patch.ForceSpeed = -1f; //на всякий случай
            if (SessionClientController.Data.AttackModule != null) SessionClientController.Data.AttackModule.Clear();
            if (SessionClientController.Data.AttackUsModule != null) SessionClientController.Data.AttackUsModule.Clear();

            SessionClientController.Disconnected("OCity_GameAttacker_Dialog_ErrorMessage".Translate());
        }
        #endregion AttackCancel

        #region TechnicalVictory
        public static void MailProcessAttackTechnicalVictory(ModelMailTrade mail)
        {
            Loger.Log("Client MailProcessAttackTechnicalVictory");

            if (SessionClientController.Data.AttackModule != null)
            {
                SessionClientController.Data.AttackModule.Finish(true);
            }
            if (SessionClientController.Data.AttackUsModule != null)
            {
                SessionClientController.Data.AttackUsModule.Finish(false);
            }

        }
        #endregion TechnicalVictory
    }
}
