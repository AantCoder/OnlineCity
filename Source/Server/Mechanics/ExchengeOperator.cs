using Model;
using OCUnion;
using ServerOnlineCity.Common;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Transfer;
using Transfer.ModelMails;

namespace ServerOnlineCity.Mechanics
{
    public class ExchengeOperator
    {
        // При расчете сделки сравниваются одна.BuyThings.MatchesThing(другая.SellThings) и наоброт, определяется макс кол-во повторов для каждой из 2х сделок
        // Например если один продавал бревна по 2 шт * 10, а другой покупал по 10 шт * 2
        // Лишнее количество в SellThings обоих сделках помещается в их яблоки (до кратности кол-во повторов)
        //Пример (sell, buy): (1 бревно, 10 монет) и (12 монет, 1 бревно) => работает + возврат 2х монет за каждый повтор в яблоко владельца второй сделки
        // (1 бревно, 12 монет) и (10 монет, 1 бревно) => не работает
        // (10 монет, 1 бревно) и (1 бревно, 12 монет) => не работает
        // т.е. должно соблюдаться оба условия одна.SellThings >= другая.BuyThings (если >, то остаток shell в яблоко)
        // (2 бревна, 10 монет) и (12 монет, 1 бревно) => работает + возврат 2х монет второму и 1 бревна первому 
        // (можно перед возвратом перерасчитать на сколько хватит сделки и увеличить её повторы, но не надо наверное)
        //В ExchengeBuy тот же функционал только предлагаемые в уплату вещи не из SellThings, а из яблока
        //Для ExchengeBuy: (1 бревно, 10 монет) и (содержимое яблока, копия "1 бревно")
        //для быстрого отсечения сделок сделать хеш BuyThings и SellThings: отсортированный DefName

        //(s, b) продает 2 за 10 повторы: 3 (6 - 30) повторы: 9 (18 - 90)
        //(b, s) покупка 3 за 90          2 (6 - 270)         1 (3  - 90)
        // обязательно чтобы s одной сделки было >= b другой, если не ==, а >, то в этом случае лишнее возвращается владельцу sZZ

        //(s, b) продает 2 за 4 повторы: 3 (6 - 12) повторы: 3 (6  - 12)
        //(b, s) покупка 3 за 3          2 (6 - 6)           4 (12 - 12)

        public BaseContainer Data;

        public List<TradeWorldObjectEntry> TradeWorldObjects; //calc from Data.Orders

        public List<TradeWorldObjectEntry> TradeWorldObjectsDeleted;

        public ConcurrentDictionary<long, TradeOrder> OrdersById; //calc from Data.Orders

        public ConcurrentDictionary<int, HashSet<TradeOrder>> OrdersByTile; //calc from Data.Orders

        private bool ImplementTradeCalcing = false;

        private ConcurrentDictionary<string, ConcurrentDictionary<int, TradeThingStorage>> CacheStorage = new ConcurrentDictionary<string, ConcurrentDictionary<int, TradeThingStorage>>();

