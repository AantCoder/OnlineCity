using HarmonyLib;
using Model;
using OCUnion;
using RimWorld;
using RimWorld.Planet;
using RimWorldOnlineCity.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using UnityEngine;
using Verse;
using RimWorldOnlineCity;

namespace RimWorldOnlineCity.GameClasses.Harmony
{
    /// ////////////////////////////////////////////////////////////

    //Следим за включением режима разработчика, если он отключен
    [HarmonyPatch(typeof(PrefsData))]
    [HarmonyPatch("Apply")]
    internal class PrefsData_Apply_Patch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Current.Game == null) return;
            if (!SessionClient.Get.IsLogined) return;

            if (SessionClientController.Data.DisableDevMode)
            {
                if (Prefs.DevMode) Prefs.DevMode = false;
                if (IdeoUIUtility.devEditMode) IdeoUIUtility.devEditMode = false;
            }
        }
    }

    //Отключаем нстройки модов из игры
    [HarmonyPatch(typeof(Dialog_Options))]
    [HarmonyPatch("DoModOptions")]
    internal class Dialog_Options_DoModOptions_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Listing_Standard listing)
        {
            //скрывать настройки модов, если после запуска хотя бы раз была нажата Серевая игра
            if (Current.Game == null && MainMenu.HasClickMainMenuNetClick)
            {
                listing.Gap();
                var rect2 = listing.GetRect(50f);
                Widgets.Label(rect2, "OCity_GamePatch_DisableModOptions".Translate());
                return false;
            }

            //скрываем настройки модов в ходе сетевой игры
            if (Current.Game == null) return true;
            if (!SessionClient.Get.IsLogined) return true;

            if (SessionClientController.Data.DisableDevMode)
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Dialog_CreateXenotype))]
    [HarmonyPatch("PostXenotypeOnGUI")]
    internal class Dialog_CreateXenotype_PostXenotypeOnGUI_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Dialog_CreateXenotype __instance)
        {
            if (Current.Game == null) return;
            if (!SessionClient.Get.IsLogined) return;

            if (!SessionClientController.Data.DisableDevMode) return;

            var that = Traverse.Create(__instance);
            that.Field("ignoreRestrictions").SetValue(false);
            //var _ignoreRestrictions = that.Field("ignoreRestrictions").GetValue<bool>();
        }
    }


    /// ////////////////////////////////////////////////////////////

    //Выключаем настройки рассказчика
    [HarmonyPatch(typeof(Page_SelectStorytellerInGame))]
    [HarmonyPatch("DoWindowContents")]
    internal class Page_SelectStorytellerInGame_DoWindowContents_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Page_SelectStorytellerInGame __instance)
        {
            if (Current.Game == null) return true;
            if (!SessionClient.Get.IsLogined) return true;
            if (Prefs.DevMode) return true; //чтобы разрешить тем, у кого есть право на админку

            if (SessionClientController.Data.GeneralSettings.DisableGameSettings)
            {
                Loger.Log("Page_SelectStorytellerInGame_DoWindowContents_Patch DisableGameSettings");
                __instance.Close();
                return false;
            }

            return true;
        }

    }

    /// ////////////////////////////////////////////////////////////

    //Выключаем настройки модов
    [HarmonyPatch(typeof(HugsLib.Utils.HugsLibUtility))]
    [HarmonyPatch("OpenModSettingsDialog")]
    internal class HugsLibUtility_OpenModSettingsDialog_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (Current.Game == null) return true;
            if (!SessionClient.Get.IsLogined) return true;
            if (Prefs.DevMode) return true; //чтобы разрешить тем, у кого есть право на админку

            if (SessionClientController.Data.GeneralSettings.DisableGameSettings)
            {
                Loger.Log("HugsLibUtility_OpenModSettingsDialog_Patch DisableGameSettings");
                return false;
            }

            return true;
        }

    }

    /// ////////////////////////////////////////////////////////////

    //Меняем начальный год
    [HarmonyPatch(typeof(GenDate), "Year")]
    internal class GenDatePatch
    {
        public static void Postfix(long absTicks, float longitude, ref int __result)
        {
            if (!SessionClient.Get.IsLogined) return;

            int needYear = SessionClientController.Data.GeneralSettings.StartGameYear;
            if (needYear < 0 || needYear == 5500) return;

            var longAdj = GenDate.TimeZoneAt(longitude) * 2500L;
            __result = needYear + (int)((absTicks + longAdj) / 3600000f);
        }
    }

    /// ////////////////////////////////////////////////////////////

    //Подключаемся к разрешению ссылок (Например Faction_10) при загрузке кусочков сейвов: при создании вещщей в GameXMLUtils.FromXml
    //public T ObjectWithLoadID<T>(string loadID)
    //[HarmonyPatch(typeof(LoadedObjectDirectory), new Type[] { typeof(Faction) })]
    //[HarmonyPatch("ObjectWithLoadID")]
    /* крашит игру
    [HarmonyPatch()]
    public class LoadedObjectDirectory_ObjectWithLoadID_Patch
    {
        static MethodBase TargetMethod()
        {
            return typeof(LoadedObjectDirectory).GetMethod("ObjectWithLoadID").MakeGenericMethod(typeof(Faction));
        }

        [HarmonyPrefix]
        public static bool Prefix(string loadID, ref object __result)
        {
            if (!GameXMLUtils.FromXmlIsActive) return true;
            if (Current.Game == null) return true;
            if (loadID == null) return true;

            if (loadID.StartsWith("Faction_"))
            {
                Loger.Log("LoadedObjectDirectory_ObjectWithLoadID_Patch " + loadID);
                var faction = Find.FactionManager.AllFactions.FirstOrDefault(f => f.GetUniqueLoadID() == loadID);
                if (faction != null)
                {
                    __result = faction;
                    return false;
                }
                return true;
            }
            return true;
        }
    }
    */
    /// ////////////////////////////////////////////////////////////

    //Выключаем настройки модов (в 1.3)
    [HarmonyPatch(typeof(CrossRefHandler))]
    [HarmonyPatch("ResolveAllCrossReferences")]
    public class CrossRefHandler_ResolveAllCrossReferences_Patch
    {

        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (!GameXMLUtils.FromXmlIsActive) return true;
            if (Current.Game == null) return true;

            if (Scribe.loader?.crossRefs?.crossReferencingExposables == null) return true;

            if (ThingEntry.crossReferencingExposables == null) ThingEntry.crossReferencingExposables = new List<IExposable>();

            Scribe.loader.crossRefs.crossReferencingExposables.AddRange(ThingEntry.crossReferencingExposables
                .Where(e => !Scribe.loader.crossRefs.crossReferencingExposables.Any(ee => ee == e))
                .ToList());

            ThingEntry.crossReferencingExposables = new List<IExposable>();

            return true;
        }

    }

    /// ////////////////////////////////////////////////////////////
    
    //Добавляем свою надпись справа внизу
    [HarmonyPatch(typeof(GlobalControlsUtility))]
    [HarmonyPatch("DoDate")]
    [StaticConstructorOnStartup] //добавлно только из-за раздражающего предупреждения о возможной ошибке 
    public class GlobalControlsUtility_DoDate_Patch
    {
        public static List<string> OutText = null;
        public static string TooltipText = null;
        public static Texture2D OutInLastLine = null;
        public static DateTime Update;

        [HarmonyPostfix]
        public static void Postfix(float leftX, float width, ref float curBaseY)
        {
            if (!SessionClient.Get.IsLogined) return;
            if (OutText == null || OutText.Count == 0) return;

            if ((DateTime.UtcNow - Update).TotalSeconds > 5)
            {
                OutText = null;
                TooltipText = null;
                OutInLastLine = null;
                return;
            }

            try
            {
                var outText = OutText;

                var height = 22 + 26 * (outText.Count - 1);

                Rect dateRect = new Rect(leftX, curBaseY - height, width, height);

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperRight;
                float num3 = outText.Aggregate(0f, (r, i) => Mathf.Max(Text.CalcSize(i).x, r)) + 7f;
                dateRect.xMin = dateRect.xMax - num3;
                if (Mouse.IsOver(dateRect))
                {
                    Widgets.DrawHighlight(dateRect);
                }
                GUI.BeginGroup(dateRect);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperRight;
                Rect rect = dateRect.AtZero();
                rect.xMax -= 7f;
                Rect rectText = rect;
                for (int i = 0; i < outText.Count; i++)
                {
                    if (i + 1 == outText.Count && OutInLastLine != null)
                    {
                        rectText = rect;
                        rect.width -= 26f;
                        rectText.x += rect.width - 2f;
                        rectText.y += 1;
                        rectText.width = 24f;
                        rectText.height = 24f;
                    }
                    Widgets.Label(rect, outText[i]);
                    rect.yMin += 26f;
                }
                if (OutInLastLine != null) GUI.DrawTexture(rectText, OutInLastLine);
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.EndGroup();

                if (TooltipText != null && Mouse.IsOver(dateRect))
                {
                    TooltipHandler.TipRegion(dateRect, new TipSignal(TooltipText, 5634323));
                }

                curBaseY -= dateRect.height;
            }
            catch
            { }
        }

    }

    /// ////////////////////////////////////////////////////////////

    //Фикс проблемы многопоточности, решение https://github.com/AantCoder/OnlineCity/issues/82
    [HarmonyPatch(typeof(PawnCapacitiesHandler))]
    [HarmonyPatch("GetLevel")]
    internal class PawnCapacitiesHandler_GetLevel_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(PawnCapacitiesHandler __instance)
        {
            Monitor.Enter(__instance);
            return true;
        }

        [HarmonyPostfix]
        public static void Postfix(PawnCapacitiesHandler __instance)
        {
            Monitor.Exit(__instance);
        }
    }

    /// ////////////////////////////////////////////////////////////

    //Фикс    
    [HarmonyPatch(typeof(GlobalTextureAtlasManager))]
    [HarmonyPatch("GlobalTextureAtlasManagerUpdate")]
    internal class GlobalTextureAtlasManager_GlobalTextureAtlasManagerUpdate_Patch
    {
        private static List<PawnTextureAtlas> pawnTextureAtlases;
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (pawnTextureAtlases == null)
            {
                var that = Traverse.Create(typeof(GlobalTextureAtlasManager));
                pawnTextureAtlases = that.Field("pawnTextureAtlases").GetValue<List<PawnTextureAtlas>>();
            }
            if (GlobalTextureAtlasManager.rebakeAtlas)
            {
                GlobalTextureAtlasManager.FreeAllRuntimeAtlases();
                PortraitsCache.Clear();
                GlobalTextureAtlasManager.rebakeAtlas = false;
            }
            foreach (PawnTextureAtlas pawnTextureAtlase in pawnTextureAtlases)
            {
                try
                {
                    pawnTextureAtlase.GC();
                }
                catch (Exception exp)
                {
                    var that = Traverse.Create(pawnTextureAtlase);
                    var _frameAssignments = that.Field("frameAssignments").GetValue<Dictionary<Pawn, PawnTextureAtlasFrameSet>>();
                    var _freeFrameSets = that.Field("freeFrameSets").GetValue<List<PawnTextureAtlasFrameSet>>();
                    if (_frameAssignments == null) Log.Message("_frameAssignments");
                    if (_freeFrameSets == null) Log.Message("_freeFrameSets");

                    var test = new Dictionary<Pawn, PawnTextureAtlasFrameSet>(_frameAssignments);
                    that.Field("frameAssignments").SetValue(test); //замена в игре через рефлексию (методы из гармони)

                    Log.Message("Exception " + exp.Message + "  Replace frameAssignments: "
                        + _frameAssignments.Keys.Aggregate("", (r, i) => r + Environment.NewLine + $"{i.LabelCap} hc{i.GetHashCode()} id{i.thingIDNumber}")
                        );
                }
            }
            return false;
        }
    }

    /*
    [HarmonyPatch(typeof(PawnTextureAtlas))]
    [HarmonyPatch("GC")]
    internal class PawnTextureAtlas_GC_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(PawnTextureAtlas __instance)
        {
            lock (Find.World)
            {

                var that = Traverse.Create(__instance);
                var _frameAssignments = that.Field("frameAssignments").GetValue<Dictionary<Pawn, PawnTextureAtlasFrameSet>>();
                var _freeFrameSets = that.Field("freeFrameSets").GetValue<List<PawnTextureAtlasFrameSet>>();
                if (_frameAssignments == null) Log.Message("_frameAssignments");
                if (_freeFrameSets == null) Log.Message("_freeFrameSets");

                var tmpPawnsToFree = new List<Pawn>();
                foreach (Pawn key in _frameAssignments.Keys)
                {
                    if (!key.SpawnedOrAnyParentSpawned)
                    {
                        tmpPawnsToFree.Add(key);
                    }
                }
                foreach (Pawn item in tmpPawnsToFree)
                {
                    try
                    {
                        _freeFrameSets.Add(_frameAssignments[item]);
                    }
                    catch (Exception exp)
                    {
                        var test = new Dictionary<Pawn, PawnTextureAtlasFrameSet>(_frameAssignments);
                        that.Field("frameAssignments").SetValue(test); //замена в игре через рефлексию (методы из гармони)
                        _freeFrameSets.Add(test[item]);

                        var th = test.Keys.FirstOrDefault(t => t.LabelCap == "Lighter, Служанка");
                        Log.Message("Exception " + exp.Message + "  " + $"{item.LabelCap} hc{item.GetHashCode()} id{item.thingIDNumber} "
                            + (test.ContainsKey(item) ? "yes " : "no ")
                            + (th == item ? "yes " : "no ")
                            + (item.Equals(th) ? "yes " : "no ")
                            + (object.ReferenceEquals(th, item) ? "yes " : "no ")
                            + Environment.NewLine + ". _frameAssignments: "
                            + _frameAssignments.Keys.Aggregate("", (r, i) => r + Environment.NewLine + $"{i.LabelCap} hc{i.GetHashCode()} id{i.thingIDNumber}")
                            + Environment.NewLine + ". test: "
                            + test.Keys.Aggregate("", (r, i) => r + Environment.NewLine + $"{i.LabelCap} hc{i.GetHashCode()} id{i.thingIDNumber}"));
                    }
                    _frameAssignments.Remove(item);
                }
            }
            return false;
        }
    }
    */
    /// ////////////////////////////////////////////////////////////

    //Кнопки торговли
    [HarmonyPatch(typeof(Settlement))]
    [HarmonyPatch("GetGizmos")]
    internal class Settlement_GetGizmos_Patch
    {
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Settlement __instance)
        {
            foreach (var value in values) yield return value;

            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "OCity_Dialog_Exchenge_Trade_Orders".Translate();
            command_Action.defaultDesc = "OCity_Dialog_Exchenge_Trade_Orders".Translate();
            command_Action.icon = GeneralTexture.TradeButtonIcon;
            command_Action.action = delegate
            {
                Find.WindowStack.Add(new Dialog_Exchenge(__instance));
            };
            yield return command_Action;
        }
    }

    [HarmonyPatch(typeof(Caravan))]
    [HarmonyPatch("GetGizmos")]
    internal class Caravan_GetGizmos_Patch
    {
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values,  Caravan __instance)
        {
            foreach (var value in values) yield return value;

            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "OCity_Dialog_Exchenge_Trade_Orders".Translate();
            command_Action.defaultDesc = "OCity_Dialog_Exchenge_Trade_Orders".Translate();
            command_Action.icon = GeneralTexture.TradeButtonIcon;
            command_Action.action = delegate
            {
                Find.WindowStack.Add(new Dialog_Exchenge(__instance));
            };
            yield return command_Action;
        }
    }

    /// ////////////////////////////////////////////////////////////
    /*
    //Отображение статуса онлайн в игре внизу справа
    [HarmonyPatch(typeof(GlobalControlsUtility))]
    [HarmonyPatch("DoDate")]
    internal class GlobalControlsUtility_DoDate_Patch
    {
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Settlement __instance)
        {
            foreach (var value in values) yield return value;

            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "OCity_Dialog_Exchenge_Trade_Orders".Translate();
            command_Action.defaultDesc = "OCity_Dialog_Exchenge_Trade_Orders".Translate();
            command_Action.icon = GeneralTexture.TradeButtonIcon;
            command_Action.action = delegate
            {
                Find.WindowStack.Add(new Dialog_Exchenge(__instance));
            };
            yield return command_Action;
        }
    }
    */
    /// ////////////////////////////////////////////////////////////
    /*
    [HarmonyPatch(typeof(Dialog_IdeoList))]
    [HarmonyPatch("ReloadFiles")]
    internal class Dialog_IdeoList_ReloadFiles_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Dialog_IdeoList __instance)
        {
            Loger.Log("Client Dialog_IdeoList 0");

            if (!SessionClient.Get.IsLogined) return true;
            if (Prefs.DevMode) return true; //чтобы разрешить тем, у кого есть право на админку

            Loger.Log("Client Dialog_IdeoList 1");
            __instance.Close(false);
            return false;
        }
    }
    */

    /// ////////////////////////////////////////////////////////////
    // Увеличение стоимости поселения на цену от вещей в онлайне

    //влияет на инцинденты
    [HarmonyPatch(typeof(Map))]
    [HarmonyPatch("PlayerWealthForStoryteller", MethodType.Getter)]
    internal class Map_PlayerWealthForStoryteller_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Map __instance, ref float __result)
        {
            if (Current.Game == null) return;
            if (!SessionClient.Get.IsLogined) return;

            if (__instance == null) return;
            if (MainTabWindow_DoStatisticsPage_Patch.PatchColonyWealth == null) return;
            if (!MainTabWindow_DoStatisticsPage_Patch.PatchColonyWealth.TryGetValue(__instance, out var wealth)) return;
            __result += wealth;
        }

    }

    //добавляет к выводимому кол-во общей стоимости
    [HarmonyPatch(typeof(WealthWatcher))]
    [HarmonyPatch("WealthTotal", MethodType.Getter)]
    internal class WealthWatcher_WealthTotal_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(WealthWatcher __instance, ref float __result)
        {
            if (Current.Game == null) return;
            if (!SessionClient.Get.IsLogined) return;

            var that = Traverse.Create(__instance);
            var _map = that.Field("map").GetValue<Map>();

            if (_map == null) return;
            if (MainTabWindow_DoStatisticsPage_Patch.PatchColonyWealth == null) return;
            if (!MainTabWindow_DoStatisticsPage_Patch.PatchColonyWealth.TryGetValue(_map, out var wealth)) return;
            __result += wealth;
        }
    }

    //Это просто вывод в UI стоимости
    [HarmonyPatch(typeof(MainTabWindow_History))]
    [HarmonyPatch("DoStatisticsPage")]
    internal class MainTabWindow_DoStatisticsPage_Patch
    {
        public static Dictionary<Map, float> PatchColonyWealth;

        public static string PatchInject1()
        {
            if (Current.Game == null) return "";
            if (!SessionClient.Get.IsLogined) return "";

            if (Find.CurrentMap == null) return "";
            if (PatchColonyWealth == null) return "";
            if (!PatchColonyWealth.TryGetValue(Find.CurrentMap, out var wealth)) return "";
            return "Online City: " + wealth.ToString("F0");
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> InjectCustomQuickstartSettings(IEnumerable<CodeInstruction> instructions)
        {
            var stringBuilderAppendLine = AccessTools.Method(typeof(StringBuilder), "AppendLine", new Type[] { typeof(string) });
            var mainTabWindow_DoStatisticsPage_Patch_PatchInject1 = AccessTools.Method(typeof(MainTabWindow_DoStatisticsPage_Patch), "PatchInject1");

            int state = 0;
            /// 1. Находим константу "ThisMapColonyWealthColonistsAndTameAnimals"
            /// 2. После находим конец оператора Ldloc_0
            /// 3. Вставляем код: StringBuilder.AppendLine(MainTabWindow_DoStatisticsPage_Patch.PatchInject1);
            var codes = new List<CodeInstruction>(instructions);
            foreach (var code in codes)
            {
                yield return code;
                if (state == 0 && code?.operand?.ToString() == "ThisMapColonyWealthColonistsAndTameAnimals") state = 1;
                if (state == 1 && code?.opcode == OpCodes.Ldloc_0)
                {
                    state = 2;
                    yield return new CodeInstruction(OpCodes.Call, mainTabWindow_DoStatisticsPage_Patch_PatchInject1); // ((Action)MainTabWindow_DoStatisticsPage_Patch.PatchInject1).Method);
                    yield return new CodeInstruction(OpCodes.Callvirt, stringBuilderAppendLine);
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                }
            }

            // //для анализа:
            //var codes = new List<CodeInstruction>(instructions);
            //foreach (var code in codes)
            //    Log.Error($"opcode={code.opcode}    operand={code.operand?.GetType().Name}={code.operand?.ToString()} blocks={code.blocks?.Count}");
            //foreach (var inst in instructions)
            //    yield return inst;
            // интерресное:https://gist.github.com/pardeike/c02e29f9e030e6a016422ca8a89eefc9

        }
    }

    /// ////////////////////////////////////////////////////////////
    /// 
    //Передача в торговый склад из капсулы, когда она приземляется, а товар теряется
    [HarmonyPatch(typeof(TravelingTransportPods))]
    [HarmonyPatch("DoArrivalAction")]
    internal class TravelingTransportPods_DoArrivalAction_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(TravelingTransportPods __instance)
        {
            if (Current.Game == null) return true;
            if (!SessionClient.Get.IsLogined) return true;

            if (__instance.arrivalAction != null) return true;
            if (__instance.destinationTile < 0) return true;

            Loger.Log($"Client TravelingTransportPods SaveGame and ExchengeStorage 1", Loger.LogLevel.EXCHANGE);

            var that = Traverse.Create(__instance);
            var pods = that.Field("pods").GetValue<List<ActiveDropPodInfo>>();

            var toTargetThing = new List<Thing>();
            for (int j = 0; j < pods.Count; j++)
            {
                for (int k = 0; k < pods[j].innerContainer.Count; k++)
                {
                    var thing = pods[j].innerContainer[k];
                    toTargetThing.Add(thing);
                }
            }

            //ниже блок передачи в яблоко на основе ExchengeUtils.MoveSelectThings

            //оставляем только то, что можно передать
            toTargetThing = toTargetThing.FilterBeforeSendServer().ToList();

            //var toTargetEntry = ExchengeUtils.CreateTradeAndDestroy(toTargetThing); //на основе этого, не без удаления, т.к. объекты удаляться в игровой функции после Prefix
            var toTargetEntry = toTargetThing.Select(t => ThingTrade.CreateTrade(t, t.stackCount)).ToList();

            Loger.Log($"Client TravelingTransportPods SaveGame and ExchengeStorage 2", Loger.LogLevel.EXCHANGE);
            //отправляем вещи toTargetEntry в красное яблоко
            //После передачи сохраняем, чтобы нельзя было обузить
            SessionClientController.SaveGameNowSingleAndCommandSafely(
                (connect) =>
                {
                    Loger.Log($"Client TravelingTransportPods SaveGame and ExchengeStorage 3", Loger.LogLevel.EXCHANGE);
                    return connect.ExchengeStorage(toTargetEntry, null, __instance.destinationTile);
                },
                () =>
                {
                    var msg = "Вещи переданы в Торговый склад".NeedTranslate();
                    Find.WindowStack.Add(new Dialog_Input("OCity_Dialog_Exchenge_Action_CarriedOut".Translate(), msg, true)); //"Выполнено"
                },
                null,
                false); //если не удалось отправить письмо, то жопа так как сейв уже прошел

            return true;
        }
    }    

    /// ////////////////////////////////////////////////////////////
}
