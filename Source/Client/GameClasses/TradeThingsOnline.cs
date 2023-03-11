using Model;
using OCUnion;
using RimWorld.Planet;
using RimWorldOnlineCity.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity
{

    /// <summary>
    /// Красное яблоко - торговый склад вещей игрока
    /// </summary>
    [StaticConstructorOnStartup]
    public class TradeThingsOnline : WorldObjectBaseOnline
    {
        public override IModelPlace Place => TradeThings;

        public TradeThingStorage TradeThings { get; set; } = new TradeThingStorage();

        public override string Label
        {
            get
            {

                return "OC_TradeThingsOnline_Storage".Translate();
            }
        }

        public override string GetInspectString()
        {
            return "OC_TradeThingsOnline_Storage".Translate() + Environment.NewLine
                + "OC_TradeThingsOnline_ThingTypes".Translate(TradeThings.Things.Count) + Environment.NewLine
                + "OC_TradeThingsOnline_Info".Translate()
                + " " + "OCity_Dialog_Exchenge_Move".Translate();
        }

        public override string GetDescription()
        {
            var res = base.GetDescription();
            res += Environment.NewLine
                + Environment.NewLine
                + "OC_TradeThingsOnline_Storage".Translate()
                + TradeThings.Things.ToStringLabel();
            return res;
        }

        public override string ToString()
        {
            return TradeThings.Things.ToStringLabel();
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
        private static Material MatTradeThingsOnlineIcon;
        private static Texture2D TradeThingsOnlineIcon = ContentFinder<Texture2D>.Get("Apple");

        public override Material Material
        {
            get
            {
                if (MatTradeThingsOnlineIcon == null) MatTradeThingsOnlineIcon = MaterialPool.MatFrom(TradeThingsOnlineIcon
                    , ShaderDatabase.WorldOverlayTransparentLit
                    , Color.white
                    , WorldMaterials.WorldObjectRenderQueue);
                return MatTradeThingsOnlineIcon;
            }
        }

        public override Texture2D ExpandingIcon
        {
            get
            {
                return TradeThingsOnlineIcon;
            }
        }

        public override string ExpandingIconName
        {
            get
            {
                return "Apple";
            }
        }
        #endregion
    }
}
