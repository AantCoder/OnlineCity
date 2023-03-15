using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld.Planet;
using Transfer;
using OCUnion;
using Model;
using RimWorldOnlineCity.Services;
using System.Threading;
using RimWorldOnlineCity.GameClasses.Harmony;

namespace RimWorldOnlineCity.UI
{
    /// <summary>
    /// В этом окне можно просмотреть все сделки отсортированные по удаленности от точки где открыто окно
    /// Можно купить по ордеру или создать свой ордер, если достаточно выбранного источника ресурсов и безналичных средств.
    /// При выставлении ордера на продажу вещи резирвируются в сделке, их можно вернуть отменив её.
    /// Также в окне можно перемещать вещи между разными объектами: поселение, караваны, торговый склад (хранение вещей на сервере).
    /// При перемещении серебра в торговый склад оно помещается в безналичный счет, при перемещении назад обналичивается из него.
    /// </summary>
    public class Dialog_Exchenge : Window
    {
        //Про безнал, или как на сервере обрабатывается серебро!
        //В красном яблоке (торговый склад) оно не хранится. При пополнении +в общий счет, при снятии -общий счет. Запрос красного яблока возвращается без серебра, если надо клиент сам добавит
        //При сделке на сервере серебро также как другие вещи блокируется в SellThings, но отнимается не из красного яблока, а из счета.
        //При создании/редактировании сделки вещи, которые нужно заблокировать (отмеченные в SellThings) передаются в красное яблоко
        // откуда сервер самостоятельно из извлечет, при фиксации нового состояния ордера.


        public Action PostCloseAction;

        private Place PlaceCurrent;

        private WorldObject WorldObjectCurrent;

        private WorldObject WorldObjectStorageCurrentTile = null;

        private List<WorldObject> WorldObjectsTile;

        private List<CaravanOnline> WorldObjectCaravanOnlinesTile;

        /// <summary>
        /// Список вещей которые можно предложить для торговли
        /// </summary>
        public List<Thing> AllThings;

        /// <summary>
        /// Список оригинальных объектов сервера, из которых сделаны AllThings. 
        /// Иначе null
        /// </summary>
        public List<ThingTrade> AllThingsOriginalEntry;

        /// <summary>
        /// Список открытых ордеров
        /// </summary>
        public List<TradeOrder> Orders;

        private string StatusLoadOrders = null;

        private AnyLoad LoaderOrders = null;

        private int FilterTileLength = 0;
        private string FilterTileLengthBuffer = "";

        private string FilterSell = "";
        private ThingDef FilterSellThing = null;
        private string FilterBuy = "";
        private ThingDef FilterBuyThing = null;

        /// <summary>
        /// Заблокированны ли все активные элементы формы, кроме закрытия, например пока идет обновление
        /// </summary>
        private bool ActiveElementBlock = false;

        /// <summary>
        /// Рассматриваемый подробно ордер
        /// </summary>
        public TradeOrder EditOrder { get; private set; }
        /// <summary>
        /// Копия ордера до изменений
        /// </summary>
        public TradeOrder EditOrderOnStart { get; private set; }

        //public float SizeOrders_Heigth;

        //public float SizeAddThingList_Width;

        private int TabIndex;

        private GridBox<TransferableOneWay> AddThingGrid;

        private GridBox<TradeOrder> OrdersGrid;

        private string EditOrderTitle = "";

        private Vector2 ScrollPositionEditOrder;

        private bool EditOrderIsMy
        {
            get { return EditOrder != null && EditOrder.Owner.Login == SessionClientController.My.Login; }
        }

        /// <summary>
        /// Условия для торговли или выставления ордера соблюдены
        /// </summary>
        private bool EditOrderToTrade;
        /// <summary>
        /// Новое значение в CountReady своей сделки, либо покупаемый объем чужой сделки
        /// </summary>
        private int EditOrderCountReady;
        /// <summary>
        /// Возможный максимум в новом значении в CountReady своей сделки
        /// </summary>
        //private int EditOrderCountMax;

        private class OrderEditBuffer
        {
            private Dialog_Exchenge That;
            public ThingTrade Thing;
            public TextFieldNumericBox TextField1;
            public TextFieldNumericBox TextField2;
            public int TotalCount => Thing.Count * That.EditOrderCountReady;

            public OrderEditBuffer(Dialog_Exchenge that, ThingTrade thing)
            {
                That = that;
                Thing = thing;
                TextField1 = new TextFieldNumericBox(
                    () => Thing.Count,
                    (cnt) => { 
                        Thing.Count = cnt;
                        //todo? ttt
                        //if (Thing.NotTrade)
                        that.EditOrderChange();
                    },
                    () => !that.ActiveElementBlock || !that.EditOrderIsMy)
                { 
                    ShowButton = false,
                    Min = 1
                };
                TextField2 = new TextFieldNumericBox(
                    () => TotalCount,
                    (cnt) => 
                    {
                        Log.Message($"OrderEditBuffer that.EditOrderCountReady{that.EditOrderCountReady} * cnt{cnt} / TotalCount{TotalCount}");
                        if (that.EditOrderCountReady <= 0) that.EditOrderCountReady = 1;
                        if (TotalCount > 0) that.EditOrderCountReady = that.EditOrderCountReady * cnt / TotalCount;
                        if (that.EditOrderCountReady <= 0) that.EditOrderCountReady = 1;

                        //if (Thing.NotTrade)
                        that.EditOrderChange();
                    },
                    () => !that.ActiveElementBlock)
                {
                    ShowButton = false,
                    Min = 1
                };
            }
        }

        private Dictionary<long, OrderEditBuffer> EditOrderEditBuffer;

        private TextFieldNumericBox EditOrderCountReadyBuffer = null;

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

