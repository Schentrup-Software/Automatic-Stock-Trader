using System;

namespace AutomaticStockTrader.Tests.Tracking
{
    public record TrackingRecord
    {
        public DateTime Date { get; set; }
        public decimal PercentageMade { get; set; }
        public string StrategyName { get; set; }
    }
}
