using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using HugsLib.Utils;
using RimWorld.Planet;
using Transfer;
using OCUnion;
using Model;

namespace RimWorldOnlineCity.UI
{
    public class Dialog_Exchenge : Window
    {
        public Action PostCloseAction;

        private static Texture2D IconSkull;

        private static Texture2D IconDelTex;

        /// <summary>
        /// Торговля ведётся от лица этого каравана, либо null, если от карты
        /// </summary>
        public Caravan PlaceCaravan;

        /// <summary>
        /// Торговля ведётся от лица этой карты, либо null, если от каравана
        /// </summary>
        public Map PlaceMap;

        private Place PlaceCurrent;

        /// <summary>
        /// Список вещей которые можно предложить для торговли
        /// </summary>
        public List<Thing> AllThings;
        /// <summary>
        /// Список объединенных в группы одинаковых вещей
        /// </summary>
        public List<TransferableOneWay> Transferables;

        /// <summary>
        /// Список открытых ордеров
        /// </summary>
        public List<OrderTrade> Orders;

        /// <summary>
        /// Рассматриваемый подробно ордер
        /// </summary>
        public OrderTrade EditOrder { get; private set; }

        public float SizeOrders_Heigth;

        public float SizeAddThingList_Width;

        private bool ShowAddThingList
        {
            get { return EditOrderIsMy; }
        }

        private GridBox<TransferableOneWay> AddThingGrid;

        private GridBox<OrderTrade> OrdersGrid;

        private string EditOrderTitle = "";

        private bool EditOrderIsMy
        {
            get { return EditOrder != null && EditOrder.Owner.Login == SessionClientController.My.Login; }
        }

        /// <summary>
        /// Условия для торговли или выставления ордера соблюдены
        /// </summary>
        private bool EditOrderToTrade;

        private Dictionary<long, string> EditOrderEditBuffer;

        private float EditOrderQualityWidth_p = 0;
        private float EditOrderQualityWidth
        {
            get
            {
                if (EditOrderQualityWidth_p == 0)
                {
                    EditOrderQualityWidth_p = 0f;
                    foreach (QualityCategory cat in Enum.GetValues(typeof(QualityCategory)))
                    {
                        try
                        {
                            var x = Text.CalcSize(cat.GetLabelShort()).x;
                            if (x > EditOrderQualityWidth_p) EditOrderQualityWidth_p = x;
                        }
                        catch
                        { }
                    }
                }
                return EditOrderQualityWidth_p;
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            //EnsureSettingsHaveValidFiles(ClientController.Settings);
            if (IconSkull == null) IconSkull = ContentFinder<Texture2D>.Get("Skull");
            if (IconDelTex == null) IconDelTex = ContentFinder<Texture2D>.Get("OCDel");
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(1024f, (float)Verse.UI.screenHeight);
            }
        }

        public Dialog_Exchenge()
        {
            SizeOrders_Heigth = (int)(InitialSize.y / 2f);
            SizeAddThingList_Width = (int)(InitialSize.x / 2f);
        }
        public Dialog_Exchenge(Caravan caravan)
            : this()
        {
            PlaceCaravan = caravan;
            AllThings = GameUtils.GetAllThings(caravan);
            Init();
        }

        public Dialog_Exchenge(Map map)
            : this()
        {
            PlaceMap = map;
            AllThings = GameUtils.GetAllThings(map);
            Init();
        }

        private void Init()
        {
            UpdateOrders();
        }

        private void SetEditOrder(OrderTrade order)
        {
            EditOrder = order;
            EditOrderEditBuffer = new Dictionary<long, string>();
            EditOrderChange();
        }

        private void EditOrderChange()
        {
            if (EditOrder == null) return;
            int k;
            EditOrderToTrade = GameUtils.ChechToTrade(
                EditOrderIsMy ? EditOrder.SellThings : EditOrder.BuyThings
                , AllThings
                , out k) != null && k > 0;
            if (EditOrderIsMy)
            {
                EditOrder.CountReady = EditOrderToTrade
                    ? k < EditOrder.CountBeginMax - EditOrder.CountFnished ? k : EditOrder.CountBeginMax - EditOrder.CountFnished
                    : 0;
            }
        }

        public override void PostClose()
        {
            base.PostClose();
            if (PostCloseAction != null) PostCloseAction();
        }

        public override void DoWindowContents(Rect inRect)
        {
            var margin = 5f;
            var btnSize = new Vector2(140f, 35f);
            var buttonYStart = inRect.height - btnSize.y;
            Text.Font = GameFont.Small;

            if (Widgets.ButtonText(new Rect(inRect.width - btnSize.x, 0, btnSize.x, btnSize.y), "Закрыть".NeedTranslate()))
            {
                Close();
            }

            Rect rect = new Rect(0f, 0f, inRect.width, btnSize.y);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "Торговые ордера".NeedTranslate());

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;


