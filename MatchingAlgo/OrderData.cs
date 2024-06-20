using System;

namespace MatchingAlgo
{
    internal sealed class OrderData
    {
        /// <summary>
        /// 委托编号
        /// </summary>
        public int OrderID { get; set; }

        /// <summary>
        /// 证券代码 600519.SH 300750.SZ
        /// </summary>
        public string Ticker { get; set; }

        /// <summary>
        /// 委托日期 20240402
        /// </summary>
        public int OrderDate { get; set; }

        /// <summary>
        /// 委托时间 10:23:56.666
        /// </summary>
        public string OrderTime { get; set; }

        /// <summary>
        /// 委托数量
        /// </summary>
        public int OrderQty { get; set; }

        /// <summary>
        /// 委托价格
        /// </summary>
        public double OrderPrc { get; set; }

        /// <summary>
        /// 累计成交数量
        /// </summary>
        public int FillQty { get; set; }

        /// <summary>
        /// 累计成交金额
        /// </summary>
        public double FillMoneyAmount { get; set; }
    }
}