using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutomaticStockTrader.Core.Alpaca;
using AutomaticStockTrader.Domain;
using AutomaticStockTrader.Repository;
using Microsoft.Extensions.Logging;

namespace AutomaticStockTrader.Core.Strategies
{
    public class StrategyHandler : IDisposable
    {
        private readonly ILogger<StrategyHandler> _logger;
        private readonly IAlpacaClient _alpacaClient;
        private readonly ITrackingRepository _trackingRepository;
        private readonly IStrategy _stategy;
        private readonly TradingFrequency _frequency;

        private readonly decimal _percentageOfEquityToAllocate;
        private bool disposedValue;

        public readonly List<StockInput> HistoricalData;
        
        public StrategyHandler(
            ILogger<StrategyHandler> logger,
            IAlpacaClient alpacaClient, 
            ITrackingRepository trackingRepository,
            IStrategy strategy,
            TradingFrequency frequency, 
            decimal percentageOfEquityToAllocate)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _alpacaClient = alpacaClient ?? throw new ArgumentException(nameof(alpacaClient));
            _trackingRepository = trackingRepository ?? throw new ArgumentException(nameof(trackingRepository));
            _stategy = strategy ?? throw new ArgumentException(nameof(strategy));
            _frequency = frequency;
            _percentageOfEquityToAllocate = percentageOfEquityToAllocate;
            HistoricalData = new List<StockInput>();
        }

        public virtual async Task HandleMinuteAgg(StockInput newValue)
        {
            HistoricalData.Add(newValue);
            var result = await _stategy.ShouldBuyStock(HistoricalData);

            var stockStrategy = new StrategysStock
            {
                StockSymbol = newValue.StockSymbol,
                Strategy = GetType().Name,
                TradingFrequency = _frequency
            };

            if (result.HasValue && result.Value)
            {
                await HandleBuy(newValue.ClosingPrice, stockStrategy);
                _logger.LogInformation($"{GetType().Name} is having a position in {newValue.StockSymbol}");
            }
            else if (result.HasValue && !result.Value)
            {
                await HandleSell(newValue.ClosingPrice, stockStrategy);
                _logger.LogInformation($"{GetType().Name} is not having a position in {newValue.StockSymbol}.");
            }
            else
            {
                _logger.LogInformation($"{GetType().Name} is not buying or selling {newValue.StockSymbol}");
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

                await _trackingRepository.AddPendingOrder(stockStrategy, order);
                await _alpacaClient.PlaceOrder(stockStrategy, order);
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

                await _trackingRepository.AddPendingOrder(stockStrategy, order);
                await _alpacaClient.PlaceOrder(stockStrategy, order);
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
