from collections import deque
from threading import Thread
import datetime
import json

class Order:
    order_counter = 1  # 订单计数器，用于生成唯一的订单ID

    def __init__(self, ticker, price, quantity, order_type, order_time=None, order_date=None):
        self.OrderID = Order.order_counter  # 设置订单ID
        Order.order_counter += 1
        self.Ticker = ticker
        self.OrderDate = order_date if order_date else datetime.datetime.now().strftime("%Y%m%d")  # 如果未提供日期，则使用当前日期
        self.OrderTime = order_time if order_time else datetime.datetime.now().strftime("%H:%M:%S.%f") # 如果未提供时间，则使用当前时间
        self.OrderType = order_type
        self.Price = price
        self.Quantity = quantity


class Trade:
    fill_counter = 1  # 订单计数器，用于生成唯一的订单ID
    def __init__(self, order_id, fill_time, fill_qty, fill_price):
        self.OrderID = order_id
        self.FillID = Trade.fill_counter
        Trade.fill_counter += 1
        self.FillTime = fill_time
        self.FillQty = fill_qty
        self.FillPrice = fill_price


class OrderBook:
    def __init__(self, ticker):
        self.Ticker = ticker
        self.bids = deque()
        self.asks = deque()

    def add(self, order):
        if order.OrderType == 'buy':
            self.bids.append(order)
            self.bids = deque(sorted(self.bids, key=lambda order: (-order.Price, order.OrderTime)))
        elif order.OrderType == 'sell':
            self.asks.append(order)
            self.asks = deque(sorted(self.asks, key=lambda order: (order.Price, order.OrderTime)))

    def remove(self, order):
        if order.OrderType == 'buy':
            self.bids.remove(order)
        elif order.OrderType == 'sell':
            self.asks.remove(order)


class MatchingEngine:
    def __init__(self, threaded=False):
        self.Orders = {}  # 存储所有订单的字典
        self.OrderBooks = {}  # 存储每只证券的订单簿
        self.Trades = deque()
        self.Threaded = threaded

        if self.Threaded:
            self.Queue = deque()
            self.Thread = Thread(target=self.run)
            self.Thread.start()

    def get_trades(self):
        trades = list(self.Trades)
        return trades
    def process(self, order):
        self.Orders[order.OrderID] = order

        if order.Ticker not in self.OrderBooks:
            self.OrderBooks[order.Ticker] = OrderBook(order.Ticker)

        if self.Threaded:
            self.Queue.append(order)
        else:
            self.match(order)

    def run(self):
        while True:
            if len(self.Queue) > 0:
                order = self.Queue.popleft()
                self.match(order)
                print("Orderbooks left:",
                      {ticker: len(book.bids) + len(book.asks) for ticker, book in self.OrderBooks.items()})
            time.sleep(1)  # 添加一个小延迟以防止忙等待

    def match(self, order):
        # 打印待匹配订单
        print("New Order (Before Matching):",
              {"type": order.OrderType, "price": order.Price, "quantity": order.Quantity})
        pending_orders = {"bids": [], "asks": []}
        orderbook = self.OrderBooks[order.Ticker]
        for bid in orderbook.bids:
            pending_orders["bids"].append({"price": bid.Price, "quantity": bid.Quantity})
        for ask in orderbook.asks:
            pending_orders["asks"].append({"price": ask.Price, "quantity": ask.Quantity})
        print("Pending Orders (Before Matching):", json.dumps(pending_orders))

        if order.OrderType == 'buy':
            filled = 0
            consumed_asks = []

            for ask in orderbook.asks:
                if ask.Price > order.Price:
                    break
                elif filled == order.Quantity:
                    break

                if filled + ask.Quantity <= order.Quantity:
                    filled += ask.Quantity
                    trade = Trade(order.OrderID, datetime.datetime.now().strftime("%H:%M:%S.%f"), ask.Quantity, ask.Price)
                    self.Trades.append(trade)
                    consumed_asks.append(ask)
                elif filled + ask.Quantity > order.Quantity:
                    volume = order.Quantity - filled
                    filled += volume
                    trade = Trade(order.OrderID, datetime.datetime.now().strftime("%H:%M:%S.%f"), volume, ask.Price)
                    self.Trades.append(trade)
                    ask.Quantity -= volume

            if filled < order.Quantity:
                orderbook.add(Order(order.Ticker, order.Price, order.Quantity - filled, "buy"))

            for ask in consumed_asks:
                orderbook.remove(ask)

        elif order.OrderType == 'sell':
            filled = 0
            consumed_bids = []

            for bid in orderbook.bids:
                if bid.Price < order.Price:
                    break
                if filled == order.Quantity:
                    break

                if filled + bid.Quantity <= order.Quantity:
                    filled += bid.Quantity
                    trade = Trade(order.OrderID, datetime.datetime.now().strftime("%H:%M:%S.%f"), bid.Quantity, bid.Price)
                    self.Trades.append(trade)
                    consumed_bids.append(bid)
                elif filled + bid.Quantity > order.Quantity:
                    volume = order.Quantity - filled
                    filled += volume
                    trade = Trade(order.OrderID, datetime.datetime.now().strftime("%H:%M:%S.%f"), volume, bid.Price)
                    self.Trades.append(trade)
                    bid.Quantity -= volume

            if filled < order.Quantity:
                orderbook.add(Order(order.Ticker, order.Price, order.Quantity - filled, "sell"))

            for bid in consumed_bids:
                orderbook.remove(bid)

        else:

            self.orderbook.add(order)

        # 打印匹配后的订单
        matched_orders = []
        for trade in self.Trades:
            matched_orders.append({"price": trade.FillPrice, "quantity": trade.FillQty})
        print("Matched Orders (After Matching):", json.dumps(matched_orders))



# 测试代码
me = MatchingEngine()

buy_order1 = Order("600519.SH", 10, 100, "buy")
me.process(buy_order1)

sell_order1 = Order("600519.SH", 10, 200, "sell")
me.process(sell_order1)

buy_order2 = Order("600519.SH", 11, 150, "buy")
me.process(buy_order2)

sell_order2 = Order("600519.SH", 9, 200, "sell")
me.process(sell_order2)
