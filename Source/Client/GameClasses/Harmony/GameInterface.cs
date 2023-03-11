using HarmonyLib;
using OCUnion;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorldOnlineCity.GameClasses
{

    [HarmonyPatch(typeof(StatsReportUtility))]
    [HarmonyPatch("DrawStatsWorker")]
    internal class StatsReportUtility_DrawStatsWorker_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref Rect rect, Thing optionalThing, WorldObject optionalWorldObject)
        {
            var iconCopy = new Rect(rect.width - 32f, 18f, 32f, 32f);
            
            var txt = "Вставить в чат".NeedTranslate();
            var font = Text.Font;
            Text.Font = GameFont.Small;
            var anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(iconCopy.x - 153f, iconCopy.y, 150f, iconCopy.height), txt);
            TooltipHandler.TipRegion(iconCopy, txt);
            Text.Anchor = anchor;
            Text.Font = font;

            var serverId = (optionalWorldObject as WorldObjectBaseOnline)?.Place?.PlaceServerId;

            if (Widgets.ButtonImage(iconCopy, GeneralTexture.OCToChat))
            {
                if (optionalThing != null)
                {
                    var msg = $"<!{optionalThing.def.defName}/>";
                    ChatController.AddToInputChat(msg, true);
                }
                else if (serverId != null)
                {
                    var msg = $"<&{serverId.Value}/>";
                    ChatController.AddToInputChat(msg, true);
                }
                else if (optionalWorldObject != null)
                {
                    int tile = optionalWorldObject.Tile;
                    var msg = $"<#{tile}/>";
                    ChatController.AddToInputChat(msg, true);
                }
            }

            if (optionalThing != null) return true;
            var OCWO = optionalWorldObject as CaravanOnline;
            if (OCWO == null || OCWO.OnlinePlayerLogin == null) return true;

            var size = 100f;

            var iconArea = new Rect(rect.width - size, iconCopy.y + iconCopy.height, size, size);

            var iconImage = GeneralTexture.Get.ByName("pl_" + OCWO.OnlinePlayerLogin);
            GUI.DrawTexture(iconArea, iconImage);

            //rect.width -= iconArea.width;

            return true;
        }
    }

    [HarmonyPatch(typeof(RimWorld.InspectPaneFiller))]
    [HarmonyPatch("DrawInspectStringFor")]
    internal class InspectPaneFiller_DrawInspectStringFor_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(ISelectable sel, ref Rect rect)
        {
            var OCWO = sel as CaravanOnline;
            if (OCWO == null || OCWO.OnlinePlayerLogin == null) return true;

            var size = 100f;

            var iconArea = new Rect(rect.width - size, 0f, size, size);

            var iconImage = GeneralTexture.Get.ByName("pl_" + OCWO.OnlinePlayerLogin);
            GUI.DrawTexture(iconArea, iconImage);

            rect.width -= iconArea.width;

            return true;
        }
    }
    
    [HarmonyPatch(typeof(MainTabWindow_Inspect))]
    [HarmonyPatch("DoInspectPaneButtons")]
    internal class MainTabWindow_Inspect_DoInspectPaneButtons_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Rect rect, ref float lineEndWidth)
        {
            if (Find.Selector.NumSelected != 1)
            {
                return;
            }
            Thing singleSelectedThing = Find.Selector.SingleSelectedThing;
            if (singleSelectedThing == null)
            {
                return;
            }

            lineEndWidth += 30f;
            var iconCopy = new Rect(rect.width - lineEndWidth, -2f, 30f, 30f);
            if (Widgets.ButtonImage(iconCopy, GeneralTexture.OCToChat))
            {
                var msg = $"<!{singleSelectedThing.def.defName}/>";
                ChatController.AddToInputChat(msg, true);
            }
        }
    }

    [HarmonyPatch(typeof(WorldInspectPane))]
    [HarmonyPatch("DoInspectPaneButtons")]
    internal class WorldInspectPane_DoInspectPaneButtons_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Rect rect, ref float lineEndWidth)
        {
            WorldObject singleSelectedObject = Find.WorldSelector.SingleSelectedObject;
            if (singleSelectedObject != null || Find.WorldSelector.selectedTile >= 0)
            {
                lineEndWidth += 30f;
                var iconCopy = new Rect(rect.width - lineEndWidth, -2f, 30f, 30f);
                if (Widgets.ButtonImage(iconCopy, GeneralTexture.OCToChat))
                {
                    var serverId = (singleSelectedObject as WorldObjectBaseOnline)?.Place?.PlaceServerId;
                    if (serverId == null && singleSelectedObject != null && singleSelectedObject.Faction.IsPlayer)
                    {
                        serverId = UpdateWorldController.GetMyByLocalId(singleSelectedObject.ID)?.PlaceServerId;
                    }
                    if (serverId != null)
                    { 
                        var msg = $"<&{serverId.Value}/>";
                        ChatController.AddToInputChat(msg, true);
                    }
                    else
                    {
                        int tile = singleSelectedObject != null ? singleSelectedObject.Tile : Find.WorldSelector.selectedTile;
                        var msg = $"<#{tile}/>";
                        ChatController.AddToInputChat(msg, true);
                    }
                }

            }
        }

    }
    
}
