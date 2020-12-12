using System;
using System.Linq;
using System.Threading.Tasks;
using AutomaticStockTrader.Core.Alpaca;
using AutomaticStockTrader.Domain;
using AutomaticStockTrader.Repository;

namespace AutomaticStockTrader.Core.Strategies.MeanReversionStrategy
{
    public class MeanReversionStrategy : Strategy
    {
        public MeanReversionStrategy(IAlpacaClient alpacaClient, ITrackingRepository trackingRepository, TradingFrequency tradingFrequency, decimal percentageOfEquityToAllocate) 
            : base(alpacaClient, trackingRepository, tradingFrequency, percentageOfEquityToAllocate) { }

        public override Task<bool?> ShouldBuyStock(StockInput newData)
        {
            HistoricalData.Add(newData);
            if (HistoricalData.Count > 20)
            {
                HistoricalData = HistoricalData.OrderByDescending(x => x.Time).Take(20).ToList();

                var avg = HistoricalData.Select(x => x.ClosingPrice).Average();
                var diff = avg - newData.ClosingPrice;

                return Task.FromResult<bool?>(diff >= 0);
            }
            else
            {
                Console.WriteLine($"Waiting on more data for {GetType().Name}.");
                return Task.FromResult<bool?>(null);
            }
        }
    }
}