            //область сверху - ордера
            var regionRectOut = new Rect(inRect.x, inRect.y + btnSize.y + margin, inRect.width, SizeOrders_Heigth);

            //Widgets.DrawMenuSection(regionRectOut);

            var regionRect = new Rect(regionRectOut);
            GUI.BeginGroup(regionRect);
            var regionRectIn = regionRect.AtZero();
            DoWindowOrders(regionRectIn);
            GUI.EndGroup();


            //область снизу слева - добавляемые в ордер вещи
            regionRectOut = new Rect(regionRectOut.x
                , regionRectOut.yMax + margin
                , SizeAddThingList_Width
                , inRect.height - (regionRectOut.yMax + margin));

            Widgets.DrawMenuSection(regionRectOut);

            regionRect = regionRectOut.ContractedBy(margin * 2f);
            GUI.BeginGroup(regionRect);
            regionRectIn = regionRect.AtZero();
            DoWindowAddThingList(regionRectIn);
            GUI.EndGroup();


            //область снизу справа - просмотр и редактирование ордера

            regionRectOut = new Rect(regionRectOut.xMax + margin
                , regionRectOut.y
                , inRect.width - (regionRectOut.xMax + margin)
                , regionRectOut.height);

            Widgets.DrawMenuSection(regionRectOut);

            regionRect = regionRectOut.ContractedBy(margin * 2f);
            GUI.BeginGroup(regionRect);
            regionRectIn = regionRect.AtZero();
            DoWindowEditOrder(regionRectIn);
            GUI.EndGroup();
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private Dictionary<Rect, string> OrdersMenu;