        //public override void PreOpen()
        //{
        //    base.PreOpen();
        //    //EnsureSettingsHaveValidFiles(ClientController.Settings);
        //}

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(1024f, (float)Verse.UI.screenHeight);
            }
        }

        private Dialog_Exchenge()
        {
            //SizeOrders_Heigth = (int)(InitialSize.y / 2f);
            //SizeAddThingList_Width = (int)(InitialSize.x / 2f);
            closeOnCancel = false;
            closeOnAccept = false;
        }

        /// <summary>
        /// Открытие окна с использованием вещей из каравана игрока
        /// </summary>
        public Dialog_Exchenge(Caravan caravan)
            : this()
        {
            SetPlaceCurrent(caravan);
            Init();
        }

        /// <summary>
        /// Открытие окна с использованием вещей из поселения игрока
        /// </summary>
        public Dialog_Exchenge(MapParent settlement)
            : this()
        {
            SetPlaceCurrent(settlement);
            Init();
        }

        /// <summary>
        /// Открытие окна без использованием вещей (только центруемся по точке с караваном или поселением другого игрока)
        /// </summary>
        public Dialog_Exchenge(WorldObjectBaseOnline onlyPlace)
            : this()
        {
            SetPlaceCurrent(onlyPlace);
            Init();
        }

        /// <summary>
        /// Открытие окна с использованием купленных ранее вещей
        /// </summary>
        public Dialog_Exchenge(TradeThingsOnline tradeThings)
            : this()
        {
            SetPlaceCurrent(tradeThings);
            Init();
        }

        /// <summary>
        /// Открытие окна с автоопределением
        /// </summary>
        public Dialog_Exchenge(WorldObject worldObject)
            : this()
        {
            SetPlaceCurrent(worldObject);
            Init();
        }

        private void SetPlaceCurrent(WorldObject placeWorldObject)
        {
            try
            {
                Loger.Log($"Client ExchengeEdit SetPlaceCurrent " + placeWorldObject.GetType().Name, Loger.LogLevel.EXCHANGE);
                WorldObjectCurrent = placeWorldObject;

                PlaceCurrent = new Place();
                WorldObject wo = placeWorldObject;
                /*if (PlaceMap != null)
                    wo = PlaceMap.info.parent;
                else
                    wo = PlaceCaravan;*/
                PlaceCurrent.Name = wo.LabelCap;
                PlaceCurrent.PlaceServerId = UpdateWorldController.GetServerInfo(wo)?.PlaceServerId ?? 0;
                PlaceCurrent.ServerName = SessionClientController.My.ServerName;
                PlaceCurrent.Tile = wo.Tile;
                PlaceCurrent.DayPath = 0;

                WorldObjectsTile = ExchengeUtils.WorldObjectsByTile(wo.Tile)
                    .Where(o => o is TradeThingsOnline
                        || (o.Faction?.IsPlayer ?? false) && (o is Settlement || o is Caravan))
                    .ToList();
                WorldObjectCaravanOnlinesTile = ExchengeUtils.WorldObjectsByTile(wo.Tile)
                    .Where(o => o is CaravanOnline)
                    .Cast<CaravanOnline>()
                    .ToList();

                if (wo is TradeThingsOnline)
                    WorldObjectStorageCurrentTile = wo;
                else
                {
                    WorldObjectStorageCurrentTile = WorldObjectsTile.FirstOrDefault(w => w is TradeThingsOnline);
                }
                if (WorldObjectStorageCurrentTile == null)
                {   // с Id = 0 это заплатка, признак, что при перемещении сюда нужно создать, см MoveSelectThings
                    WorldObjectStorageCurrentTile = new TradeThingsOnline() { Tile = wo.Tile };
                }

                if (wo is Settlement)
                {
                    AllThings = GameUtils.GetAllThings((wo as Settlement).Map);
                    AllThingsOriginalEntry = null;
                }
                else if (wo is Caravan)
                {
                    AllThings = GameUtils.GetAllThings(wo as Caravan);
                    AllThingsOriginalEntry = null;
                }
                else if (wo is TradeThingsOnline)
                {
                    //добавляем безнал искуственно, чтобы иметь возможность вывести его через интерфейс
                    //при расчете GetAvailableThings учитывает это и повторно безнал для TradeThingsOnline не добавляет
                    AllThings = GameUtils.GetAllThings(wo as TradeThingsOnline)
                        .Concat(GameUtils.GetCashlessBalanceThingList(SessionClientController.Data.CashlessBalance))
                        .ToList();
                    AllThingsOriginalEntry = (wo as TradeThingsOnline).TradeThings.Things;
                }
                else
                {
                    AllThings = new List<Thing>();
                    AllThingsOriginalEntry = null;
                }
            }
            catch (Exception exp)
            {
                ExceptionUtil.ExceptionLog(exp, "Dialog_Exchenge SetPlaceCurrent Exception");
            }
        }

        private void Init()
        {
            UpdateOrdersList();
        }

        private void SetEditOrder(TradeOrder order)
        {
            EditOrder = order;
            EditOrderOnStart = order?.Clone();
            EditOrderEditBuffer = new Dictionary<long, OrderEditBuffer>();
            EditOrderCountReady = EditOrder?.CountReady ?? 0;
            EditOrderCountReadyBuffer = null;
            EditOrderChange();
        }

        private IEnumerable<Thing> GetAvailableThingsInEditOrder()
        {
            using (var gameError = GameUtils.NormalGameError())
            {
                return !EditOrderIsMy || EditOrderOnStart == null || EditOrderOnStart.Id <= 0
                  ? new List<Thing>()
                  : EditOrderOnStart.SellThings
                    .Select(t =>
                    {
                        var th = t.CreateThing();
                        th.stackCount = th.stackCount * EditOrderOnStart.CountReady;
                        return th;
                    }).ToList();
            }
        }

        /// <summary>
        /// Вещи, которыми мы распологаем для сделки: AllThings + те что уже в нашей сделки + безнал
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Thing> GetAvailableThings(bool withoutInEditOrder = false, bool withoutCashless = false)
        {
            var thingsAlreadyInEditOrder = withoutInEditOrder
                ? new List<Thing>()
                : GetAvailableThingsInEditOrder();

            var cashlessBalanceThing = GameUtils.GetCashlessBalanceThingList(
                    withoutCashless || (WorldObjectCurrent is TradeThingsOnline) ? 0 //если это красное яблоко, то там уже добавлен безнал в SetPlaceCurrent
                    : SessionClientController.Data.CashlessBalance);

            //вещи которыми мы распологаем
            return AllThings //вещи в источнике
                .Concat(thingsAlreadyInEditOrder) //вещи в этой же сделке, если она наша и мы её редактируем
                .Concat(cashlessBalanceThing); //добавляем безнал как вещь
        }

        private void EditOrderChange()
        {
            if (EditOrder == null) return;

            //определяем кол-во повторов которое мы можем установить максимально
            EditOrderToTrade = GameUtils.ChechToTrade(
                //вещи которые мы должны отдать, для реализации сделки
                EditOrderIsMy ? EditOrder.SellThings : EditOrder.BuyThings
                , //вещи которыми мы распологаем
                    GetAvailableThings()
                , null
                , out _) != null //15.12.2022 было out EditOrderCountMax
                //15.12.2022 было && EditOrderCountMax > 0
                && (EditOrder.SellThings?.Count ?? 0) > 0
                && (EditOrder.BuyThings?.Count ?? 0) > 0;

            //15.12.2022 было if (!EditOrderToTrade) EditOrderCountMax = 0;

            if (EditOrderIsMy)
            {
                //15.12.2022 было EditOrderCountReady = EditOrderCountReady > EditOrderCountMax ? EditOrderCountMax : EditOrderCountReady < 0 ? 0 : EditOrderCountReady;
                if (EditOrderCountReady == 0 ) EditOrderCountReady = 1;
            }
        }

        private void LoaderOrdersCancel()
        {
            if (LoaderOrders == null) return;
            try
            {
                LoaderOrders.TaskFinish = null;
                LoaderOrders.TaskError = null;
                LoaderOrders.Cancel();
            }
            catch { }
            Orders = new List<TradeOrder>();
            StatusLoadOrders = null;
            LoaderOrders = null;
            OrdersGrid = null;
        }

        public override void PostClose()
        {
            LoaderOrdersCancel();
            base.PostClose();
            if (PostCloseAction != null) PostCloseAction();
        }

        public override void DoWindowContents(Rect inRect)
        {
            try
            {
                var margin = 5f;
                var btnSize = new Vector2(140f, 35f);
                var buttonYStart = inRect.height - btnSize.y;
                Text.Font = GameFont.Small;

                if (Widgets.ButtonText(new Rect(inRect.width - btnSize.x, 0, btnSize.x, btnSize.y), "OCity_Dialog_Exchenge_Close".TranslateCache()))
                {
                    Close();
                }

                Rect rect = new Rect(0f, 0f, inRect.width, btnSize.y);
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;
                //Widgets.Label(rect, "OCity_Dialog_Exchenge_Trade_Orders".TranslateCache()); //Тоговые ордера
                Widgets.Label(rect,
                    (StatusLoadOrders != null) ? "OCity_Dialog_Exchenge_Trade_OrdersLoad".TranslateCache() + " " + StatusLoadOrders
                    : (Orders == null || Orders.Count == 0) ? "OCity_Dialog_Exchenge_No_Warrants".TranslateCache()
                    : "OCity_Dialog_Exchenge_Active_Orders".TranslateCache(Orders.Count.ToString()));

                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperLeft;

                //Log.Message((InitialSize.y / 2f).ToString());
                //область сверху - ордера
                var regionRectOut = new Rect(inRect.x, inRect.y + btnSize.y + margin, inRect.width, InitialSize.y - 455f);

                var regionRect = new Rect(regionRectOut);
                GUI.BeginGroup(regionRect);
                var regionRectIn = regionRect.AtZero();
                DoWindowOrders(regionRectIn);
                GUI.EndGroup();

                regionRectOut = new Rect(regionRectOut.x
                    , regionRectOut.yMax + margin
                    , regionRectOut.width
                    , inRect.height - (regionRectOut.yMax + margin));

                var screenRect = new Rect(regionRectOut.x, regionRectOut.y + 31f, 400f, 0);
                var tabRect = new Rect(regionRectOut.x, regionRectOut.y + 31f, regionRectOut.width, regionRectOut.height - 31f);

                List<TabRecord> list = new List<TabRecord>();
                list.Add(new TabRecord("OCity_DialogExchenge_Things".Translate(), () => { TabIndex = 0; }, TabIndex == 0));
                list.Add(new TabRecord("OCity_DialogExchenge_Deal".Translate(), () => { TabIndex = 1; }, TabIndex == 1));

                Widgets.DrawMenuSection(tabRect);
                if (TabIndex == 0) // на 1 вкладке рисуем вертикальную черту
                {
                    var rt0 = new Rect(tabRect);
                    rt0.xMin += 251;
                    Widgets.DrawMenuSection(rt0);
                }
                TabDrawer.DrawTabs(screenRect, list);

                regionRect = tabRect.ContractedBy(margin * 2f);
                GUI.BeginGroup(regionRect);
                regionRectIn = regionRect.AtZero();
                if (TabIndex == 0) DoTab0ThingList(regionRectIn);
                else if (TabIndex == 1) DoTab1EditOrder(regionRectIn);
                GUI.EndGroup();

                /*
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
                */

                Text.Anchor = TextAnchor.UpperLeft;
            }
            catch (Exception e)
            {
                ExceptionUtil.ExceptionLog(e, "Dialog_Exchenge DoWindowContents");
            }
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
                OrdersGrid = new GridBox<TradeOrder>();
                //Transferables = GameUtils.DistinctThings(AllThings);
                //var dicMaxCount = Transferables.ToDictionary(t => t, t => t.MaxCount);
                OrdersGrid.DataSource = Orders;
                OrdersGrid.LineHeight = 24f;
                OrdersGrid.ShowSelected = true;
                OrdersGrid.Tooltip = null;
                OrdersGrid.OnClick += (int line, TradeOrder item) =>
                {
                    SetEditOrder(item);
                    if (EditOrderIsMy)
                        EditOrderTitle = "OCity_Dialog_Exchenge_Edit".TranslateCache();
                    else
                        EditOrderTitle = "OCity_Dialog_Exchenge_Viewing_Orders".TranslateCache() + " " + item.Owner.Login;
                };
                OrdersMenu = null;
                OrdersGrid.OnDrawLine = (int line, TradeOrder item, Rect rectLine) =>
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
                            ? "OCity_Dialog_Exchenge_Deal_Open_Everyone".TranslateCache()
                            : "OCity_Dialog_Exchenge_Deal_Open_Specific".TranslateCache(string.Join(", ", item.PrivatPlayers.Select(p => p.Login).ToArray())));
                        Widgets.Checkbox(rect2.position, ref flag, 24f, false);

                        //Ник продавца
                        rect2 = new Rect(rectLine.x + currentWidth - 200f, rectLine.y, 200f, rectLine.height);
                        if (showTop)
                        {
                            OrdersMenu = new Dictionary<Rect, string>();
                            var rect2t = new Rect(rect2.x, 0f, rect2.width, rect2.height);
                            OrdersMenu.Add(rect2t, "OCity_Dialog_Exchenge_Seller".TranslateCache());
                        }
                        currentWidth -= 200f;
                        var rect2p = new Rect(rect2.x, 0f, 24f, 24f);
                        if (Widgets.ButtonImage(rect2p, GeneralTexture.Get.ByName("pl_" + item.Owner.Login)))
                        {
                            Dialog_InfoPlayer.ShowInfoPlayer(item.Owner.Login);
                        }
                        rect2p = new Rect(rect2.x + 24f, 0f, rect2.width - 24f, rect2.height);
                        TooltipHandler.TipRegion(rect2p, item.Owner.Login + Environment.NewLine + "OCity_Dialog_Exchenge_BeenOnline".TranslateCache() + item.Owner.LastSaveTime.ToGoodUtcString());
                        Widgets.Label(rect2p, item.Owner.Login 
                            + (item.Owner.Login == SessionClientController.My.Login ? " (" + "OCity_Dialog_Exchenge_You".TranslateCache().ToString() + ")" : ""));

                        //Расстояние где торгуют, название места
                        rect2 = new Rect(rectLine.x + currentWidth - 200f, rectLine.y, 200f, rectLine.height);
                        if (showTop)
                        {
                            var rect2t = new Rect(rect2.x, 0f, rect2.width, rect2.height);
                            OrdersMenu.Add(rect2t, "OCity_Dialog_Exchenge_Location".TranslateCache());
                        }
                        currentWidth -= 200f;

                        rect2p = new Rect(rect2.x, 0f, 24f, rect2.height);
                        if (ActiveElementBlock) GUI.color = Color.gray;
                        if (Widgets.ButtonImage(rect2p, GeneralTexture.Waypoint))
                        {
                            GUI.color = Color.white;
                            if (ActiveElementBlock) return;
                            if (GameUtils.CameraJumpWorldObject(item.Tile))
                            {
                                Close();
                                //Find.WindowStack.Add(new Dialog_Exchenge(woi));
                            }
                        }
                        GUI.color = Color.white;
                        string text = "";
                        if (item.Place.DayPath > 0)
                        {
                            //text = item.Place.DayPath.ToStringDecimalIfSmall() + "OCity_Dialog_Exchenge_Days".TranslateCache();
                            text =  "OCity_Dialog_Exchenge_Tile".TranslateCache() + " " + ((int)item.Place.DayPath).ToString() + " "
                                + "OCity_Dialog_Exchenge_To".TranslateCache() + " ";
                        }
                        text += item.Place.Name;
                        rect2p = new Rect(rect2.x + 24f, 0f, rect2.width - 24f, rect2.height);
                        TooltipHandler.TipRegion(rect2p, "OCity_Dialog_Exchenge_Location_Goods".TranslateCache() + Environment.NewLine + text);
                        Widgets.Label(rect2p, text);

                        //Кол-во повторов
                        rect2 = new Rect(rectLine.x + currentWidth - 60f, rectLine.y, 60f, rectLine.height);
                        if (showTop)
                        {
                            var rect2t = new Rect(rect2.x, 0f, rect2.width, rect2.height);
                            OrdersMenu.Add(rect2t, "OCity_Dialog_Exchenge_Number".TranslateCache());
                        }
                        currentWidth -= 60f;
                        text = item.CountReady.ToString();
                        TooltipHandler.TipRegion(rect2, "OCity_Dialog_Exchenge_Max_Repetition_Transaction".TranslateCache() + Environment.NewLine + text);
                        Widgets.Label(rect2, text);

                        //Иконки и перечень (описание в подсказке)
                        rect2 = new Rect(rectLine.x, rectLine.y, currentWidth / 2f, rectLine.height); //от 0 до половины остатка
                        if (showTop)
                        {
                            var rect2t = new Rect(rect2.x, 0f, rect2.width, rect2.height);
                            OrdersMenu.Add(rect2t, "OCity_Dialog_Exchenge_Acquire".TranslateCache());
                        }
                        var rect3 = new Rect(rect2.x, rect2.y, rectLine.height, rectLine.height);
                        for (int i = 0; i < item.SellThings.Count; i++)
                        {
                            var th = item.SellThings[i];
                            //для отображения реальных иконок расскоментировать тут и ниже в UpdateOrdersList где st.DataThing == null
                            //if (th.Affiliation != PawnAffiliation.Thing && th.DataThing != null)
                            //    GameUtils.DravLineThing(rect3, th.DataThing, false);
                            //else
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
                            OrdersMenu.Add(rect2t, "OCity_Dialog_Exchenge_GiveTo".TranslateCache());
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
            //Text.Anchor = TextAnchor.MiddleCenter; //была надпись о кол-во ордеров под заголовком
            //Widgets.Label(rect, (Orders == null || Orders.Count == 0) ? "OCity_Dialog_Exchenge_No_Warrants".TranslateCache() : "OCity_Dialog_Exchenge_Active_Orders".TranslateCache(Orders.Count.ToString()));
            //фильтры
            var rectFilter = new Rect(rect);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(rectFilter, "OCity_Dialog_Exchenge_Trade_FilterDistance".TranslateCache()); // Фильтры: Расстояние
            rectFilter.xMin += 115f;
            rectFilter.width = 60f;
            Widgets.TextFieldNumeric<int>(rectFilter.ContractedBy(2f), ref FilterTileLength, ref FilterTileLengthBuffer, 0f, 9999f);
            rectFilter.x += rectFilter.width + 10f;

            rectFilter.width = 50f;
            Widgets.Label(rectFilter, "OCity_Dialog_Exchenge_Trade_FilterSell".TranslateCache());//Продажа
            rectFilter.x += rectFilter.width + 5f;
            //rectFilter.width = 200f;
            //FilterSell = GUI.TextField(rectFilter.ContractedBy(1f), FilterSell ?? "", 50);
            ShowSelectThingDef(ref rectFilter, FilterSellThing, (name, th) => { FilterSell = name; FilterSellThing = th; });
           
            rectFilter.width = 50f;
            Widgets.Label(rectFilter, "OCity_Dialog_Exchenge_Trade_FilterBuy".TranslateCache());//Покупка
            rectFilter.x += rectFilter.width + 5f;
            //rectFilter.width = 200f;
            //FilterBuy = GUI.TextField(rectFilter.ContractedBy(2f), FilterBuy ?? "", 50);
            ShowSelectThingDef(ref rectFilter, FilterBuyThing, (name, th) => { FilterBuy = name; FilterBuyThing = th; });

            //кнопка "Выбрать"
            rect.xMin += inRect.width - 140f;
            Text.Anchor = TextAnchor.MiddleCenter;
            if (LoaderOrders != null)
            {
                if (ActiveElementBlock) GUI.color = Color.gray;
                if (Widgets.ButtonText(rect.ContractedBy(1f), "OCity_Dialog_CreateWorld_BtnCancel".TranslateCache(), true, false, true))
                {
                    GUI.color = Color.white;
                    if (ActiveElementBlock) return;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);

                    Loger.Log("Client ExchengeLoad LoaderOrders.Cancel", Loger.LogLevel.EXCHANGE);
                    LoaderOrdersCancel();
                    return;
                }
                GUI.color = Color.white;
            }
            else
            {
                if (ActiveElementBlock) GUI.color = Color.gray;
                if (Widgets.ButtonText(rect.ContractedBy(1f), "OCity_Dialog_Exchenge_Update".TranslateCache(), true, false, true))
                {
                    GUI.color = Color.white;
                    if (ActiveElementBlock) return;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);

                    ActiveElementBlock = true;
                    UpdateWorldAndOrdersList();
                    return;
                }
                GUI.color = Color.white;
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

        private void UpdateWorldAndOrdersList(bool useEditOrderChange = false)
        {
            Loger.Log("Client UpdateWorldSafelyRun set");
            SessionClientController.UpdateWorldSafelyRun = () =>
            {
                try
                {
                    Loger.Log("Client UpdateWorldSafelyRun 1");
                    UpdateOrdersList();
                    //обновление яблока после обновления мира
                    //if (WorldObjectCurrent is TradeThingsOnline)
                    {
                        SetPlaceCurrent(WorldObjectCurrent);
                        if (useEditOrderChange) EditOrderChange();
                        AddThingGrid = null;
                    }
                }
                catch (Exception e)
                {
                    Log.Error("UpdateWorldAndOrdersList " + e.ToString());
                }
                ActiveElementBlock = false;
            };
        }

        private class ThingPlace
        {
            public WorldObject WO;
            public string Text => WO.LabelCap;
            public override string ToString()
            {   
                if (WO is Caravan)
                    return "<img CaravanOnExpanding> " + Text;
                else if (WO is Settlement)
                    return "<img HomeAreaOn> " + Text; //ColonyOnExpanding
                else if (WO is TradeThingsOnline)
                    return "<img AppleF> " + Text; //Things\Item\Drug\Yayo_c.png Apple
                else
                    return "<img Waypoint> " + Text; 
            }
        }
        private ListBox<ThingPlace> lbThingListPlaces = null;
        /// <summary>
        /// Первая вкладка со списком вещей
        /// </summary>
        private void DoTab0ThingList(Rect inRect)
        {
            if (lbThingListPlaces == null)
            {
                //первый запуск
                lbThingListPlaces = new ListBox<ThingPlace>();
                lbThingListPlaces.Area = new Rect(inRect.x
                    , inRect.y + 30f
                    , 250f
                    , inRect.height - 30f);
                lbThingListPlaces.LineHeight = 28f;
                lbThingListPlaces.Tooltip = (item) => item.Text;
                lbThingListPlaces.OnClick += (index, item) =>
                {
                    try
                    {
                        SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                        //Переключаем список вещей на wo
                        SetPlaceCurrent(item.WO);
                        SetEditOrder(null);
                        AddThingGrid = null;
                    }
                    catch (Exception exp)
                    {
                        ExceptionUtil.ExceptionLog(exp, "DoWindowAddThingList button ...");
                    }
                };
                var listWO = WorldObjectsTile.ToList();
                if (!listWO.Any(wo => wo is TradeThingsOnline))
                {
                    listWO.Add(WorldObjectStorageCurrentTile);
                }
                lbThingListPlaces.DataSource = listWO.Select(wo => new ThingPlace() { WO = wo }).ToList();
                lbThingListPlaces.UsePanelText = true;
                lbThingListPlaces.SelectedIndex = 0;
                if (lbThingListPlaces.DataSource.Count > 0)
                {
                    //Переключаем список вещей на wo
                    SetPlaceCurrent(lbThingListPlaces.DataSource[0].WO);
                    SetEditOrder(null);
                    AddThingGrid = null;
                }
            }
            lbThingListPlaces.Drow();

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperCenter;
            Vector2 vector = Find.WorldGrid.LongLatOf(PlaceCurrent.Tile);
            var planeTitle = vector.y.ToStringLatitude() + " " + vector.x.ToStringLongitude();
            Widgets.Label(new Rect(inRect.x, inRect.y, 250f, 30f), planeTitle);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;

            if (AddThingGrid == null)
            {
                //инициализация
                AddThingGrid = new GridBox<TransferableOneWay>();
                //Список объединенных в группы одинаковых вещей
                var transferables = AllThings.DistinctToTransferableOneWays();
                var dicMaxCount = transferables.ToDictionary(t => t, t => t.MaxCount);
                var catchMarketValue = transferables.ToDictionary(t => t, t => t.AnyThing.MarketValue.ToStringMoney());
                AddThingGrid.DataSource = transferables;
                AddThingGrid.LineHeight = 24f;
                AddThingGrid.Tooltip = null;
                var dicComponent = transferables.ToDictionary(t => t, t => new TextFieldNumericBox(t, () => !ActiveElementBlock, AddThingGridValueChanged) { Max = dicMaxCount[t] });
                AddThingGrid.OnDrawLine = (int line, TransferableOneWay item, Rect rectLine) =>
                {
                    try
                    {
                        if (!item.HasAnyThing) return;
                        float currentWidth = rectLine.width;

                        //ввод кол-во и кнопки < >
                        var componentWidth = 60f + rectLine.height * 2f;
                        currentWidth -= componentWidth;
                        dicComponent[item].Drow(new Rect(rectLine.width - componentWidth, 0f, componentWidth, rectLine.height));

                        //кол-во
                        var rect3 = new Rect(currentWidth - 60f, 0f, 60f, rectLine.height);
                        Text.WordWrap = false;
                        Text.Anchor = TextAnchor.MiddleRight;
                        Widgets.Label(rect3, dicMaxCount[item].ToString());
                        currentWidth -= 60f;

                        //цена
                        rect3 = new Rect(currentWidth - 60f, 0f, 80f, rectLine.height);
                        Text.Anchor = TextAnchor.MiddleLeft;
                        Widgets.Label(rect3, catchMarketValue[item]);
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

            var buttonWidth = 100f;
            var buttonInner = 64f;
            var buttonArea = new Rect(inRect.width - buttonWidth, 0, buttonWidth, inRect.height);

            var rect = new Rect(buttonArea.x + (buttonWidth - buttonInner) / 2, buttonArea.y, buttonInner, buttonInner);
            var rectText = new Rect(buttonArea.x, buttonArea.y, buttonWidth, 18);

            //кнопка "Выбрать"
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.MiddleCenter;
            if (ActiveElementBlock || !AddThingListOK || EditOrder?.SellThings?.Count >= 6) GUI.color = Color.gray;
            if (Widgets.ButtonImage(rect, GeneralTexture.OCE_Sell))
            {
                GUI.color = Color.white;
                if (ActiveElementBlock || !AddThingListOK || EditOrder?.SellThings?.Count >= 6) return;
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                AddThingListApply();
                return;
            }
            rect.y += rect.height;
            rectText.y += rect.height;
            Text.Anchor = TextAnchor.MiddleCenter;
            if (ActiveElementBlock || !AddThingListOK || EditOrder?.SellThings?.Count >= 6) GUI.color = Color.red;
            Widgets.Label(rectText, "OCity_DialogExchenge_Sell".Translate());
            rect.y += rectText.height;
            rectText.y += rectText.height;
            Widgets.Label(rectText, "OCity_DialogExchenge_OnExchange".Translate());
            GUI.color = Color.white;
            rect.y += rectText.height;
            rectText.y += rectText.height;
            rect.y += 10;
            rectText.y += 10;

            //кнопка "Переместить"
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            if (ActiveElementBlock) GUI.color = Color.gray;
            if (Widgets.ButtonImage(rect, GeneralTexture.OCE_Swap))
            {
                GUI.color = Color.white;
                if (ActiveElementBlock) return;
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                FloatMenuPlaceSelect((wo) =>
                {
                    //Перемещаем выделенные вещи из WorldObjectCurrent в wo
                    ActiveElementBlock = true;
                    MoveSelectThings(wo);
                }, true, true);
                return;
            }
            rect.y += rect.height;
            rectText.y += rect.height;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rectText, "OCity_DialogExchenge_Move".Translate());
            rect.y += rectText.height;
            rectText.y += rectText.height;
            Widgets.Label(rectText, "OCity_DialogExchenge_InThisPosition".Translate());
            rect.y += rectText.height;
            rectText.y += rectText.height;
            rect.y += 10;
            rectText.y += 10;

            //кнопка Доставка в удаленную точку
            if (WorldObjectCurrent is TradeThingsOnline)
            {
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.MiddleCenter;
                if (ActiveElementBlock) GUI.color = Color.gray;
                if (Widgets.ButtonImage(rect, GeneralTexture.OCE_Trans))
                {
                    GUI.color = Color.white;
                    if (ActiveElementBlock) return;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    TransferThingList();
                    return;
                }
                GUI.color = Color.white;
                rect.y += rect.height;
                rectText.y += rect.height;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rectText, "OCity_DialogExchenge_Delivery".Translate());
                rect.y += rectText.height;
                rectText.y += rectText.height;
                Widgets.Label(rectText, "OCity_DialogExchenge_ToARemotePoint".Translate());
                rect.y += rectText.height;
                rectText.y += rectText.height;
                rect.y += 10;
                rectText.y += 10;
            }

            if (!(WorldObjectCurrent is TradeThingsOnline))
            {
                //кнопка Выбросить
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.MiddleCenter;
                if (ActiveElementBlock) GUI.color = Color.gray;
                if (Widgets.ButtonImage(rect, GeneralTexture.OCE_Del))
                {
                    GUI.color = Color.white;
                    if (ActiveElementBlock) return;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    ThrowThingList();
                    return;
                }
                GUI.color = Color.white;
                rect.y += rect.height;
                rectText.y += rect.height;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rectText, "OCity_DialogExchenge_Destroy".Translate());
                rect.y += rectText.height;
                rectText.y += rectText.height;
                Widgets.Label(rectText, "OCity_DialogExchenge_ForTenPercentThePrice".Translate());
                rect.y += rectText.height;
                rectText.y += rectText.height;
                rect.y += 10;
                rectText.y += 10;
            }

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            AddThingGrid.Area = new Rect(lbThingListPlaces.Area.width + 4, 0, inRect.width - lbThingListPlaces.Area.width - buttonWidth - 8, inRect.height);
            AddThingGrid.Drow();
        }

        private bool AddThingListOK = true;
        private void AddThingGridValueChanged(TextFieldNumericBox editBox, int value)
        {
            AddThingListOK = true;
            if (AddThingGrid.DataSource == null) return;

            if (AddThingGrid.DataSource.Where(item => item.CountToTransfer > 0).Take(2).Count() > 1)
            {
                AddThingListOK = false;
                return;
            }

            if (!EditOrderIsMy) return;

            foreach (var item in AddThingGrid.DataSource)
            {
                if (item.CountToTransfer == 0) continue;
                var th = ThingTrade.CreateTrade(item.AnyThing, item.CountToTransfer);
                if (EditOrder.SellThings.Any(oth => oth.MatchesThingTrade(th)))
                {
                    AddThingListOK = false;
                    return;
                }
            }
        }

        private void FloatMenuPlaceSelect(Action<WorldObject> action, bool insertTradeThingsOnline, bool insertCaravanOnlinesOnTile = false)
        {
            var editOrder = EditOrder;
            var listWO = WorldObjectsTile
                .Where(wo => wo != WorldObjectCurrent)
                .ToList();
            if (insertTradeThingsOnline
                && !(WorldObjectCurrent is TradeThingsOnline)
                && !listWO.Any(wo => wo is TradeThingsOnline))
            {
                listWO.Add(WorldObjectStorageCurrentTile);
            }
            if (insertCaravanOnlinesOnTile)
            {
                listWO.AddRange(WorldObjectCaravanOnlinesTile);
            }

            var listFO = listWO
                .Select(wo => 
                    wo is CaravanOnline
                    ? ExchengeUtils.ExchangeOfGoods_GetFloatMenu(wo as CaravanOnline, () =>
                    {
                        SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                        action(wo);
                    })
                    : new FloatMenuOption(wo.LabelCap, () =>
                    {
                        SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                        action(wo);
                    }))
                .ToList();
            if (listFO.Count == 0) return;
            var menu = new FloatMenu(listFO);
            Find.WindowStack.Add(menu);
        }

        /// <summary>
        /// Добавляем из наших вещей в сделку
        /// </summary>
        private void AddThingListApply()
        {
            bool isCreated = false;
            if (!EditOrderIsMy)
            {
                isCreated = true;
                CreateMyOrder();
            }

            if (!EditOrderIsMy || AddThingGrid.DataSource == null) return;
            //выбранное в AddThingGrid.DataSource, где кол-во выбранного в поле CountToTransfer

            var newCountReady =
                !EditOrder.SellThings.Any(i => i.IsPawn || i.IsCorpse)
                && !EditOrder.BuyThings.Any(i => i.IsPawn || i.IsCorpse);

            //берем грубо item.AnyThing, т.к. не делаем деспавна, а отбираем только для отображения ThingTrade в редактируемой сделке
            //при создании из этого же набора AddThingGrid.DataSource будет взято с точным соответствием <Thing,кол-во>
            int newEditOrderCountReady = EditOrderCountReady;
            ThingTrade newThingTrade = null;
            foreach (var item in AddThingGrid.DataSource)
            {
                if (item.CountToTransfer == 0) continue;
                newEditOrderCountReady = item.CountToTransfer;
                newThingTrade = ThingTrade.CreateTrade(item.AnyThing, newCountReady ? 1 : item.CountToTransfer);
                EditOrder.SellThings.Add(newThingTrade);
                break;
            }
            //Log.Message($" newCountReady={newCountReady} EditOrderCountReady={EditOrderCountReady} newEditOrderCountReady={newEditOrderCountReady}");
            //Меняем всю сделку так, чтобы кол-во повторов было newEditOrderCountReady, а кол-во общее кол-во вещей не сильно поменялось
            if (newCountReady)
            {
                foreach (var item in EditOrder.SellThings)
                {
                    if (item == newThingTrade) continue;
                    //Log.Message($"S item.Count={item.Count} -> {item.Count * EditOrderCountReady / newEditOrderCountReady}");
                    item.Count = item.Count * EditOrderCountReady / newEditOrderCountReady;
                    if (item.Count <= 0) item.Count = 1;
                }
                foreach (var item in EditOrder.BuyThings)
                {
                    //Log.Message($"B item.Count={item.Count} -> {item.Count * EditOrderCountReady / newEditOrderCountReady}");
                    item.Count = item.Count * EditOrderCountReady / newEditOrderCountReady;
                    if (item.Count <= 0) item.Count = 1;
                }
            }

            /*if (isCreated)*/
            EditOrderChange();
            //todo? ttt
            AddThingGrid = null;
            TabIndex = 1;
            //SetEditOrder(EditOrder);
            if (newCountReady)
            {
                EditOrderCountReady = newEditOrderCountReady;
            }
        }

        private void ThrowThingList()//кнопка Выбросить
        {
            if (AddThingGrid.DataSource == null) return;

            var dropCost = 0f;
            foreach (var item in AddThingGrid.DataSource)
            {
                if (item.CountToTransfer == 0) continue;
                var th = ThingTrade.CreateTrade(item.AnyThing, item.CountToTransfer);
                if (th.IsCorpse || th.IsPawn) return;
                if (th.GameCost < 1) continue;

                dropCost += th.GameCost * item.CountToTransfer;
            }

            if (dropCost == 0) return;

            if (ExchengeUtils.MoveSelectThings(WorldObjectCurrent, null, AddThingGrid.DataSource, () =>
            {
                //обновляем список
                SetPlaceCurrent(WorldObjectCurrent);
                AddThingGrid = null;
            }))
            {
                var CostPercent = 10f;
                var summ = (int)(dropCost * CostPercent / 100f);
                //Log.Message("tttt " + (int)(dropCost * 0.2f));
                //если вещи были в яблоке, то возвращается % стоимости
                if (WorldObjectCurrent is TradeThingsOnline)
                {
                    Loger.Log($"Client ThrowThingList +{summ} server", Loger.LogLevel.EXCHANGE);
                    var errorMessage = SessionClientController.CommandSafely((connect) =>
                    {
                        return connect.ExchengeStorage(
                            new List<ThingTrade>() {
                                ThingTrade.CreateTrade(MainHelper.CashlessThingDef, 0f, QualityCategory.Awful, summ)
                            }
                            , null
                            , WorldObjectCurrent.Tile);
                    });
                }
                else
                {
                    var ths = GameUtils.GetCashlessBalanceThingList(summ);
                    if (WorldObjectCurrent is Caravan || WorldObjectCurrent is Settlement)
                    {
                        Loger.Log($"Client ThrowThingList +{summ} game", Loger.LogLevel.EXCHANGE);
                        if (WorldObjectCurrent is Caravan)
                            ExchengeUtils.SpawnThings(ths, WorldObjectCurrent as Caravan);
                        else
                            ExchengeUtils.SpawnThings(ths, (WorldObjectCurrent as Settlement).Map);
                    }
                }
            }

            SetPlaceCurrent(WorldObjectCurrent);
            AddThingGrid = null;
        }

        private void TransferThingList() //кнопка Доставка в удаленную точку
        {
            if (AddThingGrid.DataSource == null) return;
            if (!(WorldObjectCurrent is TradeThingsOnline)) return;

            var selectTow = AddThingGrid.DataSource;
            //код из метода MoveSelectThings {
            var select = selectTow.TransferableOneWaysToDictionary();

            var toTargetThing = select.Select(p =>
            {
                if (p.Key.stackCount != p.Value) p.Key.stackCount = p.Value;
                return p.Key;
            }).ToList();
            var toTargetEntry = toTargetThing.Select(t => ThingTrade.CreateTrade(t, t.stackCount)).ToList();
            // }

            var listFO = ExchengeUtils.WorldObjectsPlayer()
                .Where(wo => wo.Tile != WorldObjectCurrent.Tile)
                .Select(wo =>
                {
                    ExchengeUtils.CargoDeliveryCalc(WorldObjectCurrent, wo, toTargetEntry, out int cost, out int dist);

                    return new FloatMenuOption(wo.LabelCap + " " + cost + "$ (✗" + dist + ")"
                        , () =>
                        {
                            SoundDefOf.Tick_High.PlayOneShotOnCamera(null);

                            //Перемещаем выделенные вещи из WorldObjectCurrent в wo
                            ActiveElementBlock = true;

                            ExchengeUtils.CargoDelivery(WorldObjectCurrent, wo, toTargetEntry, () => 
                            { 
                                ActiveElementBlock = false;
                                UpdateWorldAndOrdersList();
                            });
                        });
                })
                .ToList();
            if (listFO.Count == 0) return;
            var menu = new FloatMenu(listFO);
            Find.WindowStack.Add(menu);

        }

        private void CreateMyOrder()
        {
            EditOrderTitle = "OCity_Dialog_Exchenge_Order_Create".TranslateCache();
            var editOrder = new TradeOrder();
            editOrder.Owner = SessionClientController.My;
            editOrder.Place = PlaceCurrent;
            editOrder.Tile = PlaceCurrent.Tile;
            editOrder.PlaceServerId = PlaceCurrent.PlaceServerId;
            editOrder.CountReady = 0;

            editOrder.SellThings = new List<ThingTrade>();
            editOrder.BuyThings = new List<ThingTrade>();
            var th = ThingTrade.CreateTrade(MainHelper.CashlessThingDef, 0f, QualityCategory.Awful, 1);
            editOrder.BuyThings.Add(th);

            editOrder.PrivatPlayers = new List<Player>();

            SetEditOrder(editOrder);
        }

        /// <summary>
        /// Вторая вкладка редактирования ордера
        /// </summary>
        /// <param name="inRect"></param>
        private void DoTab1EditOrder(Rect inRect)
        {
            if (EditOrder == null)
            {
                //Действие не выбрано: по умолчанию настраиваем панель на создание нового ордера
                CreateMyOrder();
            }

            bool existInServer = EditOrder.Id != 0;

            //заголовок
            Rect rect = new Rect(0f, 0f, inRect.width, 18);
            inRect.yMin += rect.height;
            Text.Font = GameFont.Tiny; // высота Tiny 18
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, EditOrderTitle);

            //--------------------------------------------------------------
            //Вещи в ордере

            GUI.DrawTexture(new Rect((inRect.width - 64f) / 2, inRect.y, 64f, 64f), GeneralTexture.OCE_To);

            int loopWidth = 70;
            if (EditOrderIsMy)
            {
                Rect rectLoop = new Rect(inRect.width / 2 - 50f - loopWidth, inRect.y, loopWidth, 200f);
                //Widgets.Label(rect, "OCity_Dialog_Exchenge_We_Give".TranslateCache());  NeedTranslate
                EditOrderShowSellThings(rectLoop, -loopWidth);

                rectLoop = new Rect(inRect.width / 2 + 50f, inRect.y, loopWidth, 200f);
                //Widgets.Label(rect, "OCity_Dialog_Exchenge_We_Get".NeedTranslate()); NeedTranslate
                EditOrderShowBuyThings(rectLoop, loopWidth);
            }
            else
            {
                Rect rectLoop = new Rect(inRect.width / 2 - 50f - loopWidth, inRect.y, loopWidth, 200f);
                //Widgets.Label(rect, "OCity_Dialog_Exchenge_We_Give2".TranslateCache(EditOrder.Owner.Login)); NeedTranslate
                EditOrderShowBuyThings(rectLoop, -loopWidth);

                rectLoop = new Rect(inRect.width / 2 + 50f, inRect.y, loopWidth, 200f);
                //Widgets.Label(rect, "OCity_Dialog_Exchenge_We_Get".TranslateCache()); NeedTranslate
                EditOrderShowSellThings(rectLoop, loopWidth);
            }

            //кол-во повторов
            if (EditOrderCountReadyBuffer == null)
            {
                EditOrderCountReadyBuffer = new TextFieldNumericBox(() => EditOrderCountReady
                    , (val) =>
                    {
                        EditOrderCountReady = val;
                        EditOrderChange();
                    }
                    , () => !ActiveElementBlock);
                EditOrderCountReadyBuffer.Min = 1;
                if (!EditOrderIsMy) EditOrderCountReadyBuffer.Max = EditOrder.CountReady;
                EditOrderCountReadyBuffer.ShowButton = false;
            }
            Text.Font = GameFont.Medium;
            var widthtextin = 80f;
            var widthtextx = 24f;
            var rect3 = new Rect((inRect.width - widthtextin) / 2 - widthtextx, inRect.y + 64f + 18f * 3f + 24f, 24f, 24f);
            Widgets.Label(rect3, "*");
            Text.Font = GameFont.Tiny;
            rect3.x += widthtextx;
            rect3.width = widthtextin;
            EditOrderCountReadyBuffer.Drow(rect3);
            rect3.y += rect3.height;

            if (!EditOrderIsMy)
            {
                if (EditOrderCountReady > EditOrder.CountReady) GUI.color = Color.red;
                Widgets.Label(rect3, "(max " + EditOrder.CountReady.ToString() + ")");
                GUI.color = Color.white;
            }

            inRect.yMin += 230f;

            //--------------------------------------------------------------
            //Кнопки внизу 

            var buttonwidth = (inRect.width / 2f - 20f) / 3f;
            if (buttonwidth < 160f) buttonwidth = 160f;

            rect = new Rect(inRect.x + inRect.width - buttonwidth, inRect.y + 20f, buttonwidth, 24);
            if (!EditOrderToTrade) GUI.color = Color.red;
            if (ActiveElementBlock) GUI.color = Color.gray;
            if (Widgets.ButtonText(rect.ContractedBy(1f)
                , EditOrderIsMy
                    ? existInServer ? "OCity_Dialog_Exchenge_Save".TranslateCache() : "OCity_Dialog_Exchenge_Create".TranslateCache()
                    : "OCity_Dialog_Exchenge_Trade".TranslateCache()
                , true, false, true))
            {
                GUI.color = Color.white;
                if (ActiveElementBlock) return;
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                if (!EditOrderToTrade) return;

                if (EditOrderIsMy)
                {
                    //создать или отредактировать
                    //string label = "Ордер выставлен";
                    //string text = "Вы выставили:....\n" + "Вы хотите получить:....";
                    //Find.LetterStack.ReceiveLetter(label, text, OC_LetterDefOf.GoldenLetter);
                    ApplyEditMyOrder();
                }
                else
                {
                    //торговать
                    //string label = "Предложение принято";
                    //string text = "Вы купили:...\n." + "Вы отдали:....\n" +"Продавец: Аант";
                    //Find.LetterStack.ReceiveLetter(label, text, OC_LetterDefOf.GoldenLetter);
                    ApplyEditOtherOrder();
                    return;
                }

                EditOrderChange();
                return;
            }
            GUI.color = Color.white;

            //кнопка по центру
            rect = new Rect(inRect.x + inRect.width - buttonwidth * 2f - 10f, inRect.y + 20f, buttonwidth, 24); // ширина + 8*2, т.к. не влезало
            if (!EditOrderIsMy)
            {
                if (ActiveElementBlock) GUI.color = Color.gray;
                if (Widgets.ButtonText(rect.ContractedBy(1f)
                    , "OCity_Dialog_Exchenge_Counterproposal".TranslateCache() // Встречное предложение
                    , true, false, true))
                {
                    GUI.color = Color.white;
                    if (ActiveElementBlock) return;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);

                    CreateCounterproposal();

                    return;
                }
                GUI.color = Color.white;
            }
            if (EditOrderIsMy && existInServer && EditOrder.Id != 0)
            {
                if (ActiveElementBlock) GUI.color = Color.gray;
                if (Widgets.ButtonText(rect.ContractedBy(1f)
                    , "OCity_Dialog_Exchenge_Delete".TranslateCache()
                    , true, false, true))
                {
                    GUI.color = Color.white;
                    if (ActiveElementBlock) return;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);

                    ApplyDeleteMyOrder();

                    return;
                }
                GUI.color = Color.white;
            }

            rect = new Rect(inRect.x + inRect.width - buttonwidth * 3f - 10f * 2f, inRect.y + 20f, buttonwidth, 24f);
            if (ActiveElementBlock) GUI.color = Color.gray;
            if (Widgets.ButtonText(rect.ContractedBy(1f)
                , "OCity_Dialog_Exchenge_Order_New".TranslateCache()
                , true, false, true))
            {
                GUI.color = Color.white;
                if (ActiveElementBlock) return;
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);

                SetEditOrder(null);

                return;
            }
            GUI.color = Color.white;

            Text.Anchor = TextAnchor.UpperLeft;

            //--------------------------------------------------------------

            rect = new Rect(inRect.x, inRect.y + 20f, inRect.x + inRect.width - 52f - buttonwidth * 4f - 10f * 4f, 24f);
            if (EditOrder.PrivatPlayers == null || EditOrder.PrivatPlayers.Count == 0)
            {
                Widgets.Label(rect, "OCity_Dialog_Exchenge_No_User_Restrictions".TranslateCache());
                rect.y += 24f;
            }
            else
            {
                var buttonheight = 20f;
                rect = new Rect(inRect.x, inRect.y + 10f - (EditOrder.PrivatPlayers.Count - 2) * buttonheight, inRect.x + inRect.width - 52f - buttonwidth * 4f - 10f * 4f, buttonheight);
                Widgets.Label(rect, "OCity_Dialog_Exchenge_User_Restrictions".TranslateCache());
                rect.y += buttonheight;
                for (int i = 0; i < EditOrder.PrivatPlayers.Count; i++)
                {
                    rect3 = new Rect(rect.x, rect.y, rect.width - 25f, 24f);
                    Widgets.Label(rect3, EditOrder.PrivatPlayers[i].Login);

                    rect3 = new Rect(rect.xMax - 24f, rect.y, 24f, 24f);
                    if (ActiveElementBlock) GUI.color = Color.gray;
                    if (EditOrderIsMy && Widgets.ButtonImage(rect3, GeneralTexture.IconDelTex))
                    {
                        GUI.color = Color.white;
                        if (ActiveElementBlock) return;
                        EditOrder.PrivatPlayers.RemoveAt(i--);
                    }
                    GUI.color = Color.white;
                    rect.y += buttonheight;
                }
            }
            if (EditOrderIsMy)
            {
                rect = new Rect(inRect.x + inRect.width - 52f - buttonwidth * 4f - 10f * 3f, inRect.y + 20f, buttonwidth, 24f);

                if (ActiveElementBlock) GUI.color = Color.gray;
                if (Widgets.ButtonText(rect.ContractedBy(1f)
                    , "OCity_Dialog_Exchenge_Add_User".TranslateCache()
                    , true, false, true))
                {
                    GUI.color = Color.white;
                    if (ActiveElementBlock) return;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);

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
                GUI.color = Color.white;
            }
        }

        /// <summary>
        /// область снизу справа - просмотр и редактирование ордера
        /// </summary>
        /// <param name="inRect"></param>
        public void DoWindowEditOrder(Rect inRect)
        {/*
            if (EditOrder == null)
            {
                //Действие не выбрано: по умолчанию настраиваем панель на создание нового ордера
                EditOrderTitle = "OCity_Dialog_Exchenge_Order_Create".TranslateCache();
                var editOrder = new TradeOrder();
                editOrder.Owner = SessionClientController.My;
                editOrder.Place = PlaceCurrent;
                editOrder.Tile = PlaceCurrent.Tile;
                editOrder.PlaceServerId = PlaceCurrent.PlaceServerId;
                editOrder.CountReady = 0;

                editOrder.SellThings = new List<ThingTrade>();
                editOrder.BuyThings = new List<ThingTrade>();
                var th = ThingTrade.CreateTrade(MainHelper.CashlessThingDef, 0f, QualityCategory.Awful, 1);
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

            //--------------------------------------------------------------
            //кнопка в углу
            rect = new Rect(inRect.width - 150f, 20f, 150f, 24);
            if (!EditOrderToTrade) GUI.color = Color.red;
            if (ActiveElementBlock) GUI.color = Color.gray;
            if (Widgets.ButtonText(rect.ContractedBy(1f)
                , EditOrderIsMy
                    ? existInServer ? "OCity_Dialog_Exchenge_Save".TranslateCache() : "OCity_Dialog_Exchenge_Create".TranslateCache()
                    : "OCity_Dialog_Exchenge_Trade".TranslateCache()
                , true, false, true))
            {
                GUI.color = Color.white;
                if (ActiveElementBlock) return;
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                if (!EditOrderToTrade) return;

                if (EditOrderIsMy)
                {
                    //создать или отредактировать
                    //string label = "Ордер выставлен";
                    //string text = "Вы выставили:....\n" + "Вы хотите получить:....";
                    //Find.LetterStack.ReceiveLetter(label, text, OC_LetterDefOf.GoldenLetter);
                    ApplyEditMyOrder();
                }
                else
                {
                    //торговать
                    //string label = "Предложение принято";
                    //string text = "Вы купили:...\n." + "Вы отдали:....\n" +"Продавец: Аант";
                    //Find.LetterStack.ReceiveLetter(label, text, OC_LetterDefOf.GoldenLetter);
                    ApplyEditOtherOrder();
                    return;
                }

                EditOrderChange();
                return;
            }
            GUI.color = Color.white;

            //кнопка по центру
            rect = new Rect(152f, 20f, inRect.width - 160f - 160f + 16f, 24); // ширина + 8*2, т.к. не влезало
            if (!EditOrderIsMy)
            {
                if (ActiveElementBlock) GUI.color = Color.gray;
                if (Widgets.ButtonText(rect.ContractedBy(1f)
                    , "OCity_Dialog_Exchenge_Counterproposal".TranslateCache() // Встречное предложение
                    , true, false, true))
                {
                    GUI.color = Color.white;
                    if (ActiveElementBlock) return;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);

                    CreateCounterproposal();

                    return;
                }
                GUI.color = Color.white;
            }
            if (EditOrderIsMy && existInServer && EditOrder.Id != 0)
            {
                if (ActiveElementBlock) GUI.color = Color.gray;
                if (Widgets.ButtonText(rect.ContractedBy(1f)
                    , "OCity_Dialog_Exchenge_Delete".TranslateCache()
                    , true, false, true))
                {
                    GUI.color = Color.white;
                    if (ActiveElementBlock) return;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);

                    ApplyDeleteMyOrder();

                    return;
                }
                GUI.color = Color.white;
            }

            rect = new Rect(0, 20f, 150f, 24f);
            if (ActiveElementBlock) GUI.color = Color.gray;
            if (Widgets.ButtonText(rect.ContractedBy(1f)
                , "OCity_Dialog_Exchenge_Order_New".TranslateCache()
                , true, false, true))
            {
                GUI.color = Color.white;
                if (ActiveElementBlock) return;
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);

                SetEditOrder(null);

                return;
            }
            GUI.color = Color.white;

            inRect.yMin += rect.height;
            //--------------------------------------------------------------

            var innerHeight = 20f + 24f * (5
                + EditOrder.SellThings.Count
                + EditOrder.BuyThings.Count
                + ((EditOrder.PrivatPlayers?.Count ?? 0) == 0 ? 0 : EditOrder.PrivatPlayers.Count + 1));
            var innerWidth = inRect.width - DialogControlBase.WidthScrollLine;
            ScrollPositionEditOrder = GUI.BeginScrollView(
                inRect
                , ScrollPositionEditOrder
                , new Rect(0, 0, innerWidth, innerHeight));

            rect = new Rect(0f, 0f, innerWidth, 24f);
            Text.Anchor = TextAnchor.MiddleLeft;

            if (EditOrderIsMy)
            {
                Widgets.Label(rect, "OCity_Dialog_Exchenge_No_Exchanges".TranslateCache()); // Сделать в ордере кол-во повторов:
            }
            else
            {
                Widgets.Label(rect, "OCity_Dialog_Exchenge_TradeCount".TranslateCache()); // Сколько раз купить
            }
            var rect2 = new Rect(rect.x + 250f, rect.y, 70f, rect.height);

            int countToTransfer = EditOrderCountReady;
            string editBuffer;
            if (!EditOrderEditBuffer.TryGetValue(EditOrder.GetHashCode(), out editBuffer)) EditOrderEditBuffer.Add(EditOrder.GetHashCode(), editBuffer = countToTransfer.ToString());
            Widgets.TextFieldNumeric<int>(rect2.ContractedBy(2f), ref countToTransfer, ref editBuffer, 1f, 999999999f);
            if (EditOrderEditBuffer[EditOrder.GetHashCode()] != editBuffer)
            { 
                EditOrderEditBuffer[EditOrder.GetHashCode()] = editBuffer;
                EditOrderCountReady = countToTransfer;
                EditOrderChange();
                //из-за особенностей интерфейса отображаемое в текстовом буфере может не совпадать с EditOrderCountReady, в этом случае запрещаем применение
                if (EditOrderCountReady != countToTransfer)
                {
                    EditOrderToTrade = false;
                }
            }

            //Максимум (в этой же строке)
            rect2.x += 10f + rect2.width;
            rect2.width = 120f;
            Widgets.Label(rect2, "OCity_Dialog_Exchenge_Max".TranslateCache() + " " + EditOrderCountMax.ToString());

            rect.y += 24f;

            //Сейчас доступно повторов:
            Widgets.Label(rect, "OCity_Dialog_Exchenge_No_Available_Exchange".TranslateCache() + " " + EditOrder.CountReady.ToString());
            if (EditOrderIsMy)
            {
                if (Mouse.IsOver(rect))
                {
                    Widgets.DrawHighlight(rect);
                }
                TooltipHandler.TipRegion(rect, "OCity_Dialog_Exchenge_No_Available_ExchangenInfo".TranslateCache());

                rect.xMin += 250;
                //Совершено раз:
                Widgets.Label(rect, "OCity_Dialog_Exchenge_Done_Once".TranslateCache() + " " + EditOrder.CountFnished.ToString());
                rect.xMin = 0;
            }
            rect.y += 24f;
            //--------------------------------------------------------------

            if (EditOrderIsMy)
            {
                Widgets.Label(rect, "OCity_Dialog_Exchenge_We_Give".TranslateCache());
                rect.y += 24f;
                //EditOrderShowSellThings(ref rect);

                Widgets.Label(rect, "OCity_Dialog_Exchenge_We_Get".TranslateCache());
                rect.y += 24f;
                //EditOrderShowBuyThings(ref rect);
            }
            else
            {
                Widgets.Label(rect, "OCity_Dialog_Exchenge_We_Give2".TranslateCache(EditOrder.Owner.Login));
                rect.y += 24f;
                //EditOrderShowBuyThings(ref rect);

                Widgets.Label(rect, "OCity_Dialog_Exchenge_We_Get".TranslateCache());
                rect.y += 24f;
                //EditOrderShowSellThings(ref rect);
            }
            //--------------------------------------------------------------

            if (EditOrder.PrivatPlayers == null || EditOrder.PrivatPlayers.Count == 0)
            {
                Widgets.Label(rect, "OCity_Dialog_Exchenge_No_User_Restrictions".TranslateCache());
                rect.y += 24f;
            }
            else
            {
                Widgets.Label(rect, "OCity_Dialog_Exchenge_User_Restrictions".TranslateCache());
                rect.y += 24f;
                for (int i = 0; i < EditOrder.PrivatPlayers.Count; i++)
                {
                    var rect3 = new Rect(rect.x, rect.y, rect.width - 25f, 24f);
                    Widgets.Label(rect3, EditOrder.PrivatPlayers[i].Login);

                    rect3 = new Rect(rect.xMax - 24f, rect.y, 24f, 24f);
                    if (ActiveElementBlock) GUI.color = Color.gray;
                    if (EditOrderIsMy && Widgets.ButtonImage(rect3, IconDelTex))
                    {
                        GUI.color = Color.white;
                        if (ActiveElementBlock) return;
                        EditOrder.PrivatPlayers.RemoveAt(i--);
                    }
                    GUI.color = Color.white;
                    rect.y += 24f;
                }
            }
            if (EditOrderIsMy)
            {
                var rect4 = new Rect(rect);
                rect4.width = 150f;
                if (ActiveElementBlock) GUI.color = Color.gray;
                if (Widgets.ButtonText(rect4.ContractedBy(1f)
                    , "OCity_Dialog_Exchenge_Add_User".TranslateCache()
                    , true, false, true))
                {
                    GUI.color = Color.white;
                    if (ActiveElementBlock) return;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);

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
                GUI.color = Color.white;
            }

            GUI.EndScrollView();
            */
        }

        /// <summary>
        /// Рисуем вещи сделки снизу справа
        /// </summary>
        private void EditOrderShowSellThings(Rect rect, int loopWidth)
        {
            var dataThingNeedCreateThing = false;
            for (int i = 0; i < EditOrder.SellThings.Count; i++)
            {
                var th = EditOrder.SellThings[i];
                if (th.DataThingNeedCreateThing)
                {
                    dataThingNeedCreateThing = true;
                    break;
                }
            }
            using (var gameError = !dataThingNeedCreateThing ? null
                    : GameUtils.NormalGameError())
            {
                for (int i = 0; i < EditOrder.SellThings.Count; i++)
                {
                    //это редактирование своих вещей. Тут все поля только чтение кроме крестика убрать строку
                    var th = EditOrder.SellThings[i];

                    //иконка
                    var rect3 = new Rect(rect.x + (Math.Abs(loopWidth) - 64f) / 2f, rect.y, 64f, 64f);
                    GameUtils.DravLineThing(rect3, th.DataThing, true, 64f - 24f, 64f - 24f);

                    //надписи под иконкой
                    rect3 = new Rect(rect.x + 4f, rect.y + rect3.height + 2f, rect.width - 8f, 18f);

                    //название, с трупа WornByCorpse, прочность HitPoints из MaxHitPoints и качество Quality
                    rect3.height = EditOrderShowHitAndQ(rect3, th);
                    rect3.y += rect3.height;

                    //подготовка контейнера с компонентами и выбранным кол-во
                    if (!EditOrderEditBuffer.TryGetValue(th.GetHashCode(), out var editComponent))
                        EditOrderEditBuffer.Add(th.GetHashCode(), editComponent = new OrderEditBuffer(this, th));

                    //поле ввода: количество
                    rect3.height = 24f;
                    editComponent.TextField1.Drow(rect3);
                    rect3.y += rect3.height;

                    //пропуск строки
                    rect3.height = 24f;
                    //        Widgets.Label(rect3, "x" + EditOrderCountReady.ToString()); //для отладки, просто убрать строку
                    rect3.y += rect3.height;

                    //поле ввода: количество*повторы
                    rect3.height = 24f;
                    editComponent.TextField2.Drow(rect3);
                    rect3.y += rect3.height;

                    //пропуск строки
                    rect3.height = 18f;
                    if (EditOrderIsMy)
                    {
                        if (editComponent.TotalCount > th.TradeCount) GUI.color = Color.red;
                        Widgets.Label(rect3, "(max " + th.TradeCount.ToString() + ")");
                        GUI.color = Color.white;
                    }
                    rect3.y += rect3.height;
                    
                    //крестик убрать строку
                    rect3.height = 24f;
                    if (ActiveElementBlock) GUI.color = Color.gray;
                    //GUI.DrawTexture(new Rect(rect3.x + (rect3.width - rect3.height) / 2, rect3.y, rect3.height, rect3.height), GeneralTexture.IconDelTex);
                    if (EditOrderIsMy && Widgets.ButtonImage(new Rect(rect3.x + (rect3.width - rect3.height) / 2, rect3.y, rect3.height, rect3.height), GeneralTexture.IconDelTex))
                    {
                        GUI.color = Color.white;
                        if (ActiveElementBlock) return;
                        EditOrder.SellThings.RemoveAt(i--);
                        EditOrderChange();
                    }
                    GUI.color = Color.white;

                    rect.x += loopWidth;
                }

                if (EditOrderIsMy && EditOrder.SellThings.Count < 6)
                {
                    var rect3 = new Rect(rect.x + (Math.Abs(loopWidth) - 64f) / 2f, rect.y, 64f, 64f);
                    if (ActiveElementBlock) GUI.color = Color.gray;
                    // GUI.DrawTexture(new Rect((inRect.width - 64f) / 2, inRect.y, 64f, 64f), GeneralTexture.OCE_Add);
                    if (Widgets.ButtonImage(rect3, GeneralTexture.OCE_Add))
                    {
                        GUI.color = Color.white;
                        if (ActiveElementBlock) return;
                        TabIndex = 0;
                    }
                    GUI.color = Color.white;

                    rect.x += loopWidth;
                }
            }
        }

        /// <summary>
        /// Рисует иконку, название и кнопку выбора def. Смещает rectInLine вправо. В rectInLine игнорируется переданное width, и после оно незначимое
        /// </summary>
        private void ShowSelectThingDef(ref Rect rectInLine, ThingDef selectThing, Action<string, ThingDef> setSelect)
        {
            if (selectThing != null)
            {
                //иконка
                rectInLine.width = 24f;
                GameUtils.DravLineThing(rectInLine, selectThing, false);
                rectInLine.x += rectInLine.width + 3f;
                rectInLine.width = Text.CalcSize(selectThing.LabelCap).x;
                Widgets.Label(rectInLine, selectThing.LabelCap);
                rectInLine.x += rectInLine.width + 3f;
                rectInLine.width = 100f;
                if (ActiveElementBlock) GUI.color = Color.gray;
                if (Widgets.ButtonText(rectInLine.ContractedBy(1f)
                    , "OCity_Dialog_CreateWorld_BtnCancel".TranslateCache() //отмена
                    , true, false, true))
                {
                    GUI.color = Color.white;
                    if (ActiveElementBlock) return;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);

                    setSelect("", null);
                }
                GUI.color = Color.white;
            }
            else
            {
                rectInLine.width = 100f;
                if (ActiveElementBlock) GUI.color = Color.gray;
                if (Widgets.ButtonText(rectInLine.ContractedBy(1f)
                    , "OCity_Dialog_Exchenge_Choose".TranslateCache() //выбрать
                    , true, false, true))
                {
                    GUI.color = Color.white;
                    if (ActiveElementBlock) return;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);

                    var formm = new Dialog_SelectThingDef();
                    formm.ClearFilter();
                    formm.PostCloseAction = () =>
                    {
                        if (MainHelper.DebugMode) Loger.Log("Client ShowSelectThingDef=" + (formm.SelectThingDef?.defName ?? "null"));
                        if (formm.SelectThingDef == null)
                        {
                            setSelect("", null);
                            return;
                        }
                        setSelect(formm.SelectThingDef.defName, formm.SelectThingDef);
                    };
                    Find.WindowStack.Add(formm);
                }
                GUI.color = Color.white;
            }
            rectInLine.x += rectInLine.width + 10f;
        }

        /// <summary>
        /// Рисуем вещи сделки снизу справа
        /// </summary>
        private void EditOrderShowBuyThings(Rect rect, int loopWidth)
        {
            var dataThingNeedCreateThing = false;
            for (int i = 0; i < EditOrder.BuyThings.Count; i++)
            {
                var th = EditOrder.BuyThings[i];
                if (th.Concrete && th.DataThingNeedCreateThing)
                {
                    dataThingNeedCreateThing = true;
                    break;
                }
            }
            using (var gameError = !dataThingNeedCreateThing ? null
                    : GameUtils.NormalGameError())
            {
                for (int i = 0; i < EditOrder.BuyThings.Count; i++)
                {
                    //это редактирование получаемых вещей. Тут все поля либо также только чтение либо все редактируемые
                    var th = EditOrder.BuyThings[i];

                    //иконка
                    var rect3 = new Rect(rect.x + (Math.Abs(loopWidth) - 64f) / 2f, rect.y, 64f, 64f);
                    if (th.Concrete)
                        GameUtils.DravLineThing(rect3, th.DataThing, true, 64f - 24f, 64f - 24f);
                    else
                        GameUtils.DravLineThing(rect3, th, true, Color.gray, 64f - 24f, 64f - 24f);

                    //надписи под иконкой
                    rect3 = new Rect(rect.x + 4f, rect.y + rect3.height + 2f, rect.width - 8f, 18f);

                    //название, с трупа WornByCorpse, прочность HitPoints из MaxHitPoints и качество Quality
                    rect3.height = EditOrderShowHitAndQ(rect3, th);
                    rect3.y += rect3.height;

                    //подготовка контейнера с компонентами и выбранным кол-во
                    if (!EditOrderEditBuffer.TryGetValue(th.GetHashCode(), out var editComponent))
                        EditOrderEditBuffer.Add(th.GetHashCode(), editComponent = new OrderEditBuffer(this, th));

                    //поле ввода: количество
                    rect3.height = 24f;
                    editComponent.TextField1.Drow(rect3);
                    rect3.y += rect3.height;

                    //пропуск строки
                    rect3.height = 24f;
                    //            Widgets.Label(rect3, "x" + EditOrderCountReady.ToString()); //для отладки, просто убрать строку
                    rect3.y += rect3.height;

                    //поле ввода: количество*повторы
                    rect3.height = 24f;
                    editComponent.TextField2.Drow(rect3);
                    rect3.y += rect3.height;

                    //пропуск строки
                    rect3.height = 18f;
                    if (!EditOrderIsMy)
                    {
                        if (editComponent.TotalCount > th.TradeCount) GUI.color = Color.red;
                        Widgets.Label(rect3, "(max " + th.TradeCount.ToString() + ")");
                        GUI.color = Color.white;
                    }
                    rect3.y += rect3.height;

                    //крестик убрать строку
                    rect3.height = 24f;
                    if (ActiveElementBlock) GUI.color = Color.gray;
                    //GUI.DrawTexture(new Rect(rect3.x + (rect3.width - rect3.height) / 2, rect3.y, rect3.height, rect3.height), GeneralTexture.IconDelTex);
                    if (EditOrderIsMy && Widgets.ButtonImage(new Rect(rect3.x + (rect3.width - rect3.height) / 2, rect3.y, rect3.height, rect3.height), GeneralTexture.IconDelTex))
                    {
                        GUI.color = Color.white;
                        if (ActiveElementBlock) return;
                        EditOrder.BuyThings.RemoveAt(i--);
                        EditOrderChange();
                    }
                    GUI.color = Color.white;

                    rect.x += loopWidth;
                }

                if (EditOrderIsMy && EditOrder.BuyThings.Count < 6)
                {
                    var rect3 = new Rect(rect.x + (Math.Abs(loopWidth) - 64f) / 2f, rect.y, 64f, 64f);
                    if (ActiveElementBlock) GUI.color = Color.gray;
                    // GUI.DrawTexture(new Rect((inRect.width - 64f) / 2, inRect.y, 64f, 64f), GeneralTexture.OCE_Add);
                    if (Widgets.ButtonImage(rect3, GeneralTexture.OCE_Add))
                    {
                        GUI.color = Color.white;
                        if (ActiveElementBlock) return;
                        SoundDefOf.Tick_High.PlayOneShotOnCamera(null);

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
                    GUI.color = Color.white;

                    rect.x += loopWidth;
                }
            }
        }

        /// <summary>
        /// Рисуем свойства вещи в строку
        /// </summary>
        private float EditOrderShowHitAndQ(Rect rect, ThingTrade th)
        {
            var outHeight = rect.height;
            Text.Anchor = TextAnchor.UpperCenter;
            //название
            var rect1 = new Rect(rect.x, rect.y, rect.width, rect.height
                + (th.Quality < 0 ? rect.height : 0f)); // если первой строки с качеством не выводится, вместо этого имя выводим на 2 строки
            if (th.NotTrade) GUI.color = Color.red;
            TooltipHandler.TipRegion(rect1, th.Name);
            Widgets.Label(rect1, th.Name);
            GUI.color = Color.white;
            rect.y += rect.height;
            outHeight += rect.height;

            //качество
            var rect2 = new Rect(rect.x, rect.y, rect.width, rect.height);
            if (th.Quality >= 0)
            {
                if (th.Concrete)
                {
                    //качество Quality
                    TooltipHandler.TipRegion(rect2, "OCity_Dialog_Exchenge_Quality".TranslateCache() + ((QualityCategory)th.Quality).GetLabel());
                    Widgets.Label(rect2, ((QualityCategory)th.Quality).GetLabelShort() /*+ "+"*/);
                }
                else
                {
                    //качество Quality
                    TooltipHandler.TipRegion(rect2, "OCity_Dialog_Exchenge_QualityNo_Less_Than".TranslateCache() + ((QualityCategory)th.Quality).GetLabel());
                    string t;
                    if (th.Quality == 0)
                    {
                        t = "OCity_Dialog_Exchenge_All".TranslateCache();
                    }
                    else
                    {
                        t = ((QualityCategory)th.Quality).GetLabelShort() + "+";
                    }
                    Widgets.Label(rect2, t);
                }
            }
            rect.y += rect.height;
            outHeight += rect.height;

            //с трупа WornByCorpse
            var rect3 = new Rect(rect.xMax - rect.height, rect.y, rect.height, rect.height);
            if (th.HitPoints > 0) //косвеный признак: если здоровья нет, то и с трупа вещь быть не можетъ
            {
                if (th.WornByCorpse)
                {
                    TooltipHandler.TipRegion(rect3, (th.Concrete ? "OCity_Dialog_Exchenge_FromCorpse" : "OCity_Dialog_Exchenge_FromCorpseD").TranslateCache());
                    if (EditOrderIsMy && !th.Concrete)
                    {
                        if (ActiveElementBlock) GUI.color = Color.gray;
                        if (Widgets.ButtonImage(rect3, GeneralTexture.IconSkull, Color.green))
                        {
                            GUI.color = Color.white;
                            if (ActiveElementBlock) return outHeight;
                            th.WornByCorpse = false;
                        }
                        GUI.color = Color.white;
                    }
                    else
                        Widgets.DrawTextureFitted(rect3, GeneralTexture.IconSkull, 1f);
                }
                else
                {
                    TooltipHandler.TipRegion(rect3, (th.Concrete ? "OCity_Dialog_Exchenge_FromNotCorpse" : "OCity_Dialog_Exchenge_FromNotCorpseD").TranslateCache());
                    if (EditOrderIsMy && !th.Concrete)
                    {
                        if (ActiveElementBlock) GUI.color = Color.gray;
                        if (Widgets.ButtonImage(rect3, GeneralTexture.IconSkull, Color.gray))
                        {
                            GUI.color = Color.white;
                            if (ActiveElementBlock) return outHeight;
                            th.WornByCorpse = true;
                        }
                        GUI.color = Color.white;
                    }
                }
            }
            GUI.color = Color.white;

            //прочность
            if (th.Concrete)
            {
                if (th.HitPoints >= 0)
                {
                    //прочность HitPoints из MaxHitPoints
                    var textCntW = Text.CalcSize("888/888 ").x;
                    rect3 = new Rect(rect.xMax - rect3.width - textCntW, rect.y, textCntW, rect.height);
                    TooltipHandler.TipRegion(rect3, "OCity_Dialog_Exchenge_Whole_On".TranslateCache(th.HitPoints, th.MaxHitPoints
                        , (th.HitPoints * 100 / th.MaxHitPoints).ToString()));
                    Widgets.Label(rect3, th.HitPoints + "/" + th.MaxHitPoints);
                }
            }
            else
            {
                if (th.HitPoints > 0)
                {
                    //прочность HitPoints из MaxHitPoints
                    var textCntW = Text.CalcSize("188% ").x;
                    rect3 = new Rect(rect.xMax - rect3.width - textCntW, rect.y, textCntW, rect.height);
                    TooltipHandler.TipRegion(rect3, "OCity_Dialog_Exchenge_Whole_Less_Than".TranslateCache() + (th.HitPoints * 100 / th.MaxHitPoints).ToString() + "%");
                    Widgets.Label(rect3, (th.HitPoints * 100 / th.MaxHitPoints).ToString() + "%");
                }
            }
            return outHeight;
        }

        /// <summary>
        /// Обновление главного списка ордеров
        /// </summary>
        private void UpdateOrdersList()
        {
            try
            {
                OrdersGrid = null;

                SessionClientController.Command((connect) =>
                {
                    connect.ErrorMessage = null;
                    StatusLoadOrders = ".";

                    //первый раз функция игры GameUtils.DistanceBetweenTile может работать долго, но будет кэширована
                    List<int> tiles = (FilterTileLength == 0) ? null
                        : ExchengeUtils.GetWorldObjectsForTrade()
                            .Select(wo => wo.Tile)
                            .Distinct()
                            .Where(t => GameUtils.DistanceBetweenTile(PlaceCurrent.Tile, t) <= FilterTileLength)
                            .ToList();

                    StatusLoadOrders = "...";

                    if (MainHelper.DebugMode) Loger.Log($"Client UpdateOrdersList {FilterTileLength} {FilterTileLengthBuffer} tiles=" 
                        + tiles?.Aggregate("", (r, i) => (r == "" ? "" : r + ", ") + i + "(" + GameUtils.DistanceBetweenTile(PlaceCurrent.Tile, i) + ")")
                        + " raw=" + ExchengeUtils.GetWorldObjectsForTrade()?.Aggregate("", (r, i) => (r == "" ? "" : r + ", ") + i.LabelShortCap), Loger.LogLevel.EXCHANGE);

                    var orders = connect.ExchengeLoad(tiles, FilterBuy, FilterSell);

                    StatusLoadOrders = $"({orders.Count})...";

                    Loger.Log("LoaderOrdersStart " + (orders?.Count ?? 0));
                    LoaderOrders = new AnyLoad(
                        orders.SelectMany(o => o.SellThings.Select(st => new AnyLoadTask() { Hash = st.DataHash })).ToList()
                        , (loader) =>
                        {
                            //Loger.Log("LoaderOrdersFinish I " + (orders?.Count ?? 0));
                            //как в ExchengeUtils.SpawnToWorldObject
                            //игнорировать автоисправляющуся ошибку 
                            //Could not get load ID. We're asking for something which was never added during LoadingVars. pathRelToParent=/leader, parent=PlayerTribe
                            using (var gameError = GameUtils.NormalGameError())
                            {
                                var dicHash = loader.ListLoad.GroupBy(ll => ll.Hash).ToDictionary(gg => gg.Key, gg => gg.First().Data);
                                foreach (var order in orders)
                                {
                                    //Loger.Log("LoaderOrdersFinish  ==== " + order.ToString());
                                    int di = 0;
                                    foreach (var st in order.SellThings)
                                    {
                                        st.Data = dicHash[st.DataHash];
                                        //Loger.Log($"LoaderOrdersFinish  {di++} {st.Data?.Length ?? -1}");

                                        //для отображения реальных иконок расскоментировать тут и выше в DoWindowOrders где th.DataThing != null
                                        //try
                                        //{
                                        //    if (st.DataThing == null) StatusLoadOrders = null; // строчка просто чтобы прочесть и создать DataThing
                                        //}
                                        //catch(Exception ex)
                                        //{
                                        //    //Loger.Log($"LoaderOrdersFinish  ExceptionI " + ex.ToString());
                                        //    Thread.Sleep(30);
                                        //    try
                                        //    {
                                        //        if (st.DataThing == null) StatusLoadOrders = null; // строчка просто чтобы прочесть и создать DataThing
                                        //    }
                                        //    catch
                                        //    {
                                        //        //Loger.Log($"LoaderOrdersFinish  ExceptionII " + ex.ToString());
                                        //    }
                                        //}
                                    }
                                }
                                //Loger.Log("LoaderOrdersFinish II");
                                Orders = orders
                                    .OrderBy(o => o.Place.DayPath = GameUtils.DistanceBetweenTile(PlaceCurrent.Tile, o.Tile))
                                    .ToList();
                                //Loger.Log("LoaderOrdersFinish III " + (Orders?.Count ?? 0));
                                StatusLoadOrders = null;
                                LoaderOrders = null;
                                OrdersGrid = null;
                            }
                        }
                        , (loader, precent) =>
                        {
                            StatusLoadOrders = $"({orders.Count}) {precent}%";
                        }
                        , (loader, error) =>
                        {
                            LoaderOrdersCancel();
                        });

                    if (!string.IsNullOrEmpty(connect.ErrorMessage))
                    {
                        Loger.Log("Client ExchengeLoad error: " + connect.ErrorMessage?.ServerTranslate(), Loger.LogLevel.ERROR);
                    }
                });
            }
            catch (Exception exp)
            {
                ExceptionUtil.ExceptionLog(exp, "Dialog_Exchenge UpdateOrdersList Exception");
            }
        }

        /// <summary>
        /// Кнопка "Удалить" свою сделку
        /// </summary>
        private void ApplyDeleteMyOrder()
        {
            SessionClientController.Command((connect) =>
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);

                ActiveElementBlock = true;
                EditOrder.Id = -EditOrder.Id;
                if (!connect.ExchengeEdit(EditOrder))
                {
                    EditOrder.Id = -EditOrder.Id;
                    Loger.Log("Client ExchengeEdit error: " + connect.ErrorMessage?.ServerTranslate(), Loger.LogLevel.ERROR);
                    Find.WindowStack.Add(new Dialog_Input("OCity_Dialog_Exchenge_Action_Not_CarriedOut".TranslateCache(), connect.ErrorMessage?.ServerTranslate(), true));
                }
                else
                {
                    SetEditOrder(null);
                    //можно активировать окно с "ОК" ниже, убрано, т.к. должно придти игровое письмо
                    //Find.WindowStack.Add(new Dialog_Input("OCity_Dialog_Exchenge_Delete".TranslateCache(), "OCity_Dialog_Exchenge_Action_CarriedOut".TranslateCache(), true));
                }
                UpdateWorldAndOrdersList();
            });
        }

        /// <summary>
        /// Создать свою сделку или отредактировать уже созданную свою
        /// </summary>
        private void ApplyEditMyOrder()
        {
            if (!EditOrderToTrade) return;

            if (SessionClientController.Data.BackgroundSaveGameOff)
            {
                Loger.Log($"Client ApplyEditMyOrder Cancel BackgroundSaveGameOff", Loger.LogLevel.EXCHANGE);
                return;
            }

            Loger.Log($"Client ApplyEditMyOrder EditOrderCountReady={EditOrderCountReady}", Loger.LogLevel.EXCHANGE);

            //определяем сколько нужно докинуть в яблоко из текущего места (если, конечно, и так не выбрано само яблоко)
            var thingsForAdd = (WorldObjectCurrent is TradeThingsOnline) ? new List<TransferableOneWay>()
                : GameUtils.ChechToTrade(
                    //вещи которые мы должны отдать, для реализации сделки
                    EditOrder.SellThings
                    , //сначала применяем вещи, которые уже зафиксированы в ордере и безнал, который не надо передавать на сервер
                        GetAvailableThingsInEditOrder()
                            .Concat(GameUtils.GetCashlessBalanceThingList(SessionClientController.Data.CashlessBalance))
                    , //вещи которыми мы распологаем без тех что в сделке, для внесения новых вещей на сервер, без безнала
                        GetAvailableThings(true, true)
                    , out _
                    , EditOrderCountReady);

            //создать или отредактировать - биип
            SoundDefOf.Tick_High.PlayOneShotOnCamera(null);

            EditOrder.CountReady = EditOrderCountReady; //для своей сделки это новое кол-во

            //основное действие, после переброски нужных ресурсов серверу (в красное яблоко)
            Action exchengeEdit = () =>
            {
                SessionClientController.Command((connect) =>
                {
                    if (!connect.ExchengeEdit(EditOrder))
                    {
                        Loger.Log("Client ExchengeEdit error: " + connect.ErrorMessage?.ServerTranslate());
                        Find.WindowStack.Add(new Dialog_Input("OCity_Dialog_CreateWorld_Err".TranslateCache(), connect.ErrorMessage?.ServerTranslate(), true));
                    }
                    else
                    {
                        SetEditOrder(null);
                    }
                });
            };

            //Если не хватает в текущей сделке, то берем из текущего места и кидаем в яблоко. 
            //А сервер сам возмет недостоющее и положит лишнее в красное яблоко.
            ActiveElementBlock = true;
            if (thingsForAdd != null
                && thingsForAdd.Count > 0)
            {
                MoveSelectThings(WorldObjectStorageCurrentTile, thingsForAdd, exchengeEdit);
            }
            else
            {
                exchengeEdit();
                UpdateWorldAndOrdersList();
            }
        }

        /// <summary>
        /// "Торговать!" выкупить чужую сделку
        /// </summary>
        private void ApplyEditOtherOrder()
        {
            if (!EditOrderToTrade) return;

            if (SessionClientController.Data.BackgroundSaveGameOff)
            {
                Loger.Log($"Client ApplyEditOtherOrder Cancel BackgroundSaveGameOff", Loger.LogLevel.EXCHANGE);
                return;
            }

            //определяем сколько нужно докинуть в яблоко из текущего места (если, конечно, и так не выбрано само яблоко)
            var thingsForBuy = (WorldObjectCurrent is TradeThingsOnline) ? new List<TransferableOneWay>()
                : GameUtils.ChechToTrade(
                //вещи которые мы должны отдать, для реализации сделки
                EditOrder.BuyThings
                , //сначала отнимаем безнал
                    GameUtils.GetCashlessBalanceThingList(SessionClientController.Data.CashlessBalance)
                , //вещи которыми мы будем платить из текущего места
                    GetAvailableThings(true, true)
                , out _
                , EditOrderCountReady);

            //торговать - биип
            SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
            
            //Сама сделка, EditOrderCountReady - кол-во покупаемого
            Action exchengeEdit = () =>
            {
                SessionClientController.Command((connect) =>
                {
                    if (!connect.ExchengeBuy(EditOrder.Id, EditOrderCountReady))
                    {
                        Loger.Log("Client ExchengeEdit error: " + connect.ErrorMessage?.ServerTranslate(), Loger.LogLevel.ERROR);
                        Find.WindowStack.Add(new Dialog_Input("OCity_Dialog_Exchenge_Action_Not_CarriedOut".TranslateCache(), connect.ErrorMessage?.ServerTranslate(), true));
                    }
                    else
                    {
                        SetEditOrder(null);
                        //можно активировать окно с "ОК" ниже, убрано, т.к. должно придти игровое письмо
                        //Find.WindowStack.Add(new Dialog_Input("OCity_Dialog_Exchenge_Trade".TranslateCache(), "OCity_Dialog_Exchenge_Action_CarriedOut".TranslateCache(), true));
                    }
                });
            };

            //Если не хватает в текущей сделке, то берем из текущего места и кидаем в яблоко. 
            //А сервер сам возмет недостоющее и положит лишнее в красное яблоко.
            ActiveElementBlock = true;
            if (thingsForBuy != null
                && thingsForBuy.Count > 0)
            {
                MoveSelectThings(WorldObjectStorageCurrentTile, thingsForBuy, exchengeEdit);
            }
            else
            {
                exchengeEdit();
                UpdateWorldAndOrdersList();
            }
        }

        /// <summary>
        /// Перемещаем выделенные вещи из WorldObjectCurrent в toWorldObject
        /// </summary>
        private void MoveSelectThings(WorldObject toWorldObject, List<TransferableOneWay> selectTow = null, Action finish = null)
        {
            if (selectTow == null) selectTow = AddThingGrid.DataSource;

            if (WorldObjectCurrent is TradeThingsOnline
                && !(toWorldObject is TradeThingsOnline)
                && SessionClientController.Data.CashlessBalance < 0)
            {
                Loger.Log("Client MoveSelectThings cancel: Impossible with negative balance", Loger.LogLevel.ERROR);
                Find.WindowStack.Add(new Dialog_Input("Impossible with negative balance".NeedTranslate(), "Impossible with negative balance".NeedTranslate() + " " + SessionClientController.Data.CashlessBalance, true));
                return;
            }

            if (!ExchengeUtils.MoveSelectThings(WorldObjectCurrent, toWorldObject, selectTow, () =>
            {
                if (finish != null) finish();

                //обновляем список
                UpdateWorldAndOrdersList(true);
                AddThingGrid = null;
            }))
            {
                ActiveElementBlock = false; //обязательно при выходе из этой функции, если не запускается UpdateWorldAndOrdersList
            }
        }

        /// <summary>
        /// Встречное предложение: создаем новое предложение на основе текущего, переворачивая владельца и вещи
        /// </summary>
        private void CreateCounterproposal()
        {
            if (EditOrderIsMy) return;
            
            var newOrderThings = EditOrder.Clone();
            var buyThings = newOrderThings.BuyThings;
            var sellThings = newOrderThings.SellThings;

            EditOrderTitle = "OCity_Dialog_Exchenge_Counterproposal".TranslateCache();
            var newOrder = new TradeOrder();
            newOrder.Owner = SessionClientController.My;
            newOrder.Place = PlaceCurrent;
            newOrder.Tile = PlaceCurrent.Tile;
            newOrder.PlaceServerId = PlaceCurrent.PlaceServerId;
            newOrder.CountReady = 0;
            newOrder.SellThings = new List<ThingTrade>();
            newOrder.BuyThings = new List<ThingTrade>();
            newOrder.PrivatPlayers = new List<Player>() { EditOrder.Owner };

            //обновляем сейчас, для такой же картины изменений как при создании нового ордера
            SetEditOrder(newOrder);

            //newOrder.SellThings = EditOrder.BuyThings; так нельзя, т.к. нужно указать конкретные вещи
            //var ts = GameUtils.ChechToTrade(newOrder.BuyThings
            //    , GetAvailableThings()
            //    , null
            //    , out _
            //    , newOrder.CountReady);
            int rate;
            var ts = GameUtils.ChechToTrade(buyThings
                , GetAvailableThings()
                , null
                , out rate);
            newOrder.BuyThings = sellThings;
            newOrder.SellThings = new List<ThingTrade>();
            if (ts != null && rate > 0)
            {
                EditOrderCountReady = rate;
                foreach (var item in ts)
                {
                    if (item.CountToTransfer == 0) continue;
                    if (MainHelper.DebugMode) Loger.Log($"CreateCounterproposal rate={rate} CountToTransfer={item.CountToTransfer}", Loger.LogLevel.EXCHANGE);
                    var th = ThingTrade.CreateTrade(item.AnyThing, item.CountToTransfer / rate);
                    newOrder.SellThings.Add(th);
                }
            }
            if (EditOrderCountReady < 1) EditOrderCountReady = 1;
            EditOrderChange();
        }

    }
}
