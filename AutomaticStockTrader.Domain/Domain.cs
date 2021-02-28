using System;

namespace AutomaticStockTrader.Domain
{
    public record StockInput
    {
        public decimal ClosingPrice { get; init; }
        public string StockSymbol { get; init; }
        public DateTime Time { get; init; }
    }

    public record Order
    {
        public string StockSymbol { get; init; }
        public DateTime OrderPlacedTime { get; init; }
        public long SharesBought { get; init; }
        public decimal MarketPrice { get; init; }
    }

    public record StrategysStock
    {
        public string Strategy { get; init; }
        public string StockSymbol { get; init; }
        public TradingFrequency TradingFrequency { get; init; }
    }

    public record Position
    {
        public string StockSymbol { get; init; }
        public long NumberOfShares { get; init; }
    }

    public enum TradingFrequency
    {
        Minute = 0,
        Day = 2, 
        Week = 3, 
        Month = 4, 
        Quarter = 5, 
        Year = 6
    }
}
