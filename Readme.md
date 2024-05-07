# 股票撮合系统（双代码）

原理：
Order订单对象作为匹配算法之前的元素，而Trade交易对象则是匹配之后的成交对象.

OrderBook实现订单表的管理。

有两种模式，多线程模式下用MatchingEngine()启动一个线程执行Run方法对队列进行轮询，Process方法在多线程模式下仅将新订单入队

Match为核心匹配方法：

创建两个FIFO队列；一个用于存储所有传入的订单，另一个用于存储经过匹配后所有产生的交易。我们还需要存储所有没有匹配的订单。

之后，通过调用.process（order）函数将订单传递给匹配引擎。然后将匹配生成的交易存储在队列中。

在订单队列中遍历，直到收到的订单被完全匹配为止。对于每个匹配成功的订单，都会更新订单信息并创建一个交易对象并将其添加到交易队列中，添加交易信息。如果匹配引擎无法完全完成匹配，则它将剩余量作为单独的订单再添加会订单队列中。

## C#代码

Program.cs是主方法文件；

DatabaseManager是Dto层；

FillData和OrderData是领域模型层。

用IDE打开解决方案后，直接在Program.cs中执行即可。执行后即可看到如截图中的执行结果。

### 多线程

默认多线程模式，要使用普通模式请修改注释下面的代码入口：
```
            MatchingEngine me = new MatchingEngine(true); // 启动一个线程，用多线程模式
            // MatchingEngine me = new MatchingEngine(); // 非多线程，普通模式
```            

### 数据库

SQL表创建代码在CreateTableOrder和CreateTableFill两个方法中。

数据库读写逻辑：

1. 订单入队时InsertOrder，创建订单信息
2. 撮合命中一个队列中订单时UpdateOrder，更新订单信息
3. 撮合新订单成功时InsertFill,创建成交记录

# Python代码：

直接运行main1.py中即可，测试代码为
```
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

```

原理同上，未连接数据库。
