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
        public decimal SharesBought { get; init; }
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
        public decimal NumberOfShares { get; init; }
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

    public enum OrderTiming
    {
        //
        // Summary:
        //     The order is good for the day, and it will be canceled automatically at the end
        //     of market hours.
        Day = 0,
        //
        // Summary:
        //     The order is good until canceled.
        GoodTillCanceled = 1,
        //
        // Summary:
        //     The order is placed at the time the market opens.
        AtMarketOpen = 2,
        //
        // Summary:
        //     The order is immediately filled or canceled after being placed (may partial fill).
        PartialFillOrKill = 3,
        //
        // Summary:
        //     The order is immediately filled or canceled after being placed (may not partial
        //     fill).
        FillOrKill = 4,
        //
        // Summary:
        //     The order will become a limit order if a limit price is specified or a market
        //     order otherwise at market close.
        AtMarketClose = 5
    }
}
