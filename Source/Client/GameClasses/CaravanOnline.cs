using Model;
using OCUnion;
using RimWorld.Planet;
using RimWorldOnlineCity.UI;
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
    public class CaravanOnline : WorldObjectBaseOnline
    {
        public override IModelPlace Place => OnlineWObject;

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
                var s = "OCity_Caravan_Player".Translate() + Environment.NewLine
                    //+ "OCity_Caravan_PriceThing".Translate() + Environment.NewLine
                    //+ "OCity_Caravan_PriceAnimalsPeople".Translate()
                    + "OCity_Caravan_Other".Translate();
                var s1 = string.Format(s,
                        OnlineName
                        , OnlinePlayerLogin + (IsOnline ? " Online!" : "") + " (sId:" + OnlineWObject.PlaceServerId + ")"
                        , OnlineWObject.MarketValue.ToStringMoney()
                        , OnlineWObject.MarketValuePawn.ToStringMoney()
                        , (OnlineWObject.MarketValueBalance + OnlineWObject.MarketValueStorage).ToStringMoney()
                        )
                    + ((this is BaseOnline) && SessionClientController.My.EnablePVP
                        ? Environment.NewLine + "OCity_Caravan_PlayerAttackCost".Translate(
                            AttackUtils.MaxCostAttackerCaravan(OnlineWObject.MarketValueTotal, this is BaseOnline).ToStringMoney()).ToString()
                        : "")
                    + (OnlineWObject.FreeWeight > 0 && OnlineWObject.FreeWeight < 999999
                        ? Environment.NewLine + "OCity_Caravan_FreeWeight".Translate().ToString() + OnlineWObject.FreeWeight.ToStringMass()
                        : "");
                return s1;
            }
        }

        public virtual string GetInspectExtendedString()
        {
            if (OnlineWObject == null)
                return "OCity_Caravan_Player".Translate(OnlineName, OnlinePlayerLogin) + Environment.NewLine;
            else
            {
                var s = "OCity_Caravan_Player".Translate() + Environment.NewLine
                    //+ "OCity_Caravan_PriceThing".Translate() + Environment.NewLine
                    //+ "OCity_Caravan_PriceAnimalsPeople".Translate()
                    + "OCity_Caravan_Other".Translate();
                var s1 = string.Format(s,
                        $"<&{OnlineWObject.PlaceServerId}> " /*+ OnlineName*/
                        , /*OnlinePlayerLogin +*/ (IsOnline ? $"<:check_mark_button height=16:> Online!" : "<:zzz height=16:> ") + " (sId:" + OnlineWObject.PlaceServerId + ")"
                        , "<:money_bag height=16:> " + OnlineWObject.MarketValue.ToStringMoney()
                        , "<:busts_in_silhouette height=16:> " + OnlineWObject.MarketValuePawn.ToStringMoney()
                        , "<:chart_increasing height=16:> " + (OnlineWObject.MarketValueBalance + OnlineWObject.MarketValueStorage).ToStringMoney()
                        )
                    + ((this is BaseOnline) && SessionClientController.My.EnablePVP
                        ? Environment.NewLine + "OCity_Caravan_PlayerAttackCost".Translate(
                            AttackUtils.MaxCostAttackerCaravan(OnlineWObject.MarketValueTotal, this is BaseOnline).ToStringMoney()).ToString()
                        : "")
                    + (OnlineWObject.FreeWeight > 0 && OnlineWObject.FreeWeight < 999999
                        ? Environment.NewLine + "OCity_Caravan_FreeWeight".Translate().ToString() + " <:handbag height=16:> " + OnlineWObject.FreeWeight.ToStringMass()
                        : "");
                return s1;
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

            if (SessionClientController.My == null || this.Player == null) yield break;

            var player = this.Player;
            FloatMenuOption fmoTrade;
            try
            {
                fmoTrade = ExchengeUtils.ExchangeOfGoods_GetFloatMenu(this, () =>
                {
                    caravan.pather.StartPath(this.Tile, new CaravanArrivalAction_VisitOnline(this, "exchangeOfGoods"), true);
                });
            }
            catch (Exception ex)
            {
                yield break;
            }
            yield return fmoTrade;

            // Атаковать
            if (SessionClientController.My.EnablePVP
                && this is BaseOnline
                && GameAttacker.CanStart)
            {
                FloatMenuOption fmo;
                try
                {
                    var dis = AttackUtils.CheckPossibilityAttack(SessionClientController.Data.MyEx
                        , player
                        , UpdateWorldController.GetMyByLocalId(caravan.ID).PlaceServerId
                        , this.OnlineWObject.PlaceServerId
                        , SessionClientController.Data.ProtectingNovice
                        );
                    fmo = new FloatMenuOption("OCity_Caravan_Attack".Translate(OnlinePlayerLogin + " " + OnlineName)
                        + (dis != null ? " (" + dis + ")" : "")
                        , delegate
                    {
                        caravan.pather.StartPath(this.Tile, new CaravanArrivalAction_VisitOnline(this, "attack"), true);
                    }, MenuOptionPriority.Default, null, null, 0f, null, this);

                    if (dis != null)
                    {
                        fmo.Disabled = true;
                    }

                }
                catch (Exception ex)
                {
                    yield break;
                }
                yield return fmo;
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            if (Player != null)
            {
                yield return new Command_Action()
                {
                    defaultLabel = "OCity_Dialog_ChennelPlayerInfo".Translate(),
                    defaultDesc = "OCity_Dialog_ChennelPlayerInfoTitle".Translate() + Player.Public.Login,
                    icon = GeneralTexture.OCInfo,
                    action = delegate
                    {
                        Dialog_InfoPlayer.ShowInfoPlayer(Player);
                    }
                };
            }

            yield return new Command_Action()
            {
                defaultLabel = "OCity_Dialog_Exchenge_Trade_Orders".Translate(),
                defaultDesc = "OCity_Dialog_Exchenge_Trade_Orders".Translate(),
                icon = GeneralTexture.TradeButtonIcon,
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_Exchenge(this));
                }
            };
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

        public override string ExpandingIconName
        {
            get
            {
                return IsOnline ? "CaravanOnExpanding" : "CaravanOffExpanding";
            }
        }
        #endregion
    }
}