        /// <summary>
        /// область сверху - ордера
        /// </summary>
        /// <param name="inRect"></param>
        public void DoWindowOrders(Rect inRect)
        {
            if (OrdersGrid == null && Orders != null && Orders.Count > 0)
            {
                //инициализация
                OrdersGrid = new GridBox<OrderTrade>();
                //Transferables = GameUtils.DistinctThings(AllThings);
                //var dicMaxCount = Transferables.ToDictionary(t => t, t => t.MaxCount);
                OrdersGrid.DataSource = Orders;
                OrdersGrid.LineHeight = 24f;
                OrdersGrid.ShowSelected = true;
                OrdersGrid.Tooltip = null;
                OrdersGrid.OnClick += (int line, OrderTrade item) =>
                {
                    SetEditOrder(item);
                    if (EditOrderIsMy)
                        EditOrderTitle = "Редактировать".NeedTranslate();
                    else
                        EditOrderTitle = "Просмотр ордера ".NeedTranslate() + item.Owner.Login;
                };
                OrdersMenu = null;
                OrdersGrid.OnDrawLine = (int line, OrderTrade item, Rect rectLine) =>
                {
                    bool showTop = line == 0; //при выводе таблицы, рисуем с первой же строкой
                    Text.WordWrap = false;
                    Text.Anchor = TextAnchor.MiddleLeft;
                    try
                    {
                        float currentWidth = rectLine.width;

                        //Галочка частное
                        var rect2 = new Rect(rectLine.x + rectLine.width - 24f, rectLine.y, 24f, rectLine.height);
                        currentWidth -= 24f;
                        var flag = item.PrivatPlayers == null || item.PrivatPlayers.Count == 0;
                        TooltipHandler.TipRegion(rect2, flag
                            ? "Сделка доступна всем".NeedTranslate()
                            : "Сделка доступна только: {0}".NeedTranslate(string.Join(", ", item.PrivatPlayers.Select(p => p.Login).ToArray())));
                        Widgets.Checkbox(rect2.position, ref flag, 24f, false);

                        //Ник продавца
                        rect2 = new Rect(rectLine.x + currentWidth - 200f, rectLine.y, 200f, rectLine.height);
                        if (showTop)
                        {
                            OrdersMenu = new Dictionary<Rect, string>();
                            var rect2t = new Rect(rect2.x, 0f, rect2.width, rect2.height);
                            OrdersMenu.Add(rect2t, "Продавец".NeedTranslate());
                        }
                        currentWidth -= 200f;
                        TooltipHandler.TipRegion(rect2, item.Owner.Login + Environment.NewLine + "был в сети ".NeedTranslate() + item.Owner.LastSaveTime.ToGoodUtcString());
                        Widgets.Label(rect2, item.Owner.Login);

                        //Расстояние где торгуют (todo), название места
                        rect2 = new Rect(rectLine.x + currentWidth - 200f, rectLine.y, 200f, rectLine.height);
                        if (showTop)
                        {
                            var rect2t = new Rect(rect2.x, 0f, rect2.width, rect2.height);
                            OrdersMenu.Add(rect2t, "Место".NeedTranslate());
                        }
                        currentWidth -= 200f;
                        var text = (item.Place.DayPath > 0 ? item.Place.DayPath.ToStringDecimalIfSmall() + " дней".NeedTranslate() : "")
                            + " в ".NeedTranslate() + item.Place.Name;
                        TooltipHandler.TipRegion(rect2, "Месторасположение товара:".NeedTranslate() + Environment.NewLine + text);
                        Widgets.Label(rect2, text);

                        //Кол-во повторов
                        rect2 = new Rect(rectLine.x + currentWidth - 60f, rectLine.y, 60f, rectLine.height);
                        if (showTop)
                        {
                            var rect2t = new Rect(rect2.x, 0f, rect2.width, rect2.height);
                            OrdersMenu.Add(rect2t, "Кол-во".NeedTranslate());
                        }
                        currentWidth -= 60f;
                        text = item.CountReady.ToString();
                        TooltipHandler.TipRegion(rect2, "Максимум повоторов реализации сделки:".NeedTranslate() + Environment.NewLine + text);
                        Widgets.Label(rect2, text);

                        //Иконки и перечень (описание в подсказке) 
                        rect2 = new Rect(rectLine.x, rectLine.y, currentWidth / 2f, rectLine.height); //от 0 до половины остатка
                        if (showTop)
                        {
                            var rect2t = new Rect(rect2.x, 0f, rect2.width, rect2.height);
                            OrdersMenu.Add(rect2t, "Приобрести".NeedTranslate());
                        }
                        var rect3 = new Rect(rect2.x, rect2.y, rectLine.height, rectLine.height);
                        for (int i = 0; i < item.SellThings.Count; i++)
                        {
                            var th = item.SellThings[i];
                            GameUtils.DravLineThing(rect3, th, false);
                            var textCnt = item.SellThings[i].Count.ToString();
                            var textCntW = Text.CalcSize(textCnt).x;
                            Widgets.Label(new Rect(rect3.xMax, rect3.y, textCntW, rect3.height), textCnt);
                            TooltipHandler.TipRegion(new Rect(rect3.x, rect3.y, rect3.width + textCntW, rect3.height), th.LabelText);
                            rect3.x += rectLine.height + textCntW + 2f;
                        }

                        //"-" Иконки и перечень что просят
                        rect2 = new Rect(rectLine.x + rect2.width, rectLine.y, currentWidth - rect2.width, rectLine.height); //от конца прошлого до остатка
                        if (showTop)
                        {
                            var rect2t = new Rect(rect2.x, 0f, rect2.width, rect2.height);
                            OrdersMenu.Add(rect2t, "Отдать".NeedTranslate());
                        }
                        //Дальше отличается от блока выше только SellThings -> BuyThings
                        rect3 = new Rect(rect2.x, rect2.y, rectLine.height, rectLine.height);
                        for (int i = 0; i < item.BuyThings.Count; i++)
                        {
                            var th = item.BuyThings[i];
                            GameUtils.DravLineThing(rect3, th, false);
                            var textCnt = item.BuyThings[i].Count.ToString();
                            var textCntW = Text.CalcSize(textCnt).x;
                            Widgets.Label(new Rect(rect3.xMax, rect3.y, textCntW, rect3.height), textCnt);
                            TooltipHandler.TipRegion(new Rect(rect3.x, rect3.y, rect3.width + textCntW, rect3.height), th.LabelText);
                            rect3.x += rectLine.height + textCntW + 2f;
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());
                    }
                    Text.WordWrap = true;
                };
            }

            //заголовок
            Rect rect = new Rect(inRect.x, inRect.y, inRect.width, 24f);
            inRect.yMin += rect.height;
            Text.Font = GameFont.Tiny; // высота Tiny 18
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, (Orders == null || Orders.Count == 0) ? "Ордеров нет".NeedTranslate() : "Активных ордеров {0}".NeedTranslate(Orders.Count.ToString()));

            //кнопка "Выбрать"
            rect.xMin += inRect.width - 140f;
            Text.Anchor = TextAnchor.MiddleCenter;
            if (Widgets.ButtonText(rect.ContractedBy(1f), "Обновить".NeedTranslate(), true, false, true))
            {
                SoundDefOf.TickHigh.PlayOneShotOnCamera(null);

                UpdateOrders();
                return;
            }

            //заголовок таблицы
            var rectTop = new Rect(inRect.x, inRect.y, inRect.width, 18f);
            inRect.yMin += rectTop.height;
            if (OrdersMenu != null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                foreach (var om in OrdersMenu)
                {
                    var rect2t = new Rect(om.Key.x, rectTop.y, om.Key.width, rectTop.height);
                    Widgets.Label(rect2t, om.Value);
                }
            }

            //всё что ниже это грид
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.DrawMenuSection(inRect);

            if (OrdersGrid == null) return;

            OrdersGrid.Area = inRect.ContractedBy(5f);
            OrdersGrid.Drow();
        }

        private void UpdateOrders()
        {
            OrdersGrid = null;

            SessionClientController.Command((connect) =>
            {
                connect.ErrorMessage = null;

                Orders = connect.ExchengeLoad();

                if (!string.IsNullOrEmpty(connect.ErrorMessage))
                {
                    Loger.Log("Client ExchengeLoad error: " + connect.ErrorMessage);
                }
            });
        }

