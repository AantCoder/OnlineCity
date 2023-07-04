using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Model;
using System.Reflection;
using System.Xml;
using OCUnion;
using RimWorld.Planet;
using UnityEngine;
using OCUnion.Transfer.Model;
using System.Threading;
using HarmonyLib;
using RimWorldOnlineCity.GameClasses.Harmony;
using Util;
using System.IO;
using System.Diagnostics;

namespace RimWorldOnlineCity
{
    public static class ScribeSaverHelper
    {
        public static XmlWriter GetWriter(this ScribeSaver scribeSaver)
        {
            FieldInfo fieldInfo = typeof(ScribeSaver).GetField("writer", BindingFlags.Instance | BindingFlags.NonPublic);
            XmlWriter result = (XmlWriter)fieldInfo.GetValue(scribeSaver);
            return result;
        }

        public static void SetWriter(this ScribeSaver scribeSaver, XmlWriter writer)
        {
            FieldInfo fieldInfo = typeof(ScribeSaver).GetField("writer", BindingFlags.Instance | BindingFlags.NonPublic);
            fieldInfo.SetValue(scribeSaver, writer);
        }
    }

    [StaticConstructorOnStartup]
    public static class GameUtils
    {
        internal static readonly Texture2D CircleFill = ContentFinder<Texture2D>.Get("circle-fill");

        private static int BugNum = 0;
        public static void GetBug()
        {
            var dir = Loger.PathLog.Substring(0, Loger.PathLog.Length - 1);
            var fileName = $"Log_{DateTime.Now.ToString("yyyy-MM-dd")}_*.txt";
            var list = Directory.GetFiles(dir, fileName, SearchOption.TopDirectoryOnly);
            var dataToSave = GZip.ZipMoreByteByte(list, name => File.ReadAllBytes(name.Replace("\\", "" + Path.DirectorySeparatorChar)));
            var code = $"{DateTime.Now.ToString("yyyy-MM-dd")}_{MainHelper.LockCode}_{BugNum++}";
            var dataDir = Path.Combine(dir, "Log_" + code);
            Directory.CreateDirectory(dataDir);
            File.WriteAllBytes(Path.Combine(dataDir, $"BugReport_{code}.zip"), dataToSave);
            Process.Start(dataDir);
        }

