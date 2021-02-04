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
using Transfer.ModelMails;
using Verse;

namespace RimWorldOnlineCity
{
    static class MailController
    {
        private static Dictionary<Type, Action<ModelMail>> TypeMailProcessing = new Dictionary<Type, Action<ModelMail>>()
        {
            { typeof(ModelMailTrade), MailProcessCreateThings},
            { typeof(ModelMailDeleteWO), MailProcessDeleteByServerId},
            { typeof(ModelMailAttackCancel), MailProcessAttackCancel},
            { typeof(ModelMailAttackTechnicalVictory), MailProcessAttackTechnicalVictory},
            { typeof(ModelMailStartIncident), MailProcessStartIncident},
            { typeof(ModelMailMessadge), MailProcessMessadge}
        };

        public static void MailArrived(ModelMail mail)
        {
            try
            {
                if (mail.To == null
                    || mail.To.Login != SessionClientController.My.Login) return;

                Action<ModelMail> action;
                if (TypeMailProcessing.TryGetValue(mail.GetType(), out action))
                {
                    action(mail);
                }
                else
                {
                    Loger.Log("Mail fail: error type " + mail.GetType().Name);
                }
            }
            catch(Exception e)
            {
                Loger.Log("Mail Exception: " + e.ToString());
            }
        }

        private static WorldObject GetPlace<TMailPlace>(TMailPlace mail, bool softSettlement = true, bool softNewCaravan = false)
            where TMailPlace : ModelMail, IModelMailPlace
        {
            if (mail.PlaceServerId <= 0)
            {
                Loger.Log("Mail fail: no data");
                return null;
            }
            //находим наш объект, кому пришла передача
            var placeId = UpdateWorldController.GetLocalIdByServerId(mail.PlaceServerId);

            Loger.Log($"Mail {mail.GetType().Name} {placeId} "
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

        #region MailProcessMessadge
        public static void MailProcessMessadge(ModelMail incoming)
        {
            var msg = (ModelMailMessadge)incoming;
            LetterDef def;
            switch (msg.type)
            {
                case ModelMailMessadge.MessadgeTypes.ThreatBig:
                    def = LetterDefOf.ThreatBig;
                    break;
                case ModelMailMessadge.MessadgeTypes.ThreatSmall:
                    def = LetterDefOf.ThreatSmall;
                    break;
                case ModelMailMessadge.MessadgeTypes.Death:
                    def = LetterDefOf.Death;
                    break;
                case ModelMailMessadge.MessadgeTypes.Negative:
                    def = LetterDefOf.NegativeEvent;
                    break;
                case ModelMailMessadge.MessadgeTypes.Neutral:
                    def = LetterDefOf.NeutralEvent;
                    break;
                case ModelMailMessadge.MessadgeTypes.Positive:
                    def = LetterDefOf.PositiveEvent;
                    break;
                case ModelMailMessadge.MessadgeTypes.Visitor:
                    def = LetterDefOf.AcceptVisitors;
                    break;
                default:
                    def = LetterDefOf.NeutralEvent;
                    break;
            }
            Find.LetterStack.ReceiveLetter(msg.label, msg.text, def);
        }
        #endregion

        #region MailProcessStartIncident
        public static void MailProcessStartIncident(ModelMail incoming)
        {
            var mail = (ModelMailStartIncident)incoming;

            Find.TickManager.Pause();
            
            var incident = new Incidents().GetIncident(mail.IncidentType);
            incident.mult = mail.IncidentMult;
            incident.arrivalMode = mail.IncidentArrivalMode;
            incident.strategy = mail.IncidentStrategy;
            incident.faction = mail.IncidentFaction;
            incident.place = GetPlace(mail);
            incident.TryExecuteEvent();

            if (!SessionClientController.Data.BackgroundSaveGameOff) SessionClientController.SaveGameNow(true);
        }
        #endregion

        #region CreateThings
        public static void MailProcessCreateThings(ModelMail incoming)
        {
            ModelMailTrade mail = (ModelMailTrade)incoming;

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
        public static void MailProcessDeleteByServerId(ModelMail incoming)
        {
            var mail = (ModelMailDeleteWO)incoming;

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
        public static void MailProcessAttackCancel(ModelMail incoming)
        {
            var mail = (ModelMailAttackCancel)incoming;

            Loger.Log("Client MailProcessAttackCancel");

            GameAttackTrigger_Patch.ForceSpeed = -1f; //на всякий случай
            if (SessionClientController.Data.AttackModule != null) SessionClientController.Data.AttackModule.Clear();
            if (SessionClientController.Data.AttackUsModule != null) SessionClientController.Data.AttackUsModule.Clear();

            SessionClientController.Disconnected("OCity_GameAttacker_Dialog_ErrorMessage".Translate());
        }
        #endregion AttackCancel

        #region TechnicalVictory
        public static void MailProcessAttackTechnicalVictory(ModelMail incoming)
        {
            var mail = (ModelMailAttackTechnicalVictory)incoming;

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
