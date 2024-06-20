namespace MatchingAlgo
{
    internal sealed class FillData
    {
        /// <summary>
        /// 委托编号 对应OrderData.OrderID
        /// </summary>
        public int OrderID { get; set; }

        /// <summary>
        /// 成交编号
        /// </summary>
        public int FillID { get; set; }

        /// <summary>
        /// 成交时间 10:23:56.666
        /// </summary>
        public string FillTime { get; set; }

        /// <summary>
        /// 成交数量
        /// </summary>
        public int FillQty { get; set; }

        /// <summary>
        /// 成交价格
        /// </summary>
        public double FillPrice { get; set; }

        /// <summary>
        /// 成交金额
        /// </summary>
        public double FillMoneyAmount { get; set; }
    }
}