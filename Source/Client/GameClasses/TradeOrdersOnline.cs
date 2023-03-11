using Model;
using OCUnion;
using RimWorld.Planet;
using RimWorldOnlineCity.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transfer;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity
{
    /// <summary>
    /// Зелёное яблоко - список сделок
    /// </summary>
    [StaticConstructorOnStartup]
    public class TradeOrdersOnline : WorldObjectBaseOnline
    {
        public override IModelPlace Place => TradeOrders[0];

        /// <summary>
        /// Список ордеров для биржи. Может быть пустым только если это TradeThingsOnline
        /// Изначально заполняется легковесными TradeWorldObjectEntry, для деталей нужно обновить экземпляры до TradeOrder
        /// </summary>
        public List<TradeOrderShort> TradeOrders { get; set; } = new List<TradeOrderShort>();

        public override string Label
        {
            get
            {
                return "OC_TradeOrdersOnline_Orders".Translate();
            }
        }

        public override string GetInspectString()
        {
            return "OC_TradeOrdersOnline_Orders".Translate() + " " + TradeOrders.Count;
        }

        public override string GetDescription()
        {
            var res = base.GetDescription();
            res += Environment.NewLine
                + Environment.NewLine
                + "OC_TradeOrdersOnline_Orders".Translate()
                + (TradeOrders.Count == 0 ? ""
                    : (TradeOrders[0] is TradeOrderShort ? Environment.NewLine + "OC_TradeOrdersOnline_OpenForInfo".Translate().ToString()
                    : TradeOrders.Aggregate("", (r, i) => r + Environment.NewLine + i))
                    );
            return res;
        }

        public override string ToString()
        {
            return TradeOrders.Aggregate("", (r, i) => r + Environment.NewLine + i);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "OCity_Dialog_Exchenge_Trade_Orders".Translate();
            command_Action.defaultDesc = "OCity_Dialog_Exchenge_Trade_Orders".Translate();
            command_Action.icon = GeneralTexture.TradeButtonIcon;
            command_Action.action = delegate
            {
                Find.WindowStack.Add(new Dialog_Exchenge(this));
            };
            yield return command_Action;
        }

        #region Icons
        private static Material MatTradeOnlineIcon;
        private static Texture2D TradeOnlineIcon = ContentFinder<Texture2D>.Get("GreenApple");

        public override Material Material
        {
            get
            {
                if (MatTradeOnlineIcon == null) MatTradeOnlineIcon = MaterialPool.MatFrom(TradeOnlineIcon
                    , ShaderDatabase.WorldOverlayTransparentLit
                    , Color.white
                    , WorldMaterials.WorldObjectRenderQueue);
                return MatTradeOnlineIcon;
            }
        }

        public override Texture2D ExpandingIcon
        {
            get
            {
                return TradeOnlineIcon;
            }
        }

        public override string ExpandingIconName
        {
            get
            {
                return "GreenApple";
            }
        }
        #endregion
    }
}
