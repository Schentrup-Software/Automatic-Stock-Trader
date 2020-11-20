using System;
using Microsoft.ML.Data;

namespace Stonks2
{
    public record StockInput
    {
        public decimal ClosingPrice { get; init; }
        public string StockSymbol { get; init; }
        public DateTime Time { get; init; }
    }

    public class MoneyTracker
    {
        public decimal MoneyMade { get; set; }
        public decimal CostOfLastPosition { get; set; }
    };
}