        public ExchengeOperator(BaseContainer data)
        {
            Data = data;
            TradeWorldObjects = Data.Orders.Select(o => (TradeWorldObjectEntry)new TradeOrderShort(o)).ToList();
            TradeWorldObjectsDeleted = new List<TradeWorldObjectEntry>();
            OrdersById = new ConcurrentDictionary<long, TradeOrder>(Data.Orders.ToDictionary(o => o.Id));
            OrdersByTile = new ConcurrentDictionary<int, HashSet<TradeOrder>>(
                Data.Orders
                .GroupBy(o => o.Tile)
                .ToDictionary(g => g.Key, g => g.ToHashSet()));

            foreach (var order in Data.Orders)
            {
                foreach (var st in order.SellThings)
                {
                    if (!string.IsNullOrEmpty(st.Data)) st.DataHash = Data.SetInUploadService(st.Data);
                }
            }
        }
        public bool OrderAdd(TradeOrder order)
        {
            if (GetFromStorage(order.Tile, order.Owner.GetPlayerServer(), order.SellThings, order.CountReady) == null) return false;

            //order.Id = Repository.GetData.GetChatId();
            order.Id = ChatManager.Instance.GetChatId();
            order.UpdateTime = DateTime.UtcNow;
            Data.Orders.Add(order);
            OrdersById.TryAdd(order.Id, order);
            OrdersByTile.GetOrAdd(order.Tile, new HashSet<TradeOrder>()).Add(order);

            foreach (var st in order.SellThings)
            {
                if (!string.IsNullOrEmpty(st.Data)) st.DataHash = Data.SetInUploadService(st.Data);
            }

            if (Data.OrdersPlaceServerIdByTile.TryGetValue(order.Tile, out long placeServerId))
                order.PlaceServerId = placeServerId;
            else
                Data.OrdersPlaceServerIdByTile.Add(order.Tile, order.PlaceServerId = Data.GetWorldObjectEntryId());

            var trade = new TradeOrderShort(order);
            TradeWorldObjects.Add(trade);

            ImplementTrade(order);

            return true;
        }
        public void OrderRemove(TradeOrder order)
        {
            order.UpdateTime = DateTime.UtcNow;
            Data.Orders.Remove(order);
            OrdersById.TryRemove(order.Id, out _);
            OrdersByTile.TryGetValue(order.Tile, out var obt);
            obt?.Remove(order);
            if ((obt?.Count ?? 0) == 0) OrdersByTile.TryRemove(order.Tile, out _);
            var trade = TradeWorldObjects.FirstOrDefault(to => to.Id == order.Id);
            if (trade != null)
            {
                TradeWorldObjects.Remove(trade);
                TradeWorldObjectsDeleted.Add(trade);
            }
            //Перекидываем вещи в торговый склад
            foreach (var st in order.SellThings)
            {
                st.Count *= order.CountReady;
            }
            SendToStorage(order.Tile, order.Owner.GetPlayerServer(), order.SellThings);
        }
        public bool OrderUpdate(TradeOrder newOrder, TradeOrder oldOrder)
        {
            //Перекидываем вещи, на которые уменьшилось в торговый склад:
            //удаляем из oldOrder всё что есть в новой, остатки на склад
            var newThings = newOrder.SellThings.OrderByDescendingCost();  // сначала выбираем хорошие, более требовательные вещи
            var oldThings = oldOrder.SellThings.OrderByCost(); // сначала предлагаем на соответствие более плохие вещи
            var newThingsOrig = new List<ThingTrade>(); //вещи новые в сделке, их нужно отнять с торгового склада
            for (int si = 0; si < oldThings.Count; si++)
            {
                oldThings[si] = (ThingTrade)oldThings[si].Clone();
                oldThings[si].Count *= oldOrder.CountReady;
            }
            for (int bi = 0; bi < newThings.Count; bi++)
            {
                var thing = newThings[bi];
                int newThingsCount = thing.Count * newOrder.CountReady;
                for (int si = 0; si < oldThings.Count; si++)
                {
                    var item = oldThings[si];
                    if (!thing.MatchesThingTrade(item)) continue;

                    if (item.Count > newThingsCount)
                    {
                        item.Count -= newThingsCount;
                        newThingsCount = 0;
                        break;
                    }
                    else
                    {
                        newThingsCount -= item.Count;
                        oldThings.RemoveAt(si--);
                    }
                }
                if (newThingsCount > 0)
                {
                    var ot = thing.Clone() as ThingTrade;
                    ot.Count = newThingsCount;
                    newThingsOrig.Add(ot);
                }
            }
            var playerServer = oldOrder.Owner.GetPlayerServer();
            if (newThingsOrig.Count > 0) if (GetFromStorage(oldOrder.Tile, playerServer, newThingsOrig) == null) return false;
            if (oldThings.Count > 0) SendToStorage(oldOrder.Tile, playerServer, oldThings);

            newOrder.UpdateTime = DateTime.UtcNow;
            Data.Orders[Data.Orders.IndexOf(oldOrder)] = newOrder;
            OrdersById[newOrder.Id] = newOrder;
            OrdersByTile.TryGetValue(newOrder.Tile, out var obt);
            obt.Remove(oldOrder);
            obt.Add(newOrder);

            foreach (var st in newOrder.SellThings)
            {
                if (!string.IsNullOrEmpty(st.Data)) st.DataHash = Data.SetInUploadService(st.Data);
            }

            for (int i = 0; i < TradeWorldObjects.Count; i++)
            {
                if (TradeWorldObjects[i].Id == newOrder.Id)
                {
                    var newTrade = new TradeOrderShort(newOrder);
                    TradeWorldObjects[i] = newTrade;
                    break;
                }
            }

            //Проверяем не сработаетют ли сделки
            ImplementTrade(newOrder);

            return true;
        }

