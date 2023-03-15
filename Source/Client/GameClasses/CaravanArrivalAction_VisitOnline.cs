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
                ExchengeUtils.ExchangeOfGoods_DoAction(сaravanOnline, caravan);
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

    }
}
