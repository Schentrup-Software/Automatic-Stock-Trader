using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutomaticStockTrader.Alpaca;

namespace AutomaticStockTrader.Stratagies
{
    public abstract class Strategy : IDisposable
    {
        private readonly IAlpacaClient _alpacaClient;

        public MoneyTracker MoneyTracker { get; private set; }

        public IList<StockInput> HistoricalData { get; set; }

        public Strategy(IAlpacaClient alpacaClient)
        {
            _alpacaClient = alpacaClient;
            MoneyTracker = new MoneyTracker
            {
                CostOfLastPosition = 0,
                MoneyMade = 0
            };
        }

        public abstract Task<bool?> ShouldBuyStock(StockInput newData);

        public virtual async Task HandleMinuteAgg(StockInput newValue)
        {
            var result = await ShouldBuyStock(newValue);

            if (result.HasValue && result.Value)
            {
                var cost = await _alpacaClient.EnsurePositionExists(newValue.StockSymbol, newValue.ClosingPrice);
                MoneyTracker.CostOfLastPosition += cost;

                Console.WriteLine($"{GetType().Name} is having a position in {newValue.StockSymbol}");
            }
            else if (result.HasValue && !result.Value)
            {
                var saleAmount = await _alpacaClient.EnsurePostionCleared(newValue.StockSymbol);
                if (saleAmount > 0 && MoneyTracker.CostOfLastPosition > 0)
                {
                    MoneyTracker.MoneyMade += (saleAmount - MoneyTracker.CostOfLastPosition) / MoneyTracker.CostOfLastPosition;
                }
                MoneyTracker.CostOfLastPosition = 0;

                Console.WriteLine($"{GetType().Name} is not having a position in {newValue.StockSymbol}. Percentage made so far {MoneyTracker.MoneyMade}");
            }
            else
            {
                Console.WriteLine($"{GetType().Name} is not buying or selling {newValue.StockSymbol}");
            }
        }

        public void Dispose()
        {
            _alpacaClient.Dispose();
        }
    }
}