        /// <summary>
        /// Находит все вещи с указаным def
        /// </summary>
        /// <param name="thingDef"></param>
        /// <returns>Словарь всех вещей и место где они находяться, только для чтения.</returns>
        public Dictionary<ThingTrade, TradeThingStorage> FindThingDef(PlayerServer player, ThingTrade thingDef)
        {
            return player.TradeThingStorages?
                .SelectMany(tts => tts.Things?.Where(t => t.DefName == thingDef.DefName).Select(t => new { thing = t, place = tts }))
                .Where(a => a != null)
                .ToDictionary(a => a.thing, a => a.place)
                ?? new Dictionary<ThingTrade, TradeThingStorage>();
        }
        public int CountThingDef(PlayerServer player, ThingTrade thingDef)
        {
            return player.TradeThingStorages?
                .Sum(tts => tts.Things?.Where(t => t.DefName == thingDef.DefName).Sum(t => t.Count) ?? 0)
                ?? 0;
        }

        public TradeThingStorage GetStorage(int tile, Player player, bool needAdd)
        {
            var dicPl = CacheStorage.GetOrAdd(player.Login, (login) =>
            {
                //Загружаем все данные игрока при первом обращении к игроку
                var pl = Repository.GetPlayerByLogin(player.Login);
                return new ConcurrentDictionary<int, TradeThingStorage>(pl.TradeThingStorages.ToDictionary(s => s.Tile));
            });
            if (needAdd)
            {
                var storage = dicPl.GetOrAdd(tile, (t) =>
                {
                    //При обращении к точке создаем её, если её нет
                    var ns = new TradeThingStorage()
                    {
                        Id = 0,
                        PlaceServerId = Data.GetWorldObjectEntryId(),
                        Tile = tile,
                        LoginOwner = player.Login,
                        Things = new List<ThingTrade>(),
                        Type = TradeWorldObjectEntryType.ThingsPlayer,
                        UpdateTime = DateTime.UtcNow
                    };
                    var pl = Repository.GetPlayerByLogin(player.Login);
                    pl.TradeThingStorages.Add(ns);
                    return ns;
                });
                return storage;
            }
            else
            {
                if (!dicPl.TryGetValue(tile, out var storage)) return null;
                return storage;
            }
        }