        public static Texture2D GetTextureFromSaveData(byte[] data)
        {
            Texture2D texture = new Texture2D(2, 2);//, TextureFormat.BGRA32, false);
            texture.LoadImage(data);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Иконка вещи, опционально "i". Запускается в произвольном Rect
        /// </summary>
        public static void DravLineThing(Rect rect, ThingTrade thing, bool withInfo)
        {
            DravLineThing(rect, thing, withInfo, Color.white);
        }
        /// <summary>
        /// Иконка вещи, опционально "i". Запускается в произвольном Rect
        /// </summary>
        public static void DravLineThing(Rect rect, ThingTrade thing, bool withInfo, Color labelColor, float xi = 24f, float yi = 0)
        {
            if (/*thing.IsCorpse ||*/ (thing.Def?.race?.Humanlike ?? false))
            {
                var position = new Rect(rect.x, rect.y, 24f, 24f);
                GUI.DrawTexture(position, GeneralTexture.IconHuman);
            }
            else
            {
                Widgets.ThingIcon(rect, thing.Def);
            }
            if (string.IsNullOrEmpty(thing.StuffName))
            {
                TooltipHandler.TipRegion(rect, thing.Def.LabelCap);
                GUI.color = labelColor;
                if (withInfo) Widgets.InfoCardButton(rect.x + xi, rect.y + yi, thing.Def);
                GUI.color = Color.white;
            }
            else
            {
                TooltipHandler.TipRegion(rect, thing.Def.LabelCap + "OCity_GameUtils_From".Translate() + thing.StuffDef.LabelAsStuff);
                GUI.color = labelColor;
                if (withInfo) Widgets.InfoCardButton(rect.x + xi, rect.y + yi, thing.Def, thing.StuffDef);
                GUI.color = Color.white;
            }
            // GenLabel.ThingLabel(this.Def, this.StuffDef, 1)
        }
        public static void DravLineThing(Rect rect, Thing thing, bool withInfo, float xi = 24f, float yi = 0)
        {
            if (thing == null) return;
            if (thing is Corpse) Loger.Log("DravLineThing is Corpse");
            Widgets.ThingIcon(rect, thing);
            if (withInfo) Widgets.InfoCardButton(rect.x + xi, rect.y + yi, thing);

            var localThing = thing;
            TooltipHandler.TipRegion(rect, new TipSignal(delegate
            {
                string text = localThing.LabelCapNoCount;
                string tipDescription = localThing.DescriptionFlavor;
                if (!tipDescription.NullOrEmpty())
                {
                    text = text + ": " + tipDescription;
                }
                return text;
            }, localThing.GetHashCode()));
        }
        public static void DravLineThing(Rect rect, ThingDef thing, bool withInfo)
        {
            if (thing == null) return;
            Widgets.ThingIcon(rect, thing);
            if (withInfo) Widgets.InfoCardButton(rect.x + 24f, rect.y, thing);

            var localThing = thing;
            TooltipHandler.TipRegion(rect, new TipSignal(delegate
            {
                string text = localThing.LabelCap;
                string tipDescription = localThing.DescriptionDetailed;
                if (!tipDescription.NullOrEmpty())
                {
                    text = text + ": " + tipDescription;
                }
                return text;
            }, localThing.GetHashCode()));
        }

        /// <summary>
        /// Иконка вещи, "i" и название. Должно запускаться в относительных коор
        /// </summary>
        /// <param name="rectLine"></param>
        /// <param name="thing"></param>
        /// <param name="labelColor"></param>
        public static void DravLineThing(Rect rectLine, Thing thing, Color labelColor)
        {
            //Rect rect = new Rect(-1f, -1f, 27f, 27f);
            Rect rect = new Rect(0f, 0f, 24f, 24f);

            Widgets.ThingIcon(rect, thing, 1f);

            Widgets.InfoCardButton(30f, 0f, thing);

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Tiny;
            Rect rect2 = new Rect(55f, 0f, rectLine.width - 55f, rectLine.height);
            Text.WordWrap = false;
            GUI.color = labelColor;
            Widgets.Label(rect2, thing.LabelCapNoCount);
            Text.WordWrap = true;
            GenUI.ResetLabelAlign();
            GUI.color = Color.white;

            var localThing = thing;
            TooltipHandler.TipRegion(rectLine, new TipSignal(delegate
            {
                string text = localThing.LabelCapNoCount;
                string tipDescription = localThing.DescriptionFlavor;
                if (!tipDescription.NullOrEmpty())
                {
                    text = text + ": " + tipDescription;
                }
                return text;
            }, localThing.GetHashCode()));
        }

        public static List<WorldObject> GetAllWorldObjects()
        {
            var allWorldObjectsArr = new WorldObject[Find.WorldObjects.AllWorldObjects.Count];
            Find.WorldObjects.AllWorldObjects.CopyTo(allWorldObjectsArr);

            return allWorldObjectsArr.Where(wo => wo != null).ToList();
        }

        /// <summary>
        /// Объединяет одинаковые вещи в список внутри одного контейнера TransferableOneWay
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public static List<TransferableOneWay> DistinctToTransferableOneWays(this IEnumerable<Thing> things)
        {
            var transferables = new List<TransferableOneWay>();
            foreach (var item in things)
            {
                TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching<TransferableOneWay>(item, transferables, TransferAsOneMode.Normal);
                if (transferableOneWay == null)
                {
                    transferableOneWay = new TransferableOneWay();
                    transferables.Add(transferableOneWay);
                }
                transferableOneWay.things.Add(item);
            }
            return transferables.Where(t => t.MaxCount > 0).ToList();
        }

        /// <summary>
        /// Разворачивает список из (вид вещи (внутри список конкретных вещей), кол-во выбора) до словаря (конкретная вещь, кол-во)
        /// Операция обратная DistinctThings, с сохранение кол-во выбранных (CountToTransfer)
        /// </summary>
        /// <returns></returns>
        public static Dictionary<Thing, int> TransferableOneWaysToDictionary(this IEnumerable<TransferableOneWay> selectByGroup, bool seletAll = false)
        {
            return selectByGroup.SelectMany(tow =>
            {
                var needCount = seletAll ? tow.MaxCount : tow.CountToTransfer;
                if (needCount == 0)
                {
                    return new List<Pair<Thing, int>>();
                }
                if (tow.AnyThing is Pawn)
                {
                    //Loger.Log(" ======== " + tow.AnyThing.LabelCap);
                    return new List<Pair<Thing, int>>() { new Pair<Thing, int>(tow.AnyThing, 1) };
                }
                var res = new List<Pair<Thing, int>>();
                for (int i = 0; i < tow.things.Count; i++)
                {
                    var cnt = tow.things[i].stackCount;
                    if (needCount > cnt)
                    {
                        //Loger.Log(" ======== " + tow.things[i].LabelCap + " === " + cnt);
                        res.Add(new Pair<Thing, int>(tow.things[i], cnt));
                    }
                    else
                    {
                        //Loger.Log(" ======== " + tow.things[i].LabelCap + " === " + needCount);
                        res.Add(new Pair<Thing, int>(tow.things[i], needCount));
                    }
                    needCount -= cnt;
                    if (needCount <= 0) break;
                }
                return res;
            }).ToDictionary(p => p.First, p => p.Second);
        }

        /// <summary>
        /// Возвращает набор вещей из allThings выбранных в targets или null, если чего то не хватает.
        /// Выставляет target.NotTrade
        /// </summary>
        /// <param name="targets">Искомые вещи или фильтры из ордера</param>
        /// <param name="allThings">Все доступные вещи</param>s
        /// <returns></returns>
        public static List<TransferableOneWay> ChechToTrade(IEnumerable<ThingTrade> targets, IEnumerable<Thing> allThings)
        {
            //сортируем цели для выборки более плохих вначале
            var rate = 1;//Максимальное кол-во повторов, уменьшается для каждой следующей вещи. Расчет на то, что вещи не конкурируют между собой или конкурируют незначительно
            //первый запуск для выяснения rate
            return ChechToTradeDo(targets, allThings, null, ref rate, false);

        }
        /// <summary>
        /// Возвращает набор вещей из allThings выбранных в targets или null, если чего то не хватает.
        /// Выставляет target.NotTrade
        /// </summary>
        /// <param name="targets">Искомые вещи или фильтры из ордера</param>
        /// <param name="allThings">Все доступные вещи</param>
        /// <param name="altThings">Дополнительные вещи, которые учавствуют в отборе если на позицию в allThings не хватило. Если задано, то в результате только часть из этого набора</param>
        /// <param name="rate">Кол-во повтором этой сделки</param>
        /// <param name="incomplete">Если не 0, то указано желателное rate, но если каких-то вещей нет, то результат всё равно будет выдан, и только недостающих не будет в результате</param>
        /// <returns></returns>
        public static List<TransferableOneWay> ChechToTrade(IEnumerable<ThingTrade> targets, IEnumerable<Thing> allThings, IEnumerable<Thing> altThings, out int rate, int incomplete = 0)
        {
            if (MainHelper.DebugMode) Loger.Log("GameUtils.ChechToTrade "
                + "targets: " + targets.ToList().ToStringLabel() + Environment.NewLine
                + "allThings: " + allThings.Select(t => ThingTrade.CreateTrade(t, t.stackCount)).ToList().ToStringLabel() + Environment.NewLine
                );
            
            //сортируем цели для выборки более хорошие вначале, а вещи сначала более плохие
            var trs = targets.OrderByCost();
                //.Where(t => t.Count > 0)
                //.OrderBy(t => t.DefName + "#" + (9 - t.Quality).ToString() + t.HitPoints.ToString().PadLeft(5) + t.Count.ToString().PadLeft(6))
                //.ToList();
            if (incomplete == 0)
            {
                rate = 100000000;//Максимальное кол-во повторов, уменьшается для каждой следующей вещи. Расчет на то, что вещи не конкурируют между собой или конкурируют незначительно
                //первый запуск для выяснения rate
                var res = ChechToTradeDo(trs, allThings, altThings, ref rate, true);
                if (res == null) return null;
                //повторно запускаем для корректного заполнения CountToTransfer (т.к. при уменьшении rate не перерасчитываются вещи отобранные ранее)
                return ChechToTradeDo(trs, allThings, altThings, ref rate, false);
            }
            else
            {
                rate = incomplete; //отличие от блока выше 1
                //первый запуск для выяснения rate
                var res = ChechToTradeDo(trs, allThings, altThings, ref rate, true);
                if (res == null)
                {   //отличие от блока выше 2
                    rate = incomplete;
                    return ChechToTradeDo(trs, allThings, altThings, ref rate, false, true);
                }    
                //повторно запускаем для корректного заполнения CountToTransfer (т.к. при уменьшении rate не перерасчитываются вещи отобранные ранее)
                return ChechToTradeDo(trs, allThings, altThings, ref rate, false);
            }
        }
        private static List<TransferableOneWay> ChechToTradeDo(IEnumerable<ThingTrade> targets, IEnumerable<Thing> allThings, IEnumerable<Thing> altThings, ref int rate, bool setRect, bool incomplete = false
            , bool setTradeCount = true)
        {
            bool result = true;
            var selects = new List<TransferableOneWay>();
            var source = allThings.ToDictionary(i => i, i => i.stackCount);
            //сортируем вещи сначала более плохие
            var sourceKeys = source.Keys
                .Select(t =>
                {
                    QualityCategory qq;
                    QualityUtility.TryGetQuality(t, out qq);
                    return new { thing = t, q = qq };
                })
                .OrderBy(t => t.thing.def.defName + "#" + ((int)t.q).ToString() + (10000 - t.thing.HitPoints).ToString() + t.thing.stackCount.ToString().PadLeft(6))
                .Select(t => t.thing)
                .ToList();
            //копия для alt
            var sourcealt = altThings?.ToDictionary(i => i, i => i.stackCount);
            //сортируем вещи сначала более плохие
            var sourcealtKeys = sourcealt?.Keys
                .Select(t =>
                {
                    QualityCategory qq;
                    QualityUtility.TryGetQuality(t, out qq);
                    return new { thing = t, q = qq };
                })
                .OrderBy(t => t.thing.def.defName + "#" + ((int)t.q).ToString() + (10000 - t.thing.HitPoints).ToString() + t.thing.stackCount.ToString().PadLeft(6))
                .Select(t => t.thing)
                .ToList();
            foreach (var target in targets)
            {
                target.TradeCount = 0;
                if (target.Count == 0)
                {
                    target.NotTrade = false; //где NotTrade истина, там будет красная строка в интерфейсе
                    target.TradeCount = 0; 
                    continue;
                }
                if (MainHelper.DebugMode) Log.Message("--- --- " + target.DefName.ToString() + " " + target.Count.ToString() + "*" + rate.ToString());
                if (setRect && target.Count > 100 && rate > 1000000) rate = 1000000; //от переполнения
                var select = new TransferableOneWay();
                var selectalt = new TransferableOneWay();
                //Log.Message(target.DefName);
                foreach (var thing in sourceKeys)
                {
                    if (!setTradeCount && target.Count <= select.CountToTransfer) break;
                    if (source[thing] == 0) continue;
                    if (target.MatchesThing(thing))
                    {
                        //нам подходит выбираем нужное кол-во
                        target.TradeCount += source[thing];
                        if (target.Count <= select.CountToTransfer) continue;
                        select.things.Add(thing);
                        if (MainHelper.DebugMode) Log.Message("---s T " + (source[thing]).ToString());
                        var count = target.Count * rate - select.CountToTransfer > source[thing]
                            ? source[thing]
                            : target.Count * rate - select.CountToTransfer;
                        if (MainHelper.DebugMode) Log.Message("---s + " + (select.CountToTransfer + count).ToString());
                        select.ForceTo(select.CountToTransfer + count);
                        source[thing] -= count;
                        //Log.Message(target.DefName + " == " + thing.def.defName + " o:" + source[thing].ToString() + " g:" + select.CountToTransfer.ToString() + " rate:" + rate.ToString());
                    }
                    //else Log.Message(target.DefName + " != " + thing.def.defName + " " + select.CountToTransfer.ToString());
                }
                //копия для alt + запись в selectalt
                if (altThings != null)
                {
                    foreach (var thing in sourcealtKeys)
                    {
                        if (!setTradeCount && target.Count <= select.CountToTransfer) break;
                        if (sourcealt[thing] == 0) continue;
                        if (target.MatchesThing(thing))
                        {
                            //нам подходит выбираем нужное кол-во
                            target.TradeCount += sourcealt[thing];
                            if (target.Count <= select.CountToTransfer) continue;
                            select.things.Add(thing);
                            if (MainHelper.DebugMode) Log.Message("---a T " + (sourcealt[thing]).ToString());
                            var count = target.Count * rate - select.CountToTransfer > sourcealt[thing]
                                ? sourcealt[thing]
                                : target.Count * rate - select.CountToTransfer;
                            select.ForceTo(select.CountToTransfer + count);
                            if (MainHelper.DebugMode) Log.Message("---a + " + (select.CountToTransfer + count).ToString());
                            sourcealt[thing] -= count;
                            //дополнительно записываем в selectalt
                            selectalt.things.Add(thing);
                            selectalt.ForceTo(selectalt.CountToTransfer + count);
                        }
                    }
                }
                if (!incomplete
                    && target.Count * (setRect ? 1 : rate) > select.CountToTransfer)
                {
                    result = false;
                    target.NotTrade = true;
                    if (MainHelper.DebugMode) Log.Message("---NotTrade " + target.Count.ToString() + " > " + select.CountToTransfer.ToString());
                }
                else
                {
                    if (setRect && target.Count * rate > select.CountToTransfer)
                    {
                        rate = select.CountToTransfer / target.Count;
                        //Log.Message(" rate:" + rate.ToString());
                    }
                    if (altThings == null)
                        selects.Add(select);
                    else
                        selects.Add(selectalt);
                    target.NotTrade = false;
                    if (MainHelper.DebugMode) Log.Message("---Trade " + target.Count.ToString() + " > " + select.CountToTransfer.ToString());
                }
            }
            return result ? selects : null;
        }
        private static List<TransferableOneWay> ChechToTradeNNN(IEnumerable<ThingTrade> targets, IEnumerable<Thing> allThings, IEnumerable<Thing> altThings, bool setTradeCount = true)
        {
            bool result = true;
            var selects = new List<TransferableOneWay>();
            var source = allThings.ToDictionary(i => i, i => i.stackCount);
            //сортируем вещи сначала более плохие
            var sourceKeys = source.Keys
                .Select(t =>
                {
                    QualityCategory qq;
                    QualityUtility.TryGetQuality(t, out qq);
                    return new { thing = t, q = qq };
                })
                .OrderBy(t => t.thing.def.defName + "#" + ((int)t.q).ToString() + (10000 - t.thing.HitPoints).ToString() + t.thing.stackCount.ToString().PadLeft(6))
                .Select(t => t.thing)
                .ToList();
            //копия для alt
            var sourcealt = altThings?.ToDictionary(i => i, i => i.stackCount);
            //сортируем вещи сначала более плохие
            var sourcealtKeys = sourcealt?.Keys
                .Select(t =>
                {
                    QualityCategory qq;
                    QualityUtility.TryGetQuality(t, out qq);
                    return new { thing = t, q = qq };
                })
                .OrderBy(t => t.thing.def.defName + "#" + ((int)t.q).ToString() + (10000 - t.thing.HitPoints).ToString() + t.thing.stackCount.ToString().PadLeft(6))
                .Select(t => t.thing)
                .ToList();
            foreach (var target in targets)
            {
                target.TradeCount = 0;
                if (target.Count == 0)
                {
                    target.NotTrade = false; //где NotTrade истина, там будет красная строка в интерфейсе
                    continue;
                }
                Log.Message("--- --- " + target.DefName.ToString() + " " + target.Count.ToString());
                var select = new TransferableOneWay();
                var selectalt = new TransferableOneWay();
                //Log.Message(target.DefName);
                foreach (var thing in sourceKeys)
                {
                    if (!setTradeCount && target.Count <= select.CountToTransfer) break;
                    if (source[thing] == 0) continue;
                    if (target.MatchesThing(thing))
                    {
                        //нам подходит выбираем нужное кол-во
                        target.TradeCount += source[thing];
                        if (target.Count <= select.CountToTransfer) continue;
                        select.things.Add(thing);
                        Log.Message("---s T " + (source[thing]).ToString());
                        var count = target.Count - select.CountToTransfer > source[thing]
                            ? source[thing]
                            : target.Count - select.CountToTransfer;
                        Log.Message("---s + " + (select.CountToTransfer + count).ToString());
                        select.ForceTo(select.CountToTransfer + count);
                        source[thing] -= count;
                        //Log.Message(target.DefName + " == " + thing.def.defName + " o:" + source[thing].ToString() + " g:" + select.CountToTransfer.ToString() + " rate:" + rate.ToString());
                    }
                    //else Log.Message(target.DefName + " != " + thing.def.defName + " " + select.CountToTransfer.ToString());
                }
                //копия для alt + запись в selectalt
                if (altThings != null)
                {
                    foreach (var thing in sourcealtKeys)
                    {
                        if (!setTradeCount && target.Count <= select.CountToTransfer) break;
                        if (sourcealt[thing] == 0) continue;
                        if (target.MatchesThing(thing))
                        {
                            //нам подходит выбираем нужное кол-во
                            target.TradeCount += sourcealt[thing];
                            if (target.Count <= select.CountToTransfer) continue;
                            select.things.Add(thing);
                            Log.Message("---a T " + (sourcealt[thing]).ToString());
                            var count = target.Count - select.CountToTransfer > sourcealt[thing]
                                ? sourcealt[thing]
                                : target.Count - select.CountToTransfer;
                            select.ForceTo(select.CountToTransfer + count);
                            Log.Message("---a + " + (select.CountToTransfer + count).ToString());
                            sourcealt[thing] -= count;
                            //дополнительно записываем в selectalt
                            selectalt.things.Add(thing);
                            selectalt.ForceTo(selectalt.CountToTransfer + count);
                        }
                    }
                }
                if (target.Count > select.CountToTransfer)
                {
                    result = false;
                    target.NotTrade = true;
                    Log.Message("---NotTrade " + target.Count.ToString() + " > " + select.CountToTransfer.ToString());
                }
                else
                {
                    if (altThings == null)
                        selects.Add(select);
                    else
                        selects.Add(selectalt);
                    target.NotTrade = false;
                    Log.Message("---Trade " + target.Count.ToString() + " > " + select.CountToTransfer.ToString());
                }
            }
            return result ? selects : null;
        }

        public static bool IsProtectingNovice()
        {
            if (SessionClientController.Data.IsAdmin || !SessionClientController.Data.ProtectingNovice) return false;
            
            var costAll = SessionClientController.Data.MyEx.CostAllWorldObjects();
            return SessionClientController.My.LastTick < 3600000 / 2 || costAll.MarketValueTotal < MainHelper.MinCostForTrade;
        }

        internal static IEnumerable<Thing> FilterBeforeSendServer(this IEnumerable<Thing> list)
        {
            //для идеалогии запрещаем передачу пешек, которые имеют идеологическую роль лидер или проповедника
            var rolesListForReading = Find.FactionManager.OfPlayer.ideos.PrimaryIdeo.RolesListForReading
                .Where(r => r.def.defName == "IdeoRole_Leader" || r.def.defName == "IdeoRole_Moralist")
                .ToList();
            if (MainHelper.DebugMode) foreach (var r in Find.FactionManager.OfPlayer.ideos.PrimaryIdeo.RolesListForReading) Loger.Log(" Role " + r.def.defName + " " + r.TipLabel);

            var res = list.Where(thing => !(thing is Pawn) || !rolesListForReading.Any(r => r.IsAssigned(thing as Pawn)))
                //Запрет на передачу трупов
                .Where(thing => !(thing is Corpse))
                //Запрет на передачу мешков с отходами Wastepack
                .Where(thing => thing.def.defName != "Wastepack")
                //Запрещенные настройкой
                .Where(thing => !SessionClientController.Data.GeneralSettings.ExchengeForbiddenDefNamesList.Contains(thing.def.defName));
            
            if (IsProtectingNovice()) res = res.Where(thing => thing.def.stackLimit > 1);
            return res;
        }

        public static List<Thing> GetAllThings(Caravan caravan, bool thingOnPawn = false, bool withTransferFilter = true)
        {
            IEnumerable<Thing> pawns = caravan.PawnsListForReading;
            if (withTransferFilter) pawns = pawns.FilterBeforeSendServer();
            IEnumerable<Thing> goods;
            if (thingOnPawn)
            {
                goods = GetThingOnPawn(pawns).ToList()
                    .Concat(pawns);
            }
            else
            {
                goods = CaravanInventoryUtility.AllInventoryItems(caravan).ToList()
                    .Concat(pawns);
            }
            if (withTransferFilter) goods = goods.FilterBeforeSendServer();
            return goods.ToList();
        }

        public static List<Thing> GetAllThings(Map map, bool thingOnPawn = false, bool withTransferFilter = true)
        {
            IEnumerable<Thing> pawns = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
            if (withTransferFilter) pawns = pawns.FilterBeforeSendServer();
            var goods = CaravanFormingUtility.AllReachableColonyItems(map, allowEvenIfReserved: true).ToList()
                .Concat(pawns);
            if (thingOnPawn)
            {
                goods = goods.Concat(GetThingOnPawn(pawns));
            }
            return goods.ToList();
        }

        /// <summary>
        /// Вытащить все вещи у пешки, из самых хитрых мест (оружие, одежда, инвентарь и то, что в руках)
        /// </summary>
        private static IEnumerable<Thing> GetThingOnPawn(IEnumerable<Thing> pawns)
        {
            return pawns.Cast<Pawn>().SelectMany(p =>
                    p.EquippedWornOrInventoryThings
                    .Concat(
                        p.carryTracker != null && p.carryTracker.CarriedThing != null && p.carryTracker.CarriedThing.def.category != ThingCategory.Pawn
                        ? new Thing[] { p.carryTracker.CarriedThing } : new Thing[0]
                    ));
        }

        public static List<Thing> GetAllThings(TradeThingsOnline storage)
        {
            using (var gameError = GameUtils.NormalGameError())
            {
                return storage.TradeThings.Things.Select(t => t.CreateThing()).ToList();
            }
        }

        public static CatchGameError NormalGameError() => new CatchGameError(errorText => 
            !errorText.Contains("during LoadingVars. pathRelToParent=/leader, parent")
            && !errorText.Contains("PostLoadInit on RimWorld.Pawn_IdeoTracker: System.NullReferenceException")
            );

        public static void ShortSetupForQuickTestPlay()
        {
            //частичная копия
            Current.Game = new Game();
            Current.Game.InitData = new GameInitData();
            Current.Game.Scenario = ScenarioDefOf.Crashlanded.scenario;
            Find.Scenario.PreConfigure();
            Current.Game.storyteller = new Storyteller(StorytellerDefOf.Cassandra, DifficultyDefOf.Rough);
            Current.Game.World = WorldGenerator.GenerateWorld(
                0.05f,
                GenText.RandomSeedString(),
                OverallRainfall.Normal,
                OverallTemperature.Normal,
                OverallPopulation.Little
                );
        }

        /// <summary>
        /// Куски с SpawnSetup на карту, не относящиеся к карте
        /// </summary>
        /// <param name="pawn"></param>
        public static void SpawnSetupOnCaravan(Pawn pawn)
        {
            /*
            if (Find.TickManager != null)
            {
                Find.TickManager.RegisterAllTickabilityFor(pawn);
            }
            StealAIDebugDrawer.Notify_ThingChanged(pawn);
            if (pawn is IThingHolder && Find.ColonistBar != null)
            {
                Find.ColonistBar.MarkColonistsDirty();
            }
            if (pawn.def.receivesSignals)
            {
                Find.SignalManager.RegisterReceiver(pawn);
            }
            */
            /*if (pawn.Faction != Faction.OfPlayer)
                pawn.SetFaction(Faction.OfPlayer);*/
            if (!pawn.IsWorldPawn())
            {
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
            }
        }

        public static void DeSpawnSetupOnCaravan(Caravan caravan, Pawn pawn)
        {
            caravan.PawnsListForReading.Remove(pawn);
            if (pawn is IThingHolder && Find.ColonistBar != null)
            {
                Find.ColonistBar.MarkColonistsDirty();
            }
            /*
            if (Find.TickManager != null)
            {
                Find.TickManager.RegisterAllTickabilityFor(pawn);
            }
            StealAIDebugDrawer.Notify_ThingChanged(pawn);
            if (pawn is IThingHolder && Find.ColonistBar != null)
            {
                Find.ColonistBar.MarkColonistsDirty();
            }
            if (pawn.def.receivesSignals)
            {
                Find.SignalManager.RegisterReceiver(pawn);
            }
            if (pawn.IsWorldPawn())
            {
                Find.WorldPawns.RemovePawn(pawn);
            }
            */
        }

        /// <summary>
        /// Получаем координаты ячейки куда сгружать груз для указанной карты.
        /// Ячейка - центр склада. Выбирается склад как лучший по:
        /// имеет в названии слово "торг" или "trad", не свалка с именем поумолчанию, самый большой, по названию
        /// </summary>
        public static IntVec3 GetTradeCell(Map map)
        {
            //название свалки по умолчанию
            var labelDumping = "DumpingStockpile".Translate();
            var labelDumping2 = "DumpingStockpileLabel".Translate();

            Zone zone = map.zoneManager.AllZones
                .OrderBy(z =>
                    (z.label.IndexOf("торг", StringComparison.OrdinalIgnoreCase) >= 0
                    || z.label.IndexOf("trad", StringComparison.OrdinalIgnoreCase) >= 0
                    ? "0" : "1")
                    //пробуем выбирать не свалку
                    + (z.label.IndexOf(labelDumping, StringComparison.OrdinalIgnoreCase) == 0
                    || z.label.IndexOf(labelDumping2, StringComparison.OrdinalIgnoreCase) == 0
                    ? "1" : "0")
                    + (100000000 - z.Cells.Count).ToString().PadLeft(10)
                    + z.label)
                .FirstOrDefault();
            if (zone == null) return map.Center;

            var res = zone.Cells.Aggregate(new IntVec3(), (a, i) => { a.x += i.x; a.z += i.z; return a; });
            res.x /= zone.Cells.Count;
            res.z /= zone.Cells.Count;
            return res;
        }

        public static Func<IntVec3> GetAttackCells(Map map)
        {
            IntVec3 enterCell = FindNearEdgeCell(map, null);
            return () => CellFinder.RandomSpawnCellForPawnNear(enterCell, map, 4);
        }

        /// <summary>
        /// CaravanEnterMapUtility.FindNearEdgeCell
        /// </summary>
        private static IntVec3 FindNearEdgeCell(Map map, Predicate<IntVec3> extraCellValidator)
        {
            Predicate<IntVec3> baseValidator = (IntVec3 x) => x.Standable(map) && !x.Fogged(map);
            Faction hostFaction = map.ParentFaction;
            if (CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => baseValidator(x) && (extraCellValidator == null || extraCellValidator(x)) && ((hostFaction != null && map.reachability.CanReachFactionBase(x, hostFaction)) || (hostFaction == null && map.reachability.CanReachBiggestMapEdgeDistrict(x))), map, CellFinder.EdgeRoadChance_Neutral, out var result))
            {
                return CellFinder.RandomClosewalkCellNear(result, map, 5);
            }
            if (extraCellValidator != null && CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => baseValidator(x) && extraCellValidator(x), map, CellFinder.EdgeRoadChance_Neutral, out result))
            {
                return CellFinder.RandomClosewalkCellNear(result, map, 5);
            }
            if (CellFinder.TryFindRandomEdgeCellWith(baseValidator, map, CellFinder.EdgeRoadChance_Neutral, out result))
            {
                return CellFinder.RandomClosewalkCellNear(result, map, 5);
            }
            Log.Warning("Could not find any valid edge cell.");
            return CellFinder.RandomCell(map);
        }

