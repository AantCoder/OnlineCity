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
using Verse.Steam;

namespace RimWorldOnlineCity.GameClasses
{
    /*
    [HarmonyPatch(typeof(WorldInspectPane))]
    [HarmonyPatch("DoWindowContents")]
    internal class WorldInspectPane_DoWindowContents_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(WorldInspectPane __instance, ref Rect rect)
        {
			WorldObject worldObject = Find.WorldSelector.SingleSelectedObject;
            if (worldObject != null)
            {
                //если есть иконка выводим её слева смещая панель вправо (расширяя её)
                //var OCWO = worldObject as CaravanOnline;
                //if (OCWO == null || OCWO.OnlinePlayerLogin == null) return true;
                var iconImage = GeneralTexture.Waypoint; //GeneralTexture.Get.ByName("pl_" + OCWO.OnlinePlayerLogin);
                if (iconImage != GeneralTexture.Null)
                {
                    var size = rect.height;
                    var iconArea = new Rect(rect.x, rect.y, size, size);
                    GUI.DrawTexture(iconArea, iconImage);
                    rect.xMin += size;

                    if (__instance.windowRect.width == __instance.InitialSize.x)
                    {
                        __instance.windowRect.width += size;
                    }
                }
            }
            return true;
        }
    }
    */

    /*
    [HarmonyPatch(typeof(GizmoGridDrawer))]
    [HarmonyPatch("DrawGizmoGrid")]
    internal class GizmoGridDrawer_DrawGizmoGrid_Patch
    {
        //Выжимка из кода игры
        private static Rect GizmoGridButtonDrawStart(float startX)
        {
            float num2 = (float)(Verse.UI.screenHeight - 35) - GizmoGridDrawer.GizmoSpacing.y - 75f;
            if (SteamDeck.IsSteamDeck && SteamDeck.KeyboardShowing && Find.MainTabsRoot.OpenTab == MainButtonDefOf.Architect && ((MainTabWindow_Architect)MainButtonDefOf.Architect.TabWindow).QuickSearchWidgetFocused)
            {
                num2 -= 335f;
            }
            Vector2 vector = new Vector2(startX, num2);
            return new Rect(vector.x, vector.y, 75f, 75f);
        }

        [HarmonyPrefix]
        public static bool Prefix(ref float startX)
        {
            Pawn pawn = Find.Selector.SingleSelectedThing as Pawn; //  WorldSelector.SingleSelectedObject;
            if (pawn != null)
            {
                var rect = GizmoGridButtonDrawStart(startX);
                rect.y -= rect.width + GizmoGridDrawer.GizmoSpacing.x;
                rect.width += rect.width + GizmoGridDrawer.GizmoSpacing.x;
                rect.height = rect.width;

                //var OCWO = worldObject as CaravanOnline;
                //if (OCWO == null || OCWO.OnlinePlayerLogin == null) return true;
                var iconImage = GeneralTexture.Waypoint; //GeneralTexture.Get.ByName("pl_" + OCWO.OnlinePlayerLogin);
                if (iconImage != GeneralTexture.Null)
                {
                    var size = rect.height;
                    var iconArea = new Rect(rect.x, rect.y, size, size);
                    if (Widgets.ButtonInvisible(iconArea))
                    {
                        //Find.WindowStack.Add(new Dialog_MessageBox("Работает!"));
                    }
                    GUI.DrawTexture(iconArea, Command.BGTexShrunk); //BGTex); // текстура неактивной кнопки
                    iconArea = iconArea.ContractedBy(2);
                    GUI.DrawTexture(iconArea, iconImage);
                    GenUI.AbsorbClicksInRect(iconArea);

                    startX += GizmoGridDrawer.GizmoSpacing.x + rect.width;
                }
            }
            return true;
        }
    }
    */
    // Эти данные отображаются в полноэкранном окне информации "i" Dialog_InfoCard
    [HarmonyPatch(typeof(StatsReportUtility))]
    [HarmonyPatch("DrawStatsWorker")]
    internal class StatsReportUtility_DrawStatsWorker_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref Rect rect, Thing optionalThing, WorldObject optionalWorldObject)
        {
            var iconCopy = new Rect(rect.width - 32f, 18f, 32f, 32f);
            
            var txt = "OCity_GameInterface_InsertIntoChat".Translate();
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
