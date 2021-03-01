using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutomaticStockTrader.Core.Alpaca;
using AutomaticStockTrader.Core.Strategies;
using AutomaticStockTrader.Domain;
using AutomaticStockTrader.Repository;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutomaticStockTrader.Core
{
    public class ReconcileStockData : IHostedService
    {
        private readonly ILogger<ReconcileStockData> _logger;
        private readonly IAlpacaClient _alpacaClient;
        private readonly IEnumerable<StrategyHandler> _strategies;
        private readonly ITrackingRepository _trackingRepository;

        public ReconcileStockData(
            ILogger<ReconcileStockData> logger,
            IAlpacaClient alpacaClient,
            IEnumerable<StrategyHandler> strategies,
            ITrackingRepository trackingRepository
        )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _alpacaClient = alpacaClient ?? throw new ArgumentNullException(nameof(alpacaClient));
            _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
            _trackingRepository = trackingRepository ?? throw new ArgumentNullException(nameof(trackingRepository));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting {GetType().Name} job");

            var currentPositions = (await _alpacaClient.GetPositions()).GroupBy(x => x.StockSymbol).Select(x => new Position
            {
                StockSymbol = x.Key,
                NumberOfShares = x.Select(y => y.NumberOfShares).Sum()
            });

            foreach(var currentPosition in currentPositions)
            {
                var strategies = _strategies.Where(x => string.Equals(x.StockStrategy.StockSymbol, currentPosition.StockSymbol, StringComparison.OrdinalIgnoreCase));
                if (strategies.Any())
                {
                    var sharesPerPosition = currentPosition.NumberOfShares / strategies.Count();
                    
                    foreach(var strategy in strategies)
                    {
                        await ReconcileStock(sharesPerPosition, strategy.StockStrategy);
                    }

                    var leftoverShares = currentPosition.NumberOfShares % strategies.Count();

                    await ReconcileStock(leftoverShares, strategies.First().StockStrategy);
                }
                else
                {
                    _logger.LogDebug($"Selling all {currentPosition.NumberOfShares} unneeded shares of {currentPosition.StockSymbol}");
                    await _alpacaClient.PlaceOrder(new Order
                    {
                        OrderPlacedTime = DateTime.UtcNow,
                        SharesBought = currentPosition.NumberOfShares * (-1),
                        StockSymbol = currentPosition.StockSymbol,
                    }, OrderTiming.GoodTillCanceled);
                }
            }

            _logger.LogInformation($"Finished {GetType().Name} job");
        }

        private async Task ReconcileStock(long sharesPerPosition, StrategysStock strategy)
        {
            _logger.LogDebug($"Assigning {sharesPerPosition} shares of {strategy.StockSymbol} to {strategy.Strategy}");

            var postition = await _trackingRepository.GetOrCreateEmptyPosition(strategy);
            await _trackingRepository.AddOrder(strategy, new Order
            {
                MarketPrice = 0, //This is zero because this strategy did not buy or sell this stock so it should not count for or against it.
                OrderPlacedTime = DateTime.UtcNow,
                SharesBought = sharesPerPosition - postition.NumberOfShares,
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
