using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutomaticStockTrader.Core.Alpaca;
using AutomaticStockTrader.Domain;
using AutomaticStockTrader.Repository;

namespace AutomaticStockTrader.Core.Strategies
{
    public abstract class Strategy : IDisposable
    {
        private readonly IAlpacaClient _alpacaClient;
        private readonly ITrackingRepository _trackingRepository;
        private readonly TradingFrequency _frequency;
        private readonly decimal _percentageOfEquityToAllocate;
        private bool disposedValue;

        public IList<StockInput> HistoricalData { get; set; }

        public Strategy(IAlpacaClient alpacaClient, ITrackingRepository trackingRepository, TradingFrequency frequency, decimal percentageOfEquityToAllocate)
        {
            _alpacaClient = alpacaClient ?? throw new ArgumentException(nameof(alpacaClient));
            _trackingRepository = trackingRepository ?? throw new ArgumentException(nameof(trackingRepository));
            _frequency = frequency;
            _percentageOfEquityToAllocate = percentageOfEquityToAllocate;
        }

        public abstract Task<bool?> ShouldBuyStock(StockInput newData);

        public virtual async Task HandleMinuteAgg(StockInput newValue)
        {
            var result = await ShouldBuyStock(newValue);

            var stockStrategy = new StrategysStock
            {
                StockSymbol = newValue.StockSymbol,
                Strategy = GetType().Name,
                TradingFrequency = _frequency
            };

            if (result.HasValue && result.Value)
            {
                await HandleBuy(newValue.ClosingPrice, stockStrategy);
                Console.WriteLine($"{GetType().Name} is having a position in {newValue.StockSymbol}");
            }
            else if (result.HasValue && !result.Value)
            {
                await HandleSell(newValue.ClosingPrice, stockStrategy);
                Console.WriteLine($"{GetType().Name} is not having a position in {newValue.StockSymbol}.");
            }
            else
            {
                Console.WriteLine($"{GetType().Name} is not buying or selling {newValue.StockSymbol}");
            }
        }

        private async Task HandleSell(decimal marketPrice, StrategysStock stockStrategy)
        {
            var currentPosition = await _trackingRepository.GetOrCreateEmptyPosition(stockStrategy);

            if (currentPosition.NumberOfShares > 0)
            {
                var order = new Order
                {
                    SharesBought = currentPosition.NumberOfShares * (-1),
                    MarketPrice = marketPrice,
                    OrderPlacedTime = DateTime.UtcNow
                };

                await _alpacaClient.PlaceOrder(stockStrategy, order);
                await _trackingRepository.AddPendingOrder(stockStrategy, order);
            }
        }

        private async Task HandleBuy(decimal marketPrice, StrategysStock stockStrategy)
        {
            var equityTask = _alpacaClient.GetTotalEquity();
            var currentPosition = await _trackingRepository.GetOrCreateEmptyPosition(stockStrategy);

            var sharesNeeded = CalculateNumberOfSharesNeeded(currentPosition.NumberOfShares, marketPrice, await equityTask);

            if (sharesNeeded > 0)
            {
                var order = new Order
                {
                    SharesBought = sharesNeeded,
                    MarketPrice = marketPrice,
                    OrderPlacedTime = DateTime.UtcNow
                };

                await _alpacaClient.PlaceOrder(stockStrategy, order);
                await _trackingRepository.AddPendingOrder(stockStrategy, order);
            }
        }

        private long CalculateNumberOfSharesNeeded(long numberOfSharesCurrentlyOwned, decimal marketPrice, decimal equity)
        {
            var targetEquityAmount = equity * _percentageOfEquityToAllocate;

            var missingEquity = targetEquityAmount - (numberOfSharesCurrentlyOwned * marketPrice);

            var numberOfSharesNeeded = (long)Math.Floor(missingEquity / marketPrice);

            return numberOfSharesNeeded > 0 ? numberOfSharesNeeded : 0;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _alpacaClient?.Dispose();
                    _trackingRepository?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