        /// <summary>
        /// Добавляет вещи в хранилище игрока, если хранилищя нет, он создается
        /// </summary>
        public void SendToStorage(int tile, PlayerServer player, List<ThingTrade> things)
        {
            things = things.OrderByDescendingCost();  // сначала выбираем хорошие, более требовательные вещи, также проверка на 0 кол-во
            if (things.Count == 0) return;

            Loger.Log($"Server SendToStorage tile={tile} pl={player.Public.Login} things=" + things.ToStringLabel());

            var storage = GetStorage(tile, player.Public, true);
            var storageRead = storage.Things.OrderByCost(); // сначала предлагаем на соответствие более плохие вещи
            
            //находим вещи, которые уже есть в хранилище и объединяем их, остальные просто добавляем
            for (int bi = 0; bi < things.Count; bi++)
            {
                var thing = things[bi];
                if (thing.DefName == MainHelper.CashlessThingDefName)
                {
                    player.CashlessBalance += thing.Count;
                    continue;
                }
                ThingTrade found = null;
                for (int si = 0; si < storageRead.Count; si++)
                {
                    var item = storageRead[si];
                    if (!thing.MatchesThingTrade(item, true)) continue;
                    found = item;
                    break;
                }
                if (found != null) found.Count += thing.Count;
                else
                {
                    storage.Things.Add(thing);
                    storageRead.Add(thing);
                }
            }
            storage.UpdateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Изымает вещи из хранилища по фильтру. Если хотя бы чего-то не хватает результат null
        /// </summary>
        public List<ThingTrade> GetFromStorage(int tile, PlayerServer player, List<ThingTrade> filter, int filterRate = 1)
        {
            Loger.Log($"Server GetFromStorage tile={tile} pl={player.Public.Login} rate={filterRate} filter=" + filter?.ToStringThing());

            filter = filter.OrderByDescendingCost();  // сначала выбираем хорошие, более требовательные вещи
            var storage = GetStorage(tile, player.Public, false);
            var storageThings = storage?.Things ?? new List<ThingTrade>();
            var storageRead = storageThings.OrderByCost(); // сначала предлагаем на соответствие более плохие вещи

            var select = new List<ThingTrade>();
            var selectCashless = 0;
            var newCountStorage = new Dictionary<ThingTrade, int>();
            for (int bi = 0; bi < filter.Count; bi++)
            {
                var need = filter[bi];
                var countSelected = 0;

                if (need.DefName == MainHelper.CashlessThingDefName)
                {
                    if (need.Count * filterRate >= player.CashlessBalance)
                    {
                        selectCashless = (int)player.CashlessBalance;
                        countSelected = selectCashless;
                    }
                    else
                    {
                        selectCashless = need.Count * filterRate;
                        countSelected = selectCashless;
                    }
                }
                else
                    for (int si = 0; si < storageRead.Count; si++)
                    {
                        var item = storageRead[si];

                        if (!newCountStorage.TryGetValue(item, out var itemCount)) itemCount = item.Count;
                        if (itemCount == 0) continue;

                        if (!need.MatchesThingTrade(item)) continue;

                        if (need.Count * filterRate - countSelected >= itemCount)
                        {
                            select.Add(item);
                            newCountStorage[item] = 0;
                            countSelected += itemCount;
                        }
                        else
                        {
                            var itemOut = (ThingTrade)item.Clone();
                            itemOut.Count = need.Count * filterRate - countSelected;
                            newCountStorage[item] = itemCount - itemOut.Count;
                            select.Add(itemOut);
                            countSelected += itemOut.Count;
                        }
                    }
                if (countSelected < need.Count * filterRate) return null;
            }
            //если выхода не было, значит вещей достаточно, выбираем со склада
            if (selectCashless > 0)
            {
                player.CashlessBalance -= selectCashless;
                select.Add(ThingTrade.CreateTradeServer(MainHelper.CashlessThingDefName, selectCashless));
            }
            foreach (var nc in newCountStorage)
            {
                if (nc.Value > 0) nc.Key.Count = nc.Value;
                else
                {
                    storageRead.Remove(nc.Key);
                    storageThings.Remove(nc.Key);
                }
            }
            if (newCountStorage.Any() && storage != null)
            {
                storage.UpdateTime = DateTime.UtcNow;
            }
            return select;
        }

        /// <summary>
        /// Два списка вещей и их максимальное количество повторов.
        /// </summary>
        /// <returns>Неизрасходованные вещи в sellThings для каждой сделки с учетом повторов в sellRepeat, но без учета возможных повторов сверх того</returns>
        private List<ThingTrade> CompareListTrade(List<ThingTrade> sellThings, List<ThingTrade> buyThings
            , ref int sellRepeat, ref int buyRepeat
            , int sellAllCount, int buyAllCount
            , out List<ThingTrade> sellByBuyThings)
        {
            buyThings = buyThings.OrderByDescendingCost();  // сначала выбираем хорошие, более требовательные вещи
            var sts = sellThings.OrderByCost(); // сначала предлагаем на соответствие более плохие вещи
            var excess = new List<ThingTrade>();
            sellByBuyThings = new List<ThingTrade>();
            for (int bi = 0; bi < buyThings.Count; bi++)
            {
                bool isOk = false;
                for (int si = 0; si < sts.Count; si++)
                {
                    var buy = buyThings[bi];
                    var sell = sts[si];
                    if (!buy.MatchesThingTrade(sell)) continue;
                    //это одна и та же вешь, теперь вопрос в количестве

                    //если предложение меньше спроса, то может быть можно установить разные пропорции повтора сделки
                    if (sell.Count * sellRepeat < buy.Count * buyRepeat) 
                    {
                        //если тербуется разные повторы у sell и buy при том, что они уже заданы, то сделка невозможна
                        if (sellRepeat != 1 || buyRepeat != 1) continue;

                        //кол-во единиц товара для минимального оборота каждой стороны (при возможных разных кол-во повторов для обоих сторон)
                        var cnt = NOK(sell.Count, buy.Count); // (2,3)=6  (10,5)=10

                        //достаточно ли количества для хотя бы 1 сделки
                        if (sellAllCount * sell.Count < cnt || buyAllCount * buy.Count < cnt) continue;

                        sellRepeat = cnt / sell.Count;
                        buyRepeat = cnt / buy.Count;

                        if (sellRepeat > 1) foreach (var item in excess) item.Count *= sellRepeat;
                    }
                    if (sell.Count * sellRepeat > buy.Count * buyRepeat)
                    {
                        //отмечаем лишние ресурсы не учавствующие в сделке
                        var sellClone = (ThingTrade)sell.Clone();
                        sellClone.Count = sell.Count * sellRepeat - buy.Count * buyRepeat;
                        excess.Add(sellClone);
                    }
                    //пропорции уравновешины, лишнее убрано (лишнее когда покупатель готов заплатить больше чем требует продавец)
                    var sellByBuyThing = (ThingTrade)sell.Clone();
                    sellByBuyThing.Count = buy.Count * buyRepeat;
                    sellByBuyThings.Add(sellByBuyThing);
                    isOk = true;
                    sts.RemoveAt(si--);
                    break;
                }
                if (!isOk) return null;
            }
            return excess;
        }

        public static int NOD(int a, int b)
        {
            return (int)BigInteger.GreatestCommonDivisor((BigInteger)a, (BigInteger)b);
        }
        public static int NOK(int a, int b)
        {
            return Math.Abs(a * b) / NOD(a, b);
        }

        private class ImplementSelectTrade
        {
            public float Cost { get; set; }
            public TradeOrder Order1 { get; set; }
            public TradeOrder Order2 { get; set; }
            public int Count1 { get; set; }
            public int Count2 { get; set; }
            public List<ThingTrade> SellEnd1 { get; set; }
            public List<ThingTrade> SellEnd2 { get; set; }
            public List<ThingTrade> ThingsFor1 { get; set; }
            public List<ThingTrade> ThingsFor2 { get; set; }
        }

        /// <summary>
        /// Реализация сделки о всеми с которыми возможно в её точке.
        /// Запускать для любой измененной или созданной сделки.
        /// </summary>
        private void ImplementTrade(TradeOrder o1)
        {
            if (ImplementTradeCalcing) return;
            ImplementTradeCalcing = true;
            if (!OrdersByTile.TryGetValue(o1.Tile, out var to)) return;
            var tradeOrders = to.ToList();
            tradeOrders.Remove(o1);

            //цикл каждый с каждым
            ImplementSelectTrade best;
            do
            {
                best = null;
                for (int io = 0; io < tradeOrders.Count; io++)
                {
                    var o2 = tradeOrders[io];
                    if (o1.SellThingsHash != o2.BuyThingsHash
                        || o2.SellThingsHash != o1.BuyThingsHash) continue;

                    int repeat1 = 1;
                    int repeat2 = 1;
                    var sellEnd1 = CompareListTrade(o1.SellThings, o2.BuyThings, ref repeat1, ref repeat2, o1.CountReady, o2.CountReady, out var thingsFor2);
                    if (sellEnd1 == null) continue;
                    var sellEnd2 = CompareListTrade(o2.SellThings, o1.BuyThings, ref repeat2, ref repeat1, o2.CountReady, o1.CountReady, out var thingsFor1);
                    if (sellEnd2 == null) continue;

                    //общие повторы разных повторов repeat1 и repeat2
                    var repeatRepeat = o1.CountReady / repeat1 < o2.CountReady / repeat2 ? o1.CountReady / repeat1 : o2.CountReady / repeat2;

                    if (repeatRepeat > 1)
                    {
                        foreach (var item in sellEnd1) item.Count *= repeatRepeat;
                        foreach (var item in sellEnd2) item.Count *= repeatRepeat;
                        foreach (var item in thingsFor1) item.Count *= repeatRepeat;
                        foreach (var item in thingsFor2) item.Count *= repeatRepeat;
                    }

                    var that = new ImplementSelectTrade()
                    {
                        //определяем выгоду сделки, по остатку от реализации SellEnd1/2 чем больше тут кол-во тем выгодней
                        //цена умножается на цену в игре, это имеет значение только если двое разных SellEnd1/2
                        Cost = sellEnd1.Select(t => t.Count * t.GameCost).Sum(),
                        Count1 = repeat1 * repeatRepeat,
                        Count2 = repeat2 * repeatRepeat,
                        Order1 = o1,
                        Order2 = o2,
                        SellEnd1 = sellEnd1,
                        SellEnd2 = sellEnd2,
                        ThingsFor1 = thingsFor1,
                        ThingsFor2 = thingsFor2
                    };
                    if (best == null || best.Cost < that.Cost) best = that;
                }
                //заключаем лучшу сделку
                if (best != null)
                {
                    Loger.Log("Server Trade! " + Environment.NewLine
                        + "trade x" + best.Count1 + " " + best.Order1.ToString() + Environment.NewLine
                        + "trade x" + best.Count2 + " " + best.Order2.ToString());

                    var msg0 = "OC_ExchengeOperator_tradeSold0 {0} OC_ExchengeOperator_tradeSold1 {1}. OC_ExchengeOperator_tradeSold2 {2}"; //локализация этих фраз OC_ ChatController.ServerCharTranslate
                    var msg1 = string.Format(msg0
                        , best.Count1
                        , best.Order1.CountReady == best.Count1 ? "OC_ExchengeOperator_Closed" : "OC_ExchengeOperator_left" + (best.Order1.CountReady - best.Count1).ToString()
                        , best.ThingsFor1.ToStringLabel());
                    var msg2 = string.Format(msg0
                        , best.Count2
                        , best.Order2.CountReady == best.Count2 ? "OC_ExchengeOperator_Closed" : "OC_ExchengeOperator_left" + (best.Order2.CountReady - best.Count2).ToString()
                        , best.ThingsFor2.ToStringLabel());

                    var playerServer1 = best.Order1.Owner.GetPlayerServer();
                    var playerServer2 = best.Order2.Owner.GetPlayerServer();

                    //отправляем купленное
                    SendToStorage(o1.Tile, playerServer1, best.ThingsFor1);
                    SendToStorage(o1.Tile, playerServer2, best.ThingsFor2);

                    //реализуем сделку, кол-во раз для первой и второй сделки может быть разным: t1.CountReady - t1Count и t2.CountReady - t2Count
                    //отправляем избыток в яблоки (например, если кто-то продает дешевле, чем мы собирались купить)
                    msg0 = "OC_ExchengeOperator_OrderClosedBetter";
                    if (best.SellEnd1.Count > 0)
                    {
                        msg1 += Environment.NewLine + msg0 + best.SellEnd1.ToStringLabel();
                        SendToStorage(o1.Tile, playerServer1, best.SellEnd1);
                    }
                    if (best.SellEnd2.Count > 0)
                    {
                        msg1 += Environment.NewLine + msg0 + best.SellEnd2.ToStringLabel();
                        SendToStorage(o1.Tile, playerServer2, best.SellEnd2);
                    }

                    //уменьшаем счетчики сделок
                    best.Order1.CountReady -= best.Count1;
                    best.Order2.CountReady -= best.Count2;

                    //удаляем ставшие пустыми сделки
                    if (best.Order2.CountReady == 0)
                    {
                        OrderRemove(best.Order2);
                        tradeOrders.Remove(best.Order2);
                    }
                    if (best.Order1.CountReady == 0)
                    {
                        OrderRemove(best.Order1);
                        break;
                    }

                    //рассылка писем
                    var pl1 = best.Order1.Owner.GetPlayerServer();
                    var pl2 = best.Order2.Owner.GetPlayerServer();
                    HelperMailMessadge.Send(
                        Repository.GetData.PlayerSystem
                        , pl1
                        , "OC_ExchengeOperator_OrderDone"
                        , msg1
                        , ModelMailMessadge.MessadgeTypes.GoldenLetter
                        , best.Order1.Tile
                        );
                    HelperMailMessadge.Send(
                        Repository.GetData.PlayerSystem
                        , pl2
                        , "OC_ExchengeOperator_OrderDone"
                        , msg2
                        , ModelMailMessadge.MessadgeTypes.GoldenLetter
                        , best.Order1.Tile
                        );
                }
            } while (best != null);

            ImplementTradeCalcing = false;
        }

        /// <summary>
        /// Икрок хочет купить указанную сделку не за другую сделку, а за содержимое торгового склада
        /// </summary>
        public bool ImplementTradeByStorage(TradeOrder order, PlayerServer player, int needRepeat)
        {
            if (order.CountReady < needRepeat) return false;
            var thingsForOrder = GetFromStorage(order.Tile, player, order.BuyThings, needRepeat);
            if (thingsForOrder == null) return false;

            Loger.Log("Server Trade! " + player.Public.Login + " x" + needRepeat + " buy " + order.ToString());

            var orderPlayer = order.Owner.GetPlayerServer();
            SendToStorage(order.Tile, orderPlayer, thingsForOrder);

            var msg1 = string.Format("OC_ExchengeOperator_tradeSold0 {0} OC_ExchengeOperator_tradeSold1 {1}. OC_ExchengeOperator_tradeSold2 {2}" // Ваша сделка сработала
                , needRepeat
                , order.CountReady == needRepeat ? "OC_ExchengeOperator_Closed" : "OC_ExchengeOperator_left" + (order.CountReady - needRepeat).ToString()
                , thingsForOrder.ToStringLabel());

            var thingsFromOrder = order.SellThings.Select(t =>
                {
                    var tt = (ThingTrade)t.Clone();
                    tt.Count *= needRepeat;
                    return tt;
                }).ToList();

            SendToStorage(order.Tile, player, thingsFromOrder);

            var msg2 = string.Format("OC_ExchengeOperator_tradeBought0 {0} OC_ExchengeOperator_tradeBought1 {1}" // Вы приобрели по сделке
                , needRepeat
                , thingsFromOrder.ToStringLabel());

            //уменьшаем счетчики сделок
            order.CountReady -= needRepeat;

            //удаляем ставшие пустыми сделки
            if (order.CountReady == 0)
            {
                OrderRemove(order);
            }

            HelperMailMessadge.Send(
                Repository.GetData.PlayerSystem
                , orderPlayer
                , "OC_ExchengeOperator_OrderDone" // Ваша сделка сработала
                , msg1
                , ModelMailMessadge.MessadgeTypes.GoldenLetter
                , order.Tile
                );

            HelperMailMessadge.Send(
                Repository.GetData.PlayerSystem
                , player
                , "OC_ExchengeOperator_YouBought" // Вы купили по сделке
                , msg2
                , ModelMailMessadge.MessadgeTypes.GoldenLetter
                , order.Tile
                );

            return true;
        }

        internal IEnumerable<Tuple<ThingTrade, int>> GetThingsInServer(PlayerServer player) =>
            player.TradeThingStorages
                .SelectMany(x => x.Things)
                .Select(x => new Tuple<ThingTrade, int>(x, 1))
                .Union(Data?.Orders
                    .Where(o => o.Owner.Login == player.Public.Login)
                    .SelectMany(o => o.SellThings.Select(x => new Tuple<ThingTrade, int>(x, o.CountReady)))
                );

        public void DayPassed(PlayerServer player)
        {
            var data = Repository.GetData;

            //для пешек и вещей которые портятся от тепла
            var summ = data.OrderOperator.GetThingsInServer(player)
                ?.Where(t => t.Item1.IsPawn || t.Item1.Rottable)
                ?.Sum(x => x.Item1.GameCost * x.Item1.Count * x.Item2) ?? 0;

            //налог = цена в серебре / средняя цена пешки (5000$) * цена сухпайка (25$)
            summ *= 25f / 5000f;
            player.CashlessBalance -= (int)summ;
        }

    }
}
