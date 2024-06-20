using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MatchingAlgo;

namespace MatchingEngine
{
    internal class Order
    {
        private static int orderCounter = 1;

        public int OrderID { get; set; }
        public string Ticker { get; set; }
        public int OrderDate { get; set; }
        public string OrderTime { get; set; }
        public string OrderType { get; set; }
        public double Price { get; set; }
        public int Quantity { get; set; }

        public Order(string ticker, double price, int quantity, string orderType, string orderTime = null, int orderDate = 0)
        {
            OrderID = orderCounter++;
            Ticker = ticker;
            OrderDate = orderDate == 0 ? int.Parse(DateTime.Now.ToString("yyyyMMdd")) : orderDate;
            OrderTime = orderTime ?? DateTime.Now.ToString("HH:mm:ss.fff");
            OrderType = orderType;
            Price = price;
            Quantity = quantity;
        }
    }

    internal class Trade
    {
        private static int fillCounter = 1;

        public int OrderID { get; set; }
        public int FillID { get; set; }
        public string FillTime { get; set; }
        public int FillQty { get; set; }
        public double FillPrice { get; set; }

        public Trade(int orderID, string fillTime, int fillQty, double fillPrice)
        {
            OrderID = orderID;
            FillID = fillCounter++;
            FillTime = fillTime;
            FillQty = fillQty;
            FillPrice = fillPrice;
        }
    }

    internal class OrderBook
    {
        public string ticker;
        public Queue<Order> bids = new Queue<Order>();
        public Queue<Order> asks = new Queue<Order>();

        public OrderBook(string ticker)
        {
            this.ticker = ticker;
        }

        public void Add(Order order)
        {
            if (order.OrderType == "buy")
            {
                bids.Enqueue(order);
                bids.OrderBy(o => -o.Price).ThenBy(o => o.OrderTime);
            }
            else if (order.OrderType == "sell")
            {
                asks.Enqueue(order);
                asks.OrderBy(o => o.Price).ThenBy(o => o.OrderTime);
            }
        }

        public void Remove(Order order)
        {
            if (order.OrderType == "buy")
                bids.Dequeue();
            else if (order.OrderType == "sell")
                asks.Dequeue();
        }
    }

    internal class MatchingEngine
    {
        Dictionary<int, Order> orders = new Dictionary<int, Order>();
        Dictionary<string, OrderBook> orderBooks = new Dictionary<string, OrderBook>();
        Queue<Trade> trades = new Queue<Trade>();
        bool threaded;

        Queue<Order> queue = new Queue<Order>();
        Thread thread;

        public MatchingEngine(bool threaded = false)
        {
            this.threaded = threaded;
            if (threaded)
            {
                thread = new Thread(Run);
                thread.Start();
            }
        }

        public List<Trade> GetTrades()
        {
            return trades.ToList();
        }

        public void Process(Order order)
        {
            orders.Add(order.OrderID, order);

            // 将订单信息保存到数据库
            DatabaseManager.InsertOrder(new OrderData()
            {
                OrderID = order.OrderID,
                Ticker = order.Ticker,
                OrderDate = order.OrderDate,
                OrderTime = order.OrderTime,
                OrderQty = order.Quantity,
                OrderPrc = order.Price
            });
            
            if (!orderBooks.ContainsKey(order.Ticker))
                orderBooks.Add(order.Ticker, new OrderBook(order.Ticker));

            if (threaded)
                queue.Enqueue(order);
            else
                Match(order);
        }

        private void Run()
        {
            while (true)
            {
                if (queue.Count > 0)
                {
                    var order = queue.Dequeue();
                    Match(order);
                    Console.WriteLine("Orderbooks left: " + string.Join(", ", orderBooks.Select(kv => kv.Key + ": " + (kv.Value.bids.Count + kv.Value.asks.Count))));
                }
                Thread.Sleep(1000); // 添加一个小延迟以防止忙等待
            }
        }

