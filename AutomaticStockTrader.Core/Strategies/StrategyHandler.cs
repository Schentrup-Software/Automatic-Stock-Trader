using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IStrategy _strategy;

        private readonly TradingFrequency _tradingFrequency;
        private readonly string _stockSymbol;
        private readonly decimal _percentageOfEquityToAllocate;
        private bool disposedValue;
      
        public readonly List<StockInput> HistoricalData;

        public StrategysStock StockStrategy => new StrategysStock
        {
            StockSymbol = _stockSymbol,
            Strategy = _strategy.GetType().Name,
            TradingFrequency = _tradingFrequency
        };

    public StrategyHandler(
            ILogger<StrategyHandler> logger,
            IAlpacaClient alpacaClient, 
            ITrackingRepository trackingRepository,
            IStrategy strategy,
            TradingFrequency frequency, 
            decimal percentageOfEquityToAllocate,
            string stockSymbol)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _alpacaClient = alpacaClient ?? throw new ArgumentException(nameof(alpacaClient));
            _trackingRepository = trackingRepository ?? throw new ArgumentException(nameof(trackingRepository));
            _strategy = strategy ?? throw new ArgumentException(nameof(strategy));
            _tradingFrequency = frequency;
            _percentageOfEquityToAllocate = percentageOfEquityToAllocate;
            _stockSymbol = stockSymbol;

            HistoricalData = new List<StockInput>();
        }

        public async Task HandleNewData(StockInput newValue)
        {
            if (!string.Equals(newValue.StockSymbol, _stockSymbol, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidOperationException($"Cannot handle {newValue.StockSymbol} stock with {_stockSymbol} handler.");
            }

            HistoricalData.Add(newValue);
            await RunStrategy(HistoricalData);
        }

        public async Task RunStrategy(IList<StockInput> historicalData)
        {
            historicalData = historicalData.OrderByDescending(x => x.Time).ToList();
            var result = await _strategy.ShouldBuyStock(historicalData);

            if (result.HasValue && result.Value)
            {
                await HandleBuy(historicalData.First().ClosingPrice, StockStrategy);
                _logger.LogInformation($"{_strategy.GetType().Name} is having a position in {_stockSymbol}");
            }
            else if (result.HasValue && !result.Value)
            {
                await HandleSell(historicalData.First().ClosingPrice, StockStrategy);
                _logger.LogInformation($"{_strategy.GetType().Name} is not having a position in {_stockSymbol}.");
            }
            else
            {
                _logger.LogInformation($"{_strategy.GetType().Name} is not buying or selling {_stockSymbol}");
            }
        }

        internal async Task HandleSell(decimal marketPrice, StrategysStock stockStrategy)
        {
            var currentPosition = await _trackingRepository.GetOrCreateEmptyPosition(stockStrategy);

            if (currentPosition.NumberOfShares > 0)
            {
                var order = new Order
                {
                    StockSymbol = stockStrategy.StockSymbol,
                    SharesBought = currentPosition.NumberOfShares * (-1),
                    MarketPrice = marketPrice,
                    OrderPlacedTime = DateTime.UtcNow
                };

                await _trackingRepository.AddPendingOrder(stockStrategy, order);
                await _alpacaClient.PlaceOrder(order);
            }
        }

        internal async Task HandleBuy(decimal marketPrice, StrategysStock stockStrategy)
        {
            var equityTask = _alpacaClient.GetTotalEquity();
            var currentPosition = await _trackingRepository.GetOrCreateEmptyPosition(stockStrategy);

            var sharesNeeded = CalculateNumberOfSharesNeeded(currentPosition.NumberOfShares, marketPrice, await equityTask);

            if (sharesNeeded > 0)
            {
                var order = new Order
                {
                    StockSymbol = stockStrategy.StockSymbol,
                    SharesBought = sharesNeeded,
                    MarketPrice = marketPrice,
                    OrderPlacedTime = DateTime.UtcNow
                };

                await _trackingRepository.AddPendingOrder(stockStrategy, order);
                await _alpacaClient.PlaceOrder(order);
            }
        }

        private decimal CalculateNumberOfSharesNeeded(decimal numberOfSharesCurrentlyOwned, decimal marketPrice, decimal? equity)
        {
            var targetEquityAmount = (equity ?? 0) * _percentageOfEquityToAllocate;
            var missingEquity = targetEquityAmount - (numberOfSharesCurrentlyOwned * marketPrice);
            var percentageMissging = missingEquity / targetEquityAmount;

            var numberOfSharesNeeded = Math.Floor(missingEquity / marketPrice); 

            //Only buy if we are missing at least 10% equity. This helps prevent thrashing.
            return percentageMissging >= 0.1m ? numberOfSharesNeeded : 0;
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
