using HugsLib.Utils;
using Model;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorldOnlineCity
{
    public class CaravanArrivalAction_VisitOnline : CaravanArrivalAction//, ITrader
    {
        private CaravanOnline сaravanOnline;

        private string mode;
        
        public CaravanArrivalAction_VisitOnline()
        {
        }

        public CaravanArrivalAction_VisitOnline(CaravanOnline сaravanOnline, string mode)
        {
            this.сaravanOnline = сaravanOnline;
            this.mode = mode;
        }

        public override string ReportString
        {
            get
            {
                if (сaravanOnline == null) return "";
                return (mode == "exchangeOfGoods" ? "OCity_Caravan_GoTrade".Translate() : "OCity_Caravan_GoTrade2".Translate())
                    .Translate(new object[]
                    {
                        сaravanOnline.Label
                    });
            }
        }

        public override bool ShouldFail { get { return false; } }

        public override void Arrived(Caravan caravan)
        {
            if (mode == "exchangeOfGoods")
            {
                //Pawn bestNegotiator = CaravanVisitUtility.BestNegotiator(caravan);
                ThingOwner<Thing> сontainer = new ThingOwner<Thing>();
                Dialog_TradeOnline form = null;
                if (сaravanOnline.OnlineWObject == null)
                {
                    Log.Error("OCity_Caravan_LOGNoData".Translate());
                    return;
                }

                var goods = CaravanInventoryUtility.AllInventoryItems(caravan).ToList().Concat(
                    caravan.PawnsListForReading
                    .Cast<Thing>()
                    ).ToList();
                
                form = new Dialog_TradeOnline(goods
                    , сaravanOnline.OnlinePlayerLogin
                    , сaravanOnline.OnlineWObject.FreeWeight
                    , () =>
                {
                    var select = form.GetSelect();
                    bool selectAllCaravan = caravan.PawnsListForReading.Count == select.Count(s => s.Key is Pawn);
                    if (selectAllCaravan)
                    {
                        Log.Message("OCity_Caravan_LOGSwap".Translate());
                        select = new Dictionary<Thing, int>();
                        foreach (var pawn in caravan.PawnsListForReading)
                        {
                            foreach (var item in pawn.inventory.innerContainer.ToDictionary(t => t, t => t.stackCount))
                                select.Add(item.Key, item.Value);
                            select.Add(pawn, 1);
                            pawn.inventory.innerContainer.Clear();
                        }
                    }
                    //передаем выбранные товары из caravan к другому игроку в сaravanOnline
                    var sendThings = new List<ThingEntry>();
                    foreach (var pair in select)
                    {
                        var thing = pair.Key;
                        var numToTake = pair.Value;
                        if (thing is Pawn)
                        {
                            var pawn = thing as Pawn;
                            //если отдали пешку, то выкладываем все другим и удаляемся из каравана
                            var things = pawn.inventory.innerContainer.ToList();
                            pawn.inventory.innerContainer.Clear();
                            GameUtils.DeSpawnSetupOnCaravan(caravan, pawn);
                            foreach (var thin in things)
                            {
                                var p = CaravanInventoryUtility.FindPawnToMoveInventoryTo(thin, caravan.PawnsListForReading, null);
                                if (p != null)
                                    p.inventory.innerContainer.TryAdd(thin, true);
                            }
                        }
                        sendThings.Add(new ThingEntry(thing, numToTake));
                    }
                    SessionClientController.Command((connect) =>
                    {
                        connect.SendThings(sendThings
                            , SessionClientController.My.Login
                            , сaravanOnline.OnlinePlayerLogin
                            , сaravanOnline.OnlineWObject.ServerId
                            , сaravanOnline.Tile
                            );
                    });

                    if (selectAllCaravan)
                    {
                        //удаляем пешку из игры
                        foreach (var pawn in caravan.PawnsListForReading)
                        {
                            pawn.Destroy(DestroyMode.Vanish);
                        }

                        Find.WorldObjects.Remove(caravan);
                    }
                    else
                    {
                        foreach (var pair in select)
                        {
                            var thing = pair.Key;
                            var numToTake = pair.Value;
                            if (thing is Pawn)
                            {
                                var pawn = thing as Pawn;
                                //удаляем пешку из игры
                                pawn.Destroy(DestroyMode.Vanish);
                                //Find.WorldPawns.RemovePawn(pawn); не проверенное не полное удаление, если её вернут назад
                            }
                            else
                            {
                                //если отдали вешь, то находим кто её тащит и убираем с него
                                Pawn ownerOf = CaravanInventoryUtility.GetOwnerOf(caravan, thing);
                                ownerOf.inventory.innerContainer.TryTransferToContainer(thing, сontainer, numToTake);
                            }
                        }
                    }
                });
                Find.WindowStack.Add(form);
            }
        }
    }
}