        /// <summary>
        /// область снизу слева - добавляемые в ордер вещи
        /// </summary>
        /// <param name="inRect"></param>
        public void DoWindowAddThingList(Rect inRect)
        {
            if (!ShowAddThingList) return;

            if (AddThingGrid == null)
            {
                //инициализация
                AddThingGrid = new GridBox<TransferableOneWay>();
                Transferables = GameUtils.DistinctThings(AllThings);
                var dicMaxCount = Transferables.ToDictionary(t => t, t => t.MaxCount);
                AddThingGrid.DataSource = Transferables;
                AddThingGrid.LineHeight = 24f;
                AddThingGrid.Tooltip = null;
                AddThingGrid.OnDrawLine = (int line, TransferableOneWay item, Rect rectLine) =>
                {
                    try
                    {
                        if (!item.HasAnyThing) return;
                        float currentWidth = rectLine.width;

                        //ввод кол-во и кнопки < > 
                        currentWidth -= 60f + rectLine.height * 2f;
                        Rect rect3 = new Rect(rectLine.width - 60f - rectLine.height, 0f, 60f, rectLine.height);
                        int num2 = GenUI.CurrentAdjustmentMultiplier(); //зажали кнопку для прибавления по 10/100
                        if (item.CanAdjustBy(-1 * num2).Accepted)
                        {
                            Rect rect4 = new Rect(rect3.x - rectLine.height, 0f, rectLine.height, rectLine.height).ContractedBy(1f);
                            if (Widgets.ButtonText(rect4, "<", true, false, true))
                            {
                                item.AdjustBy(-1 * num2);
                                SoundDefOf.TickHigh.PlayOneShotOnCamera(null);
                            }
                        }
                        int countToTransfer = item.CountToTransfer;
                        string editBuffer = item.EditBuffer;
                        Widgets.TextFieldNumeric<int>(rect3.ContractedBy(2f), ref countToTransfer, ref editBuffer, 0f, (float)dicMaxCount[item]);
                        item.AdjustTo(countToTransfer);
                        item.EditBuffer = editBuffer;
                        if (item.CanAdjustBy(1 * num2).Accepted)
                        {
                            Rect rect4 = new Rect(rect3.xMax, 0f, rectLine.height, rectLine.height).ContractedBy(1f);
                            if (Widgets.ButtonText(rect4, ">", true, false, true))
                            {
                                item.AdjustBy(1 * num2);
                                SoundDefOf.TickHigh.PlayOneShotOnCamera(null);
                            }
                        }

                        //кол-во
                        rect3 = new Rect(currentWidth - 60f, 0f, 60f, rectLine.height);
                        Text.WordWrap = false;
                        Text.Anchor = TextAnchor.MiddleRight;
                        Widgets.Label(rect3, dicMaxCount[item].ToString());
                        currentWidth -= 60f;

                        //цена
                        rect3 = new Rect(currentWidth - 60f, 0f, 80f, rectLine.height);
                        Text.Anchor = TextAnchor.MiddleLeft;
                        Widgets.Label(rect3, item.AnyThing.MarketValue.ToStringMoney());
                        Text.WordWrap = true;
                        currentWidth -= 60f;

                        //Иконка i и описание
                        GameUtils.DravLineThing(new Rect(0f, 0f, currentWidth, rectLine.height), item.AnyThing, Color.white);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());
                    }

                };
            }

            //заголовок
            Rect rect = new Rect(0f, 0f, inRect.width, 24);
            inRect.yMin += rect.height;
            Text.Font = GameFont.Tiny; // высота Tiny 18
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(rect, "Выбор на продажу из {0}".NeedTranslate(PlaceCurrent.Name));

            //кнопка "Выбрать"
            rect.xMin += inRect.width - 150f;
            Text.Anchor = TextAnchor.MiddleCenter;
            if (Widgets.ButtonText(rect.ContractedBy(1f), "Выбрать".NeedTranslate(), true, false, true))
            {
                SoundDefOf.TickHigh.PlayOneShotOnCamera(null);
                AddThingListApply();
                return;
            }

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;