        public static IntVec3 SpawnCaravanPirate(Map map, List<ThingEntry> pawns, Action<Thing, ThingEntry> spawn = null)
        {
            var nextCell = GameUtils.GetAttackCells(map);
            return SpawnList(map, pawns, true, (p) => true, spawn, (p) => nextCell());
        }

        //используется только в ПВП
        public static IntVec3 SpawnList<TE>(Map map, List<TE> pawns, bool attackCell
            , Func<TE, bool> getPirate
            , Action<Thing, TE> spawn = null
            , Func<Thing, IntVec3> getCell = null)
            where TE : ThingEntry
        {
            if (MainHelper.DebugMode) Loger.Log("SpawnList...");

            //на основе UpdateWorldController.DropToWorldObjectDo
            var factionPirate = Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.defName == "Pirate")
                    ?? Find.FactionManager.OfAncientsHostile; //SessionClientController.Data.FactionPirate;

            IntVec3 ret = new IntVec3();
            ModBaseData.RunMainThreadSync(() =>
            {
                Thing thinXZ;
                for (int i = 0; i < pawns.Count; i++)
                {
                    var thing = pawns[i];
                    //GenSpawn.Spawn(pawn, cell, map, Rot4.Random, WipeMode.Vanish, false);

                    if (getPirate(thing)) thing.Affiliation = PawnAffiliation.Enemy;
                    if (MainHelper.DebugMode) Loger.Log("Prepare... " + thing.Affiliation.ToString());
                    var thin = thing.CreateThing();

                    var cell = getCell != null ? getCell(thin) : thin.Position;
                    if (i == 0) ret = cell;

                    //if (MainHelper.DebugMode) 
                    try
                    {
                        Loger.Log("Spawn... " + thin.Label);
                    }
                    catch
                    {
                        Loger.Log("Spawn... ");
                    }
                    if (thin is Pawn)
                    {
                        if (MainHelper.DebugMode) Loger.Log("Pawn... " + thin.Position.x + " " + thin.Position.y);
                        try
                        {
                            GenSpawn.Spawn((Pawn)thin, cell, map);
                        }
                        catch (Exception exp)
                        {
                            Loger.Log("SpawnList Exception " + thing.Name + ": " + exp.ToString(), Loger.LogLevel.ERROR);
                            Thread.Sleep(5);
                            GenSpawn.Spawn((Pawn)thin, cell, map);
                        }
                    }
                    else
                        GenDrop.TryDropSpawn(thin, cell, map, ThingPlaceMode.Near, out thinXZ, null);
                    if (spawn != null) spawn(thin, thing);
                    if (MainHelper.DebugMode) Loger.Log("Spawn...OK");
                }

            });
            return ret;
        }

        public static void PawnDestroy(Pawn pawn)
        {
            pawn.Destroy(DestroyMode.Vanish);
            Find.WorldPawns.RemovePawn(pawn); //не проверенное полное удаление, чтобы не появлялось клонов пешки после возврата её назад
        }

        public static void ApplyState(Thing thing, AttackThingState state, bool pawnHealthStateDead = false)
        {
            //полезное из игры: RecoverFromUnwalkablePositionOrKill
            if (state.StackCount > 0 && thing.stackCount != state.StackCount)
            {
                Loger.Log("Client ApplyState Set StackCount " + thing.stackCount.ToString() + " -> " + state.StackCount.ToString());
                thing.stackCount = state.StackCount;
            }

            if (thing.Position.x != state.Position.x || thing.Position.z != state.Position.z)
            {
                thing.Position = state.Position.Get();
                if (thing is Pawn)
                {
                    var pawn = (Pawn)thing;
                    //после 1.4
                    if (CellFinder.TryFindBestPawnStandCell(pawn, out var cell))
                    {
                        pawn.Position = cell;
                        pawn.Notify_Teleported(endCurrentJob: true, resetTweenedPos: false);
                    }
                    /* что было до обновления 1.4:
                    try
                    {
                        pawn.Notify_Teleported(true, true);
                    }
                    catch (Exception ext)
                    {
                        Loger.Log("Client ApplyState Exception " + ext.ToString(), Loger.LogLevel.ERROR);
                    }
                    pawn.Drawer.DrawTrackerTick();
                    */
                }
            }

            if (thing is Fire)
            {
                (thing as Fire).fireSize = (float)state.HitPoints / 10000f;
            }
            else
            {
                if (thing.def.useHitPoints)
                {
                    Loger.Log("Client ApplyState Set HitPoints " + thing.HitPoints.ToString() + " -> " + state.HitPoints.ToString());
                    thing.HitPoints = state.HitPoints;
                }
            }

            if (thing is Pawn)
            {
                var pawn = thing as Pawn;
                if ((int)pawn.health.State != (int)state.DownState)
                {
                    if (pawn.health.State == PawnHealthState.Dead)
                    {
                        Loger.Log("Client ApplyState Set pawn state is Dead! Error to change on " + state.DownState.ToString());
                    }
                    else if (state.DownState == AttackThingState.PawnHealthState.Dead)
                    {
                        if (pawnHealthStateDead)
                        {
                            Loger.Log("Client ApplyState Set pawn state (1): " + pawn.health.State.ToString() + " -> " + state.DownState.ToString());
                            HealthUtility.DamageUntilDead(pawn);
                            //PawnKill(pawn);
                        }
                    }
                    else if (state.DownState == AttackThingState.PawnHealthState.Down)
                    {
                        Loger.Log("Client ApplyState Set pawn state (2): " + pawn.health.State.ToString() + " -> " + state.DownState.ToString());
                        //Применяем наркоз?
                        HealthUtility.DamageUntilDowned(pawn, false);
                    }
                    else
                    {
                        Loger.Log("Client ApplyState Set pawn state (3): " + pawn.health.State.ToString() + " -> " + state.DownState.ToString());
                        //полное лечение
                        pawn.health.Notify_Resurrected();
                    }
                }
            }

        }
        /*
        public static void PawnKill(Pawn pawn)
        {
            //заменено на HealthUtility.DamageUntilDead(p);
            DamageDef crush = DamageDefOf.Crush;
            float amount = 99999f;
            float armorPenetration = 999f;
            BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
            DamageInfo damageInfo = new DamageInfo(crush, amount, armorPenetration, -1f, null, brain, null, DamageInfo.SourceCategory.Collapse, null);
            pawn.TakeDamage(damageInfo);
            if (!pawn.Dead)
            {
                pawn.Kill(new DamageInfo?(damageInfo), null);
            }
        }
        */

        private static bool DialodShowing = false;
        private static Queue<Action> DialodQueue = new Queue<Action>();

        public static void ShowDialodOKCancel(string title
            , string text
            , Action ActOK
            , Action ActCancel
            , GlobalTargetInfo? target = null
            , string AltText = null
            , Action ActAlt = null)
        {
            DiaNode diaNode = new DiaNode(text);

            if (target != null)
            {
                var diaOptionT = new DiaOption("JumpToLocation".Translate()); //"Перейти к месту"
                diaOptionT.action = () =>
                {
                    CameraJumper.TryJumpAndSelect(target.Value);
                };
                diaNode.options.Add(diaOptionT);
            }

            DiaOption diaOption = new DiaOption("OCity_GameUtils_Ok".Translate()); //OK -> Принять передачу
            diaOption.action = () => { ActOK(); DialodQueueGoNext(); };
            /*{ спавн пешки бегущей "на помощь"
                GenSpawn.Spawn(refugee, spawnSpot, map, WipeMode.Vanish);
                refugee.SetFaction(Faction.OfPlayer, null);
                CameraJumper.TryJump(refugee);
                QueuedIncident qi = new QueuedIncident(new FiringIncident(IncidentDefOf.RaidEnemy, null, raidParms), Find.TickManager.TicksGame + IncidentWorker_RefugeeChased.RaidDelay.RandomInRange, 0);
                Find.Storyteller.incidentQueue.Add(qi);

            };*/
            diaOption.resolveTree = true;
            diaNode.options.Add(diaOption);

            if (!string.IsNullOrEmpty(AltText) && ActAlt != null)
            {
                diaOption = new DiaOption(AltText);
                diaOption.action = () => { ActAlt(); DialodQueueGoNext(); };
                diaOption.resolveTree = true;
                diaNode.options.Add(diaOption);
            }

            if (ActCancel != null)
            {
                diaOption = new DiaOption("RejectLetter".Translate());
                //RansomDemand_Reject это "Отказаться"
                //RejectLetter это Отклонить
                diaOption.action = () => { ActCancel(); DialodQueueGoNext(); };
                diaOption.resolveTree = true;
                diaNode.options.Add(diaOption);
            }

            Action show = () => Find.WindowStack.Add(new Dialog_NodeTreeWithFactionInfo(diaNode, null, true, true, title));

            //если окно одно, то запускаем, если это окно создается при уже открытом другом, то ставим в очередь
            lock (DialodQueue)
            {
                if (!DialodShowing)
                {
                    DialodShowing = true;
                    show();
                }
                else
                {
                    DialodQueue.Enqueue(show);
                }
            }
        }

        private static void DialodQueueGoNext()
        {
            lock (DialodQueue)
            {
                if (DialodQueue.Count > 0)
                {
                    DialodQueue.Dequeue()();
                }
                else
                {
                    DialodShowing = false;
                }
            }
        }

        /// <summary>
        /// Рисуем кружок с цифрой. Код из мода ResearchTree (MIT license)
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="main"></param>
        /// <param name="background"></param>
        /// <param name="label"></param>
        public static void DrawLabel(Rect canvas, Color main, Color background, int label)
        {
            // draw coloured tag
            GUI.color = main;
            GUI.DrawTexture(canvas, CircleFill);

            // if this is not first in line, grey out centre of tag
            if (background != main)
            {
                GUI.color = background;
                GUI.DrawTexture(canvas.ContractedBy(2f), CircleFill);
            }

            // draw queue number
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(canvas, label.ToString());
            Text.Anchor = TextAnchor.UpperLeft;
        }

        /// <summary>
        /// Ищем вещь по образцу, и выбрать нужное количество. Выбор начинается с карты с максимальным кол-во вещей.
        /// </summary>
        /// <param name="def">Искомый образец</param>
        /// <param name="select">Сколько выбрать в thingsMaxByMap. Если не хватает, то выбрано будет столько сколько есть</param>
        /// <param name="getMaxByMap">Выводить не общую сумму, а максимальное количество на одной из карт.</param>
        /// <param name="thingsMaxByMap">Если не null, заполняется для карты с максимальным количеством. Не виляет на удаление.</param>
        /// <returns>Количество найденных вещей до удаления. Если это число меньше destroy, значит удаление не было произведено вообще.</returns>
        public static int FindThings(ThingDef def, int select, bool getMaxByMap, out Dictionary<Thing, int> thingsSelect)
        {
            int countAll = 0;
            int countMax = 0;
            List<Pair<List<Thing>, int>> maps = new List<Pair<List<Thing>, int>>();
            for (int i = 0; i < Current.Game.Maps.Count; i++)
            {
                var m = Current.Game.Maps[i];
                if (m.IsPlayerHome)
                {
                    List<Thing> things = GameUtils.GetAllThings(m)
                        .Where(t => t.def == def).ToList();
                    var c = things.Sum(t => t.stackCount);
                    maps.Add(new Pair<List<Thing>, int>(things, c));
                    countAll += c;
                    if (countMax < c) countMax = c;
                }
            }
            int count = getMaxByMap ? countMax : countAll;
            if (select == 0 || select > count) select = count;
            thingsSelect = new Dictionary<Thing, int>();
            if (select > 0)
            {
                var selectProcess = select;
                while (maps.Count > 0 && selectProcess > 0)
                {
                    var m = maps.OrderByDescending(p => p.Second).First();
                    foreach (Thing thing in m.First)
                    {
                        var sc = thing.stackCount;
                        if (sc < selectProcess)
                        {
                            thingsSelect.Add(thing, sc);
                            selectProcess -= sc;
                        }
                        else
                        {
                            thingsSelect.Add(thing, selectProcess);
                            selectProcess = 0;
                            break;
                        }
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// Ищем вещь по образцу, и удаляем нужное количество. Удаление начинается с карты с максимальным кол-во вещей.
        /// </summary>
        /// <param name="def">Искомый образец</param>
        /// <param name="destroy">Сколько удалить. Не удаляет нисколько, если нужного количества не будет</param>
        /// <param name="getMaxByMap">Выводить не общую сумму, а максимальное количество на одной из карт. Влияет на удаление.</param>
        /// <returns>Количество найденных вещей до удаления. Если это число меньше destroy, значит удаление не было произведено вообще.</returns>
        public static int FindThings(ThingDef def, int destroy, bool getMaxByMap)
        {
            int countAll = 0;
            int countMax = 0;
            List<Pair<List<Thing>, int>> maps = new List<Pair<List<Thing>, int>>();
            for (int i = 0; i < Current.Game.Maps.Count; i++)
            {
                var m = Current.Game.Maps[i];
                if (m.IsPlayerHome)
                {
                    List<Thing> things = GameUtils.GetAllThings(m)
                        .Where(t => t.def == def).ToList();
                    var c = things.Sum(t => t.stackCount);
                    maps.Add(new Pair<List<Thing>, int>(things, c));
                    countAll += c;
                    if (countMax < c) countMax = c;
                }
            }
            int count = getMaxByMap ? countMax : countAll;
            if (destroy > 0 && destroy < count)
            {
                var destroyProcess = destroy;
                while (maps.Count > 0 && destroyProcess > 0)
                {
                    var m = maps.OrderByDescending(p => p.Second).First();
                    foreach (Thing thing in m.First)
                    {
                        if (thing.stackCount < destroyProcess)
                        {
                            destroyProcess -= thing.stackCount;
                            thing.Destroy();
                        }
                        else
                        {
                            thing.SplitOff(destroyProcess);
                            destroyProcess = 0;
                            break;
                        }
                    }
                }
            }
            return count;
        }

        /*
        public static Dictionary<string, Scenario> AllScenarios()
        {
            var res = new Dictionary<string, Scenario>();

            ScenarioLister.AllScenarios().ToList();

            foreach (ScenarioDef allDef in DefDatabase<ScenarioDef>.AllDefs)
            {
                if (!res.ContainsKey(allDef.defName)) res.Add(allDef.defName, allDef.scenario);
            }
            foreach (Scenario item2 in ScenarioFiles.AllScenariosWorkshop)
            {
                if (!res.ContainsKey(item2.fileName)) res.Add(item2.fileName, item2);
            }
            foreach (Scenario item in ScenarioFiles.AllScenariosLocal)
            {
                if (!res.ContainsKey(item.fileName)) res.Add(item.fileName, item);
            }

            return res;
        }*/

        public static StorytellerDef GetStorytallerByName(string name)
        {
            foreach (StorytellerDef teller in DefDatabase<StorytellerDef>.AllDefs)
            {
                if (teller.defName == name)
                    return teller;
            }
            return null;
        }

        public static Dictionary<string, Scenario> AllowedScenarios()
        {
            var res = new Dictionary<string, Scenario>();

            ScenarioLister.AllScenarios().ToList();

            foreach (ScenarioDef allDef in DefDatabase<ScenarioDef>.AllDefs)
            {
                //Loger.Log($"AllowedScenarios {allDef.defName}={allDef.LabelCap}={allDef.fileName}=Name:{allDef.modContentPack.Name}=RootDir:{allDef.modContentPack.RootDir}");
                if (allDef.modContentPack.Name != "OnlineCity" && allDef.modContentPack.Name != "OnlineCity_Debug") continue;
                //// старое перечисление всех ванильных
                //if (allDef.defName == "Crashlanded"
                //    || allDef.defName == "Tutorial"
                //    || allDef.defName == "LostTribe"
                //    || allDef.defName == "TheRichExplorer"
                //    || allDef.defName == "NakedBrutality")
                //{
                //    continue;
                //}
                if (!res.ContainsKey(allDef.defName))
                {
                    res.Add(allDef.defName, allDef.scenario);
                    //Loger.Log($"AllowedScenarios ok");
                }
            }

            return res;
        }

        public static int DistanceBetweenTile(int start, int end)
        {
            var key = new Pair<int, int>(start, end);
            if (!SessionClientController.Data.DistanceBetweenTileCache.TryGetValue(key, out int res))
            {
                res = Find.WorldGrid.TraversalDistanceBetween(start, end);

                SessionClientController.Data.DistanceBetweenTileCache[key] = res;
            }
            return res;
        }

        public static List<Thing> GetCashlessBalanceThingList(float cashlessBalance)
        {
            if (cashlessBalance > 0) return new List<Thing>() { GetCashlessBalanceThing(cashlessBalance) };
            else return new List<Thing>();
        }

        public static Thing GetCashlessBalanceThing(float cashlessBalance)
        {
            var thing = ThingMaker.MakeThing(MainHelper.CashlessThingDef);
            thing.stackCount = (int)Math.Truncate(cashlessBalance);
            return thing;
        }

        private static Texture2D _TextureCashlessBalance = null;
        public static Texture2D TextureCashlessBalance
        {
            get
            {
                if (_TextureCashlessBalance == null)
                {
                    _TextureCashlessBalance = Widgets.GetIconFor(MainHelper.CashlessThingDef);
                }
                return _TextureCashlessBalance;
            }
        }

        public static bool isBuilding(Thing thing)
        {
            if(thing.def.category == ThingCategory.Building && thing.def.destroyable)
            {
                if (thing.def.building.IsDeconstructible && thing.def.building.uninstallWork > 0)
                    return true;
            }
            return false;
        }

        public static string GetHillinessLabel(Hilliness h)
        {
            switch (h)
            {
                case Hilliness.Flat:
                    return "Hilliness_Flat";
                case Hilliness.SmallHills:
                    return "Hilliness_SmallHills";
                case Hilliness.LargeHills:
                    return "Hilliness_LargeHills";
                case Hilliness.Mountainous:
                    return "Hilliness_Mountainous";
                case Hilliness.Impassable:
                    return "Hilliness_Impassable";
                default:
                    return h.ToString();
            }
        }

        public static void CameraJump(WorldObject wo)
        {
            var ti = new GlobalTargetInfo(wo);
            CameraJumper.TryJumpAndSelect(ti);
        }
        public static void CameraJump(int tile)
        {
            var ti = new GlobalTargetInfo(tile);
            CameraJumper.TryJumpAndSelect(ti);
        }
        public static bool CameraJumpWorldObject(int tile)
        {
            var wos = ExchengeUtils.WorldObjectsByTile(tile)
                .Where(o => o is TradeThingsOnline
                    || o is WorldObjectBaseOnline
                    || (o.Faction?.IsPlayer ?? false) && (o is Settlement || o is Caravan))
                .ToList();
            var woi = wos.FirstOrDefault(o => o is TradeThingsOnline)
                ?? wos.FirstOrDefault(o => (o.Faction?.IsPlayer ?? false) && (o is Settlement || o is Caravan))
                ?? wos.FirstOrDefault(o => o is WorldObjectBaseOnline)
                ?? wos.FirstOrDefault();
            if (woi == null) return false;
            GameUtils.CameraJump(woi);
            return true;
        }
    }
}
