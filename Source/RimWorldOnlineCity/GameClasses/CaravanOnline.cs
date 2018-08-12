using Model;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorldOnlineCity
{
    public class CaravanOnline : WorldObject
    {
        public string OnlinePlayerLogin
        {
            get { return OnlineWObject == null ? null : OnlineWObject.LoginOwner; }
        }
        public string OnlineName
        {
            get { return OnlineWObject == null ? "" : OnlineWObject.Name; }
        }

        public WorldObjectEntry OnlineWObject;

        public override string Label
        {
            get
            {
                return "OCity_Caravan_Player".Translate().Translate(new object[]
                    {
                        OnlineName,
                        OnlinePlayerLogin
                    });
                /*
                return "Караван {0}".Translate(new object[]
                    {
                        OnlinePlayerLogin + " " + OnlineName
                    });
                */
            }
        }

        public override string GetInspectString()
        {
            if (OnlineWObject == null)
                return ("OCity_Caravan_Player".Translate() + Environment.NewLine
                    ).Translate(new object[]
                    {
                        OnlineName
                        , OnlinePlayerLogin
                    });
            else
                return ("OCity_Caravan_Player".Translate() + Environment.NewLine
                    + "OCity_Caravan_PriceThing".Translate() + Environment.NewLine
                    + "OCity_Caravan_PriceAnimalsPeople".Translate()
                    + "OCity_Caravan_Other".Translate()
                    ).Translate(new object[]
                    {
                        OnlineName
                        , OnlinePlayerLogin + " (sId:" + OnlineWObject.ServerId +")"
                        , OnlineWObject.MarketValue.ToStringMoney()
                        , OnlineWObject.MarketValuePawn.ToStringMoney()
                        , OnlineWObject.FreeWeight > 0 && OnlineWObject.FreeWeight < 999999
                            ? Environment.NewLine + "OCity_Caravan_FreeWeight".Translate() + OnlineWObject.FreeWeight.ToStringMass()
                            : ""
                        , "" //todo Environment.NewLine + GameUtils.PlayerTextInfo(OnlineWObject.)
                    });
        }
        
        [DebuggerHidden]
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            foreach (FloatMenuOption o in base.GetFloatMenuOptions(caravan))
            {
                yield return o;
            }

            yield return new FloatMenuOption("OCity_Caravan_Trade".Translate(new object[]
            {
                OnlinePlayerLogin + " " + OnlineName
            }), delegate
            {
                caravan.pather.StartPath(this.Tile, new CaravanArrivalAction_VisitOnline(this, "exchangeOfGoods"), true);
            }, MenuOptionPriority.Default, null, null, 0f, null, this);

        }
    }
}
