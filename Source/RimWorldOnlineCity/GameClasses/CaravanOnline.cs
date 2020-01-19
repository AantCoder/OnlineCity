using Model;
using OCUnion;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity
{
    [StaticConstructorOnStartup]
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

        public PlayerClient Player
        {
            get
            {
                return SessionClientController.Data.Players.TryGetValue(OnlinePlayerLogin);
            }
        }
        public bool IsOnline
        {
            get
            {
                return Player != null && Player.Online;
            }
        }

        public WorldObjectEntry OnlineWObject;

        public override string Label
        {
            get
            {
                return "OCity_Caravan_Player".Translate(OnlineName, OnlinePlayerLogin);
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
                return "OCity_Caravan_Player".Translate(OnlineName, OnlinePlayerLogin) + Environment.NewLine;
            else
            {
                return ("OCity_Caravan_Player".Translate() + Environment.NewLine
                    + "OCity_Caravan_PriceThing".Translate() + Environment.NewLine
                    + "OCity_Caravan_PriceAnimalsPeople".Translate()
                    + "OCity_Caravan_Other".Translate()
                    ).Translate(
                        OnlineName
                        , OnlinePlayerLogin + (IsOnline ? " Online!" : "") + " (sId:" + OnlineWObject.ServerId +")"
                        , OnlineWObject.MarketValue.ToStringMoney()
                        , OnlineWObject.MarketValuePawn.ToStringMoney()
                        , OnlineWObject.FreeWeight > 0 && OnlineWObject.FreeWeight < 999999
                            ? Environment.NewLine + "OCity_Caravan_FreeWeight".Translate() + OnlineWObject.FreeWeight.ToStringMass()
                            : ""
                        , "" //todo Environment.NewLine + GameUtils.PlayerTextInfo(OnlineWObject.)
                    );
            }
        }

        public override string GetDescription()
        {
            var res = base.GetDescription();
            res += Environment.NewLine
                + Environment.NewLine
                + GetInspectString()
                + (Player == null ? "" :
                    Environment.NewLine
                    + Environment.NewLine
                    + Player.GetTextInfo()
                    );
            return res;
        }

        [DebuggerHidden]
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            foreach (FloatMenuOption o in base.GetFloatMenuOptions(caravan))
            {
                yield return o;
            }

            yield return new FloatMenuOption("OCity_Caravan_Trade".Translate(OnlinePlayerLogin + " " + OnlineName), delegate
            {
                caravan.pather.StartPath(this.Tile, new CaravanArrivalAction_VisitOnline(this, "exchangeOfGoods"), true);
            }, MenuOptionPriority.Default, null, null, 0f, null, this);

            if (SessionClientController.My.EnablePVP
                && this is BaseOnline 
                && GameAttacker.CanStart)
            {
                if (AttackUtils.CheckPossibilityAttack(SessionClientController.Data.MyEx
                    , this.Player
                    , UpdateWorldController.GetMyByLocalId(caravan.ID).ServerId
                    , this.OnlineWObject.ServerId
                    ) == null
                    )
                { 
                    yield return new FloatMenuOption(string.Format("OCity_Caravan_Attack".Translate()
                        , OnlinePlayerLogin + " " + OnlineName
                    ), delegate
                    {
                        caravan.pather.StartPath(this.Tile, new CaravanArrivalAction_VisitOnline(this, "attack"), true);
                    }, MenuOptionPriority.Default, null, null, 0f, null, this);
                }
            }
            //}
        }

        #region Icons
        private Material MatCaravanOn;

        private Material MatCaravanOff;

        private static Texture2D CaravanOn;

        private static Texture2D CaravanOff;

        private static Texture2D CaravanOnExpanding;

        private static Texture2D CaravanOffExpanding;

        static CaravanOnline()
        {
            CaravanOn = ContentFinder<Texture2D>.Get("CaravanOn");
            CaravanOff = ContentFinder<Texture2D>.Get("CaravanOff");
            CaravanOnExpanding = ContentFinder<Texture2D>.Get("CaravanOnExpanding");
            CaravanOffExpanding = ContentFinder<Texture2D>.Get("CaravanOffExpanding");
        }

        public override Material Material
        {
            get
            {
                if (IsOnline)
                {
                    if (this.MatCaravanOn == null) this.MatCaravanOn = MaterialPool.MatFrom(CaravanOn
                        , ShaderDatabase.WorldOverlayTransparentLit
                        , Color.white
                        , WorldMaterials.WorldObjectRenderQueue);
                    //MaterialPool.MatFrom(CaravanOn);
                    //MaterialPool.MatFrom(base.Faction.def.homeIconPath, ShaderDatabase.WorldOverlayTransparentLit, base.Faction.Color, WorldMaterials.WorldObjectRenderQueue);
                    return this.MatCaravanOn;
                }
                else
                {
                    if (this.MatCaravanOff == null) this.MatCaravanOff = MaterialPool.MatFrom(CaravanOff
                        , ShaderDatabase.WorldOverlayTransparentLit
                        , Color.white
                        , WorldMaterials.WorldObjectRenderQueue);
                    return this.MatCaravanOff;
                }
            }
        }

        public override Texture2D ExpandingIcon
        {
            get
            {
                return IsOnline ? CaravanOnExpanding : CaravanOffExpanding;
            }
        }
        #endregion
    }
}