            AddThingGrid.Area = inRect;
            AddThingGrid.Drow();
        }

        /// <summary>
        /// Добавляем из наших вещей в сделку
        /// </summary>
        private void AddThingListApply()
        {
            if (!EditOrderIsMy || AddThingGrid.DataSource == null) return;
            //выбранное в AddThingGrid.DataSource, где кол-во выбранного в поле CountToTransfer

            foreach (var item in AddThingGrid.DataSource)
            {
                if (item.CountToTransfer == 0) continue;
                var th = ThingTrade.CreateTrade(item.AnyThing, item.CountToTransfer);
                EditOrder.SellThings.Add(th);
            }

            EditOrderChange();
            AddThingGrid = null;
        }

        /// <summary>
        /// область снизу справа - просмотр и редактирование ордера
        /// </summary>
        /// <param name="inRect"></param>
        public void DoWindowEditOrder(Rect inRect)
        {
            if (PlaceCurrent == null)
            {
                PlaceCurrent = new Place();
                WorldObject wo;
                if (PlaceMap != null)
                    wo = PlaceMap.info.parent;
                else
                    wo = PlaceCaravan;
                PlaceCurrent.Name = wo.LabelCap;
                PlaceCurrent.PlaceServerId = UpdateWorldController.GetServerInfo(wo).ServerId;
                PlaceCurrent.ServerName = SessionClientController.My.ServerName;
                PlaceCurrent.Tile = wo.Tile;
                PlaceCurrent.DayPath = 0;
            }
            if (EditOrder == null)
            {
                //Действие не выбрано: по умолчанию настраиваем панельна создание нового ордера
                EditOrderTitle = "Создать новый ордер".NeedTranslate();
                var editOrder = new OrderTrade();
                editOrder.Owner = SessionClientController.My;
                editOrder.Place = PlaceCurrent;
                editOrder.CountBeginMax = 1;

                editOrder.SellThings = new List<ThingTrade>();
                editOrder.BuyThings = new List<ThingTrade>();
                var silverDef = (ThingDef)GenDefDatabase.GetDef(typeof(ThingDef), "Silver");
                var th = ThingTrade.CreateTrade(silverDef, 0f, QualityCategory.Awful, 1);
                editOrder.BuyThings.Add(th);

                editOrder.PrivatPlayers = new List<Player>();

                SetEditOrder(editOrder);
            }

            bool existInServer = EditOrder.Id != 0;

            //заголовок
            Rect rect = new Rect(0f, 0f, inRect.width, 18);
            inRect.yMin += rect.height;
            Text.Font = GameFont.Tiny; // высота Tiny 18
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, EditOrderTitle);

            ///todo
            ///полоску проскрутки

            //кнопка в углу
            rect = new Rect(inRect.width - 150f, 20f, 150f, 24);
            if (!EditOrderToTrade) GUI.color = Color.red;
            if (Widgets.ButtonText(rect.ContractedBy(1f)
                , EditOrderIsMy
                    ? existInServer ? "Сохранить".NeedTranslate() : "Создать".NeedTranslate()
                    : "ТОРГОВАТЬ".NeedTranslate()
                , true, false, true))
            {
                GUI.color = Color.white;
                SoundDefOf.TickHigh.PlayOneShotOnCamera(null);
                if (!EditOrderToTrade) return;

                if (!EditOrderIsMy)
                {
                    //торговать
                    //todo
                }
                else
                {
                    //создать или отредактировать
                    SessionClientController.Command((connect) =>
                    {
                        SoundDefOf.TickHigh.PlayOneShotOnCamera(null);

                        if (!connect.ExchengeEdit(EditOrder))
                        {
                            Loger.Log("Client ExchengeEdit error: " + connect.ErrorMessage);
                            Find.WindowStack.Add(new Dialog_Message("Действие не выполнено".NeedTranslate(), connect.ErrorMessage));
                        }
                        else
                        {
                            SetEditOrder(null);
                        }

                        UpdateOrders();
                    });
                    return;
                }

                EditOrderChange();
                return;
            }
            GUI.color = Color.white;

            //кнопка
            if (!EditOrderIsMy)
            {
                rect = new Rect(160f, 20f, inRect.width - 160f - 160f, 24);
                if (Widgets.ButtonText(rect.ContractedBy(1f)
                    , "Встречное предложение".NeedTranslate()
                    , true, false, true))
                {
                    SoundDefOf.TickHigh.PlayOneShotOnCamera(null);

                    //todo

                    return;
                }
            }
            if (EditOrderIsMy && existInServer && EditOrder.Id != 0)
            {
                rect = new Rect(160f, 20f, 100f, 24);
                if (Widgets.ButtonText(rect.ContractedBy(1f)
                    , "Удалить".NeedTranslate()
                    , true, false, true))
                {
                    SoundDefOf.TickHigh.PlayOneShotOnCamera(null);

                    SessionClientController.Command((connect) =>
                    {
                        SoundDefOf.TickHigh.PlayOneShotOnCamera(null);

                        EditOrder.Id = -EditOrder.Id;
                        if (!connect.ExchengeEdit(EditOrder))
                        {
                            EditOrder.Id = -EditOrder.Id;
                            Loger.Log("Client ExchengeEdit error: " + connect.ErrorMessage);
                            Find.WindowStack.Add(new Dialog_Message("Действие не выполнено".NeedTranslate(), connect.ErrorMessage));
                        }
                        else
                        {
                            SetEditOrder(null);
                        }

                        UpdateOrders();
                    });

                    return;
                }
            }

            rect = new Rect(0, 20f, 150f, 24f);
            if (Widgets.ButtonText(rect.ContractedBy(1f)
                , "Новый ордер".NeedTranslate()
                , true, false, true))
            {
                SoundDefOf.TickHigh.PlayOneShotOnCamera(null);

                SetEditOrder(null);

                return;
            }

            inRect.yMin += rect.height;

            rect = new Rect(0f, 44f, inRect.width, 24f);
            Text.Anchor = TextAnchor.MiddleLeft;

            if (EditOrderIsMy)
            {
                Widgets.Label(rect, "Кол-во таких обменов:".NeedTranslate());
                var rect2 = new Rect(rect.x + 250f, rect.y, 70f, rect.height);

                int countToTransfer = EditOrder.CountBeginMax;
                string editBuffer;
                if (!EditOrderEditBuffer.TryGetValue(EditOrder.GetHashCode(), out editBuffer)) EditOrderEditBuffer.Add(EditOrder.GetHashCode(), editBuffer = countToTransfer.ToString());
                Widgets.TextFieldNumeric<int>(rect2.ContractedBy(2f), ref countToTransfer, ref editBuffer, 1f, 999999999f);
                EditOrderEditBuffer[EditOrder.GetHashCode()] = editBuffer;
                if (countToTransfer > 0)
                {
                    EditOrder.CountBeginMax = countToTransfer;
                    EditOrderChange();
                }

                rect.y += 24f;
            }
            
            Widgets.Label(rect, "Кол-во ещё доступно для обмена: ".NeedTranslate() + EditOrder.CountReady.ToString());
            if (EditOrderIsMy)
            {
                rect.xMin += 250;
                Widgets.Label(rect, "Совершено раз: ".NeedTranslate() + EditOrder.CountFnished.ToString());
                rect.xMin = 0;
            }
            rect.y += 24f;

            if (EditOrderIsMy)
            {
                Widgets.Label(rect, "Мы отдаем:".NeedTranslate());
                rect.y += 24f;
                EditOrderShowSellThings(ref rect);

                Widgets.Label(rect, "Мы получаем:".NeedTranslate());
                rect.y += 24f;
                EditOrderShowBuyThings(ref rect);
            }
            else
            {
                Widgets.Label(rect, "Мы получаем:".NeedTranslate());
                rect.y += 24f;
                EditOrderShowBuyThings(ref rect);

                Widgets.Label(rect, "Мы отдаем {0}:".NeedTranslate(EditOrder.Owner.Login));
                rect.y += 24f;
                EditOrderShowSellThings(ref rect);
            }

            if (EditOrder.PrivatPlayers == null || EditOrder.PrivatPlayers.Count == 0)
            {
                Widgets.Label(rect, "Ограичений по пользователям нет".NeedTranslate());
                rect.y += 24f;
            }
            else
            {

                Widgets.Label(rect, "Сделка доступна только пользователям:".NeedTranslate());
                rect.y += 24f;
                for (int i = 0; i < EditOrder.PrivatPlayers.Count; i++)
                {
                    var rect3 = new Rect(rect.x, rect.y, 24f, 24f);
                    Widgets.Label(rect3, EditOrder.PrivatPlayers[i].Login);

                    rect3 = new Rect(rect.xMax - 24f, rect.y, 24f, 24f);
                    if (EditOrderIsMy && Widgets.ButtonImage(rect3, IconDelTex))
                    {
                        EditOrder.PrivatPlayers.RemoveAt(i--);
                    }
                    rect.y += 24f;
                }
            }
            var rect4 = new Rect(rect);
            rect4.width = 150f;
            if (Widgets.ButtonText(rect4.ContractedBy(1f)
                , "+ добавить пользователя".NeedTranslate()
                , true, false, true))
            {
                SoundDefOf.TickHigh.PlayOneShotOnCamera(null);

                var editOrder = EditOrder;
                var list = SessionClientController.Data.Players.Keys
                    .Where(p => !editOrder.PrivatPlayers.Any(pp => pp.Login == p) && p != "system")
                    .Select(p => new FloatMenuOption(p,
                        () =>
                        {
                            if (editOrder.PrivatPlayers.Any(pp => pp.Login == p)) return;
                            editOrder.PrivatPlayers.Add(SessionClientController.Data.Players[p].Public);
                        }))
                    .ToList();
                if (list.Count == 0) return;
                var menu = new FloatMenu(list);
                Find.WindowStack.Add(menu);

                return;
            }

        }

        private void EditOrderShowSellThings(ref Rect rect)
        {
            for (int i = 0; i < EditOrder.SellThings.Count; i++)
            {
                //это редактирование своих вещей. Тут все поля только чтение кроме крестика убрать строку
                var th = EditOrder.SellThings[i];
                var xl = 0f;
                var xr = 0f;

                //иконка
                var rect3 = new Rect(rect.x, rect.y, 24f, 24f);
                xl = rect.x + 24f * 2f;
                GameUtils.DravLineThing(rect3, th.DataThing, true);

                //"x" перед кол-во
                var textCntW = Text.CalcSize("* x").x;
                rect3 = new Rect(xl, rect.y, textCntW, 24f);
                xl += textCntW;
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(rect3, "x");
                Text.Anchor = TextAnchor.MiddleLeft;

                //кол-во Count
                rect3 = new Rect(xl, rect.y, 50f, 24f);
                xl += 50f;
                if (EditOrderIsMy)
                {
                    int countToTransfer = th.Count;
                    string editBuffer;
                    if (!EditOrderEditBuffer.TryGetValue(th.GetHashCode(), out editBuffer)) EditOrderEditBuffer.Add(th.GetHashCode(), editBuffer = countToTransfer.ToString());
                    Widgets.TextFieldNumeric<int>(rect3.ContractedBy(2f), ref countToTransfer, ref editBuffer, 0f, 999999999f);
                    EditOrderEditBuffer[th.GetHashCode()] = editBuffer;
                    if (countToTransfer > 0 && th.Count != countToTransfer)
                    {
                        th.Count = countToTransfer;
                        EditOrderChange();
                    }
                }
                else
                {
                    Widgets.Label(rect3, th.Count.ToString());
                }

                //множитель кол-во повторов
                textCntW = 40f;
                Text.Anchor = TextAnchor.MiddleLeft;
                rect3 = new Rect(xl, rect.y, textCntW, 15f);
                GUI.color = Color.gray;
                Widgets.Label(rect3, "*" + EditOrder.CountReady.ToString());
                rect3 = new Rect(xl, rect.y + 12f, textCntW, 15f);
                GUI.color = Color.white;
                Widgets.Label(rect3, "="+ (EditOrder.CountReady * th.Count).ToString());
                xl += textCntW;

                //далее всё в обратном порядке справа-налево

                //крестик убрать строку
                xr = rect.xMax - 24f;
                rect3 = new Rect(xr, rect.y, 24f, 24f);
                if (EditOrderIsMy && Widgets.ButtonImage(rect3, IconDelTex))
                {
                    EditOrder.SellThings.RemoveAt(i--);
                    EditOrderChange();
                }

                //с трупа WornByCorpse, прочность HitPoints из MaxHitPoints и качество Quality
                EditOrderShowHitAndQ(ref rect, ref xr, th);

                //название
                rect3 = new Rect(xl, rect.y, xr - xl, 24f);
                if (th.NotTrade) GUI.color = Color.red;
                Widgets.Label(rect3, th.Name);
                GUI.color = Color.white;

                rect.y += 24f;
            }
        }

        private void EditOrderShowBuyThings(ref Rect rect)
        {
            for (int i = 0; i < EditOrder.BuyThings.Count; i++)
            {
                //это редактирование получаемых вещей. Тут все поля либо также только чтение либо все редактируемые
                var th = EditOrder.BuyThings[i];
                var xl = 0f;
                var xr = 0f;

                //иконка
                var rect3 = new Rect(rect.x, rect.y, 24f, 24f);
                xl = rect.x + 24f * 2f;
                if (th.Concrete)
                    GameUtils.DravLineThing(rect3, th.DataThing, true);
                else
                    GameUtils.DravLineThing(rect3, th, true, Color.gray);

                //"x" перед кол-во
                var textCntW = Text.CalcSize("* x").x;
                rect3 = new Rect(xl, rect.y, textCntW, 24f);
                xl += textCntW;
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(rect3, th.Concrete ? "* x" : "x");
                Text.Anchor = TextAnchor.MiddleLeft;
                if (th.Concrete)
                    TooltipHandler.TipRegion(rect3, "Задан конкретный объект".NeedTranslate());

                //кол-во Count
                rect3 = new Rect(xl, rect.y, 50f, 24f);
                xl += 50f;
                if (EditOrderIsMy)
                {
                    int countToTransfer = th.Count;
                    string editBuffer;
                    if (!EditOrderEditBuffer.TryGetValue(th.GetHashCode(), out editBuffer)) EditOrderEditBuffer.Add(th.GetHashCode(), editBuffer = countToTransfer.ToString());
                    Widgets.TextFieldNumeric<int>(rect3.ContractedBy(2f), ref countToTransfer, ref editBuffer, 0f, 999999999f);
                    EditOrderEditBuffer[th.GetHashCode()] = editBuffer;
                    if (countToTransfer > 0 && th.Count != countToTransfer)
                    {
                        th.Count = countToTransfer;
                        EditOrderChange();
                    }
                }
                else
                {
                    Widgets.Label(rect3, th.Count.ToString());
                }
                
                //множитель кол-во повторов
                textCntW = 40f;
                Text.Anchor = TextAnchor.MiddleLeft;
                rect3 = new Rect(xl, rect.y, textCntW, 15f);
                GUI.color = Color.gray;
                Widgets.Label(rect3, "*" + EditOrder.CountReady.ToString());
                rect3 = new Rect(xl, rect.y + 12f, textCntW, 15f);
                GUI.color = Color.white;
                Widgets.Label(rect3, "=" + (EditOrder.CountReady * th.Count).ToString());
                xl += textCntW;

                //далее всё в обратном порядке справа-налево

                //крестик убрать строку
                xr = rect.xMax - 24f;
                rect3 = new Rect(xr, rect.y, 24f, 24f);
                if (EditOrderIsMy && Widgets.ButtonImage(rect3, IconDelTex))
                {
                    EditOrder.BuyThings.RemoveAt(i--);
                }

                //с трупа WornByCorpse, прочность HitPoints из MaxHitPoints и качество Quality
                EditOrderShowHitAndQ(ref rect, ref xr, th);
                
                //название
                rect3 = new Rect(xl, rect.y, xr - xl, 24f);
                Widgets.Label(rect3, th.Name);

                rect.y += 24f;
            }
            if (EditOrderIsMy)
            {
                var rect4 = new Rect(rect);
                rect4.width = 150f;
                if (Widgets.ButtonText(rect4.ContractedBy(1f)
                    , "+ добавить".NeedTranslate()
                    , true, false, true))
                {
                    SoundDefOf.TickHigh.PlayOneShotOnCamera(null);

                    var formm = new Dialog_SelectThingDef();
                    formm.ClearFilter();
                    formm.PostCloseAction = () =>
                    {
                        if (formm.SelectThingDef == null) return;
                        var th = ThingTrade.CreateTrade(formm.SelectThingDef, formm.SelectHitPointsPercents.min, formm.SelectQualities.min, 1);
                        EditOrder.BuyThings.Add(th);
                        EditOrderChange();
                    };
                    Find.WindowStack.Add(formm);

                    return;
                }
                rect.y += 24f;
            }
        }

        private void EditOrderShowHitAndQ(ref Rect rect, ref float xr, ThingTrade th)
        {
            //с трупа WornByCorpse
            xr -= 24f;
            var rect3 = new Rect(xr, rect.y, 24f, 24f);
            if (th.WornByCorpse)
            {
                TooltipHandler.TipRegion(rect3, (th.Concrete ? "Снята с трупа" : "Для одежды допустимо быть снятой с трупа").NeedTranslate());
                if (EditOrderIsMy && !th.Concrete)
                    if (Widgets.ButtonImage(rect3, IconSkull, Color.green)) th.WornByCorpse = false;
                else
                    Widgets.DrawTextureFitted(rect3, IconSkull, 1f);
            }
            else
            {
                TooltipHandler.TipRegion(rect3, (th.Concrete ? "НЕ с трупа" : "Для одежды НЕ допустимо быть снятой с трупа.").NeedTranslate());
                if (EditOrderIsMy && !th.Concrete)
                    if (Widgets.ButtonImage(rect3, IconSkull, Color.gray)) th.WornByCorpse = true;
            }
            GUI.color = Color.white;

            if (th.Concrete)
            {
                //прочность HitPoints из MaxHitPoints
                var textCntW = Text.CalcSize("888/888 ").x;
                xr -= textCntW;
                rect3 = new Rect(xr, rect.y, textCntW, 24f);
                TooltipHandler.TipRegion(rect3, "Цело на {0} из {1} ({2}%)".NeedTranslate(th.HitPoints, th.MaxHitPoints
                    , (th.HitPoints * 100 / th.MaxHitPoints).ToString()));
                Widgets.Label(rect3, th.HitPoints + "/" + th.MaxHitPoints);

                //качество Quality
                textCntW = EditOrderQualityWidth + 10f;
                xr -= textCntW;
                rect3 = new Rect(xr, rect.y, textCntW, 24f);
                TooltipHandler.TipRegion(rect3, "Качество ".NeedTranslate() + ((QualityCategory)th.Quality).GetLabel());
                Widgets.Label(rect3, ((QualityCategory)th.Quality).GetLabelShort() + "+");
            }
            else
            {
                //прочность HitPoints из MaxHitPoints
                var textCntW = Text.CalcSize("188% ").x;
                xr -= textCntW;
                rect3 = new Rect(xr, rect.y, textCntW, 24f);
                TooltipHandler.TipRegion(rect3, "Цело не меньше чем ".NeedTranslate() + (th.HitPoints * 100 / th.MaxHitPoints).ToString() + "%");
                Widgets.Label(rect3, (th.HitPoints * 100 / th.MaxHitPoints).ToString() + "%");

                //качество Quality
                textCntW = EditOrderQualityWidth + 10f;
                xr -= textCntW;
                rect3 = new Rect(xr, rect.y, textCntW, 24f);
                TooltipHandler.TipRegion(rect3, "Качество не меньше чем ".NeedTranslate() + ((QualityCategory)th.Quality).GetLabel());
                Widgets.Label(rect3, th.Quality == 0
                    ? "все".NeedTranslate()
                    : ((QualityCategory)th.Quality).GetLabelShort() + "+"
                    );
            }
        }



    }
}
