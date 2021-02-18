using Model;
using OCUnion;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        //Пример: посетить
        public override string Label
        {
            get
            {
                if (сaravanOnline == null) return "";
                return string.Format(mode == "exchangeOfGoods" ? "OCity_Caravan_GoTrade".Translate()
                        : mode == "attack" ? "OCity_Caravan_Go_Attack_Target".Translate()
                        : "OCity_Caravan_GoTrade2".Translate()
                    , сaravanOnline.Label);
            }
        }

        //Пример: посещает
        public override string ReportString
        {
            get
            {
                return Label;
            }
        }

        //public override bool ShouldFail { get { return false; } }

        public override void Arrived(Caravan caravan)
        {
            if (mode == "exchangeOfGoods")
            {
                exchangeOfGoods(caravan);
            }
            else if (mode == "attack")
            {
                attack(caravan);
            }
        }

        private void attack(Caravan caravan)
        {
            Find.TickManager.Pause();
            Action<bool> att = (testMode) =>
            {
                if (GameAttacker.Create())
                {
                    GameAttacker.Get.Start(caravan, (BaseOnline)сaravanOnline, testMode);
                }
            };

            GameUtils.ShowDialodOKCancel("OCity_Caravan_Go_Attack_Target".Translate() + " " + сaravanOnline.Label
                , "OCity_Caravan_Confirm_Attack_TestBattle_Possible".Translate()
                , () => att(false)
                , () => { }
                , null
                , "OCity_Caravan_Practive".Translate()
                , () => att(true)
            );
        }

        private void exchangeOfGoods(Caravan caravan)
        {
            //Pawn bestNegotiator = CaravanVisitUtility.BestNegotiator(caravan);
            ThingOwner<Thing> сontainer = new ThingOwner<Thing>();
            Dialog_TradeOnline form = null;
            if (сaravanOnline.OnlineWObject == null)
            {
                Log.Error("OCity_Caravan_LOGNoData".Translate());
                return;
            }

            var goods = GameUtils.GetAllThings(caravan);

            form = new Dialog_TradeOnline(goods
                , сaravanOnline.OnlinePlayerLogin
                , сaravanOnline.OnlineWObject.FreeWeight
                , () =>
            {
                if (!SessionClientController.Data.BackgroundSaveGameOff)
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
                        sendThings.Add(ThingEntry.CreateEntry(thing, numToTake));
                    }

                    if (selectAllCaravan)
                    {
                        //удаляем пешку из игры
                        foreach (var pawn in caravan.PawnsListForReading)
                        {
                            GameUtils.PawnDestroy(pawn);
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
                                GameUtils.PawnDestroy(pawn);
                            }
                            else
                            {
                                //если отдали вешь, то находим кто её тащит и убираем с него
                                Pawn ownerOf = CaravanInventoryUtility.GetOwnerOf(caravan, thing);
                                ownerOf.inventory.innerContainer.TryTransferToContainer(thing, сontainer, numToTake);
                            }
                        }
                    }

                    //После передачи сохраняем, чтобы нельзя было обузить
                    SessionClientController.SaveGameNow(true, () =>
                    {
                        SessionClientController.Command((connect) =>
                        {
                            int repeat = 0;
                            do
                            {
                                if (connect.SendThings(sendThings
                                    , SessionClientController.My.Login
                                    , сaravanOnline.OnlinePlayerLogin
                                    , сaravanOnline.OnlineWObject.ServerId
                                    , сaravanOnline.Tile
                                    ))
                                {
                                    repeat = 1000;
                                }
                                else
                                {
                                    Thread.Sleep(5000);
                                    //вроде как этот механизм не нужен, т.к. в SessionClientController.Command добавили ожидание реконнекта
                                    Loger.Log("Client exchangeOfGoods: try SendThings again ");
                                }
                            }
                            while (++repeat < 3); //делаем 3 попытки включая первую
                            //если не удалось отправить письмо (repeat < 1000), то жопа так как сейв уже прошел
                        });
                    });
                }
            });
            Find.WindowStack.Add(form);
        }


    }
}