        private void Match(Order order)
        {
            Console.WriteLine("New Order (Before Matching): " + $"{{\"type\": \"{order.OrderType}\", \"price\": {order.Price}, \"quantity\": {order.Quantity}}}");
            var pendingOrders = new Dictionary<string, List<Order>>();
            foreach (var book in orderBooks.Values)
            {
                pendingOrders[book.ticker] = new List<Order>();
                foreach (var bid in book.bids)
                    pendingOrders[book.ticker].Add(new Order(bid.Ticker, bid.Price, bid.Quantity, bid.OrderType, bid.OrderTime, bid.OrderDate));
                foreach (var ask in book.asks)
                    pendingOrders[book.ticker].Add(new Order(ask.Ticker, ask.Price, ask.Quantity, ask.OrderType, ask.OrderTime, ask.OrderDate));
            }
            Console.WriteLine("Pending Orders (Before Matching): " + Newtonsoft.Json.JsonConvert.SerializeObject(pendingOrders));

            if (order.OrderType == "buy")
            {
                var filled = 0;
                var consumedAsks = new List<Order>();

                foreach (var ask in orderBooks[order.Ticker].asks)
                {
                    if (ask.Price > order.Price)
                        break;
                    else if (filled == order.Quantity)
                        break;

                    if (filled + ask.Quantity <= order.Quantity)
                    {
                        filled += ask.Quantity;
                        trades.Enqueue(new Trade(order.OrderID, DateTime.Now.ToString("HH:mm:ss.fff"), ask.Quantity, ask.Price));
                        consumedAsks.Add(ask);
                        DatabaseManager.UpdateOrder(ask.OrderID, ask.Quantity, ask.Quantity * ask.Price);
                    }
                    else if (filled + ask.Quantity > order.Quantity)
                    {
                        var volume = order.Quantity - filled;
                        filled += volume;
                        trades.Enqueue(new Trade(order.OrderID, DateTime.Now.ToString("HH:mm:ss.fff"), volume, ask.Price));
                        ask.Quantity -= volume;
                        DatabaseManager.UpdateOrder(ask.OrderID, volume, volume * ask.Price);
                    }
                }

                if (filled < order.Quantity)
                    orderBooks[order.Ticker].Add(new Order(order.Ticker, order.Price, order.Quantity - filled, "buy"));

                foreach (var ask in consumedAsks)
                    orderBooks[order.Ticker].Remove(ask);
            }
            else if (order.OrderType == "sell")
            {
                var filled = 0;
                var consumedBids = new List<Order>();

                foreach (var bid in orderBooks[order.Ticker].bids)
                {
                    if (bid.Price < order.Price)
                        break;
                    if (filled == order.Quantity)
                        break;

                    if (filled + bid.Quantity <= order.Quantity)
                    {
                        filled += bid.Quantity;
                        trades.Enqueue(new Trade(order.OrderID, DateTime.Now.ToString("HH:mm:ss.fff"), bid.Quantity, bid.Price));
                        consumedBids.Add(bid);
                        DatabaseManager.UpdateOrder(bid.OrderID, bid.Quantity, bid.Quantity * bid.Price);
                    }
                    else if (filled + bid.Quantity > order.Quantity)
                    {
                        var volume = order.Quantity - filled;
                        filled += volume;
                        trades.Enqueue(new Trade(order.OrderID, DateTime.Now.ToString("HH:mm:ss.fff"), volume, bid.Price));
                        bid.Quantity -= volume;
                        DatabaseManager.UpdateOrder(bid.OrderID, volume, volume * bid.Price);
                    }
                }

                if (filled < order.Quantity)
                    orderBooks[order.Ticker].Add(new Order(order.Ticker, order.Price, order.Quantity - filled, "sell"));
                
                // 撮合成功后更新数据库中的委托信息
                if (filled > 0)
                {
                    DatabaseManager.UpdateOrder(order.OrderID, filled, filled * order.Price);
                }

                // 将成交信息保存到数据库
                foreach (var trade in trades)
                {
                    DatabaseManager.InsertFill(new FillData
                    {
                        OrderID = trade.OrderID,
                        FillID = trade.FillID,
                        FillTime = trade.FillTime,
                        FillQty = trade.FillQty,
                        FillPrice = trade.FillPrice,
                        FillMoneyAmount = trade.FillQty * trade.FillPrice // 计算成交金额
                    });
                }

                foreach (var bid in consumedBids)
                {
                    orderBooks[order.Ticker].Remove(bid);
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            MatchingEngine me = new MatchingEngine(true); // 启动一个线程，用多线程模式
            // MatchingEngine me = new MatchingEngine(); // 非多线程，普通模式

            Order buyOrder1 = new Order("600519.SH", 10, 100, "buy");
            me.Process(buyOrder1);

            Order sellOrder1 = new Order("600519.SH", 10, 200, "sell");
            me.Process(sellOrder1);

            Order buyOrder2 = new Order("600519.SH", 11, 150, "buy");
            me.Process(buyOrder2);

            Order sellOrder2 = new Order("600519.SH", 9, 200, "sell");
            me.Process(sellOrder2);

            Console.ReadLine();
        }
    }
}
