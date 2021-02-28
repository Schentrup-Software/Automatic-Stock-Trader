using AutomaticStockTrader.Core.Alpaca;
using AutomaticStockTrader.Core.Strategies;
using AutomaticStockTrader.Repository;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutomaticStockTrader.Core.Traders
{
    public class CloseStreamingTrader : IJob
    {
        private readonly ILogger<CloseStreamingTrader> _logger;
        private readonly IAlpacaClient _alpacaClient;
        private readonly IEnumerable<StrategyHandler> _strategies;
        private readonly ITrackingRepository _trackingRepository;

        public CloseStreamingTrader(
            ILogger<CloseStreamingTrader> logger,
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

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation($"Starting {GetType().Name} job");

            await _alpacaClient.DisconnectStreamApis();

            foreach (var strategy in _strategies.Where(x => x.StockStrategy.TradingFrequency == Domain.TradingFrequency.Minute))
            {
                var position = await _trackingRepository.GetOrCreateEmptyPosition(strategy.StockStrategy);

                if(position.NumberOfShares > 0)
                {
                    var marketPriceTask = _alpacaClient.GetStockData(position.StockSymbol, strategy.StockStrategy.TradingFrequency, 1);
                    await _alpacaClient.PlaceOrder(new Domain.Order
                    {
                        StockSymbol = strategy.StockStrategy.StockSymbol,
                        MarketPrice = (await marketPriceTask).FirstOrDefault()?.ClosingPrice ?? 0,
                        OrderPlacedTime = DateTime.UtcNow,
                        SharesBought = position.NumberOfShares * (-1)
                    });
                }
            }

            _logger.LogInformation($"Finished {GetType().Name} job");
        }
    }
}
